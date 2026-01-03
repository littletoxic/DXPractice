using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;

namespace Windows.Win32;

internal static partial class PInvoke {
    internal static HRESULT D3D12GetDebugInterface<T>(out T debug) where T : class, ID3D12Debug {
        var hr = D3D12GetDebugInterface(typeof(T).GUID, out var result);
        debug = result as T;
        return hr;
    }

    internal static HRESULT CreateDXGIFactory2<T>(DXGI_CREATE_FACTORY_FLAGS flags, out T factory) where T : class, IDXGIFactory2 {
        var hr = CreateDXGIFactory2(flags, typeof(T).GUID, out var result);
        factory = result as T;
        return hr;
    }

    internal static HRESULT D3D12CreateDevice<T>(IDXGIAdapter1 adapter, D3D_FEATURE_LEVEL level, out T device) where T : class, ID3D12Device {
        var hr = D3D12CreateDevice(adapter, level, typeof(T).GUID, out var result);
        device = result as T;
        return hr;
    }
}

internal unsafe sealed class ComPtr<T>(T managed) : IDisposable {
    public T Managed { get; } = managed;
    public void* Ptr { get; private set; } = ComInterfaceMarshaller<T>.ConvertToUnmanaged(managed);
    private bool _disposed = false;

    public void Dispose() {
        if (_disposed)
            return;

        ComInterfaceMarshaller<T>.Free(Ptr);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~ComPtr() {
        Dispose();
    }
}

internal static class Extensions {
    // ID3D12Device 实例扩展
    internal static void CreateCommandQueue<T>(this ID3D12Device device, in D3D12_COMMAND_QUEUE_DESC desc, out T commandQueue) where T : class, ID3D12CommandQueue {
        device.CreateCommandQueue(desc, typeof(T).GUID, out var result);
        commandQueue = result as T;
    }

    internal static void CreateCommandAllocator<T>(this ID3D12Device device, D3D12_COMMAND_LIST_TYPE type, out T commandAllocator) where T : class, ID3D12CommandAllocator {
        device.CreateCommandAllocator(type, typeof(T).GUID, out var result);
        commandAllocator = result as T;
    }

    internal static void CreateCommandList<T>(this ID3D12Device device, uint nodeMask, D3D12_COMMAND_LIST_TYPE type, ID3D12CommandAllocator allocator, ID3D12PipelineState pipelineState, out T commandList) where T : class, ID3D12CommandList {
        device.CreateCommandList(nodeMask, type, allocator, pipelineState, typeof(T).GUID, out var result);
        commandList = result as T;
    }

    internal static void CreateDescriptorHeap<T>(this ID3D12Device device, in D3D12_DESCRIPTOR_HEAP_DESC desc, out T descriptorHeap) where T : class, ID3D12DescriptorHeap {
        device.CreateDescriptorHeap(desc, typeof(T).GUID, out var result);
        descriptorHeap = result as T;
    }

    internal static void CreateFence<T>(this ID3D12Device device, ulong initialValue, D3D12_FENCE_FLAGS flags, out T fence) where T : class, ID3D12Fence {
        device.CreateFence(initialValue, flags, typeof(T).GUID, out var result);
        fence = result as T;
    }

    internal unsafe static void CreateRootSignature<T>(this ID3D12Device device, uint nodeMask, void* pBlobWithRootSignature, nuint blobLengthInBytes, out T fence) where T : class, ID3D12RootSignature {
        device.CreateRootSignature(nodeMask, pBlobWithRootSignature, blobLengthInBytes, typeof(T).GUID, out var result);
        fence = result as T;
    }

    internal static void CreateGraphicsPipelineState<T>(this ID3D12Device device, in D3D12_GRAPHICS_PIPELINE_STATE_DESC desc, out T pipelineState) where T : class, ID3D12PipelineState {
        device.CreateGraphicsPipelineState(desc, typeof(T).GUID, out var result);
        pipelineState = result as T;
    }

    internal static unsafe void CreateCommittedResource<T>(this ID3D12Device @this, in D3D12_HEAP_PROPERTIES pHeapProperties, D3D12_HEAP_FLAGS HeapFlags, in D3D12_RESOURCE_DESC pDesc, D3D12_RESOURCE_STATES InitialResourceState, [Optional] D3D12_CLEAR_VALUE? pOptimizedClearValue, out T resource) where T : class, ID3D12Resource {
        var riidResource = typeof(T).GUID;

        resource = null;
        void* __ppvResource_native = default;

        fixed (D3D12_RESOURCE_DESC* pDescLocal = &pDesc) {
            fixed (D3D12_HEAP_PROPERTIES* pHeapPropertiesLocal = &pHeapProperties) {
                D3D12_CLEAR_VALUE pOptimizedClearValueLocal = pOptimizedClearValue ?? default;
                @this.CreateCommittedResource(pHeapPropertiesLocal, HeapFlags, pDescLocal, InitialResourceState, pOptimizedClearValue.HasValue ? &pOptimizedClearValueLocal : null, &riidResource, &__ppvResource_native);
            }
        }

        resource = ComInterfaceMarshaller<T>.ConvertToManaged(__ppvResource_native);
        ComInterfaceMarshaller<object>.Free(__ppvResource_native);
    }

    // IDXGISwapChain 实例扩展
    internal static void GetBuffer<T>(this IDXGISwapChain swapChain, uint bufferIndex, out T surface) where T : class, ID3D12Resource {
        swapChain.GetBuffer(bufferIndex, typeof(T).GUID, out var result);
        surface = result as T;
    }
}
