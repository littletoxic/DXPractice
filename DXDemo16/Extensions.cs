using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct2D;

namespace Windows.Win32;

internal static partial class PInvoke {
    internal static unsafe PCSTR AllocatePCSTR(string str) => (PCSTR)(byte*)Marshal.StringToHGlobalAnsi(str);

    internal static HRESULT D2D1CreateFactory<T>(D2D1_FACTORY_TYPE factoryType, [Optional] D2D1_FACTORY_OPTIONS? pFactoryOptions, out T device) where T : class, ID2D1Factory {
        var hr = D2D1CreateFactory(factoryType, typeof(T).GUID, pFactoryOptions, out var result);
        device = (T)result;
        return hr;
    }
}
