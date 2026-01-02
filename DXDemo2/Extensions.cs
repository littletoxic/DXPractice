using System.Runtime.InteropServices.Marshalling;
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

internal unsafe sealed class ComPtr<T>(T managed) : IDisposable {
    public T Managed { get; } = managed;
    public void* Ptr { get; private set; } = ComInterfaceMarshaller<T>.ConvertToUnmanaged(managed);
    private bool disposed = false;

    public void Dispose() {
        if (disposed)
            return;

        ComInterfaceMarshaller<T>.Free(Ptr);
        disposed = true;
        GC.SuppressFinalize(this);
    }

    ~ComPtr() {
        Dispose();
    }
}

internal static class Extensions {
    // ID3D12Device 实例扩展
    public static void CreateCommandQueue<T>(this ID3D12Device device, in D3D12_COMMAND_QUEUE_DESC desc, out T ppCommandQueue) where T : class, ID3D12CommandQueue {
        device.CreateCommandQueue(desc, typeof(T).GUID, out var result);
        ppCommandQueue = result as T;
    }

    public static void CreateCommandAllocator<T>(this ID3D12Device device, D3D12_COMMAND_LIST_TYPE type, out T ppCommandAllocator) where T : class, ID3D12CommandAllocator {
        device.CreateCommandAllocator(type, typeof(T).GUID, out var result);
        ppCommandAllocator = result as T;
    }

    public static void CreateCommandList<T>(this ID3D12Device device, uint nodeMask, D3D12_COMMAND_LIST_TYPE type, ID3D12CommandAllocator allocator, ID3D12PipelineState pipelineState, out T ppCommandList) where T : class, ID3D12CommandList {
        device.CreateCommandList(nodeMask, type, allocator, pipelineState, typeof(T).GUID, out var result);
        ppCommandList = result as T;
    }

    public static void CreateDescriptorHeap<T>(this ID3D12Device device, in D3D12_DESCRIPTOR_HEAP_DESC desc, out T ppDescriptorHeap) where T : class, ID3D12DescriptorHeap {
        device.CreateDescriptorHeap(desc, typeof(T).GUID, out var result);
        ppDescriptorHeap = result as T;
    }

    public static void CreateFence<T>(this ID3D12Device device, ulong initialValue, D3D12_FENCE_FLAGS flags, out T ppFence) where T : class, ID3D12Fence {
        device.CreateFence(initialValue, flags, typeof(T).GUID, out var result);
        ppFence = result as T;
    }

    // IDXGISwapChain 实例扩展
    public static void GetBuffer<T>(this IDXGISwapChain swapChain, uint buffer, out T ppSurface) where T : class, ID3D12Resource {
        swapChain.GetBuffer(buffer, typeof(T).GUID, out var result);
        ppSurface = result as T;
    }
}
