// https://blog.csdn.net/DGAF2198588973/article/details/144488018

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DXDemo1;

internal static class Program {
    [STAThread]
    static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }

}
internal sealed class DX12Engine {
    private const int WindowWidth = 640;
    private const int WindowHeight = 480;

    private HWND hwnd;

    private unsafe void InitWindow(SafeHandle hins) {
        const string className = "DX12 Game";

        fixed (char* pClassName = className) {
            WNDCLASSW wc = new() {
                hInstance = new(hins.DangerousGetHandle()),
                lpfnWndProc = &CallBackFunc,
                lpszClassName = pClassName,
            };

            RegisterClass(wc);
        }

        hwnd = CreateWindowEx(
            0,
            className,
            "DX12 Game Window",
            WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_OVERLAPPED,
            10,
            10,
            WindowWidth,
            WindowHeight,
            HWND.Null,
            null,
            hins,
            null);

        ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);
    }

    private void RenderLoop() {
        bool exit = false;
        while (!exit) {
            while (PeekMessage(out MSG msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE)) {
                if (msg.message == WM_QUIT) {
                    exit = true;
                    break;
                }

                TranslateMessage(msg);
                DispatchMessage(msg);
            }
        }

    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT CallBackFunc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
        switch (msg) {
            case WM_DESTROY:
                PostQuitMessage(0);
                break;
            default:
                return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        return new(0);
    }


    internal static void Run(SafeHandle hins) {
        DX12Engine engine = new();
        engine.InitWindow(hins);
        engine.RenderLoop();
    }

}