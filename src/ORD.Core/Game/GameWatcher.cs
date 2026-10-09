using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ORD.Core.Game;

public class GameLiveStatus
{
    public bool IsGameRunning { get; set; }
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string DetectedVersion { get; set; } = "미실행";
    public string DetectedMap { get; set; } = string.Empty;
    public bool IsMapActive { get; set; }
    public bool TmoBridgeConnected { get; set; }
    public DateTime LastCheckTime { get; set; } = DateTime.Now;
    public string StatusMessage { get; set; } = "대기 중 (워크래프트3 미실행)";
    public List<string> AutoDetectedUnits { get; set; } = new();
}

public class GameWatcher : IDisposable
{
    private readonly MemoryReader _memoryReader = new();
    private static readonly HttpClient _httpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ORD-Helper");
        return client;
    }

    public GameLiveStatus CheckStatus()
    {
        var status = new GameLiveStatus { LastCheckTime = DateTime.Now };

        // 1. 워크래프트 3 프로세스 탐색
        var process = FindWarcraftProcess();
        if (process != null)
        {
            status.IsGameRunning = true;
            status.ProcessId = process.Id;
            status.ProcessName = process.ProcessName;

            var processInfo = _memoryReader.Attach(process);
            if (processInfo != null)
            {
                status.DetectedVersion = processInfo.DetectedVersion switch
                {
                    War3Version.Reforged => "Warcraft III: Reforged (64-bit)",
                    War3Version.Classic_128 => "Warcraft III: Classic (1.28.5)",
                    War3Version.Classic_130 => "Warcraft III: Classic (1.30+)",
                    _ => "Warcraft III (감지됨)"
                };
            }
            else
            {
                status.DetectedVersion = "Warcraft III (실행 중)";
            }

            // 2. 로그 파일에서 현재 맵 로딩 상태 분석
            CheckLogFile(status);

            if (status.IsMapActive)
            {
                status.StatusMessage = $"🎮 원랜디 v2.323 자체 엔진 연동됨 ({status.DetectedMap})";
            }
            else
            {
                status.StatusMessage = $"🎮 워크3 실행 중 (PID: {status.ProcessId}, 인게임 방 대기)";
            }
        }
        else
        {
            status.IsGameRunning = false;
            status.StatusMessage = "대기 중 (독립 스마트 도우미 모드)";
        }

        return status;
    }

    private Process? FindWarcraftProcess()
    {
        var names = new[] { "Warcraft III", "war3" };
        foreach (var name in names)
        {
            var procs = Process.GetProcessesByName(name);
            if (procs.Length > 0)
            {
                return procs[0];
            }
        }
        return null;
    }

    private void CheckLogFile(GameLiveStatus status)
    {
        var logPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Warcraft III", "Logs", "War3Log.txt"),
            @"C:\Users\vcckm\OneDrive\문서\Warcraft III\Logs\War3Log.txt"
        };

        foreach (var logPath in logPaths)
        {
            if (File.Exists(logPath))
            {
                try
                {
                    using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    string? line;
                    string lastOpeningMap = string.Empty;
                    bool gameEnded = false;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("Opening map -"))
                        {
                            var match = Regex.Match(line, @"Opening map\s*-\s*(.*)");
                            if (match.Success)
                            {
                                lastOpeningMap = Path.GetFileName(match.Groups[1].Value.Trim());
                                gameEnded = false;
                            }
                        }
                        else if (line.Contains("GameMain Ended"))
                        {
                            gameEnded = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(lastOpeningMap) && !gameEnded)
                    {
                        status.DetectedMap = lastOpeningMap;
                        status.IsMapActive = lastOpeningMap.Contains("ORD", StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // 로그 파일 읽기 오류 무시
                }
                break;
            }
        }
    }

    private void CheckTmoBridge(GameLiveStatus status)
    {
        // TMO.GG Desktop 로컬 서버(포트 47786 기본, 48123/48124 보조)
        int[] commonPorts = { 47786, 48123, 48124, 8080 };
        foreach (var port in commonPorts)
        {
            try
            {
                using var msg = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/status");
                msg.Headers.Add("Origin", "https://tmo.gg");
                var task = _httpClient.SendAsync(msg);
                if (task.Wait(300))
                {
                    var resp = task.Result;
                    if (resp.IsSuccessStatusCode)
                    {
                        status.TmoBridgeConnected = true;
                        FetchTmoDatas(status, port);
                        break;
                    }
                }
            }
            catch
            {
                // 브릿지 미실행 상태
            }
        }
    }

    private void FetchTmoDatas(GameLiveStatus status, int port)
    {
        try
        {
            using var msg = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/datas");
            msg.Headers.Add("Origin", "https://tmo.gg");
            var task = _httpClient.SendAsync(msg);
            if (task.Wait(400))
            {
                var resp = task.Result;
                if (resp.IsSuccessStatusCode)
                {
                    var json = resp.Content.ReadAsStringAsync().Result;
                    // TMO JSON 파싱 시 유닛 ID 수집 가능
                    if (!string.IsNullOrEmpty(json))
                    {
                        // JSON 데이터 처리 로직
                    }
                }
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _memoryReader.Dispose();
    }
}
