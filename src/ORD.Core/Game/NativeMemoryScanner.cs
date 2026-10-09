using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ORD.Core.Models;

namespace ORD.Core.Game;

public class NativeMemoryScanner : IDisposable
{
    private const int PROCESS_VM_READ = 0x0010;
    private const int PROCESS_QUERY_INFORMATION = 0x0400;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;
        public ulong AllocationBase;
        public uint AllocationProtect;
        public uint __alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
        public uint __alignment2;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, int dwLength);

    private readonly List<OrdUnit> _allUnits = new();
    private readonly HashSet<string> _detectedCodes = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _scanTask;
    private int _targetPid;

    public NativeMemoryScanner(IEnumerable<OrdUnit>? units = null)
    {
        if (units != null)
        {
            _allUnits.AddRange(units);
        }
    }

    public void SetUnits(IEnumerable<OrdUnit> units)
    {
        lock (_lock)
        {
            _allUnits.Clear();
            _allUnits.AddRange(units);
        }
    }

    public List<string> GetDetectedUnitCodes()
    {
        lock (_lock)
        {
            return _detectedCodes.ToList();
        }
    }

    public void Start(int processId)
    {
        if (_scanTask != null && _targetPid == processId) return;

        Stop();
        _targetPid = processId;
        _cts = new CancellationTokenSource();
        _scanTask = Task.Run(() => ScanLoop(_targetPid, _cts.Token));
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
            _scanTask?.Wait(500);
        }
        catch { }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _scanTask = null;
        }
    }

    private void ScanLoop(int pid, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                PerformScan(pid);
            }
            catch
            {
                // 스캔 도중 오류 무시 후 재시도
            }

            try
            {
                Task.Delay(3000, token).Wait(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void PerformScan(int pid)
    {
        IntPtr hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, pid);
        if (hProcess == IntPtr.Zero) return;

        try
        {
            // 상위 유닛(초월, 불멸, 영원, 제한, 전설 등) 위주로 시그니처 준비
            List<OrdUnit> candidateUnits;
            lock (_lock)
            {
                candidateUnits = _allUnits
                    .Where(u => u.Tier is UnitTier.Transcendence or UnitTier.Immortal or UnitTier.Eternal or UnitTier.Limited or UnitTier.Legendary or UnitTier.Hidden or UnitTier.Gorosei)
                    .ToList();
            }

            if (candidateUnits.Count == 0) return;

            var targets = candidateUnits.Select(u => new
            {
                Unit = u,
                Bytes = Encoding.UTF8.GetBytes(u.Name)
            }).ToList();

            var foundCodes = new HashSet<string>();
            ulong addr = 0x10000;
            MEMORY_BASIC_INFORMATION64 mbi;

            // 최대 500MB까지 가상 메모리 탐색
            ulong scannedBytes = 0;
            const ulong maxScanLimit = 500 * 1024 * 1024;

            while (VirtualQueryEx(hProcess, (IntPtr)addr, out mbi, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64))) > 0)
            {
                if (mbi.State == 0x1000 && (mbi.Protect == 0x04 || mbi.Protect == 0x02) && mbi.RegionSize < 20 * 1024 * 1024)
                {
                    int chunkSize = Math.Min((int)mbi.RegionSize, 1024 * 1024);
                    byte[] buffer = new byte[chunkSize];

                    if (ReadProcessMemory(hProcess, (IntPtr)mbi.BaseAddress, buffer, buffer.Length, out var read) && (long)read > 16)
                    {
                        scannedBytes += (ulong)read;
                        int readLen = (int)read;

                        foreach (var target in targets)
                        {
                            if (foundCodes.Contains(target.Unit.Id)) continue;

                            if (ContainsSubsequence(buffer, readLen, target.Bytes))
                            {
                                foundCodes.Add(target.Unit.Id);
                            }
                        }
                    }

                    if (scannedBytes > maxScanLimit) break;
                }

                addr = mbi.BaseAddress + mbi.RegionSize;
                if (addr >= 0x7FFFFFFFFFFF) break;
            }

            if (foundCodes.Count > 0)
            {
                lock (_lock)
                {
                    foreach (var code in foundCodes)
                    {
                        _detectedCodes.Add(code);
                    }
                }
            }
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    private static bool ContainsSubsequence(byte[] source, int length, byte[] pattern)
    {
        if (pattern.Length == 0 || length < pattern.Length) return false;
        int limit = length - pattern.Length;
        for (int i = 0; i <= limit; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return true;
        }
        return false;
    }

    public void Dispose()
    {
        Stop();
    }
}
