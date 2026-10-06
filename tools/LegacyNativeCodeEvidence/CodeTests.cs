using System.Buffers.Binary;
using LegacyNativeEvidence;
namespace LegacyNativeCodeEvidence;

static class CodeTests
{
    const ulong Image = 0x100000, Code = Image + 0x1000, Function = Image + 0x100;
    public static void Run() {
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("[PASS] native code " + name); }
        void Reject(Action action, string name) { try { action(); throw new Exception("Accepted " + name); } catch (ReadFailure) { Console.WriteLine("[PASS] native code rejects " + name); } }
        var f = new Fixture();
        f.Put(Code, Convert.FromHexString("B8E8000000C3")); // E8 in MOV immediate is not a call.
        var sample = f.Reader.Sample(Code);
        Check(sample.Instructions.Length == 2 && sample.Edges.Length == 0, "embedded opcode is not treated as a branch");
        f.Put(Code + 6, System.Text.Encoding.ASCII.GetBytes("account-secret"));
        Check(!System.Text.Json.JsonSerializer.Serialize(f.Reader.Sample(Code)).Contains(Convert.ToHexString(System.Text.Encoding.ASCII.GetBytes("account-secret"))), "trailing unreachable bytes not exported");
        f = new Fixture(); f.Put(Code, Convert.FromHexString("E81B000000C3")); f.Put(Code + 32, [0xc3]);
        sample = f.Reader.Sample(Code);
        Check(sample.Edges.Single().TargetRva == 0x1020 && sample.Instructions.Length == 2, "decoded direct call uses exact relative target");
        f = new Fixture(); f.Put(Code, Convert.FromHexString("7403B001C3B002C3"));
        sample = f.Reader.Sample(Code);
        Check(sample.Instructions.Length == 5 && sample.Instructions.Any(x => x.Rva == 0x1005), "conditional branch explores both reachable paths");
        f = new Fixture(); f.Put(Code, Convert.FromHexString("FFE0"));
        Check(f.Reader.Sample(Code).Limits.Contains("Indirect target not followed."), "indirect code targets are never dereferenced");
        f = new Fixture(); f.Put(Code, [0xeb, 0xfe]);
        Check(f.Reader.Sample(Code).Instructions.Length == 1, "reachable loop terminates without repeated decode");
        f = new Fixture(); f.Put(Code, Convert.FromHexString("EB01B8C3"));
        Check(f.Reader.Sample(Code).Instructions.Length == 2, "unconditional branch skips unreachable bytes");
        f = new Fixture(); f.Put(Code, Convert.FromHexString("E9FFFFFF7F"));
        Check(f.Reader.Sample(Code).Limits.Contains("Direct control flow leaves main executable image."), "external direct targets remain explicit and unsampled");
        f = new Fixture(); f.Memory.Bytes[^1] = 0x0f;
        Check(f.Reader.Sample(Image + (ulong)f.Memory.Bytes.Length - 1).Limits.Contains("Invalid or truncated reachable instruction."), "truncated tail recorded without reading past section");
        f = new Fixture(); f.Put(Code, [0xc3]);
        Check(f.Reader.Sample(Code).EntryRva == 0x1000, "output uses image-relative code coordinates");
        Reject(() => f.Reader.Sample(Image + 1), "non-executable sampling address");
        f.Memory.Partial = true; Reject(() => f.Reader.Sample(Code), "partial code read");
        f = new Fixture(); f.Put(Code, [0xc3]); int reads = 0;
        f.Memory.Override = (address, size) => address == Code && ++reads == 2 ? Enumerable.Repeat((byte)0xcc, size).ToArray() : null;
        Reject(() => f.Reader.Sample(Code), "code changed between bounded reads");
        f = new Fixture(); f.NativeHeader(0x98, 24, 4); var declaration = new FunctionDeclaration("GetNumProfiles", [new("ReturnValue", "IntProperty", null, "0000000000000480")]);
        var root = f.Reader.ValidateFunction("PlayerControllerBase", declaration.Name, Function, 0x98, declaration, 24);
        Check(root.EntryRva == 0x1000 && root.ParmsSize == 4, "source metadata layout corroborates int getter");
        Reject(() => f.Reader.ValidateFunction("x", "x", Function, 0x90, declaration, 24), "unsupported metadata layout");
        f.Write(Function + 0x98, 0, 4); Reject(() => f.Reader.ValidateFunction("x", "x", Function, 0x98, declaration, 24), "missing FUNC_Native flag");
        f.NativeHeader(0x98, 24, 4); f.Memory.Bytes[(int)(Function + 0x98 + 4 - Image)] = 2;
        Reject(() => f.Reader.ValidateFunction("x", "x", Function, 0x98, declaration, 24), "parameter count disagrees with validated declaration");
        f.NativeHeader(0x98, 24, 8); Reject(() => f.Reader.ValidateFunction("x", "x", Function, 0x98, declaration, 24), "incorrect scalar parameter size");
        f.NativeHeader(0x98, 24, 4); f.Write(Function + 0x98 + 8, 4, 2);
        Reject(() => f.Reader.ValidateFunction("x", "x", Function, 0x98, declaration, 24), "return offset outside parameter block");
        f.NativeHeader(0x88, 40, 16); var guid = new FunctionDeclaration("GetCloudPlayerId", [new("ReturnValue", "StructProperty", "/Script/CoreUObject.Guid", "0000000000000480")]);
        Check(f.Reader.ValidateFunction("x", "x", Function, 0x88, guid, 40).ParmsSize == 16, "source fastcall variant corroborates Guid getter");
        f.NativeHeader(0x98, 24, 4); f.Write(Function + 0x98 + 24, Image + 32, 8);
        Reject(() => f.Reader.ValidateFunction("x", "x", Function, 0x98, declaration, 24), "function pointer targets non-executable image data");
        var reader = new LegacyReader(new BoundedMemory(f.Memory));
        Reject(() => reader.AcceptedFunction("PlayerControllerBase", "GetNumProfiles"), "sampling requested before completed declaration gates");
        f = new Fixture(); f.Pe(0x60000000);
        Check(NativeCode.ReadRanges(new BoundedMemory(f.Memory), Image, f.Memory.Bytes.Length).Single().Start == Code, "PE executable range selected within image");
        f.Pe(0xe0000000); Check(NativeCode.ReadRanges(new BoundedMemory(f.Memory), Image, f.Memory.Bytes.Length).Length == 1, "readable executable sections need no protection changes even if image permits writes");
        f.Pe(0x20000000); Reject(() => NativeCode.ReadRanges(new BoundedMemory(f.Memory), Image, f.Memory.Bytes.Length), "image has no readable executable section");
        f.Pe(0x60000000); f.Write(Image + 100, 0xffff, 4);
        Reject(() => NativeCode.ReadRanges(new BoundedMemory(f.Memory), Image, f.Memory.Bytes.Length), "executable section outside module bounds");
        f.Write(Image, 0, 2); Reject(() => NativeCode.ReadRanges(new BoundedMemory(f.Memory), Image, f.Memory.Bytes.Length), "non-PE code range source");
        Check(WindowsMemory.Access == 0x410, "attachment requests only query/read rights");
    }
    sealed class FakeMemory : IMemory {
        internal readonly byte[] Bytes = new byte[8192]; internal bool Partial; internal Func<ulong, int, byte[]?>? Override;
        public byte[] Read(ulong address, int size) {
            var value = Override?.Invoke(address, size); if (value != null) return value;
            if (address < Image || address - Image > (ulong)(Bytes.Length - size)) throw new ReadFailure("Fixture bounds.");
            return Bytes.AsSpan((int)(address - Image), Partial ? size - 1 : size).ToArray();
        }
    }
    sealed class Fixture {
        internal readonly FakeMemory Memory = new();
        internal NativeCode Reader => new(new BoundedMemory(Memory), Image, [new(Code, 4096)]);
        internal void Put(ulong address, byte[] value) => value.CopyTo(Memory.Bytes, (int)(address - Image));
        internal void Write(ulong address, ulong value, int size) { var bytes = new byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(bytes, value); Put(address, bytes[..size]); }
        internal void NativeHeader(int offset, int delta, int size) { Put(Function + (ulong)offset, new byte[delta + 8]); Write(Function + (ulong)offset, 0x400, 4); Write(Function + (ulong)offset + 4, 1, 1); Write(Function + (ulong)offset + 6, (ulong)size, 2); Write(Function + (ulong)offset + (ulong)delta, Code, 8); }
        internal void Pe(uint flags) { Write(Image, 0x5a4d, 2); Write(Image + 60, 64, 4); Write(Image + 64, 0x4550, 4); Write(Image + 68, 0x00018664, 4); Write(Image + 84, 0, 2); Write(Image + 96, 4096, 4); Write(Image + 100, 4096, 4); Write(Image + 124, flags, 4); }
    }
}
