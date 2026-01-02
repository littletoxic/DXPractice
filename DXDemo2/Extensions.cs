using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;

namespace Windows.Win32;

internal static partial class PInvoke {
    public static HRESULT D3D12GetDebugInterface<T>(out T ppvDebug) where T : class, ID3D12Debug {
        var hr = D3D12GetDebugInterface(typeof(T).GUID, out var result);
        ppvDebug = result as T;
        return hr;
    }

    public static HRESULT CreateDXGIFactory2<T>(DXGI_CREATE_FACTORY_FLAGS flags, out T ppvFactory) where T : class, IDXGIFactory2 {
        var hr = CreateDXGIFactory2(flags, typeof(T).GUID, out var result);
        ppvFactory = result as T;
        return hr;
    }

    public static HRESULT D3D12CreateDevice<T>(IDXGIAdapter1 adapter, D3D_FEATURE_LEVEL level, out T ppvDevice) where T : class, ID3D12Device {
        var hr = D3D12CreateDevice(adapter, level, typeof(T).GUID, out var result);
        ppvDevice = result as T;
        return hr;
    }
}
