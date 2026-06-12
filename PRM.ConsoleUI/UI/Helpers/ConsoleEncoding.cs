using System.Runtime.InteropServices;
using System.Text;

namespace PRM.ConsoleUI.UI.Helpers;

public static class ConsoleEncoding
{
    public static void Configure()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        if (OperatingSystem.IsWindows())
        {
            EnableVirtualTerminalProcessing();
        }
    }

    private static void EnableVirtualTerminalProcessing()
    {
        var handle = GetStdHandle(StdOutputHandle);
        if (handle == nint.Zero || handle == new nint(-1))
        {
            return;
        }
        if (!GetConsoleMode(handle, out var mode))
        {
            return;
        }
        SetConsoleMode(handle, mode | EnableVirtualTerminalProcessingFlag);
    }

    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessingFlag = 0x0004;
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int nStdHandle);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
}
