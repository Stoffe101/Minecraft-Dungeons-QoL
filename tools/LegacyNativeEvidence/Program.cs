using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using LegacyNativeEvidence;

if (args.SequenceEqual(new[] { "--self-test" })) { EvidenceTests.Run(); return 0; }
bool serializationContracts = args.Length == 3 && args[2] == "--serialization-contracts";
if ((args.Length != 2 && !serializationContracts) || !int.TryParse(args[0], out var pid)) { Console.Error.WriteLine("Usage: LegacyNativeEvidence <Dungeons process ID> <new output directory> [--serialization-contracts] OR --self-test"); return 2; }
if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) { Console.Error.WriteLine("Collection requires Windows x64. No game files have been changed."); return 2; }
var output = Path.GetFullPath(args[1]);
var allowed = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ".research")) + Path.DirectorySeparatorChar;
if (!output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || Directory.Exists(output) || File.Exists(output)) { Console.Error.WriteLine("Use a new directory below this repository's .research folder."); return 2; }
Directory.CreateDirectory(output); Capture capture; LegacyReader? reader = null; BoundedMemory? memory = null; string stage = "process-attachment";
try {
    using var process = Process.GetProcessById(pid);
    if (process.ProcessName != "Dungeons") throw new ReadFailure("Selected process is not Dungeons; refusing attachment.");
    using var native = new WindowsMemory(pid); memory = new BoundedMemory(native);
    var module = process.MainModule ?? throw new ReadFailure("Main module unavailable; do not bypass permissions.");
    stage = "image-data";
    var regions = ImageData.Read(memory, (ulong)module.BaseAddress, module.ModuleMemorySize);
    reader = new LegacyReader(memory, serializationContracts); capture = reader.Collect(regions);
} catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) {
    capture = new Capture(false, [ex is ReadFailure or BudgetExceeded ? ex.Message : "Process access failed or changed; capture incomplete."], []);
}
// No executable bytes, object addresses, item instances, saves, account IDs or local paths.
File.WriteAllText(Path.Combine(output, "REPORT.json"), JsonSerializer.Serialize(new {
    schemaVersion = 1, readerRevision = "legacy-serialization-contracts-v6", targetSet = serializationContracts ? "favorites-serialization" : "favorites-core", capture.Completed, capture.Issues, capture.Declarations,
    diagnostics = new { stage = reader?.Stage ?? stage, readCalls = memory?.ReadCalls ?? 0, readBytes = memory?.ReadBytes ?? 0, elapsedMilliseconds = memory?.ElapsedMilliseconds ?? 0 },
    mode = "external-read-only-legacy-declarations", gameProcessModified = false,
    note = "Experimental reader; matching seven call shapes does not establish native ABI or favorite persistence. No instance values or memory dumps exported."
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(capture.Completed ? "Native declaration capture completed." : "Capture incomplete; see REPORT.json. Do not alter game protections or reinstall UE4SS.");
return capture.Completed ? 0 : 1;

static class ImageData
{
    public static List<Region> Read(IMemory memory, ulong image, int imageSize) {
        if (imageSize < 4096 || imageSize > 512 * 1024 * 1024) throw new ReadFailure("Invalid image size.");
        var header = memory.Read(image, 4096);
        if (BinaryPrimitives.ReadUInt16LittleEndian(header) != 0x5a4d) throw new ReadFailure("Invalid image header.");
        int pe = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(60));
        if (pe < 64 || pe > 3072 || BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(pe)) != 0x4550 || BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 4)) != 0x8664) throw new ReadFailure("Expected x64 PE image.");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 6)), optional = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 20)); int start = pe + 24 + optional;
        if (count < 1 || count > 64 || start + count * 40 > header.Length) throw new ReadFailure("Invalid section table.");
        var regions = new List<Region>(); long total = 0;
        for (int i = 0; i < count; i++) {
            var section = header.AsSpan(start + i * 40, 40); uint flags = BinaryPrimitives.ReadUInt32LittleEndian(section[36..]);
            if ((flags & 0x80000000) == 0 || (flags & 0x20000000) != 0) continue; // writable, non-executable data
            int size = BinaryPrimitives.ReadInt32LittleEndian(section[8..]), rva = BinaryPrimitives.ReadInt32LittleEndian(section[12..]);
            if (size < 0 || rva < 0 || rva > imageSize - size || (total += size) > 64 * 1024 * 1024) throw new ReadFailure("Image data exceeds bounded scan.");
            for (int offset = 0; offset < size; offset += 1024 * 1024 - 4096) {
                int n = Math.Min(1024 * 1024, size - offset); if (n < 32) break;
                regions.Add(new Region(image + (ulong)rva + (ulong)offset, memory.Read(image + (ulong)rva + (ulong)offset, n)));
            }
        }
        if (regions.Count == 0) throw new ReadFailure("No readable image data sections."); return regions;
    }
}
