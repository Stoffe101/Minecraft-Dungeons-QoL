using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace LegacyNativeEvidence;

interface IMemory { byte[] Read(ulong address, int size); }
sealed class ReadFailure(string message) : IOException(message);
sealed class BudgetExceeded(string reason = "unspecified") : IOException("Read/time budget exceeded (" + reason + "); capture is incomplete.");
sealed class BoundedMemory(IMemory inner, int maxCalls = 2_000_000, long maxBytes = 256L * 1024 * 1024, TimeSpan? timeout = null) : IMemory
{
    readonly Stopwatch clock = Stopwatch.StartNew(); int calls; long bytes;
    public int ReadCalls => calls;
    public long ReadBytes => bytes;
    public long ElapsedMilliseconds => clock.ElapsedMilliseconds;
    public byte[] Read(ulong address, int size) {
        if (size < 1 || size > 1024 * 1024 || address < 0x10000 || address > 0x00007fffffffffffUL - (ulong)size) throw new ReadFailure("Invalid bounded read range.");
        if (++calls > maxCalls) throw new BudgetExceeded("calls");
        if ((bytes += size) > maxBytes) throw new BudgetExceeded("bytes");
        if (clock.Elapsed > (timeout ?? TimeSpan.FromSeconds(90))) throw new BudgetExceeded("time");
        var result = inner.Read(address, size);
        if (result.Length != size) throw new ReadFailure("Partial memory read."); return result;
    }
}
static class MemoryValues
{
    public static ulong U64(this IMemory m, ulong a) => BinaryPrimitives.ReadUInt64LittleEndian(m.Read(a, 8));
    public static int I32(this IMemory m, ulong a) => BinaryPrimitives.ReadInt32LittleEndian(m.Read(a, 4));
    public static bool Pointer(ulong p) => p >= 0x10000 && p < 0x0000800000000000UL && p % 8 == 0;
}
// No write, injection, privilege escalation, suspend, remote thread or driver APIs.
sealed class WindowsMemory : IMemory, IDisposable
{
    public const uint Access = 0x0400 | 0x0010; // QUERY_INFORMATION | VM_READ only
    readonly SafeProcessHandle handle;
    public WindowsMemory(int pid) {
        handle = OpenProcess(Access, false, pid);
        if (handle.IsInvalid) { var error = Marshal.GetLastWin32Error(); handle.Dispose(); throw new ReadFailure($"OpenProcess denied/failed ({error}). Do not change ownership, ACLs or protections."); }
    }
    public byte[] Read(ulong address, int size) {
        var data = new byte[size];
        if (!ReadProcessMemory(handle, (nint)address, data, (nuint)size, out var read) || read != (nuint)size) throw new ReadFailure("Memory region inaccessible or changed during collection.");
        return data;
    }
    public void Dispose() => handle.Dispose();
    [DllImport("kernel32.dll", SetLastError = true)] static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ReadProcessMemory(SafeProcessHandle process, nint address, [Out] byte[] data, nuint size, out nuint read);
}
