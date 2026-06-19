using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32;

internal static partial class PInvoke {
    internal static unsafe PCSTR AllocatePCSTR(string str) => (PCSTR)(byte*)Marshal.StringToHGlobalAnsi(str);
}
