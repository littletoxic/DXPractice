using static Windows.Win32.PInvoke;

namespace DXDemo8WBOIT;

internal static class Program {

    [STAThread]
    static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }
}