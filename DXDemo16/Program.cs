// https://blog.csdn.net/DGAF2198588973/article/details/158853736

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct2D;
using Windows.Win32.Graphics.Direct2D.Common;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Direct3D11on12;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Imaging;
using Windows.Win32.System.Com;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DXDemo16;


internal static class CallBackWrapper {

    internal static Func<HWND, uint, WPARAM, LPARAM, LRESULT> BrokerFunc { get; set; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    internal static LRESULT CallBackFunc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) => BrokerFunc(hwnd, msg, wParam, lParam);
}

internal sealed class D2DEngine {
    private uint _d3d11CreateDeviceFlag;

    private ID3D11Device5 _d3d11Device;
    private ID3D11DeviceContext4 _d3d11DeviceContext;
    private ID3D11On12Device1 _d3d11On12Device;

    private D2D1_FACTORY_OPTIONS _d2dCreateFactoryOptions = new();

    private readonly D2D1_DEVICE_CONTEXT_OPTIONS _d2dCreateDeviceContextOptions = D2D1_DEVICE_CONTEXT_OPTIONS_NONE;

    private ID2D1Factory7 _d2dFactory;
    private ID2D1Device6 _d2dDevice;
    private ID2D1DeviceContext6 _d2dDeviceContext;

    private uint _dpi;

    private ID3D11Resource[] _d3d11WrappedRenderTarget;
    private ID2D1Bitmap1[] _d2dRenderTarget;


    private IWICImagingFactory _wicFactory;
    private IWICBitmapDecoder _wicBitmapDecoder;
    private IWICBitmapFrameDecode _wicBitmapDecoderFrame;
    private IWICFormatConverter _wicFormatConverter;
    private IWICBitmapFlipRotator _wicBitmapFlipRotator;

    private ID2D1Bitmap _inventoryBitmap;
    private ID2D1Bitmap _hudBitmap;
    private ID2D1Bitmap _flippedHUDBitmap;

    private const string InventoryBitmapFileName = "resource/widgets.png";
    private const string HUDBitmapFileName = "resource/icons.png";

    internal float ComponentsScaleRate { get; set; } = 2.0f;
    internal int SelectedSlotIndex { get; set; }

    private const float HotBarWidth = 180;
    private const float HotBarHeight = 20;
    private D2D_RECT_F _sourceHotBarInventoryRect;
    private D2D_RECT_F _destinationHotBarInventoryRect;

    private const float SlotWidth = 22;
    private const float SlotHeight = 22;
    private D2D_RECT_F _sourceSlotRect;
    private D2D_RECT_F _destinationSlotRect;

    private const float XPBarWidth = 182;
    private const float XPBarHeight = 5;
    private D2D_RECT_F _sourceXpBarRect;
    private D2D_RECT_F _destinationXpBarRect;

    private const float HeartWidth = 9;
    private const float HeartHeight = 9;
    private D2D_RECT_F _sourceEmptyHeartRect;
    private D2D_RECT_F _sourceFullHeartRect;
    private D2D_RECT_F _destinationHeartRect;

    private const float HungerBarWidth = 9;
    private const float HungerBarHeight = 9;
    private D2D_RECT_F _sourceHungerBarRect;
    private D2D_RECT_F _destinationHungerBarRect;

    private const float CrossHairWidth = 9;
    private const float CrossHairHeight = 9;
    private D2D_RECT_F _sourceCrossHairRect;
    private D2D_RECT_F _destinationCrossHairRect;


    internal void D2D_STEP01_CreateD3D11Device(ID3D12Device4 device, ID3D12CommandQueue commandQueue) {
#if DEBUG
        _d3d11CreateDeviceFlag |= (uint)D3D11_CREATE_DEVICE_DEBUG;
#endif
        _d3d11CreateDeviceFlag |= (uint)D3D11_CREATE_DEVICE_BGRA_SUPPORT;

        D3D11On12CreateDevice(
            device,
            _d3d11CreateDeviceFlag,
            [],
            [commandQueue],
            0,
            out var tempD3D11Device,
            out var tempD3D11DeviceContext).ThrowOnFailure();

        _d3d11Device = (ID3D11Device5)tempD3D11Device;
        _d3d11DeviceContext = (ID3D11DeviceContext4)tempD3D11DeviceContext;

        _d3d11On12Device = (ID3D11On12Device1)_d3d11Device;
    }

    internal void D2D_STEP02_CreateD2DDevice() {
#if DEBUG
        _d2dCreateFactoryOptions.debugLevel = D2D1_DEBUG_LEVEL_INFORMATION;
#endif

        D2D1CreateFactory(
            D2D1_FACTORY_TYPE_SINGLE_THREADED,
            _d2dCreateFactoryOptions,
            out _d2dFactory).ThrowOnFailure();

        var tempDXGIDevice = (IDXGIDevice)_d3d11Device;

        _d2dFactory.CreateDevice(tempDXGIDevice, out _d2dDevice);
        _d2dDevice.CreateDeviceContext(_d2dCreateDeviceContextOptions, out _d2dDeviceContext);
    }

    internal void D2D_STEP03_CreateD2DRenderTarget(HWND mainWindowHwnd, ComPtr<ID3D12Resource>[] d3d12RenderTarget) {


        _dpi = GetDpiForWindow(mainWindowHwnd);

        var bitmapProperties = new D2D1_BITMAP_PROPERTIES1_unmanaged() {
            bitmapOptions = D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
            pixelFormat = new() {
                format = DXGI_FORMAT_UNKNOWN,
                alphaMode = D2D1_ALPHA_MODE_PREMULTIPLIED
            },
            dpiX = _dpi,
            dpiY = _dpi,
            colorContext = null
        };

        _d3d11WrappedRenderTarget = new ID3D11Resource[DX12Engine.FrameCount];
        _d2dRenderTarget = new ID2D1Bitmap1[DX12Engine.FrameCount];
        for (uint i = 0; i < DX12Engine.FrameCount; i++) {
            var resourceFlags = new D3D11_RESOURCE_FLAGS() {
                BindFlags = (uint)D3D11_BIND_RENDER_TARGET,
            };

            _d3d11On12Device.CreateWrappedResource(
                d3d12RenderTarget[i].Managed,
                resourceFlags,
                D3D12_RESOURCE_STATE_RENDER_TARGET,
                D3D12_RESOURCE_STATE_PRESENT,
                out _d3d11WrappedRenderTarget[i]);

            var tempDXGISurface = (IDXGISurface)_d3d11WrappedRenderTarget[i];

            _d2dDeviceContext.CreateBitmapFromDxgiSurface(tempDXGISurface, bitmapProperties, out _d2dRenderTarget[i]);
        }
    }

    internal bool D2D_STEP04_LoadAtlasIntoD2DBitmaps() {
        CoCreateInstance(
            CLSID_WICImagingFactory2,
            null,
            CLSCTX.CLSCTX_INPROC_SERVER,
            out _wicFactory).ThrowOnFailure();

        try {
            _wicBitmapDecoder = _wicFactory.CreateDecoderFromFilename(
                InventoryBitmapFileName,
                null,
                GENERIC_ACCESS_RIGHTS.GENERIC_READ,
                WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
        } catch (Exception ex) {
            MessageBox(default, ex.Message, "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            return false;
        }

        _wicBitmapDecoder.GetFrame(0, out _wicBitmapDecoderFrame);

        _wicFactory.CreateFormatConverter(out _wicFormatConverter);

        _wicFormatConverter.Initialize(
            _wicBitmapDecoderFrame,
            GUID_WICPixelFormat32bppPBGRA,
            WICBitmapDitherType.WICBitmapDitherTypeNone,
            null,
            0.0,
            WICBitmapPaletteType.WICBitmapPaletteTypeCustom);

        _d2dDeviceContext.CreateBitmapFromWicBitmap(_wicFormatConverter, default(D2D1_BITMAP_PROPERTIES?), out _inventoryBitmap);


        try {
            _wicBitmapDecoder = _wicFactory.CreateDecoderFromFilename(
                HUDBitmapFileName,
                null,
                GENERIC_ACCESS_RIGHTS.GENERIC_READ,
                WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
        } catch (Exception ex) {
            MessageBox(default, ex.Message, "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            return false;
        }

        _wicBitmapDecoder.GetFrame(0, out _wicBitmapDecoderFrame);

        _wicFactory.CreateFormatConverter(out _wicFormatConverter);

        _wicFormatConverter.Initialize(
            _wicBitmapDecoderFrame,
            GUID_WICPixelFormat32bppPBGRA,
            WICBitmapDitherType.WICBitmapDitherTypeNone,
            null,
            0.0,
            WICBitmapPaletteType.WICBitmapPaletteTypeCustom);

        _d2dDeviceContext.CreateBitmapFromWicBitmap(_wicFormatConverter, default(D2D1_BITMAP_PROPERTIES?), out _hudBitmap);


        _wicFactory.CreateBitmapFlipRotator(out _wicBitmapFlipRotator);

        _wicBitmapFlipRotator.Initialize(
            _wicFormatConverter,
            WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal);

        _d2dDeviceContext.CreateBitmapFromWicBitmap(_wicBitmapFlipRotator, default(D2D1_BITMAP_PROPERTIES?), out _flippedHUDBitmap);

        return true;
    }

    internal void D2DUIRender(uint currentFrameIndex, uint windowWidth, uint windowHeight) {

        _d3d11On12Device.AcquireWrappedResources([_d3d11WrappedRenderTarget[currentFrameIndex]]);
        _d2dDeviceContext.SetTarget(_d2dRenderTarget[currentFrameIndex]);
        _d2dDeviceContext.BeginDraw();

        _sourceHotBarInventoryRect = new() { left = 1, top = 1, right = 1 + HotBarWidth, bottom = 1 + HotBarHeight };
        _destinationHotBarInventoryRect = new() {
            left = (windowWidth - HotBarWidth * ComponentsScaleRate) / 2,
            top = windowHeight - HotBarHeight * ComponentsScaleRate,
            right = (windowWidth - HotBarWidth * ComponentsScaleRate) / 2 + HotBarWidth * ComponentsScaleRate,
            bottom = windowHeight
        };
        _d2dDeviceContext.DrawBitmap(_inventoryBitmap, _destinationHotBarInventoryRect, 1, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR, _sourceHotBarInventoryRect);

        _sourceSlotRect = new() { left = 1, top = 23, right = 1 + SlotWidth, bottom = 23 + SlotHeight };
        _destinationSlotRect = new() {
            left = _destinationHotBarInventoryRect.left - 1 * ComponentsScaleRate + SelectedSlotIndex * 20 * ComponentsScaleRate,
            top = _destinationHotBarInventoryRect.top - 1 * ComponentsScaleRate,
            right = _destinationHotBarInventoryRect.left - 1 * ComponentsScaleRate + SelectedSlotIndex * 20 * ComponentsScaleRate + SlotWidth * ComponentsScaleRate,
            bottom = _destinationHotBarInventoryRect.top - 1 * ComponentsScaleRate + SlotHeight * ComponentsScaleRate
        };
        _d2dDeviceContext.DrawBitmap(_inventoryBitmap, _destinationSlotRect, 1, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR, _sourceSlotRect);

        _sourceXpBarRect = new() { left = 0, top = 64, right = XPBarWidth, bottom = 64 + XPBarHeight };
        _destinationXpBarRect = new() {
            left = _destinationHotBarInventoryRect.left,
            top = _destinationHotBarInventoryRect.top - 7 * ComponentsScaleRate,
            right = _destinationHotBarInventoryRect.right,
            bottom = _destinationHotBarInventoryRect.top - 7 * ComponentsScaleRate + XPBarHeight * ComponentsScaleRate
        };
        _d2dDeviceContext.DrawBitmap(_hudBitmap, _destinationXpBarRect, 1, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR, _sourceXpBarRect);

        _sourceEmptyHeartRect = new() { left = 16, top = 0, right = 16 + HeartWidth, bottom = HeartHeight };
        _sourceFullHeartRect = new() { left = 52, top = 0, right = 52 + HeartWidth, bottom = HeartHeight };
        _destinationHeartRect = new() {
            left = _destinationXpBarRect.left,
            top = _destinationXpBarRect.top - 10 * ComponentsScaleRate,
            right = _destinationXpBarRect.left + HeartWidth * ComponentsScaleRate,
            bottom = _destinationXpBarRect.top - 10 * ComponentsScaleRate + HeartHeight * ComponentsScaleRate
        };

        for (uint i = 0; i < 10; i++) {
            _d2dDeviceContext.DrawBitmap(_hudBitmap, _destinationHeartRect, 1, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR, _sourceEmptyHeartRect);
            _d2dDeviceContext.DrawBitmap(_hudBitmap, _destinationHeartRect, 1, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR, _sourceFullHeartRect);

            _destinationHeartRect.left += (HeartWidth - 1) * ComponentsScaleRate;
            _destinationHeartRect.right += (HeartWidth - 1) * ComponentsScaleRate;
        }

        _sourceHungerBarRect = new() { left = 256 - 16 - HungerBarWidth, top = 36, right = 256 - 16, bottom = 36 + HungerBarHeight };
        _destinationHungerBarRect = new() {
            left = _destinationXpBarRect.right - HungerBarWidth * ComponentsScaleRate,
            top = _destinationXpBarRect.top - 10 * ComponentsScaleRate,
            right = _destinationXpBarRect.right,
            bottom = _destinationXpBarRect.top - 10 * ComponentsScaleRate + HungerBarHeight * ComponentsScaleRate
        };

        for (uint i = 0; i < 10; i++) {
            _d2dDeviceContext.DrawBitmap(_flippedHUDBitmap, _destinationHungerBarRect, 1, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR, _sourceHungerBarRect);

            _destinationHungerBarRect.left -= (HungerBarWidth - 1) * ComponentsScaleRate;
            _destinationHungerBarRect.right -= (HungerBarWidth - 1) * ComponentsScaleRate;
        }

        _sourceCrossHairRect = new() { left = 3, top = 3, right = 3 + CrossHairWidth, bottom = 3 + CrossHairHeight };
        _destinationCrossHairRect = new() {
            left = (float)(windowWidth / 2.0 - CrossHairWidth * ComponentsScaleRate / 2.0),
            top = (float)(windowHeight / 2.0 - CrossHairHeight * ComponentsScaleRate / 2.0),
            right = (float)(windowWidth / 2.0 - CrossHairWidth * ComponentsScaleRate / 2.0 + CrossHairWidth * ComponentsScaleRate),
            bottom = (float)(windowHeight / 2.0 - CrossHairHeight * ComponentsScaleRate / 2.0 + CrossHairHeight * ComponentsScaleRate)
        };
        _d2dDeviceContext.DrawBitmap(_hudBitmap, _destinationCrossHairRect, 1, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR, _sourceCrossHairRect);

        _d2dDeviceContext.EndDraw(out _, out _).ThrowOnFailure();
        _d2dDeviceContext.SetTarget(null);

        _d3d11On12Device.ReleaseWrappedResources([_d3d11WrappedRenderTarget[currentFrameIndex]]);
        _d3d11DeviceContext.Flush();
    }

}

internal sealed class DX12Engine {

    internal const int FrameCount = 3;
    private static readonly float[] SkyBlue = [0.529411793f, 0.807843208f, 0.921568692f, 1f];


    // DX12 支持的所有功能版本，你的显卡最低需要支持 11
    private static readonly D3D_FEATURE_LEVEL[] DX12SupportLevels = [
        D3D_FEATURE_LEVEL_12_2,        // 12.2
        D3D_FEATURE_LEVEL_12_1,        // 12.1
        D3D_FEATURE_LEVEL_12_0,        // 12
        D3D_FEATURE_LEVEL_11_1,        // 11.1
        D3D_FEATURE_LEVEL_11_0         // 11
    ];

    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;

    private HWND _hwnd;

    private ID3D12Debug _d3d12DebugDevice;
    private DXGI_CREATE_FACTORY_FLAGS _dxgiCreateFactoryFlag;

    private IDXGIFactory5 _dxgiFactory;
    private IDXGIAdapter1 _dxgiAdapter;
    private ID3D12Device4 _d3d12Device;

    private ID3D12CommandQueue _commandQueue;
    private ID3D12CommandAllocator _commandAllocator;
    private ID3D12GraphicsCommandList _commandList;

    private ID3D12DescriptorHeap _rtvHeap;
    private IDXGISwapChain3 _dxgiSwapChain;
    private ComPtr<ID3D12Resource>[] _renderTargets;
    private D3D12_CPU_DESCRIPTOR_HANDLE _rtvHandle;
    private uint _rtvDescriptorSize;
    private uint _frameIndex;

    private ID3D12Fence _fence;
    private ulong _fenceValue;
    private SafeHandle _renderEvent;
    private D3D12_RESOURCE_BARRIER _beginBarrier;
    // private D3D12_RESOURCE_BARRIER _endBarrier;


    private readonly D3D12_VIEWPORT _viewPort = new() {
        TopLeftX = 0,
        TopLeftY = 0,
        Width = WindowWidth,
        Height = WindowHeight,
        MinDepth = D3D12_MIN_DEPTH,
        MaxDepth = D3D12_MAX_DEPTH
    };
    private readonly RECT _scissorRect = new() {
        left = 0,
        top = 0,
        right = WindowWidth,
        bottom = WindowHeight
    };

    private readonly D2DEngine _d2dEngine = new();


    private unsafe void STEP01_InitWindow(SafeHandle hInstance) {
        const string className = "DX12 Game";
        var pClassName = stackalloc char[className.Length + 1]; // +1 for null terminator

        className.AsSpan().CopyTo(new Span<char>(pClassName, className.Length));
        pClassName[className.Length] = '\0';

        WNDCLASSW wc = new() {
            hInstance = new(hInstance.DangerousGetHandle()),
            lpfnWndProc = &CallBackWrapper.CallBackFunc,
            lpszClassName = pClassName,
        };

        CallBackWrapper.BrokerFunc = CallBackFunc;

        RegisterClass(wc);

        _hwnd = CreateWindowEx(
            0,
            className,
            "Minecraft (当前放大比例：2.0 倍 UI)",
            WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_OVERLAPPED,
            10,
            10,
            WindowWidth,
            WindowHeight,
            HWND.Null,
            null,
            hInstance);

        ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
    }

    [Conditional("DEBUG")]
    private void STEP02_CreateDebugDevice() {
        // [STAThread] attribute on Main method handles this
        //CoInitialize();

        D3D12GetDebugInterface(out _d3d12DebugDevice).ThrowOnFailure();
        _d3d12DebugDevice.EnableDebugLayer();

        _dxgiCreateFactoryFlag = DXGI_CREATE_FACTORY_DEBUG;
    }

    private bool STEP03_CreateDevice() {
        CreateDXGIFactory2(_dxgiCreateFactoryFlag, out _dxgiFactory).ThrowOnFailure();

        for (uint i = 0; _dxgiFactory.EnumAdapters1(i, out _dxgiAdapter) != HRESULT.DXGI_ERROR_NOT_FOUND; i++) {
            // 找到显卡，就创建 D3D12 设备，从高到低遍历所有功能版本，创建成功就跳出
            foreach (var level in DX12SupportLevels) {
                if (!D3D12CreateDevice(_dxgiAdapter, level, out _d3d12Device).Succeeded)
                    continue;
                var adapter = _dxgiAdapter.GetDesc();
                Debug.WriteLine($"当前使用的显卡：{adapter.Description}");
                return true;
            }
        }

        MessageBox(default, "找不到任何能支持 DX12 的显卡，请升级电脑上的硬件！", "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        return false;
    }

    private void STEP04_CreateCommandComponents() {
        const D3D12_COMMAND_LIST_TYPE type = D3D12_COMMAND_LIST_TYPE_DIRECT;

        var queueDesc = new D3D12_COMMAND_QUEUE_DESC() {
            Type = type,
        };

        _d3d12Device.CreateCommandQueue(queueDesc, out _commandQueue);

        _d3d12Device.CreateCommandAllocator(type, out _commandAllocator);

        _d3d12Device.CreateCommandList(
            0,
            type,
            _commandAllocator,
            null,
            out _commandList);

        _commandList.Close();
    }

    private void STEP05_CreateRenderTarget() {
        const D3D12_DESCRIPTOR_HEAP_TYPE type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;

        var rtvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = FrameCount,
            Type = type,
        };
        _d3d12Device.CreateDescriptorHeap(rtvHeapDesc, out _rtvHeap);

        var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1() {
            BufferCount = FrameCount,
            Width = WindowWidth,
            Height = WindowHeight,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
            BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,
            SampleDesc = new() {
                Count = 1,
            },
        };

        _dxgiFactory.CreateSwapChainForHwnd(
            _commandQueue,
            _hwnd,
            swapChainDesc,
            null,
            null,
            out var tempSwapChain);

        // CreateSwapChainForHwnd 不能直接用于创建高版本接口
        _dxgiSwapChain = (IDXGISwapChain3)tempSwapChain;

        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _rtvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(type);

        _renderTargets = new ComPtr<ID3D12Resource>[FrameCount];
        for (uint i = 0; i < FrameCount; i++) {
            _dxgiSwapChain.GetBuffer<ID3D12Resource>(i, out var resource);
            _renderTargets[i] = new(resource);

            _d3d12Device.CreateRenderTargetView(_renderTargets[i].Managed, default(D3D12_RENDER_TARGET_VIEW_DESC?), _rtvHandle);

            _rtvHandle.ptr += _rtvDescriptorSize;
        }
    }

    private void STEP06_CreateFenceAndBarrier() {
        _renderEvent = CreateEvent(null, false, true);

        _d3d12Device.CreateFence(0, D3D12_FENCE_FLAG_NONE, out _fence);

        // 设置资源屏障
        // _beginBarrier 起始屏障：Present 呈现状态 -> Render Target 渲染目标状态
        _beginBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _beginBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATE_PRESENT;
        _beginBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATE_RENDER_TARGET;

        // _endBarrier 终止屏障：Render Target 渲染目标状态 -> Present 呈现状态
        //_endBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        //_endBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET;
        //_endBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATE_PRESENT;
    }

    private void STEP07_InitializeD2DEngine() {
        _d2dEngine.D2D_STEP01_CreateD3D11Device(_d3d12Device, _commandQueue);
        _d2dEngine.D2D_STEP02_CreateD2DDevice();
        _d2dEngine.D2D_STEP03_CreateD2DRenderTarget(_hwnd, _renderTargets);

        _d2dEngine.D2D_STEP04_LoadAtlasIntoD2DBitmaps();
    }


    private unsafe void Render() {

        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _frameIndex = _dxgiSwapChain.GetCurrentBackBufferIndex();
        _rtvHandle.ptr += _frameIndex * _rtvDescriptorSize;


        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator);

        _commandList.RSSetViewports([_viewPort]);
        _commandList.RSSetScissorRects([_scissorRect]);

        _beginBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_beginBarrier]);

        _commandList.OMSetRenderTargets(1, _rtvHandle, false);

        _commandList.ClearRenderTargetView(_rtvHandle, SkyBlue);

        //_endBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        //_commandList.ResourceBarrier([_endBarrier]);

        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);

        _d2dEngine.D2DUIRender(_frameIndex, WindowWidth, WindowHeight);

        _dxgiSwapChain.Present(1, 0);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void STEP08_RenderLoop() {
        var exit = false;
        while (!exit) {
            var activeEvent = MsgWaitForMultipleObjects(
                [new(_renderEvent.DangerousGetHandle())],
                false,
                INFINITE,
                QUEUE_STATUS_FLAGS.QS_ALLINPUT);

            switch (activeEvent - WAIT_EVENT.WAIT_OBJECT_0) {
                case 0: // ActiveEvent 是 0，说明渲染事件已经完成了，进行下一次渲染
                    Render();
                    break;
                case 1: // ActiveEvent 是 1，说明渲染事件未完成，CPU 主线程同时处理窗口消息，防止界面假死
                    while (PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE)) {
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

    private LRESULT CallBackFunc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
        switch (msg) {
            case WM_DESTROY:
                PostQuitMessage(0);
                break;

            case WM_CHAR:
                var ch = (char)(nuint)wParam;
                switch (ch) {
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        _d2dEngine.SelectedSlotIndex = ch - '1';
                        break;

                    case 'Q':
                    case 'q':
                        _d2dEngine.ComponentsScaleRate = Math.Min(3.0f, _d2dEngine.ComponentsScaleRate + 0.1f);
                        SetWindowText(_hwnd, $"Minecraft (当前放大比例：{_d2dEngine.ComponentsScaleRate:F1} 倍 UI)");
                        break;

                    case 'E':
                    case 'e':
                        _d2dEngine.ComponentsScaleRate = Math.Max(1.0f, _d2dEngine.ComponentsScaleRate - 0.1f);
                        SetWindowText(_hwnd, $"Minecraft (当前放大比例：{_d2dEngine.ComponentsScaleRate:F1} 倍 UI)");
                        break;
                }
                break;

            case WM_MOUSEWHEEL:
                var delta = (short)(((nuint)wParam >> 16) & 0xFFFF);
                if (delta > 0)
                    _d2dEngine.SelectedSlotIndex = (_d2dEngine.SelectedSlotIndex - 1 + 9) % 9;
                else if (delta < 0)
                    _d2dEngine.SelectedSlotIndex = (_d2dEngine.SelectedSlotIndex + 1) % 9;
                break;

            default:
                return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        return new(0);
    }

    internal static void Run(SafeHandle hInstance) {
        DX12Engine engine = new();
        engine.STEP01_InitWindow(hInstance);
        engine.STEP02_CreateDebugDevice();
        engine.STEP03_CreateDevice();
        engine.STEP04_CreateCommandComponents();
        engine.STEP05_CreateRenderTarget();
        engine.STEP06_CreateFenceAndBarrier();

        engine.STEP07_InitializeD2DEngine();

        engine.STEP08_RenderLoop();
    }

}

internal static class Program {

    [STAThread]
    private static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }
}
