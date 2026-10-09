namespace ORD.Core.Game;

public enum War3Version
{
    Unknown = 0,
    Classic_128,    // 1.28.5 (M16 등 클래식 클라이언트)
    Classic_130,    // 1.30 ~ 1.31
    Reforged        // 리포지드 최신 클라이언트 (64비트)
}

public class GameProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public IntPtr ProcessHandle { get; set; } = IntPtr.Zero;
    public IntPtr GameModuleBase { get; set; } = IntPtr.Zero; // Game.dll 또는 Warcraft III.exe 베이스 주소
    public War3Version DetectedVersion { get; set; } = War3Version.Unknown;
    public bool Is64Bit { get; set; }
    public bool IsConnected => ProcessHandle != IntPtr.Zero;
}
