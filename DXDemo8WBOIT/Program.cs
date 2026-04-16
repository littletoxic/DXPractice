namespace DXDemo8WBOIT;

internal static class Program {

    [STAThread]
    private static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }
}
