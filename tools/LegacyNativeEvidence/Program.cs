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
