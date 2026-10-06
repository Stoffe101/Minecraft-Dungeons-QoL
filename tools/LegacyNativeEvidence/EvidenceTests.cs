using System.Buffers.Binary;
using System.Text;
namespace LegacyNativeEvidence;

static class EvidenceTests
{
    public static void Run() {
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("[PASS] native reader " + name); }
        void Reject(Action action, string name) { try { action(); throw new Exception("Accepted " + name); } catch (ReadFailure) { Console.WriteLine("[PASS] native reader rejects " + name); } }
        foreach (int chars in new[] { 12, 16 }) foreach (int children in new[] { 0x38, 0x48 }) foreach (int target in new[] { 0x70, 0x78, 0x80 }) {
            var f = new Fixture(chars, children, target); var result = f.Collect();
            Check(result.Completed && result.Declarations.Length == 11, $"legacy layout {chars:x}/{children:x}/{target:x}");
            Check(result.Declarations.Single(x => x.Name.EndsWith(".PlayerCharacterSaveSlot")).Functions.Single().Parameters.Single().Type == "/Script/CoreUObject.Guid", "GUID type retained");
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            Check(!json.Contains("0x") && !json.Contains("account-secret") && !json.Contains("Address"), "no addresses or instance values exported");
        }
        var f1 = new Fixture(); f1.Set(f1.GuidReturn + 0x70, f1.Classes["CharacterSaveData"]); Reject(() => f1.Collect(), "wrong GUID target");
        var f2 = new Fixture(); f2.Set(f2.BoolInput + 56, 0x480); Reject(() => f2.Collect(), "input bool mistaken for return");
        var f3 = new Fixture(); f3.Set(f3.FirstFunction + 40, f3.FirstFunction); Reject(() => f3.Collect(), "cyclic child chain");
        var f4 = new Fixture(); f4.Set(f4.ObjectHeader + 20, 127, 4); Reject(() => f4.Collect(), "implausible object count");
        var f5 = new Fixture(); f5.Set(f5.FirstObject + 12, 300, 4); Reject(() => f5.Collect(), "object internal index mismatch");
        var f6 = new Fixture(); f6.Set(f6.NameHeader + 1028, 2, 4); Reject(() => f6.Collect(), "name chunk count mismatch");
        var f7 = new Fixture(); f7.Set(f7.NameHeader + 1024, 0, 4); Reject(() => f7.Collect(), "modern/empty name pool");
        var f8 = new Fixture(); f8.Set(f8.GuidReturn + 40, f8.GuidReturn); Reject(() => f8.Collect(), "cyclic parameter chain");
        var f9 = new Fixture(); f9.Memory.Partial = true; Reject(() => f9.Collect(), "partial read");
        try { new BoundedMemory(f1.Memory, maxCalls: 0).Read(Fixture.Base, 8); throw new Exception("Budget ignored"); } catch (BudgetExceeded) { Check(true, "hard read budget stops traversal"); }
        Reject(() => new BoundedMemory(f1.Memory).Read(ulong.MaxValue, 8), "overflowing address");
        Reject(() => ImageData.Read(new BoundedMemory(f1.Memory), Fixture.Base, 4096), "non-PE image");
        Check(WindowsMemory.Access == 0x410, "attachment requests query/read only");
        if (OperatingSystem.IsWindows()) {
            var p = System.Runtime.InteropServices.Marshal.AllocHGlobal(8);
            try {
                System.Runtime.InteropServices.Marshal.Copy(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 0, p, 8);
                using var self = new WindowsMemory(Environment.ProcessId);
                Check(self.Read((ulong)p, 8).SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }), "Windows query/read handle reads own fixture only");
            } finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(p); }
        }
    }
    internal sealed class FakeMemory : IMemory {
        internal readonly byte[] Bytes = new byte[4 * 1024 * 1024]; public bool Partial;
        public byte[] Read(ulong address, int size) {
            if (address < Fixture.Base || address - Fixture.Base > (ulong)(Bytes.Length - size)) throw new ReadFailure("Fixture read out of bounds.");
            return Bytes.AsSpan((int)(address - Fixture.Base), Partial ? size - 1 : size).ToArray();
        }
    }
    sealed class Fixture
    {
        public const ulong Base = 0x100000; public readonly FakeMemory Memory = new(); public readonly Dictionary<string, ulong> Classes = new();
        public ulong NameHeader = Base + 0x2000, ObjectHeader = Base + 128, FirstObject, GuidReturn, BoolInput, FirstFunction;
        readonly Dictionary<string, int> names = new(); readonly Dictionary<ulong, ulong> last = new();
        ulong next = Base + 0x30000, nameChunk = Base + 0x4000, objectTable = Base + 0x10000, objectChunk = Base + 0x11000;
        readonly int chars, children, target; int index; ulong classMeta, scriptMeta, functionMeta, packageMeta;
        public Fixture(int chars = 12, int children = 0x48, int target = 0x70) {
            this.chars = chars; this.children = children; this.target = target;
            Set(Base, NameHeader); Set(NameHeader, nameChunk); Set(NameHeader + 1028, 1, 4);
            Set(ObjectHeader, objectTable); Set(objectTable, objectChunk); Set(ObjectHeader + 16, 65536, 4); Set(ObjectHeader + 20, 1024, 4); Set(ObjectHeader + 24, 1, 4); Set(ObjectHeader + 28, 1, 4);
            Name("None"); Name("ByteProperty"); Name("IntProperty");
            classMeta = Object("Class", 0, 0); FirstObject = classMeta; Set(classMeta + 16, classMeta);
            packageMeta = Object("Package", classMeta, 0); scriptMeta = Object("ScriptStruct", classMeta, 0); functionMeta = Object("Function", classMeta, 0);
            var core = Object("/Script/CoreUObject", packageMeta, 0);
            foreach (var obj in new[] { classMeta, packageMeta, scriptMeta, functionMeta }) Set(obj + 32, core);
            foreach (var kind in new[] { "IntProperty", "BoolProperty", "StructProperty", "ObjectProperty" }) Classes[kind] = Object(kind, classMeta, core);
            var game = Object("/Script/Dungeons", packageMeta, 0);
            foreach (var name in new[] { "InventoryItem", "InventoryItemSlot", "ItemStashComponent", "PlayerControllerBase", "PlayerCharacterSaveSlot", "CharacterSaveData", "DungeonsGameInstance", "DungeonsUserManager" }) Classes[name] = Object(name, classMeta, game);
            foreach (var name in new[] { "InventoryItemData", "InventoryItemMetaData", "SerializableItemId", "ItemSalvageUndoInfo" }) Classes[name] = Object(name, scriptMeta, game);
            Classes["Guid"] = Object("Guid", scriptMeta, core);
            GuidReturn = Param(Function("PlayerCharacterSaveSlot", "GetCloudPlayerId"), "ReturnValue", "StructProperty", 0x480, Classes["Guid"]);
            foreach (var name in new[] { "GetRecentSaveDataIndex", "GetNumProfiles", "GetSaveLocalUserNum" }) Param(Function("PlayerControllerBase", name), "ReturnValue", "IntProperty", 0x480);
            var fn = Function("PlayerControllerBase", "GetAvailableSaveDataByIndex"); FirstFunction = fn;
            Param(fn, "Index", "IntProperty", 0x80); Param(fn, "ReturnValue", "ObjectProperty", 0x480, Classes["CharacterSaveData"]);
            fn = Function("PlayerControllerBase", "GetCharacterSlotByIndex"); Param(fn, "Index", "IntProperty", 0x80); BoolInput = Param(fn, "Option", "BoolProperty", 0x80); Param(fn, "ReturnValue", "ObjectProperty", 0x480, Classes["PlayerCharacterSaveSlot"]);
            fn = Function("ItemStashComponent", "SalvageItemInSlot"); Param(fn, "Slot", "ObjectProperty", 0x80, Classes["InventoryItemSlot"]); Param(fn, "Success", "BoolProperty", 0x180); Param(fn, "ReturnValue", "StructProperty", 0x480, Classes["ItemSalvageUndoInfo"]);
            Encoding.ASCII.GetBytes("account-secret").CopyTo(Memory.Bytes, 0x200000);
        }
        public void Set(ulong address, ulong value, int size = 8) {
            var span = Memory.Bytes.AsSpan((int)(address - Base), size); if (size == 8) BinaryPrimitives.WriteUInt64LittleEndian(span, value); else BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)value);
        }
        ulong Allocate() { var p = next; next += 256; return p; }
        int Name(string name) {
            if (names.TryGetValue(name, out var i)) return i;
            i = names.Count; names.Add(name, i); var entry = Allocate(); Set(nameChunk + (ulong)i * 8, entry); Set(entry + 8, (ulong)i * 2, 4);
            Encoding.ASCII.GetBytes(name).CopyTo(Memory.Bytes, (int)(entry + (ulong)chars - Base)); Set(NameHeader + 1024, (ulong)names.Count, 4); return i;
        }
        ulong Object(string name, ulong kind, ulong outer) {
            var obj = Allocate(); Set(objectChunk + (ulong)index * 24, obj); Set(obj + 12, (ulong)index++, 4); Set(obj + 16, kind); Set(obj + 24, (ulong)Name(name), 4); Set(obj + 32, outer); return obj;
        }
        void Add(ulong owner, ulong field) { if (last.TryGetValue(owner, out var prev)) Set(prev + 40, field); else Set(owner + (ulong)children, field); last[owner] = field; }
        ulong Function(string owner, string name) { var obj = Object(name, functionMeta, Classes[owner]); Add(Classes[owner], obj); return obj; }
        ulong Param(ulong fn, string name, string kind, ulong flags, ulong type = 0) { var obj = Object(name, Classes[kind], fn); Set(obj + 56, flags); Set(obj + (ulong)target, type); Add(fn, obj); return obj; }
        public Capture Collect() => new LegacyReader(new BoundedMemory(Memory)).Collect([new Region(Base, Memory.Read(Base, 4096))]);
    }
}
