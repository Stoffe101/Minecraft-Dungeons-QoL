using System.Diagnostics;
using System.Text.Json;
using LegacyNativeEvidence;
using LegacyNativeCodeEvidence;

if (args.SequenceEqual(new[] { "--self-test" })) { CodeTests.Run(); return 0; }
if (args.Length != 2 || !int.TryParse(args[0], out var pid)) { Console.Error.WriteLine("Usage: LegacyNativeCodeEvidence <Dungeons process ID> <new output directory> OR --self-test"); return 2; }
if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) { Console.Error.WriteLine("Collection requires Windows x64."); return 2; }
var output = Path.GetFullPath(args[1]);
var allowed = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ".research")) + Path.DirectorySeparatorChar;
if (!output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || Directory.Exists(output) || File.Exists(output)) { Console.Error.WriteLine("Use a new directory below this repository's .research folder."); return 2; }
Directory.CreateDirectory(output);
Capture? declarations = null; CodeCapture? code = null; object? imageIdentity = null; string[] issues = []; BoundedMemory? memory = null;
try {
    using var process = Process.GetProcessById(pid);
    if (process.ProcessName != "Dungeons") throw new ReadFailure("Selected process is not Dungeons.");
    using var native = new WindowsMemory(pid); memory = new BoundedMemory(native);
    var module = process.MainModule ?? throw new ReadFailure("Main module unavailable; do not bypass permissions.");
    ulong image = (ulong)module.BaseAddress;
    var reader = new LegacyReader(memory, serializationContracts: true);
    declarations = reader.Collect(ImageData.Read(memory, image, module.ModuleMemorySize));
    if (!declarations.Completed) throw new ReadFailure("All fourteen native declarations are required before sampling.");
    var ranges = NativeCode.ReadRanges(memory, image, module.ModuleMemorySize);
    var peHeader = memory.Read(image, 4096);
    int pe = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(peHeader.AsSpan(60));
    imageIdentity = new { moduleImageSize = module.ModuleMemorySize, peTimeDateStamp = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(peHeader.AsSpan(pe + 8)).ToString("x8") };
    code = new NativeCode(memory, image, ranges).Collect(reader);
} catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException or OverflowException) {
    issues = [ex is ReadFailure or BudgetExceeded ? ex.Message : "Process access/layout changed; capture incomplete."];
}
bool completed = declarations?.Completed == true && code?.Completed == true && issues.Length == 0;
File.WriteAllText(Path.Combine(output, "REPORT.json"), JsonSerializer.Serialize(new {
    schemaVersion = 1, readerRevision = "legacy-serialization-code-v1", completed, issues, imageIdentity, declarations, code,
    diagnostics = new { readCalls = memory?.ReadCalls ?? 0, readBytes = memory?.ReadBytes ?? 0, elapsedMilliseconds = memory?.ElapsedMilliseconds ?? 0 },
    mode = "external-read-only-bounded-native-code", gameProcessModified = false,
    note = "Private native code snippets / main-image-relative RVAs. No native calls, process writes, item/save values, whole-image dump or loader. Decoder follows named roots and one level of direct targets only; limits do not establish full behavior or callable ABI."
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(completed ? "Bounded native code evidence collected." : "Capture incomplete; keep REPORT.json. Do not change protections or install a loader.");
return completed ? 0 : 1;
