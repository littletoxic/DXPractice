using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace DXDemo8WBOIT;

internal static class CallBackWrapper {

    internal static Func<HWND, uint, WPARAM, LPARAM, LRESULT> BrokerFunc { get; set; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    internal static LRESULT CallBackFunc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
        return BrokerFunc(hwnd, msg, wParam, lParam);
    }
}
