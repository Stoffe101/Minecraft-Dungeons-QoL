using System.Buffers.Binary;
using Iced.Intel;
using LegacyNativeEvidence;

namespace LegacyNativeCodeEvidence;

sealed record ExecutableRange(ulong Start, int Length);
sealed record CodeInstruction(uint Rva, string Bytes);
sealed record CodeEdge(uint FromRva, uint TargetRva, string Kind);
sealed record CodeSample(uint EntryRva, CodeInstruction[] Instructions, CodeEdge[] Edges, string[] Limits);
sealed record CodeRoot(string Owner, string Function, string FunctionFlags, int NumParms, int ParmsSize, int ReturnValueOffset, uint EntryRva);
sealed record CodeCapture(bool Completed, string[] Issues, CodeRoot[] Roots, CodeSample[] Samples);

// No code is executed and no remote process state is changed. Only exact, already
// validated reflection roots and decoded direct branches/calls in the main image.
sealed class NativeCode(IMemory memory, ulong imageBase, ExecutableRange[] ranges)
{
    internal const int Window = 4096, MaxRoutines = 48;
    static readonly (string Owner, string Function)[] Targets = [
        ("ItemStashComponent", "SerializeSaveState"), ("PlayerControllerBase", "SaveCharacterData"),
        ("CharacterSerializeComponent", "AssignCharacter"), ("CharacterSerializeComponent", "GetCloudPlayerId"),
        ("PlayerControllerBase", "GetCharacterSlotByIndex"), ("PlayerControllerBase", "GetAvailableSaveDataByIndex"),
        ("ItemStashComponent", "GetInventorySlots"), ("ItemStashComponent", "GetStorageChestSlots"),
        ("InventoryItemSlot", "Swap")];
    static readonly (string Owner, string Function)[] LayoutAnchors = [
        ("PlayerCharacterSaveSlot", "GetCloudPlayerId"), ("PlayerControllerBase", "GetRecentSaveDataIndex"),
        ("PlayerControllerBase", "GetNumProfiles"), ("PlayerControllerBase", "GetSaveLocalUserNum"),
        ("PlayerControllerBase", "GetAvailableSaveDataByIndex"), ("PlayerControllerBase", "GetCharacterSlotByIndex"),
        ("ItemStashComponent", "SalvageItemInSlot")];
    internal static ExecutableRange[] ReadRanges(IMemory memory, ulong imageBase, int imageSize) {
        var header = memory.Read(imageBase, 4096);
        int pe = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(60));
        if (imageSize < 4096 || imageSize > 512 * 1024 * 1024 || BinaryPrimitives.ReadUInt16LittleEndian(header) != 0x5a4d
            || pe < 64 || pe > 3072 || BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(pe)) != 0x4550
            || BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 4)) != 0x8664) throw new ReadFailure("Expected bounded x64 PE image.");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 6));
        int start = pe + 24 + BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 20));
        if (count < 1 || count > 64 || start + count * 40 > header.Length) throw new ReadFailure("Invalid section table.");
        var result = new List<ExecutableRange>();
        for (int i = 0; i < count; i++) {
            var section = header.AsSpan(start + i * 40, 40);
            uint flags = BinaryPrimitives.ReadUInt32LittleEndian(section[36..]);
            int length = BinaryPrimitives.ReadInt32LittleEndian(section[8..]), rva = BinaryPrimitives.ReadInt32LittleEndian(section[12..]);
            if (length < 0 || rva < 0 || rva > imageSize - length) throw new ReadFailure("Section outside image.");
            if ((flags & 0x20000000) == 0) continue;
            if ((flags & 0x40000000) == 0 || length < 1) continue; // Sampling is read-only even when the image permits writes.
            result.Add(new(imageBase + (ulong)rva, length));
        }
        var sorted = result.OrderBy(x => x.Start).ToArray();
        for (int i = 1; i < sorted.Length; i++) if (sorted[i - 1].Start + (ulong)sorted[i - 1].Length > sorted[i].Start) throw new ReadFailure("Overlapping executable sections.");
        if (sorted.Length == 0) throw new ReadFailure("No readable executable image sections.");
        return sorted;
    }
    ExecutableRange? Range(ulong address) => ranges.SingleOrDefault(x => address >= x.Start && address - x.Start < (ulong)x.Length);
    uint Rva(ulong address) => checked((uint)(address - imageBase));
    internal CodeRoot ValidateFunction(string owner, string name, ulong function, int flagsOffset, FunctionDeclaration declaration, int funcDelta) {
        if (flagsOffset is not 0x88 and not 0x98 || funcDelta is not 24 and not 40) throw new ReadFailure("Unsupported source layout candidate.");
        var bytes = memory.Read(function + (ulong)flagsOffset, funcDelta + 8);
        uint flags = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        int count = bytes[4], size = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6)), ret = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8));
        ulong pointer = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(funcDelta));
        if ((flags & 0x400) == 0 || count != declaration.Parameters.Length || declaration.Parameters.Any(x => (Convert.ToUInt64(x.Flags, 16) & 0x80) == 0)
            || size > 4096 || Range(pointer) == null) throw new ReadFailure("Native function metadata/code range mismatch: " + owner + "." + name);
        var returns = declaration.Parameters.Where(x => (Convert.ToUInt64(x.Flags, 16) & 0x400) != 0).ToArray();
        if (returns.Length > 1 || (returns.Length == 0 ? ret != 0xffff : ret >= size)) throw new ReadFailure("Native return layout mismatch: " + owner + "." + name);
        // Independent scalar and Guid anchors prevent accepting an arbitrary header.
        if (declaration.Parameters.Length == 0 && size != 0) throw new ReadFailure("Void native parameter size mismatch.");
        if (declaration.Parameters.Length == 1 && returns.Length == 1) {
            int expected = returns[0].Kind == "IntProperty" ? 4 : returns[0].Type == "/Script/CoreUObject.Guid" ? 16 : -1;
            if (expected > 0 && (size != expected || ret != 0)) throw new ReadFailure("Scalar/Guid native parameter size mismatch.");
        }
        return new(owner, name, flags.ToString("x8"), count, size, ret, Rva(pointer));
    }
    internal CodeSample Sample(ulong entry) {
        var range = Range(entry) ?? throw new ReadFailure("Code entry outside readable executable main image.");
        int length = (int)Math.Min(Window, range.Start + (ulong)range.Length - entry);
        var bytes = memory.Read(entry, length);
        var pending = new Queue<ulong>(); pending.Enqueue(entry);
        var instructions = new SortedDictionary<ulong, CodeInstruction>(); var edges = new List<CodeEdge>(); var limits = new HashSet<string>();
        while (pending.Count > 0) {
            ulong ip = pending.Dequeue();
            while (ip >= entry && ip - entry < (ulong)length && !instructions.ContainsKey(ip)) {
                int offset = (int)(ip - entry);
                var decoder = Decoder.Create(64, new ByteArrayCodeReader(bytes, offset, length - offset), ip);
                var instruction = decoder.Decode();
                if (instruction.IsInvalid || instruction.Length == 0) { limits.Add("Invalid or truncated reachable instruction."); break; }
                if (instructions.Any(x => ip < x.Key + (ulong)(x.Value.Bytes.Length / 2) && ip + (ulong)instruction.Length > x.Key)) { limits.Add("Overlapping reachable instructions."); break; }
                instructions.Add(ip, new(Rva(ip), Convert.ToHexString(bytes.AsSpan(offset, instruction.Length))));
                bool direct = instruction.Op0Kind == OpKind.NearBranch64;
                if (direct && instruction.FlowControl is FlowControl.Call or FlowControl.UnconditionalBranch or FlowControl.ConditionalBranch) {
                    ulong target = instruction.NearBranchTarget;
                    if (Range(target) != null) {
                        edges.Add(new(Rva(ip), Rva(target), instruction.FlowControl.ToString()));
                        if (instruction.FlowControl != FlowControl.Call && target >= entry && target - entry < (ulong)length) pending.Enqueue(target);
                    } else limits.Add("Direct control flow leaves main executable image.");
                }
                if (instruction.FlowControl is FlowControl.IndirectCall or FlowControl.IndirectBranch) limits.Add("Indirect target not followed.");
                if (instruction.FlowControl is FlowControl.Return or FlowControl.UnconditionalBranch or FlowControl.IndirectBranch or FlowControl.Exception or FlowControl.Interrupt) break;
                ip = instruction.NextIP;
                if (ip - entry >= (ulong)length) limits.Add("Reachable path exceeds bounded window.");
            }
        }
        // The output contains only decoded reachable instructions, not the full window.
        if (!memory.Read(entry, length).SequenceEqual(bytes)) throw new ReadFailure("Sampled code changed during capture.");
        return new(Rva(entry), instructions.Values.ToArray(), edges.Distinct().ToArray(), limits.Order().ToArray());
    }
    internal CodeCapture Collect(LegacyReader reader) {
        var valid = new List<int>();
        foreach (int delta in new[] { 24, 40 }) try {
            foreach (var (owner, name) in LayoutAnchors) { var fn = reader.AcceptedFunction(owner, name); ValidateFunction(owner, name, fn.Address, fn.FlagsOffset, fn.Declaration, delta); }
            valid.Add(delta);
        } catch (ReadFailure) { }
        if (valid.Count != 1) throw new ReadFailure("Native function-pointer layout not uniquely corroborated by all seven anchors.");
        int layout = valid[0];
        var metadata = Targets.Select(x => (x, Info: reader.AcceptedFunction(x.Owner, x.Function))).ToArray();
        var roots = metadata.Select(x => ValidateFunction(x.x.Owner, x.x.Function, x.Info.Address, x.Info.FlagsOffset, x.Info.Declaration, layout)).ToArray();
        var samples = new Dictionary<uint, CodeSample>(); var issues = new List<string>();
        // Prioritize every named root, then only its directly decoded outgoing targets.
        foreach (var root in roots) if (!samples.ContainsKey(root.EntryRva)) samples.Add(root.EntryRva, Sample(imageBase + root.EntryRva));
        var callees = samples.Values.SelectMany(x => x.Edges.Where(e => e.Kind == "Call" || !x.Instructions.Any(i => i.Rva == e.TargetRva))).Select(x => x.TargetRva).Distinct().ToArray();
        foreach (var callee in callees) {
            if (samples.ContainsKey(callee)) continue;
            if (samples.Count >= MaxRoutines) { issues.Add("Direct target count exceeds bounded routine limit."); break; }
            samples.Add(callee, Sample(imageBase + callee));
        }
        foreach (var x in metadata) {
            var current = reader.AcceptedFunction(x.x.Owner, x.x.Function);
            if (current.Address != x.Info.Address || current.FlagsOffset != x.Info.FlagsOffset || !ValidateFunction(x.x.Owner, x.x.Function, current.Address, current.FlagsOffset, current.Declaration, layout).Equals(roots.Single(r => r.Owner == x.x.Owner && r.Function == x.x.Function))) throw new ReadFailure("Native sampling root changed during capture.");
        }
        // Limits are explicit. Completion means collection succeeded, never full
        // transitive coverage or proof of callable ABI / permanent item identity.
        return new(issues.Count == 0, issues.ToArray(), roots, samples.Values.OrderBy(x => x.EntryRva).ToArray());
    }
}
