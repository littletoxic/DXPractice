using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using static Windows.Win32.PInvoke;

namespace DXDemo2;

internal static class Program {
    [STAThread]
    static void Main() {
        var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }

}
internal unsafe class DX12Engine {
    private const int WindowWidth = 640;
    private const int WindowHeight = 480;
    private const int FrameCount = 3;

    private HWND m_hwnd;

    private ID3D12Debug m_D3D12DebugDevice;
    private DXGI_CREATE_FACTORY_FLAGS m_DXGICreateFactoryFlag;

    private IDXGIFactory5 m_DXGIFactory;
    private IDXGIAdapter1 m_DXGIAdapter;
    private ID3D12Device4 m_D3D12Device;

    private ID3D12CommandQueue m_CommandQueue;
    private ID3D12CommandAllocator m_CommandAllocator;
    private ID3D12GraphicsCommandList m_CommandList;

    private ID3D12DescriptorHeap m_RTVHeap;
    private IDXGISwapChain3 m_DXGISwapChain;
    private ID3D12Resource[] m_RenderTarget;
    private D3D12_CPU_DESCRIPTOR_HANDLE RTVHandle;
    private uint RTVDescriptorSize = 0;
    private uint FrameIndex = 0;

    private ID3D12Fence m_Fence;
    ulong FenceValue = 0;
    HANDLE RenderEvent;
    D3D12_RESOURCE_BARRIER beg_barrier;
    D3D12_RESOURCE_BARRIER end_barrier;

    private readonly float[] SkyBlue = [0.529411793f, 0.807843208f, 0.921568692f, 1f];

    private void InitWindow(HINSTANCE hins) {
        const string className = "DX12 Game";

        fixed (char* pClassName = className) {
            WNDCLASSW wc = new() {
                hInstance = hins,
                lpfnWndProc = &CallBackFunc,
                lpszClassName = pClassName,
            };

            RegisterClass(wc);
        }

        m_hwnd = CreateWindowEx(
            0,
            className,
            "DX12 Game Window",
            WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_OVERLAPPED,
            10,
            10,
            WindowWidth,
            WindowHeight,
            HWND.Null,
            HMENU.Null,
            hins,
            null);

        ShowWindow(m_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
    }

    private void CreateDebugDevice() {
        // [STAThread] attribute on Main method handles this
        //CoInitialize();

#if DEBUG
        D3D12GetDebugInterface(typeof(ID3D12Debug).GUID, out var ppvDebug);
        m_D3D12DebugDevice = ppvDebug as ID3D12Debug;
        m_D3D12DebugDevice.EnableDebugLayer();

        m_DXGICreateFactoryFlag = DXGI_CREATE_FACTORY_FLAGS.DXGI_CREATE_FACTORY_DEBUG;
#endif
    }

    private bool CreateDevice() {
        CreateDXGIFactory2(m_DXGICreateFactoryFlag, typeof(IDXGIFactory5).GUID, out var ppvFactory);
        m_DXGIFactory = ppvFactory as IDXGIFactory5;

        // DX12 支持的所有功能版本，你的显卡最低需要支持 11.0
        D3D_FEATURE_LEVEL[] dx12SupportLevel = [
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_2,        // 12.2
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_1,        // 12.1
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0,        // 12.0
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,        // 11.1
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0         // 11.0
        ];

        for (uint i = 0; m_DXGIFactory.EnumAdapters1(i, out m_DXGIAdapter) != HRESULT.DXGI_ERROR_NOT_FOUND; i++) {
            // 找到显卡，就创建 D3D12 设备，从高到低遍历所有功能版本，创建成功就跳出
            foreach (var level in dx12SupportLevel) {
                if (D3D12CreateDevice(m_DXGIAdapter, level, typeof(ID3D12Device4).GUID, out var ppvDevice).Succeeded) {
                    m_D3D12Device = ppvDevice as ID3D12Device4;
                    return true;
                }
            }
        }

        MessageBox(default, "找不到任何能支持 DX12 的显卡，请升级电脑上的硬件！", "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        return false;
    }

    private void CreateCommandComponents() {
        const D3D12_COMMAND_LIST_TYPE type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT;

        D3D12_COMMAND_QUEUE_DESC queueDesc = new() {
            Type = type,
        };

        m_D3D12Device.CreateCommandQueue(queueDesc, typeof(ID3D12CommandQueue).GUID, out var ppCommandQueue);
        m_CommandQueue = ppCommandQueue as ID3D12CommandQueue;

        m_D3D12Device.CreateCommandAllocator(type, typeof(ID3D12CommandAllocator).GUID, out var ppCommandAllocator);
        m_CommandAllocator = ppCommandAllocator as ID3D12CommandAllocator;

        m_D3D12Device.CreateCommandList(
            0,
            type,
            m_CommandAllocator,
            null,
            typeof(ID3D12GraphicsCommandList).GUID,
            out var ppCommandList);
        m_CommandList = ppCommandList as ID3D12GraphicsCommandList;

        m_CommandList.Close();
    }

    private void CreateRenderTarget() {
        const D3D12_DESCRIPTOR_HEAP_TYPE type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV;

        D3D12_DESCRIPTOR_HEAP_DESC RTVHeapDesc = new() {
            NumDescriptors = FrameCount,
            Type = type,
        };
        m_D3D12Device.CreateDescriptorHeap(RTVHeapDesc, typeof(ID3D12DescriptorHeap).GUID, out var ppRTVHeap);
        m_RTVHeap = ppRTVHeap as ID3D12DescriptorHeap;

        DXGI_SWAP_CHAIN_DESC1 swapchainDesc = new() {
            BufferCount = FrameCount,
            Width = WindowWidth,
            Height = WindowHeight,
            Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
            SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD,
            BufferUsage = DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            SampleDesc = new() {
                Count = 1,
            },
        };

        m_DXGIFactory.CreateSwapChainForHwnd(
            m_CommandQueue,
            m_hwnd,
            swapchainDesc,
            null,
            null,
            out IDXGISwapChain1 _temp_swapchain);

        // CreateSwapChainForHwnd 不能直接用于创建高版本接口
        m_DXGISwapChain = _temp_swapchain.As<IDXGISwapChain3>();

        RTVHandle = m_RTVHeap.GetCPUDescriptorHandleForHeapStart();
        RTVDescriptorSize = m_D3D12Device.GetDescriptorHandleIncrementSize(type);

        m_RenderTarget = new ID3D12Resource[FrameCount];
        for (uint i = 0; i < FrameCount; i++) {
            m_DXGISwapChain.GetBuffer(i, typeof(ID3D12Resource).GUID, out var ppRenderTarget);
            m_RenderTarget[i] = ppRenderTarget as ID3D12Resource;

            m_D3D12Device.CreateRenderTargetView(m_RenderTarget[i], null, RTVHandle);

            RTVHandle.ptr += RTVDescriptorSize;
        }
    }

    private void CreateFenceAndBarrier() {
        RenderEvent = CreateEvent(null, false, true, null);

        m_D3D12Device.CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, typeof(ID3D12Fence).GUID, out var ppFence);
        m_Fence = ppFence as ID3D12Fence;

        // 设置资源屏障
        // beg_barrier 起始屏障：Present 呈现状态 -> Render Target 渲染目标状态
        beg_barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        beg_barrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT;
        beg_barrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;

        // end_barrier 终止屏障：Render Target 渲染目标状态 -> Present 呈现状态
        end_barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        end_barrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;
        end_barrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT;
    }

    private void Render() {
        RTVHandle = m_RTVHeap.GetCPUDescriptorHandleForHeapStart();
        FrameIndex = m_DXGISwapChain.GetCurrentBackBufferIndex();
        RTVHandle.ptr += FrameIndex * RTVDescriptorSize;

        m_CommandAllocator.Reset();
        m_CommandList.Reset(m_CommandAllocator, null);

        using (var pResource = new ComPtr<ID3D12Resource>(m_RenderTarget[FrameIndex])) {
            beg_barrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)pResource.Ptr;
            m_CommandList.ResourceBarrier([beg_barrier]);
        }

        m_CommandList.OMSetRenderTargets(1, RTVHandle, false, null);
        m_CommandList.ClearRenderTargetView(RTVHandle, SkyBlue, 0, null);

        using (var pResource0 = new ComPtr<ID3D12Resource>(m_RenderTarget[FrameIndex])) {
            end_barrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)pResource0.Ptr;
            m_CommandList.ResourceBarrier([end_barrier]);
        }

        m_CommandList.Close();

        m_CommandQueue.ExecuteCommandLists([m_CommandList]);

        m_DXGISwapChain.Present(1, 0);


        FenceValue++;
        m_CommandQueue.Signal(m_Fence, FenceValue);
        m_Fence.SetEventOnCompletion(FenceValue, RenderEvent);
    }

    private void RenderLoop() {
        bool exit = false;
        while (!exit) {
            var activeEvent = MsgWaitForMultipleObjects(
                [RenderEvent],
                false,
                INFINITE,
                QUEUE_STATUS_FLAGS.QS_ALLINPUT);

            switch (activeEvent - WAIT_EVENT.WAIT_OBJECT_0) {
                case 0: // ActiveEvent 是 0，说明渲染事件已经完成了，进行下一次渲染
                    Render();
                    break;
                case 1: // ActiveEvent 是 1，说明渲染事件未完成，CPU 主线程同时处理窗口消息，防止界面假死
                    while (PeekMessage(out MSG msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE)) {
                        if (msg.message == WM_QUIT) {
                            exit = true;
                            break;
                        }

                        TranslateMessage(msg);
                        DispatchMessage(msg);
                    }
                    break;
                case (uint)WAIT_EVENT.WAIT_TIMEOUT: // 渲染超时
                    break;
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

    internal static void Run(HINSTANCE hins) {
        DX12Engine engine = new();
        engine.InitWindow(hins);
        engine.CreateDebugDevice();
        engine.CreateDevice();
        engine.CreateCommandComponents();
        engine.CreateRenderTarget();
        engine.CreateFenceAndBarrier();

        engine.RenderLoop();
    }

}

internal unsafe readonly struct ComPtr<T>(T managed) : IDisposable {
    public void* Ptr { get; } = ComInterfaceMarshaller<T>.ConvertToUnmanaged(managed);

    public void Dispose() {
        ComInterfaceMarshaller<T>.Free(Ptr);
    }
}
