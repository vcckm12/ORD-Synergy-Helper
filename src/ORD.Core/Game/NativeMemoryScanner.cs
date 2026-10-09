using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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

    // ORD 유닛 이름 및 티어 구분자: "|r - |c"
    private static readonly byte[] UnitDelimiter = Encoding.UTF8.GetBytes("|r - |c");

    public NativeMemoryScanner(IEnumerable<OrdUnit>? units = null)
    {
        if (units != null)
        {
            SetUnits(units);
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORD NativeScanner Error] {ex.Message}");
            }

            try
            {
                Task.Delay(2000, token).Wait(token);
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
            List<OrdUnit> unitsSnapshot;
            lock (_lock)
            {
                unitsSnapshot = _allUnits.ToList();
            }

            if (unitsSnapshot.Count == 0) return;

            var foundCodes = new HashSet<string>();
            Console.WriteLine($"[ORD NativeScanner] Starting scan on PID {pid} (Units in DB: {unitsSnapshot.Count})...");

            // 워크래프트 3 64비트 리포지드 동적 게임 로그 힙 영역
            ulong addr = 0x1C130000000;
            const ulong maxAddr = 0x1C250000000;
            MEMORY_BASIC_INFORMATION64 mbi;

            while (VirtualQueryEx(hProcess, (IntPtr)addr, out mbi, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64))) > 0)
            {
                if (mbi.State == 0x1000 && (mbi.Protect == 0x04 || mbi.Protect == 0x02) && mbi.RegionSize < 50 * 1024 * 1024)
                {
                    int chunkSize = Math.Min((int)mbi.RegionSize, 2 * 1024 * 1024);
                    byte[] buffer = new byte[chunkSize];

                    if (ReadProcessMemory(hProcess, (IntPtr)mbi.BaseAddress, buffer, buffer.Length, out var read) && (long)read > 32)
                    {
                        int limit = (int)read;
                        int dLen = UnitDelimiter.Length;

                        for (int i = 0; i <= limit - dLen; i++)
                        {
                            bool matchDelim = true;
                            for (int j = 0; j < dLen; j++)
                            {
                                if (buffer[i + j] != UnitDelimiter[j])
                                {
                                    matchDelim = false;
                                    break;
                                }
                            }

                            if (!matchDelim) continue;

                            // 앞뒤 컨텍스트 추출
                            int start = Math.Max(0, i - 55);
                            int after = Math.Min(limit, i + dLen + 35);
                            string beforeStr = Encoding.UTF8.GetString(buffer, start, i - start);
                            string afterStr = Encoding.UTF8.GetString(buffer, i + dLen, after - (i + dLen));

                            // 인게임 유닛 획득/제작 로그 여부 검증
                            bool isPlayerLog = beforeStr.Contains("획득") ||
                                               beforeStr.Contains("1 :") ||
                                               beforeStr.Contains("1 R") ||
                                               beforeStr.Contains("00:") ||
                                               beforeStr.Contains("goodisgood");

                            if (!isPlayerLog) continue;

                            // 유닛 이름 파싱
                            int barIdx = beforeStr.LastIndexOf('|');
                            string rawName = (barIdx >= 0) ? beforeStr.Substring(barIdx + 1) : beforeStr;
                            rawName = rawName.Trim();

                            // 색상 접두어 제거 (|cff..., |c00...)
                            if (rawName.Length > 8 && (rawName.StartsWith("cff", StringComparison.OrdinalIgnoreCase) || rawName.StartsWith("c00", StringComparison.OrdinalIgnoreCase)))
                            {
                                rawName = rawName.Substring(8).Trim();
                            }

                            // 티어 파싱
                            int endBar = afterStr.IndexOf("|r");
                            if (endBar <= 0) continue;

                            string rawTier = afterStr.Substring(0, endBar).Trim();
                            if (rawTier.Length > 8)
                            {
                                rawTier = rawTier.Substring(8).Trim();
                            }

                            var matchedTier = MapTier(rawTier);
                            if (matchedTier == null) continue;

                            // 데이터베이스 유닛과 매칭
                            var matchedUnit = FindMatchingUnit(unitsSnapshot, rawName, matchedTier.Value);
                            if (matchedUnit != null)
                            {
                                foundCodes.Add(matchedUnit.Id);
                                Console.WriteLine($"[ORD NativeScanner] Detected: {matchedUnit.Name} [{matchedUnit.Id}] (Tier: {rawTier})");
                            }
                        }
                    }
                }

                addr = mbi.BaseAddress + mbi.RegionSize;
                if (addr >= maxAddr) break;
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
            Console.WriteLine($"[ORD NativeScanner] Scan finished. Newly found: {foundCodes.Count}, Total detected: {_detectedCodes.Count}");
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    private static UnitTier? MapTier(string rawTier)
    {
        if (rawTier.Contains("초월")) return UnitTier.Transcendence;
        if (rawTier.Contains("불멸")) return UnitTier.Immortal;
        if (rawTier.Contains("영원")) return UnitTier.Eternal;
        if (rawTier.Contains("제한") || rawTier.Contains("세라핌")) return UnitTier.Limited;
        if (rawTier.Contains("전설")) return UnitTier.Legendary;
        if (rawTier.Contains("히든")) return UnitTier.Hidden;
        if (rawTier.Contains("희귀")) return UnitTier.Rare;
        if (rawTier.Contains("특별")) return UnitTier.Special;
        if (rawTier.Contains("오로성")) return UnitTier.Gorosei;
        return null;
    }

    private static OrdUnit? FindMatchingUnit(List<OrdUnit> allUnits, string rawName, UnitTier tier)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return null;

        // 1. 정확한 이름 + 티어 일치
        var unit = allUnits.Find(u => u.Tier == tier && u.Name.Equals(rawName, StringComparison.OrdinalIgnoreCase));
        if (unit != null) return unit;

        // 2. 이름 접두사 일치 (e.g. "마르코 환수종" -> "마르코 환수종")
        unit = allUnits.Find(u => u.Tier == tier && (u.Name.StartsWith(rawName, StringComparison.OrdinalIgnoreCase) || rawName.StartsWith(u.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase)));
        if (unit != null) return unit;

        // 3. 괄호 제거 후 매칭 (e.g. "토키" vs "아마츠키 토키", "쿠마 폭군" vs "바솔로뮤 쿠마")
        string cleanRaw = Regex.Replace(rawName, @"[\s\.\-]+", "");
        unit = allUnits.Find(u =>
        {
            if (u.Tier != tier) return false;
            string cleanDb = Regex.Replace(u.Name, @"[\s\.\-\(\)]+", "");
            return cleanDb.Contains(cleanRaw) || cleanRaw.Contains(cleanDb);
        });

        return unit;
    }

    public void Dispose()
    {
        Stop();
    }
}
