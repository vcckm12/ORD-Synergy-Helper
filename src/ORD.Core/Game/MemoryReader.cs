using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ORD.Core.Game;

public class MemoryReader : IDisposable
{
    private const int PROCESS_VM_READ = 0x0010;
    private const int PROCESS_QUERY_INFORMATION = 0x0400;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    public GameProcessInfo? Attach(Process process)
    {
        try
        {
            var handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, process.Id);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var info = new GameProcessInfo
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                ProcessHandle = handle,
                Is64Bit = Environment.Is64BitOperatingSystem && !IsWow64(handle)
            };

            // Game.dll (클래식) 또는 메인 모듈 베이스 주소 탐색
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals("Game.dll", StringComparison.OrdinalIgnoreCase))
                {
                    info.GameModuleBase = module.BaseAddress;
                    info.DetectedVersion = War3Version.Classic_128;
                    break;
                }
                else if (module.ModuleName.Equals("Warcraft III.exe", StringComparison.OrdinalIgnoreCase))
                {
                    info.GameModuleBase = module.BaseAddress;
                    info.DetectedVersion = War3Version.Reforged;
                }
            }

            return info;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process([In] IntPtr processHandle, [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

    private static bool IsWow64(IntPtr processHandle)
    {
        if (Environment.OSVersion.Version.Major >= 5 && Environment.OSVersion.Version.Minor >= 1)
        {
            if (IsWow64Process(processHandle, out bool isWow64))
            {
                return isWow64;
            }
        }
        return false;
    }

    public bool ReadBytes(IntPtr hProcess, IntPtr address, byte[] buffer)
    {
        if (hProcess == IntPtr.Zero || address == IntPtr.Zero) return false;
        return ReadProcessMemory(hProcess, address, buffer, buffer.Length, out _);
    }

    public bool ReadInt32(IntPtr hProcess, IntPtr address, out int value)
    {
        value = 0;
        var buf = new byte[4];
        if (ReadBytes(hProcess, address, buf))
        {
            value = BitConverter.ToInt32(buf, 0);
            return true;
        }
        return false;
    }

    public bool ReadPointer(IntPtr hProcess, IntPtr address, bool is64Bit, out IntPtr ptr)
    {
        ptr = IntPtr.Zero;
        int size = is64Bit ? 8 : 4;
        var buf = new byte[size];
        if (ReadBytes(hProcess, address, buf))
        {
            ptr = is64Bit ? (IntPtr)BitConverter.ToInt64(buf, 0) : (IntPtr)BitConverter.ToInt32(buf, 0);
            return true;
        }
        return false;
    }

    public void Detach(GameProcessInfo info)
    {
        if (info.ProcessHandle != IntPtr.Zero)
        {
            CloseHandle(info.ProcessHandle);
            info.ProcessHandle = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        // Cleanup resources
    }
}
