using System.Buffers.Binary;
using System.Text;
namespace LegacyNativeEvidence;

sealed record Region(ulong Address, byte[] Bytes);
sealed record MemberDeclaration(string Name, string Kind, string? Type, string Flags);
sealed record FunctionDeclaration(string Name, MemberDeclaration[] Parameters);
sealed record ClassDeclaration(string Name, string? Super, MemberDeclaration[] Properties, FunctionDeclaration[] Functions);
sealed record Capture(bool Completed, string[] Issues, ClassDeclaration[] Declarations);

// Candidates must match independent retail call sites before metadata is accepted.
// These reads are not native calls, ABI certification or item-instance identity.
sealed class LegacyReader(IMemory memory)
{
    readonly Dictionary<int, string> nameCache = new();
    ulong names, objects; int nameCount, objectCount, nameCapacity, charsOffset, childrenOffset, targetOffset;
    static readonly string[] Targets = ["InventoryItem", "InventoryItemSlot", "ItemStashComponent", "PlayerControllerBase",
        "PlayerCharacterSaveSlot", "CharacterSaveData", "DungeonsGameInstance", "DungeonsUserManager",
        "InventoryItemData", "InventoryItemMetaData", "SerializableItemId"];
    public Capture Collect(IReadOnlyList<Region> regions) {
        var nameCandidates = new HashSet<(ulong Header, int Chars, int Capacity)>(); var attemptedHeaders = new HashSet<(ulong Header, int Capacity)>(); var objectCandidates = new HashSet<ulong>();
        foreach (var region in regions) for (int i = 0; i + 32 <= region.Bytes.Length; i += 8) {
            var p = BinaryPrimitives.ReadUInt64LittleEndian(region.Bytes.AsSpan(i));
            // UE 4.22 NameTypes.h uses 4M / 16384 = 256 inline chunk pointers.
            // Retain the earlier 128 candidate; accept only uniquely validated names/contracts.
            foreach (var header in new[] { region.Address + (ulong)i, p }) foreach (int capacity in new[] { 128, 256 }) {
                if (!MemoryValues.Pointer(header)) continue;
                if (header == region.Address + (ulong)i) {
                    if (i + capacity * 8 + 8 > region.Bytes.Length) continue;
                    int n = BinaryPrimitives.ReadInt32LittleEndian(region.Bytes.AsSpan(i + capacity * 8)), c = BinaryPrimitives.ReadInt32LittleEndian(region.Bytes.AsSpan(i + capacity * 8 + 4));
                    if (n < 3 || n > 2_000_000 || c < (n + 16383) / 16384 || c > capacity) continue;
                }
                if (!attemptedHeaders.Add((header, capacity))) continue;
                try {
                    int count = memory.I32(header + (ulong)capacity * 8), chunks = memory.I32(header + (ulong)capacity * 8 + 4);
                    if (count < 3 || count > 2_000_000 || chunks < (count + 16383) / 16384 || chunks > capacity) continue;
                    foreach (var offset in new[] { 12, 16 }) {
                        names = header; nameCount = count; charsOffset = offset; nameCache.Clear();
                        try { if (Name(0) == "None" && Name(1) == "ByteProperty" && Name(2) == "IntProperty") nameCandidates.Add((header, offset, capacity)); } catch (ReadFailure) { }
                    }
                } catch (ReadFailure) { }
            }
            int countObjects = BinaryPrimitives.ReadInt32LittleEndian(region.Bytes.AsSpan(i + 20)), maxObjects = BinaryPrimitives.ReadInt32LittleEndian(region.Bytes.AsSpan(i + 16));
            int countChunks = BinaryPrimitives.ReadInt32LittleEndian(region.Bytes.AsSpan(i + 28)), maxChunks = BinaryPrimitives.ReadInt32LittleEndian(region.Bytes.AsSpan(i + 24));
            if (MemoryValues.Pointer(p) && countObjects >= 128 && countObjects <= 2_000_000 && maxObjects >= countObjects && maxObjects <= 2_000_000
                && countChunks == (countObjects + 65535) / 65536 && maxChunks >= countChunks && maxChunks <= 32) objectCandidates.Add(region.Address + (ulong)i);
        }
        if (nameCandidates.Count != 1) throw new ReadFailure($"Legacy name array not uniquely validated ({nameCandidates.Count} matches); no declarations accepted.");
        (names, charsOffset, nameCapacity) = nameCandidates.Single(); nameCount = memory.I32(names + (ulong)nameCapacity * 8); nameCache.Clear();
        var validated = new List<ulong>();
        foreach (var candidate in objectCandidates) try {
            objects = memory.U64(candidate); objectCount = memory.I32(candidate + 20); int good = 0;
            for (int i = 0; i < Math.Min(objectCount, 128); i++) {
                ulong obj = ObjectAt(i); if (obj == 0) continue;
                if (memory.I32(obj + 12) != i || NameOf(obj).Length == 0 || NameOf(memory.U64(obj + 16)).Length == 0) throw new ReadFailure("Object array index/class mismatch."); good++;
            }
            if (good >= 8) validated.Add(candidate);
        } catch (ReadFailure) { }
        if (validated.Count != 1) throw new ReadFailure($"Legacy object array not uniquely validated ({validated.Count} matches from {objectCandidates.Count} candidates); no declarations accepted.");
        objects = memory.U64(validated[0]); objectCount = memory.I32(validated[0] + 20);
        var selected = new Dictionary<string, ulong>();
        for (int i = 0; i < objectCount; i++) {
            ulong obj = ObjectAt(i); if (obj == 0) continue;
            if (memory.I32(obj + 12) != i) throw new ReadFailure("Object table changed during collection.");
            var kind = Kind(obj); if (kind is not "Class" and not "ScriptStruct") continue;
            var name = NameOf(obj); if (!Targets.Contains(name) || FullName(obj) != "/Script/Dungeons." + name) continue;
            if (!selected.TryAdd(name, obj)) throw new ReadFailure("Duplicate native declaration.");
        }
        var layouts = new List<(int Children, int Target)>();
        var mismatches = new List<string>();
        foreach (var child in new[] { 0x38, 0x48 }) foreach (var target in new[] { 0x70, 0x78, 0x80 }) {
            childrenOffset = child; targetOffset = target;
            try { VerifyKnownContracts(selected); layouts.Add((child, target)); }
            catch (ReadFailure ex) { mismatches.Add($"candidate {child:x}/{target:x}: {ex.Message}"); }
        }
        if (layouts.Count != 1) throw new ReadFailure("Native layout did not uniquely match all seven retail call contracts. " + string.Join("; ", mismatches));
        (childrenOffset, targetOffset) = layouts.Single();
        var declarations = selected.Values.Select(Declaration).OrderBy(x => x.Name).ToArray();
        VerifyKnownContracts(selected);
        if (memory.U64(validated[0]) != objects || memory.I32(validated[0] + 20) != objectCount) throw new ReadFailure("Object table changed during collection.");
        var missing = Targets.Where(x => !selected.ContainsKey(x)).Select(x => "Native declaration missing: " + x).ToArray();
        return new Capture(missing.Length == 0, missing, declarations);
    }
    ulong ObjectAt(int i) {
        ulong chunk = memory.U64(objects + (ulong)(i / 65536) * 8); if (!MemoryValues.Pointer(chunk)) throw new ReadFailure("Invalid object chunk.");
        ulong value = memory.U64(chunk + (ulong)(i % 65536) * 24); if (value != 0 && !MemoryValues.Pointer(value)) throw new ReadFailure("Invalid object address."); return value;
    }
    internal string Name(int index) {
        if (index < 0 || index >= nameCount) throw new ReadFailure("Invalid legacy name index.");
        if (nameCache.TryGetValue(index, out var name)) return name;
        ulong chunk = memory.U64(names + (ulong)(index / 16384) * 8), entry = memory.U64(chunk + (ulong)(index % 16384) * 8);
        int encoded = memory.I32(entry + 8); if ((encoded >> 1) != index) throw new ReadFailure("Legacy name entry index mismatch.");
        bool wide = (encoded & 1) != 0; var bytes = new List<byte>();
        for (int i = 0; i < 256; i++) {
            var c = memory.Read(entry + (ulong)charsOffset + (ulong)i * (wide ? 2UL : 1UL), wide ? 2 : 1);
            if (c.All(x => x == 0)) {
                name = wide ? Encoding.Unicode.GetString(bytes.ToArray()) : Encoding.ASCII.GetString(bytes.ToArray());
                if (name.Length == 0 || name.Any(char.IsControl)) throw new ReadFailure("Invalid legacy name text."); nameCache[index] = name; return name;
            }
            if (!wide && (c[0] < 32 || c[0] > 126)) throw new ReadFailure("Unsupported ANSI name."); bytes.AddRange(c);
        }
        throw new ReadFailure("Unterminated/oversized name.");
    }
    string NameOf(ulong obj) {
        if (!MemoryValues.Pointer(obj)) throw new ReadFailure("Invalid object pointer.");
        var number = memory.I32(obj + 28); var name = Name(memory.I32(obj + 24)); return number == 0 ? name : name + "_" + (number - 1);
    }
    string Kind(ulong obj) => NameOf(memory.U64(obj + 16));
    string FullName(ulong obj) {
        var parts = new List<string>(); var seen = new HashSet<ulong>();
        while (obj != 0) { if (parts.Count >= 16 || !seen.Add(obj)) throw new ReadFailure("Cyclic/oversized outer chain."); parts.Add(NameOf(obj)); obj = memory.U64(obj + 32); }
        parts.Reverse(); return string.Join('.', parts);
    }
    IEnumerable<ulong> Children(ulong owner) {
        var seen = new HashSet<ulong>(); ulong child = memory.U64(owner + (ulong)childrenOffset);
        while (child != 0) {
            if (seen.Count >= 1024 || !seen.Add(child) || memory.U64(child + 32) != owner) throw new ReadFailure("Invalid/cyclic legacy UField chain.");
            yield return child; child = memory.U64(child + 40);
        }
    }
    MemberDeclaration Property(ulong obj) => Property(obj, new HashSet<ulong>());
    MemberDeclaration Property(ulong obj, HashSet<ulong> chain) {
        if (chain.Count >= 16 || !chain.Add(obj)) throw new ReadFailure("Cyclic/oversized property type.");
        var kind = Kind(obj); if (!kind.EndsWith("Property", StringComparison.Ordinal)) throw new ReadFailure("Non-property in parameter list.");
        ulong flags = memory.U64(obj + 56);
        var type = kind switch {
            "StructProperty" or "ObjectProperty" or "ClassProperty" or "SoftObjectProperty" => FullName(memory.U64(obj + (ulong)targetOffset)),
            "ArrayProperty" => ArrayType(memory.U64(obj + (ulong)targetOffset), chain), _ => null
        };
        return new MemberDeclaration(NameOf(obj), kind, type, flags.ToString("x16"));
    }
    string ArrayType(ulong inner, HashSet<ulong> chain) { var p = Property(inner, chain); return "array<" + p.Kind + (p.Type == null ? "" : ":" + p.Type) + ">"; }
    FunctionDeclaration Function(ulong obj) => new(NameOf(obj), Children(obj).Select(Property).ToArray());
    ClassDeclaration Declaration(ulong obj) {
        var fields = Children(obj).ToArray(); ulong super = memory.U64(obj + (ulong)childrenOffset - 8);
        return new(FullName(obj), super == 0 ? null : FullName(super), fields.Where(x => Kind(x).EndsWith("Property", StringComparison.Ordinal)).Select(Property).ToArray(), fields.Where(x => Kind(x) == "Function").Select(Function).ToArray());
    }
    void VerifyKnownContracts(Dictionary<string, ulong> classes) {
        void Check(string owner, string method, params string[] expected) {
            if (!classes.TryGetValue(owner, out var cls)) throw new ReadFailure("Contract owner absent: " + owner);
            var matches = Children(cls).Where(x => Kind(x) == "Function" && NameOf(x) == method).ToArray(); if (matches.Length != 1) throw new ReadFailure("Contract function missing/ambiguous: " + method);
            var actual = Function(matches[0]).Parameters.Select(x => {
                ulong flags = Convert.ToUInt64(x.Flags, 16); if ((flags & 0x80) == 0) throw new ReadFailure("Non-parameter field in native function.");
                return ((flags & 0x400) != 0 ? "return:" : (flags & 0x100) != 0 ? "out:" : "in:") + x.Kind + (x.Type == null ? "" : ":" + x.Type);
            }).ToArray();
            if (!actual.SequenceEqual(expected)) throw new ReadFailure("Retail contract mismatch: " + method);
        }
        Check("PlayerCharacterSaveSlot", "GetCloudPlayerId", "return:StructProperty:/Script/CoreUObject.Guid");
        foreach (var method in new[] { "GetRecentSaveDataIndex", "GetNumProfiles", "GetSaveLocalUserNum" }) Check("PlayerControllerBase", method, "return:IntProperty");
        Check("PlayerControllerBase", "GetAvailableSaveDataByIndex", "in:IntProperty", "return:ObjectProperty:/Script/Dungeons.CharacterSaveData");
        Check("PlayerControllerBase", "GetCharacterSlotByIndex", "in:IntProperty", "in:BoolProperty", "return:ObjectProperty:/Script/Dungeons.PlayerCharacterSaveSlot");
        Check("ItemStashComponent", "SalvageItemInSlot", "in:ObjectProperty:/Script/Dungeons.InventoryItemSlot", "out:BoolProperty", "return:StructProperty:/Script/Dungeons.ItemSalvageUndoInfo");
    }
}
