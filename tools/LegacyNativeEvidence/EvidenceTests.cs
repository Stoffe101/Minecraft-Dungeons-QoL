using System.Buffers.Binary;
using System.Text;
namespace LegacyNativeEvidence;

static class EvidenceTests
{
    public static void Run() {
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("[PASS] native reader " + name); }
        void Reject(Action action, string name) { try { action(); throw new Exception("Accepted " + name); } catch (ReadFailure) { Console.WriteLine("[PASS] native reader rejects " + name); } }
        foreach (int capacity in new[] { 128, 256 }) foreach (int chars in new[] { 12, 16 }) foreach (int children in new[] { 0x38, 0x48 }) foreach (int target in new[] { 0x70, 0x78, 0x80 }) {
            var f = new Fixture(chars, children, target, capacity); var result = f.Collect();
            Check(result.Completed && result.Declarations.Length == 11, $"legacy layout {capacity}/{chars:x}/{children:x}/{target:x}");
            Check(result.Declarations.Single(x => x.Name.EndsWith(".PlayerCharacterSaveSlot")).Functions.Single().Parameters.Single().Type == "/Script/CoreUObject.Guid", "GUID type retained");
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            Check(!json.Contains("0x") && !json.Contains("account-secret") && !json.Contains("Address"), "no addresses or instance values exported");
        }
        var reserved = new Fixture(capacity: 256); reserved.Set(reserved.NameHeader + 2052, 2, 4); Check(reserved.Collect().Completed, "reserved name chunks allowed by engine Reserve");
        var inline = new Fixture(capacity: 256); inline.Set(Fixture.Base, 0); Check(inline.CollectInline().Completed, "256-chunk table discovered inline without global pointer");
        var corrupt = new Fixture(capacity: 256); corrupt.CorruptFirstName(); Reject(() => corrupt.Collect(), "256-chunk name entry index mismatch");
        var defaultCapacity = new Fixture(capacity: 256); defaultCapacity.SetObjectCapacity(33, 1);
        Check(defaultCapacity.Collect().Completed, "engine default 2Mi reservation rounds to 33 object chunks");
        var preallocated = new Fixture(capacity: 256); preallocated.SetObjectCapacity(33, 33);
        Check(preallocated.Collect().Completed, "preallocated object chunks may exceed live-count chunks");
        var tooSmall = new Fixture(); tooSmall.SetObjectCapacity(1, 0); Reject(() => tooSmall.Collect(), "object chunks cannot cover live count");
        var capacityMismatch = new Fixture(); capacityMismatch.Set(capacityMismatch.ObjectHeader + 16, 65535, 4); Reject(() => capacityMismatch.Collect(), "object capacity inconsistent with chunk table");
        var overBound = new Fixture(); overBound.SetObjectCapacity(65, 1); Reject(() => overBound.Collect(), "object reserved capacity exceeds bound");
        var tooManyChunks = new Fixture(); tooManyChunks.SetObjectCapacity(1, 2); Reject(() => tooManyChunks.Collect(), "allocated object chunks exceed reserved capacity");
        var liveLimit = new Fixture(); liveLimit.SetObjectCapacity(33, 33); liveLimit.Set(liveLimit.ObjectHeader + 20, 2_000_001, 4); Reject(() => liveLimit.Collect(), "live traversal limit unchanged");
        var smallInventory = new Fixture(capacity: 256); smallInventory.AddInventoryInstances(0); smallInventory.Collect();
        var largeInventory = new Fixture(capacity: 256); largeInventory.AddInventoryInstances(2048); var largeResult = largeInventory.Collect();
        Check(largeResult.Completed && largeResult.Declarations.Length == 11, "2048 inventory instances retain allowlisted declarations");
        Check(largeInventory.Memory.Calls - smallInventory.Memory.Calls <= 2048 * 3, "inventory traversal bounded to at most three added reads per instance");
        var changing = new Fixture(); int rootReads = 0;
        changing.Memory.Override = (address, size) => {
            if (address == changing.ObjectTable && size == 8 && ++rootReads == 3) return new byte[8];
            return null;
        };
        Reject(() => changing.Collect(), "cached object chunk replaced before final validation");
        var failedReader = new LegacyReader(new BoundedMemory(new Fixture().Memory, maxCalls: 0));
        try { failedReader.Collect([new Region(Fixture.Base, new Fixture().Memory.Read(Fixture.Base, 4096))]); throw new Exception("Budget ignored"); }
        catch (BudgetExceeded ex) { Check(ex.Message.Contains("calls") && failedReader.Stage == "global-discovery", "budget cause and failing stage distinguishable without addresses"); }
        var emptySlots = new Fixture(capacity: 256); emptySlots.Set(emptySlots.ObjectHeader + 20, 4096, 4); emptySlots.Collect();
        var baseSlots = new Fixture(capacity: 256); baseSlots.Collect();
        Check(emptySlots.Memory.Calls - baseSlots.Memory.Calls <= 2, "3072 empty slots require at most two additional block reads");
        var replacedDeclaration = new Fixture(); bool slotChanged = false;
        replacedDeclaration.Memory.Override = (address, size) => {
            if (address == replacedDeclaration.SelectedSlot && size == 8) { slotChanged = true; return new byte[8]; }
            return null;
        };
        Reject(() => replacedDeclaration.Collect(), "selected declaration slot replaced before acceptance");
        Check(slotChanged, "selected declaration pointers rechecked directly after batched scan");
        var boundary = new Fixture(capacity: 256); boundary.AddChunkBoundaryInstance(); bool finalBlockBounded = false;
        boundary.Memory.Override = (address, size) => {
            if (address == Fixture.Base + 0x280000) finalBlockBounded = size == 24;
            return null;
        };
        Check(boundary.Collect().Completed && finalBlockBounded, "slot blocks cross 65536 boundary without reading beyond live tail");
        Check(boundary.Memory.Calls - baseSlots.Memory.Calls < 1000, "65537 slot traversal uses bounded block count");
        var partialBlock = new Fixture(); partialBlock.Memory.Override = (address, size) => address == partialBlock.ObjectChunk && size > 8 ? new byte[size - 1] : null;
        Reject(() => partialBlock.Collect(), "partial slot block read");
        var expanded = new Fixture(capacity: 256); expanded.AddSerializationClasses();
        var expandedResult = expanded.CollectSerialization();
        Check(expandedResult.Completed && expandedResult.Declarations.Length == 14, "focused serialization set returns fourteen allowlisted declarations");
        Check(expanded.Collect().Declarations.Length == 11, "default collection retains original scope");
        var missingSerialization = new Fixture().CollectSerialization();
        Check(!missingSerialization.Completed && missingSerialization.Declarations.Length == 11 && missingSerialization.Issues.Length == 3, "absent serialization classes recorded explicitly without claiming complete evidence");
        var f1 = new Fixture(); f1.Set(f1.GuidReturn + 0x70, f1.Classes["CharacterSaveData"]); Reject(() => f1.Collect(), "wrong GUID target");
        var f2 = new Fixture(); f2.Set(f2.BoolInput + 56, 0x480); Reject(() => f2.Collect(), "input bool mistaken for return");
        var f3 = new Fixture(); f3.Set(f3.FirstFunction + 40, f3.FirstFunction); Reject(() => f3.Collect(), "cyclic child chain");
        var f4 = new Fixture(); f4.Set(f4.ObjectHeader + 20, 127, 4); Reject(() => f4.Collect(), "implausible object count");
        var f5 = new Fixture(); f5.Set(f5.FirstObject + 12, 300, 4); Reject(() => f5.Collect(), "object internal index mismatch");
        var f6 = new Fixture(); f6.Set(f6.NameHeader + 1028, 129, 4); Reject(() => f6.Collect(), "name chunk count mismatch");
        var f7 = new Fixture(); f7.Set(f7.NameHeader + 1024, 0, 4); Reject(() => f7.Collect(), "modern/empty name pool");
        var f8 = new Fixture(); f8.Set(f8.GuidReturn + 40, f8.GuidReturn); Reject(() => f8.Collect(), "cyclic parameter chain");
        var f9 = new Fixture(); f9.Memory.Partial = true; Reject(() => f9.Collect(), "partial read");
        try { new BoundedMemory(f1.Memory, maxBytes: 0).Read(Fixture.Base, 8); throw new Exception("Byte budget ignored"); }
        catch (BudgetExceeded ex) { Check(ex.Message.Contains("bytes"), "byte budget cause explicit"); }
        try { new BoundedMemory(f1.Memory, timeout: TimeSpan.FromTicks(-1)).Read(Fixture.Base, 8); throw new Exception("Time budget ignored"); }
        catch (BudgetExceeded ex) { Check(ex.Message.Contains("time"), "time budget cause explicit"); }
        try { new BoundedMemory(f1.Memory, maxCalls: 0).Read(Fixture.Base, 8); throw new Exception("Budget ignored"); } catch (BudgetExceeded) { Check(true, "hard read budget stops traversal"); }
        Reject(() => new BoundedMemory(f1.Memory).Read(ulong.MaxValue, 8), "overflowing address");
        Reject(() => ImageData.Read(new BoundedMemory(f1.Memory), Fixture.Base, 4096), "non-PE image");
        var image = new Fixture();
        image.Set(Fixture.Base, 0x5a4d, 4); image.Set(Fixture.Base + 60, 64, 4); image.Set(Fixture.Base + 64, 0x4550, 4);
        image.Set(Fixture.Base + 68, 0x00018664, 4); image.Set(Fixture.Base + 84, 0, 4);
        image.Set(Fixture.Base + 96, 1024 * 1024 + 8192, 4); image.Set(Fixture.Base + 100, 4096, 4); image.Set(Fixture.Base + 124, 0x80000000, 4);
        var regions = ImageData.Read(new BoundedMemory(image.Memory), Fixture.Base, 2 * 1024 * 1024);
        ulong seamHeader = Fixture.Base + 4096 + 1024 * 1024 - 2048;
        Check(regions.Any(r => r.Address <= seamHeader && r.Address + (ulong)r.Bytes.Length >= seamHeader + 2056), "256-chunk header wholly covered across image scan seam");
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
        internal readonly byte[] Bytes = new byte[4 * 1024 * 1024]; public bool Partial; public int Calls; public Func<ulong, int, byte[]?>? Override;
        public byte[] Read(ulong address, int size) {
            Calls++; var overridden = Override?.Invoke(address, size); if (overridden != null) return overridden;
            if (address < Fixture.Base || address - Fixture.Base > (ulong)(Bytes.Length - size)) throw new ReadFailure("Fixture read out of bounds.");
            return Bytes.AsSpan((int)(address - Fixture.Base), Partial ? size - 1 : size).ToArray();
        }
    }
    sealed class Fixture
    {
        public const ulong Base = 0x100000; public readonly FakeMemory Memory = new(); public readonly Dictionary<string, ulong> Classes = new();
        public ulong NameHeader = Base + 0x2000, ObjectHeader = Base + 128, FirstObject, GuidReturn, BoolInput, FirstFunction;
        readonly Dictionary<string, int> names = new(); readonly Dictionary<ulong, ulong> last = new();
        ulong gamePackage;
        ulong next = Base + 0x30000, nameChunk = Base + 0x4000, objectTable = Base + 0x10000, objectChunk = Base + 0x100000;
        readonly int chars, children, target, capacity; int index; ulong classMeta, scriptMeta, functionMeta, packageMeta;
        public Fixture(int chars = 12, int children = 0x48, int target = 0x70, int capacity = 128) {
            this.chars = chars; this.children = children; this.target = target; this.capacity = capacity;
            Set(Base, NameHeader); Set(NameHeader, nameChunk); Set(NameHeader + (ulong)capacity * 8 + 4, 1, 4);
            Set(ObjectHeader, objectTable); Set(objectTable, objectChunk); Set(ObjectHeader + 16, 65536, 4); Set(ObjectHeader + 20, 1024, 4); Set(ObjectHeader + 24, 1, 4); Set(ObjectHeader + 28, 1, 4);
            Name("None"); Name("ByteProperty"); Name("IntProperty");
            classMeta = Object("Class", 0, 0); FirstObject = classMeta; Set(classMeta + 16, classMeta);
            packageMeta = Object("Package", classMeta, 0); scriptMeta = Object("ScriptStruct", classMeta, 0); functionMeta = Object("Function", classMeta, 0);
            var core = Object("/Script/CoreUObject", packageMeta, 0);
            foreach (var obj in new[] { classMeta, packageMeta, scriptMeta, functionMeta }) Set(obj + 32, core);
            foreach (var kind in new[] { "IntProperty", "BoolProperty", "StructProperty", "ObjectProperty" }) Classes[kind] = Object(kind, classMeta, core);
            var game = Object("/Script/Dungeons", packageMeta, 0); gamePackage = game;
            foreach (var name in new[] { "InventoryItem", "InventoryItemSlot", "ItemStashComponent", "PlayerControllerBase", "PlayerCharacterSaveSlot", "CharacterSaveData", "DungeonsGameInstance", "DungeonsUserManager" }) Classes[name] = Object(name, classMeta, game);
            foreach (var name in new[] { "InventoryItemData", "InventoryItemMetaData", "SerializableItemId", "ItemSalvageUndoInfo" }) Classes[name] = Object(name, scriptMeta, game);
            Classes["Guid"] = Object("Guid", scriptMeta, core);
            GuidReturn = Param(Function("PlayerCharacterSaveSlot", "GetCloudPlayerId"), "ReturnValue", "StructProperty", 0x480, Classes["Guid"]);
            foreach (var name in new[] { "GetRecentSaveDataIndex", "GetNumProfiles", "GetSaveLocalUserNum" }) Param(Function("PlayerControllerBase", name), "ReturnValue", "IntProperty", 0x480);
            var fn = Function("PlayerControllerBase", "GetAvailableSaveDataByIndex"); FirstFunction = fn;
            Param(fn, "Index", "IntProperty", 0x80); Param(fn, "ReturnValue", "ObjectProperty", 0x480, Classes["CharacterSaveData"]);
            fn = Function("PlayerControllerBase", "GetCharacterSlotByIndex"); Param(fn, "Index", "IntProperty", 0x80); BoolInput = Param(fn, "Option", "BoolProperty", 0x80); Param(fn, "ReturnValue", "ObjectProperty", 0x480, Classes["PlayerCharacterSaveSlot"]);
            fn = Function("ItemStashComponent", "SalvageItemInSlot"); Param(fn, "Slot", "ObjectProperty", 0x80, Classes["InventoryItemSlot"]); Param(fn, "Success", "BoolProperty", 0x180); Param(fn, "ReturnValue", "StructProperty", 0x480, Classes["ItemSalvageUndoInfo"]);
            Encoding.ASCII.GetBytes("account-secret").CopyTo(Memory.Bytes, 0x3f0000);
        }
        public void Set(ulong address, ulong value, int size = 8) {
            var span = Memory.Bytes.AsSpan((int)(address - Base), size); if (size == 8) BinaryPrimitives.WriteUInt64LittleEndian(span, value); else BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)value);
        }
        ulong Allocate() { var p = next; next += 256; return p; }
        int Name(string name) {
            if (names.TryGetValue(name, out var i)) return i;
            i = names.Count; names.Add(name, i); var entry = Allocate(); Set(nameChunk + (ulong)i * 8, entry); Set(entry + 8, (ulong)i * 2, 4);
            Encoding.ASCII.GetBytes(name).CopyTo(Memory.Bytes, (int)(entry + (ulong)chars - Base)); Set(NameHeader + (ulong)capacity * 8, (ulong)names.Count, 4); return i;
        }
        ulong Object(string name, ulong kind, ulong outer) {
            var obj = Allocate(); Set((index < 65536 ? objectChunk : Base + 0x280000) + (ulong)(index % 65536) * 24, obj); Set(obj + 12, (ulong)index++, 4); Set(obj + 16, kind); Set(obj + 24, (ulong)Name(name), 4); Set(obj + 32, outer); return obj;
        }
        void Add(ulong owner, ulong field) { if (last.TryGetValue(owner, out var prev)) Set(prev + 40, field); else Set(owner + (ulong)children, field); last[owner] = field; }
        ulong Function(string owner, string name) { var obj = Object(name, functionMeta, Classes[owner]); Add(Classes[owner], obj); return obj; }
        ulong Param(ulong fn, string name, string kind, ulong flags, ulong type = 0) { var obj = Object(name, Classes[kind], fn); Set(obj + 56, flags); Set(obj + (ulong)target, type); Add(fn, obj); return obj; }
        public void AddSerializationClasses() { foreach (var name in new[] { "CharacterSerializeComponent", "BaseCharacter", "EquipmentComponent" }) Classes[name] = Object(name, classMeta, gamePackage); }
        public Capture CollectSerialization() => new LegacyReader(new BoundedMemory(Memory), serializationContracts: true).Collect([new Region(Base, Memory.Read(Base, 4096))]);
        public ulong ObjectTable => objectTable;
        public ulong ObjectChunk => objectChunk;
        public void AddChunkBoundaryInstance() { Set(objectTable + 8, Base + 0x280000); SetObjectCapacity(2, 2); index = 65536; Object("boundary-item", Classes["InventoryItem"], 0); Set(ObjectHeader + 20, (ulong)index, 4); }
        public ulong SelectedSlot => objectChunk + (ulong)BinaryPrimitives.ReadInt32LittleEndian(Memory.Bytes.AsSpan((int)(Classes["InventoryItem"] + 12 - Base), 4)) * 24;
        public void AddInventoryInstances(int count) { for (int i = 0; i < count; i++) Object("ordinary-item", Classes["InventoryItem"], 0); Set(ObjectHeader + 20, (ulong)Math.Max(128, index), 4); }
        public void SetObjectCapacity(int maximumChunks, int allocatedChunks) { Set(ObjectHeader + 16, (ulong)maximumChunks * 65536, 4); Set(ObjectHeader + 24, (ulong)maximumChunks, 4); Set(ObjectHeader + 28, (ulong)allocatedChunks, 4); }
        public void CorruptFirstName() => Set(BinaryPrimitives.ReadUInt64LittleEndian(Memory.Read(nameChunk, 8)) + 8, 4, 4);
        public Capture CollectInline() => new LegacyReader(new BoundedMemory(Memory)).Collect([new Region(Base, Memory.Read(Base, 4096)), new Region(NameHeader, Memory.Read(NameHeader, 4096))]);
        public Capture Collect() => new LegacyReader(new BoundedMemory(Memory)).Collect([new Region(Base, Memory.Read(Base, 4096))]);
    }
}
