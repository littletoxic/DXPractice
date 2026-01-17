using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Imaging;
using Windows.Win32.System.Com;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace DXDemo8WBOIT;

internal sealed class DX12Engine {

    private const int FrameCount = 3;
    private static readonly float[] SkyBlue = [0.529411793f, 0.807843208f, 0.921568692f, 1f];


    // DX12 支持的所有功能版本，你的显卡最低需要支持 11
    private static readonly D3D_FEATURE_LEVEL[] DX12SupportLevels = [
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_2,        // 12.2
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_1,        // 12.1
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0,        // 12
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,        // 11.1
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0         // 11
    ];

    private const int WindowWidth = 640;
    private const int WindowHeight = 480;

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
    private D3D12_RESOURCE_BARRIER _endBarrier;

    private ID3D12DescriptorHeap _dsvHeap;
    private D3D12_CPU_DESCRIPTOR_HANDLE _dsvHandle;

    // DSV 资源的格式
    // 深度模板缓冲只支持四种格式:
    // DXGI_FORMAT_D24_UNORM_S8_UINT	(每个像素占用四个字节 32 位，24 位无符号归一化浮点数留作深度值，8 位整数留作模板值)
    // DXGI_FORMAT_D32_FLOAT_S8X24_UINT	(每个像素占用八个字节 64 位，32 位浮点数留作深度值，8 位整数留作模板值，其余 24 位保留不使用)
    // DXGI_FORMAT_D16_UNORM			(每个像素占用两个字节 16 位，16 位无符号归一化浮点数留作深度值，范围 [0,1]，不使用模板)
    // DXGI_FORMAT_D32_FLOAT			(每个像素占用四个字节 32 位，32 位浮点数留作深度值，不使用模板)
    // 这里我们选择最常用的格式 DXGI_FORMAT_D24_UNORM_S8_UINT
    private readonly DXGI_FORMAT _dsvFormat = DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT;
    private ID3D12Resource _depthStencilBuffer;

    private ID3D12DescriptorHeap _srvHeap;

    private readonly ModelManager _modelManager = new();

    private IWICImagingFactory _wicFactory;
    private IWICBitmapDecoder _wicBitmapDecoder;
    private IWICBitmapFrameDecode _wicBitmapDecoderFrame;
    private IWICFormatConverter _wicFormatConverter;
    private IWICBitmapSource _wicBitmapSource;
    private DXGI_FORMAT _textureFormat = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;
    private uint _textureWidth;
    private uint _textureHeight;
    private uint _bitsPerPixel;

    private uint _bytesPerRowSize;
    private uint _textureSize;
    private uint _uploadResourceRowSize;
    private uint _uploadResourceSize;

    private struct CBuffer {
        internal Matrix4x4 MVPMatrix;
    }
    private ID3D12Resource _cbvResource;
    private nint mvpBuffer;

    private ComPtr<ID3D12RootSignature> _rootSignature;

    private static readonly PCSTR Position = CreatePCSTR("POSITION");
    private static readonly PCSTR TexCoord = CreatePCSTR("TEXCOORD");
    private static readonly PCSTR Matrix = CreatePCSTR("MATRIX");

    private readonly D3D12_INPUT_ELEMENT_DESC[] _inputElementDesc = new D3D12_INPUT_ELEMENT_DESC[6];

    private D3D12_GRAPHICS_PIPELINE_STATE_DESC _opaquePSODesc;
    private ID3D12PipelineState _opaquePSO;
    private D3D12_GRAPHICS_PIPELINE_STATE_DESC _transparentPSODesc;
    private ID3D12PipelineState _transparentPSO;
    private D3D12_GRAPHICS_PIPELINE_STATE_DESC _translucencePSODesc;
    private ID3D12PipelineState _translucencePSO;

    // WBOIT 资源
    private ComPtr<ID3D12Resource> _wboitAccumTarget;       // RGBA16F 累积缓冲
    private ComPtr<ID3D12Resource> _wboitRevealTarget;      // R16F 透明度缓冲
    private D3D12_CPU_DESCRIPTOR_HANDLE _wboitAccumRtvHandle;
    private D3D12_CPU_DESCRIPTOR_HANDLE _wboitRevealRtvHandle;

    // WBOIT SRV (用于合成 Pass)
    private ID3D12DescriptorHeap _wboitSRVHeap;
    private D3D12_GPU_DESCRIPTOR_HANDLE _wboitSrvGpuHandle;

    // WBOIT PSO
    private ID3D12PipelineState _wboitAccumPSO;
    private ID3D12PipelineState _wboitCompositePSO;
    private ComPtr<ID3D12RootSignature> _wboitCompositeRootSignature;

    private readonly Camera _firstCamera = new();

    private D3D12_VIEWPORT _viewPort = new() {
        TopLeftX = 0,
        TopLeftY = 0,
        Width = WindowWidth,
        Height = WindowHeight,
        MinDepth = D3D12_MIN_DEPTH,
        MaxDepth = D3D12_MAX_DEPTH
    };
    private RECT _scissorRect = new() {
        left = 0,
        top = 0,
        right = WindowWidth,
        bottom = WindowHeight
    };


    private unsafe void InitWindow(SafeHandle hInstance) {
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
            "DX12 Game Window",
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
    private void CreateDebugDevice() {
        // [STAThread] attribute on Main method handles this
        //CoInitialize();

        D3D12GetDebugInterface(out _d3d12DebugDevice).ThrowOnFailure();
        _d3d12DebugDevice.EnableDebugLayer();

        _dxgiCreateFactoryFlag = DXGI_CREATE_FACTORY_FLAGS.DXGI_CREATE_FACTORY_DEBUG;
    }

    private bool CreateDevice() {
        CreateDXGIFactory2(_dxgiCreateFactoryFlag, out _dxgiFactory).ThrowOnFailure();

        for (uint i = 0; _dxgiFactory.EnumAdapters1(i, out _dxgiAdapter) != HRESULT.DXGI_ERROR_NOT_FOUND; i++) {
            // 找到显卡，就创建 D3D12 设备，从高到低遍历所有功能版本，创建成功就跳出
            foreach (var level in DX12SupportLevels) {
                if (D3D12CreateDevice(_dxgiAdapter, level, out _d3d12Device).Succeeded) {
                    var adap = _dxgiAdapter.GetDesc();
                    Debug.WriteLine(adap.Description.ToString());
                    return true;
                }
            }
        }

        MessageBox(default, "找不到任何能支持 DX12 的显卡，请升级电脑上的硬件！", "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        return false;
    }

    private void CreateCommandComponents() {
        const D3D12_COMMAND_LIST_TYPE type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT;

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

    private void CreateRenderTarget() {
        const D3D12_DESCRIPTOR_HEAP_TYPE type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV;

        var rtvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = FrameCount,
            Type = type,
        };
        _d3d12Device.CreateDescriptorHeap(rtvHeapDesc, out _rtvHeap);

        var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1() {
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

        _dxgiFactory.CreateSwapChainForHwnd(
            _commandQueue,
            _hwnd,
            swapChainDesc,
            null,
            null,
            out var tempSwapChain);

        // CreateSwapChainForHwnd 不能直接用于创建高版本接口
        _dxgiSwapChain = tempSwapChain as IDXGISwapChain3;

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

    private void CreateFenceAndBarrier() {
        _renderEvent = CreateEvent(null, false, true, null);

        _d3d12Device.CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, out _fence);

        // 设置资源屏障
        // _beginBarrier 起始屏障：Present 呈现状态 -> Render Target 渲染目标状态
        _beginBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _beginBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT;
        _beginBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;

        // _endBarrier 终止屏障：Render Target 渲染目标状态 -> Present 呈现状态
        _endBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _endBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;
        _endBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT;
    }

    private void CreateDSVHeap() {
        var dsvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 1,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
        };

        _d3d12Device.CreateDescriptorHeap(dsvHeapDesc, out _dsvHeap);

        _dsvHandle = _dsvHeap.GetCPUDescriptorHandleForHeapStart();
    }

    private void CreateDepthStencilBuffer() {
        var dsvResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Format = _dsvFormat,
            Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Width = WindowWidth,
            Height = WindowHeight,
            MipLevels = 1,
            DepthOrArraySize = 1,
            SampleDesc = new() { Count = 1, Quality = 0 },
            Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL
        };

        var depthStencilBufferClearValue = new D3D12_CLEAR_VALUE() {
            Format = _dsvFormat,
            Anonymous = new D3D12_CLEAR_VALUE._Anonymous_e__Union() {
                DepthStencil = new D3D12_DEPTH_STENCIL_VALUE() {
                    Depth = 1.0f,
                    Stencil = 0,
                }
            }
        };

        _d3d12Device.CreateCommittedResource(
            new D3D12_HEAP_PROPERTIES() {
                Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,
            },
            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
            dsvResourceDesc,
            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_DEPTH_WRITE,
            depthStencilBufferClearValue,
            out _depthStencilBuffer);
    }

    private void CreateDSV() {
        var dsvViewDesc = new D3D12_DEPTH_STENCIL_VIEW_DESC() {
            Format = _dsvFormat,
            ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2D,
            Flags = D3D12_DSV_FLAGS.D3D12_DSV_FLAG_NONE,
        };

        _d3d12Device.CreateDepthStencilView(
            _depthStencilBuffer,
            dsvViewDesc,
            _dsvHandle);
    }

    private void CreateSRVHeap() {
        var srvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = (uint)_modelManager.TextureSRVMap.Count,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
            Flags = D3D12_DESCRIPTOR_HEAP_FLAGS.D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
        };

        _d3d12Device.CreateDescriptorHeap(srvHeapDesc, out _srvHeap);
    }

    private ID3D12DescriptorHeap _wboitRTVHeap;  // WBOIT 专用 RTV 堆

    private unsafe void CreateWBOITResources() {
        // 1. 创建 WBOIT 专用的 RTV 堆 (2 个描述符: 累积 + 透明度)
        var wboitRtvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 2,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
        };
        _d3d12Device.CreateDescriptorHeap(wboitRtvHeapDesc, out _wboitRTVHeap);

        var rtvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(
            D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

        // 2. 创建累积目标资源 (RGBA16F)
        var accumResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Width = WindowWidth,
            Height = WindowHeight,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
            Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET
        };

        var accumClearValue = new D3D12_CLEAR_VALUE() {
            Format = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
            Anonymous = new D3D12_CLEAR_VALUE._Anonymous_e__Union() {
                Color = WBOITAccumClear  // 清除颜色为 (0, 0, 0, 0)
            }
        };

        _d3d12Device.CreateCommittedResource(
            new D3D12_HEAP_PROPERTIES() { Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT },
            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
            accumResourceDesc,
            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET,
            accumClearValue,
            out ID3D12Resource accumResource);
        _wboitAccumTarget = new(accumResource);

        // 3. 创建透明度目标资源 (R16F)
        var revealResourceDesc = accumResourceDesc;
        revealResourceDesc.Format = DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT;

        var revealClearValue = new D3D12_CLEAR_VALUE() {
            Format = DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT,
            Anonymous = new D3D12_CLEAR_VALUE._Anonymous_e__Union() {
                Color = WBOITRevealClear  // reveal 初始值为 1
            }
        };

        _d3d12Device.CreateCommittedResource(
            new D3D12_HEAP_PROPERTIES() { Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT },
            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
            revealResourceDesc,
            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET,
            revealClearValue,
            out ID3D12Resource revealResource);
        _wboitRevealTarget = new(revealResource);

        // 4. 创建 RTV
        _wboitAccumRtvHandle = _wboitRTVHeap.GetCPUDescriptorHandleForHeapStart();
        _d3d12Device.CreateRenderTargetView(_wboitAccumTarget.Managed, default(D3D12_RENDER_TARGET_VIEW_DESC?), _wboitAccumRtvHandle);

        _wboitRevealRtvHandle = _wboitAccumRtvHandle;
        _wboitRevealRtvHandle.ptr += rtvDescriptorSize;
        _d3d12Device.CreateRenderTargetView(_wboitRevealTarget.Managed, default(D3D12_RENDER_TARGET_VIEW_DESC?), _wboitRevealRtvHandle);

        // 5. 创建 WBOIT SRV 堆 (2 个描述符，用于合成 Pass 读取)
        var wboitSrvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 2,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
            Flags = D3D12_DESCRIPTOR_HEAP_FLAGS.D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
        };
        _d3d12Device.CreateDescriptorHeap(wboitSrvHeapDesc, out _wboitSRVHeap);

        var srvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(
            D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

        // 6. 创建 SRV
        var accumSrvHandle = _wboitSRVHeap.GetCPUDescriptorHandleForHeapStart();
        var accumSrvDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC() {
            Format = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
            ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE2D,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Anonymous = new D3D12_SHADER_RESOURCE_VIEW_DESC._Anonymous_e__Union() {
                Texture2D = new D3D12_TEX2D_SRV() {
                    MipLevels = 1,
                    MostDetailedMip = 0,
                }
            }
        };
        _d3d12Device.CreateShaderResourceView(_wboitAccumTarget.Managed, accumSrvDesc, accumSrvHandle);

        var revealSrvHandle = accumSrvHandle;
        revealSrvHandle.ptr += srvDescriptorSize;
        var revealSrvDesc = accumSrvDesc;
        revealSrvDesc.Format = DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT;
        _d3d12Device.CreateShaderResourceView(_wboitRevealTarget.Managed, revealSrvDesc, revealSrvHandle);

        // 保存 GPU 句柄供渲染时使用
        _wboitSrvGpuHandle = _wboitSRVHeap.GetGPUDescriptorHandleForHeapStart();

        // 初始化 WBOIT 资源屏障 (复用模板，渲染时设置 pResource)
        _wboitBarrierToSRV.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _wboitBarrierToSRV.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;
        _wboitBarrierToSRV.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;

        _wboitBarrierToRTV.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _wboitBarrierToRTV.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
        _wboitBarrierToRTV.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;
    }

    private void StartCommandRecord() {
        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);
    }

    private bool LoadTextureFromFile(string textureFilename) {
        if (_wicFactory == null) {
            CoCreateInstance(
                CLSID_WICImagingFactory2,
                null,
                CLSCTX.CLSCTX_SERVER,
                out _wicFactory).ThrowOnFailure();
        }

        try {
            _wicBitmapDecoder = _wicFactory.CreateDecoderFromFilename(
               textureFilename,
               null,
               GENERIC_ACCESS_RIGHTS.GENERIC_READ,
               WICDecodeOptions.WICDecodeMetadataCacheOnLoad);
        } catch (Exception ex) {
            MessageBox(default, ex.Message, "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            return false;
        }

        _wicBitmapDecoder.GetFrame(0, out _wicBitmapDecoderFrame);

        _wicBitmapDecoderFrame.GetPixelFormat(out var pixelFormat);

        if (DX12TextureHelper.GetTargetPixelFormat(pixelFormat, out var targetFormat)) {
            _textureFormat = DX12TextureHelper.GetDXGIFormatFromPixelFormat(targetFormat); // 获取 DX12 支持的格式
        } else {
            MessageBox(default, "此纹理不受支持!", "提示", MESSAGEBOX_STYLE.MB_OK);
            return false;
        }

        _wicFactory.CreateFormatConverter(out _wicFormatConverter);

        _wicFormatConverter.Initialize(
            _wicBitmapDecoderFrame,
            targetFormat,
            WICBitmapDitherType.WICBitmapDitherTypeNone,
            null,
            0.0,
            WICBitmapPaletteType.WICBitmapPaletteTypeCustom);

        _wicBitmapSource = _wicFormatConverter;

        _wicBitmapSource.GetSize(out _textureWidth, out _textureHeight);

        _wicFactory.CreateComponentInfo(targetFormat, out var componentInfo);
        var pixelInfo = componentInfo as IWICPixelFormatInfo;
        pixelInfo.GetBitsPerPixel(out _bitsPerPixel);

        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CeilToMultiple(uint value, uint multiple) {
        // assumes `multiple` is power-of-two (true for D3D12_TEXTURE_DATA_PITCH_ALIGNMENT)
        return (value + multiple - 1) & ~(multiple - 1);
    }

    private void CreateUploadAndDefaultResource(TextureMapInfo info) {
        _bytesPerRowSize = (_textureWidth * _bitsPerPixel + 7) / 8;

        _textureSize = _bytesPerRowSize * _textureHeight;

        _uploadResourceRowSize = CeilToMultiple(_bytesPerRowSize, D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);

        _uploadResourceSize = _uploadResourceRowSize * (_textureHeight - 1) + _bytesPerRowSize;


        var uploadResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = _uploadResourceSize,
            Height = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource<ID3D12Resource>(
            new() {
                Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD,
            },
            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out var uploadTextureResource);
        info.UploadHeapTextureResource = new(uploadTextureResource);


        var defaultResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Width = _textureWidth,
            Height = _textureHeight,
            Format = _textureFormat,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource<ID3D12Resource>(
            new() {
                Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,
            },
            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
            defaultResourceDesc,
            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
            null,
            out var defaultTextureResource);
        info.DefaultHeapTextureResource = new(defaultTextureResource);
    }

    private unsafe void CopyTextureDataToDefaultResource(TextureMapInfo info) {
        var textureData = ArrayPool<byte>.Shared.Rent((int)_textureSize);

        _wicBitmapSource.CopyPixels(default, _bytesPerRowSize, textureData);

        info.UploadHeapTextureResource.Managed.Map(0, null, out var transferPointer);

        int rowBytes = (int)_bytesPerRowSize;
        ReadOnlySpan<byte> allSrcData = textureData;
        byte* dstBasePtr = (byte*)transferPointer;
        for (int i = 0; i < _textureHeight; i++) {
            var srcRow = allSrcData.Slice(i * rowBytes, rowBytes);
            var dstRow = new Span<byte>(dstBasePtr + i * _uploadResourceRowSize, rowBytes);
            srcRow.CopyTo(dstRow);
        }

        info.UploadHeapTextureResource.Managed.Unmap(0, default(D3D12_RANGE?));

        ArrayPool<byte>.Shared.Return(textureData);

        var placedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT();
        var defaultResourceDesc = info.DefaultHeapTextureResource.Managed.GetDesc();

        _d3d12Device.GetCopyableFootprints(
            defaultResourceDesc,
            0,
            0,
            new(ref placedFootprint));

        var dstLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
            Anonymous = new() { SubresourceIndex = 0 },
            pResource = (ID3D12Resource_unmanaged*)info.DefaultHeapTextureResource.Ptr,
        };

        var srcLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
            Anonymous = new() { PlacedFootprint = placedFootprint },
            pResource = (ID3D12Resource_unmanaged*)info.UploadHeapTextureResource.Ptr,
        };

        _commandList.CopyTextureRegion(dstLocation, 0, 0, 0, srcLocation, default(D3D12_BOX?));
    }

    private void CreateSRV(
        TextureMapInfo info,
        D3D12_CPU_DESCRIPTOR_HANDLE cpuHandle,
        D3D12_GPU_DESCRIPTOR_HANDLE gpuHandle) {

        var srvDescriptorDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC() {
            ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE2D,
            Format = _textureFormat,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Anonymous = new() { Texture2D = new() { MipLevels = 1 } },
        };

        _d3d12Device.CreateShaderResourceView(info.DefaultHeapTextureResource.Managed, srvDescriptorDesc, cpuHandle);

        info.CPUHandle = cpuHandle;
        info.GPUHandle = gpuHandle;
    }

    private void StartCommandExecute() {
        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);
        _dxgiSwapChain.Present(1, 0);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void CreateModelTextureResource() {
        CreateSRVHeap();

        var currentCPUHandle = _srvHeap.GetCPUDescriptorHandleForHeapStart();
        var currentGPUHandle = _srvHeap.GetGPUDescriptorHandleForHeapStart();
        var srvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

        StartCommandRecord();

        foreach (var textureInfo in _modelManager.TextureSRVMap.Values) {
            LoadTextureFromFile(textureInfo.TextureFilePath);
            CreateUploadAndDefaultResource(textureInfo);
            CopyTextureDataToDefaultResource(textureInfo);
            CreateSRV(textureInfo, currentCPUHandle, currentGPUHandle);

            currentCPUHandle.ptr += srvDescriptorSize;
            currentGPUHandle.ptr += srvDescriptorSize;
        }

        StartCommandExecute();
    }

    private void CreateModelVertexAndIndexResource() {
        _modelManager.CreateBlock();
        _modelManager.CreateModelResource(_d3d12Device);
    }

    private unsafe void CreateCBVResource() {
        uint cBufferSize = CeilToMultiple((uint)Unsafe.SizeOf<CBuffer>(), D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT);

        var cbvResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = cBufferSize,
            Height = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            new() {
                Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD,
            },
            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
            cbvResourceDesc,
            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _cbvResource);

        _cbvResource.Map(0, null, out var cbvPointer);
        mvpBuffer = (nint)cbvPointer;
    }

    private unsafe void CreateRootSignature() {
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER[2];

        // 把更新频率高的根参数放前面，低的放后面，可以优化性能 (微软官方文档建议)
        // 因为 DirectX API 能对根签名进行 Version Control 版本控制，在根签名越前面的根参数，访问速度更快

        var cbvRootDescriptorDesc = new D3D12_ROOT_DESCRIPTOR() {
            ShaderRegister = 0,
            RegisterSpace = 0,
        };

        rootParameters[0] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_ALL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_CBV,
            Anonymous = new() { Descriptor = cbvRootDescriptorDesc },
        };


        var srvDescriptorDesc = new D3D12_DESCRIPTOR_RANGE() {
            RangeType = D3D12_DESCRIPTOR_RANGE_TYPE.D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
            NumDescriptors = 1,
            BaseShaderRegister = 0,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0,
        };

        var rootDescriptorTableDesc = new D3D12_ROOT_DESCRIPTOR_TABLE() {
            NumDescriptorRanges = 1,
            pDescriptorRanges = &srvDescriptorDesc,
        };

        rootParameters[1] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_PIXEL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            Anonymous = new() { DescriptorTable = rootDescriptorTableDesc },
        };


        var staticSamplerDesc = new D3D12_STATIC_SAMPLER_DESC() {
            ShaderRegister = 0,
            RegisterSpace = 0,
            ShaderVisibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_PIXEL,
            Filter = D3D12_FILTER.D3D12_FILTER_COMPARISON_MIN_MAG_MIP_POINT,
            AddressU = D3D12_TEXTURE_ADDRESS_MODE.D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            AddressV = D3D12_TEXTURE_ADDRESS_MODE.D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            AddressW = D3D12_TEXTURE_ADDRESS_MODE.D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            MinLOD = 0.0f,
            MaxLOD = D3D12_FLOAT32_MAX,
            MipLODBias = 0,
            MaxAnisotropy = 1,
            ComparisonFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_NEVER,
        };


        var rootSignatureDesc = new D3D12_ROOT_SIGNATURE_DESC() {
            NumParameters = 2,
            pParameters = rootParameters,
            NumStaticSamplers = 1,
            pStaticSamplers = &staticSamplerDesc,
            // 根签名标志，可以设置渲染管线不同阶段下的输入参数状态。注意这里！我们要从 IA 阶段输入顶点数据，所以要通过根签名，设置渲染管线允许从 IA 阶段读入数据
            Flags = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT
        };

        D3D12SerializeRootSignature(
            rootSignatureDesc,
            D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1_0,
            out var signatureBlob,
            out var errorBlob).ThrowOnFailure();

        if (errorBlob != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlob.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        _d3d12Device.CreateRootSignature<ID3D12RootSignature>(
            0,
            signatureBlob.GetBufferPointer(),
            signatureBlob.GetBufferSize(),
            out var rootSignature);
        _rootSignature = new(rootSignature);
    }

    private unsafe void CreateWBOITCompositeRootSignature() {
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER[1];

        // 描述符表包含 2 个 SRV (累积纹理 t0 + 透明度纹理 t1)
        var srvDescriptorRange = new D3D12_DESCRIPTOR_RANGE() {
            RangeType = D3D12_DESCRIPTOR_RANGE_TYPE.D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
            NumDescriptors = 2,
            BaseShaderRegister = 0,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0,
        };

        var rootDescriptorTableDesc = new D3D12_ROOT_DESCRIPTOR_TABLE() {
            NumDescriptorRanges = 1,
            pDescriptorRanges = &srvDescriptorRange,
        };

        rootParameters[0] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_PIXEL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            Anonymous = new() { DescriptorTable = rootDescriptorTableDesc },
        };

        // 合成 Pass 不需要静态采样器，因为使用 Load() 而非 Sample()
        var rootSignatureDesc = new D3D12_ROOT_SIGNATURE_DESC() {
            NumParameters = 1,
            pParameters = rootParameters,
            NumStaticSamplers = 0,
            pStaticSamplers = null,
            // 合成 Pass 使用 SV_VertexID 生成顶点，不需要 IA 输入
            Flags = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_NONE
        };

        D3D12SerializeRootSignature(
            rootSignatureDesc,
            D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1_0,
            out var signatureBlob,
            out var errorBlob).ThrowOnFailure();

        if (errorBlob != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlob.GetBufferPointer());
            Debug.WriteLine($"WBOIT Composite RootSignature Error: {errorMessage}");
        }

        _d3d12Device.CreateRootSignature<ID3D12RootSignature>(
            0,
            signatureBlob.GetBufferPointer(),
            signatureBlob.GetBufferSize(),
            out var rootSignature);
        _wboitCompositeRootSignature = new(rootSignature);
    }

    // 这里的代码和原教程不同，只将 D3D12_INPUT_ELEMENT_DESC 作为成员变量保存
    private void CreateInputLayout() {
        _inputElementDesc[0] = new() {
            SemanticName = Position,
            SemanticIndex = 0,
            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT,
            InputSlot = 0,
            AlignedByteOffset = 0,
            // 输入流类型，一种是我们现在用的 D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA 逐顶点输入流,还有一种叫逐实例输入流，后面再学
            InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        _inputElementDesc[1] = new() {
            SemanticName = TexCoord,
            SemanticIndex = 0,
            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
            InputSlot = 0,
            // 在输入槽中的偏移，因为 position 与 color 在同一输入槽(0号输入槽)
            // position 是 float4，有 4 个 float ，每个 float 占 4 个字节，所以要偏移 4*4=16 个字节，这样才能确定 color 参数的位置，不然装配的时候会覆盖原先 position 的数据
            AlignedByteOffset = 16,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        for (uint i = 0; i < 4; i++) {
            _inputElementDesc[2 + i] = new() {
                SemanticName = Matrix,
                SemanticIndex = i,
                Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT,
                InputSlot = 1,
                AlignedByteOffset = (uint)(i * Unsafe.SizeOf<Vector4>()),
                InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                InstanceDataStepRate = 0,
            };
        }
    }

    private unsafe void CreateOpaquePSO() {

        D3DCompileFromFile(
            "OpaqueShader.hlsl",
            null,
            null,
            "VSMain",
            "vs_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var vertexShaderBlob,
            out var errorBlobVS).ThrowOnFailure();

        if (errorBlobVS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobVS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        D3DCompileFromFile(
            "OpaqueShader.hlsl",
            null,
            null,
            "PSMain",
            "ps_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var pixelShaderBlob,
            out var errorBlobPS).ThrowOnFailure();

        if (errorBlobPS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobPS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        _opaquePSODesc.VS.pShaderBytecode = vertexShaderBlob.GetBufferPointer();
        _opaquePSODesc.VS.BytecodeLength = vertexShaderBlob.GetBufferSize();

        _opaquePSODesc.PS.pShaderBytecode = pixelShaderBlob.GetBufferPointer();
        _opaquePSODesc.PS.BytecodeLength = pixelShaderBlob.GetBufferSize();

        _opaquePSODesc.RasterizerState.CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_BACK;
        _opaquePSODesc.RasterizerState.FillMode = D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID;

        _opaquePSODesc.pRootSignature = (ID3D12RootSignature_unmanaged*)_rootSignature.Ptr;

        _opaquePSODesc.DSVFormat = _dsvFormat;
        _opaquePSODesc.DepthStencilState.DepthEnable = true;
        _opaquePSODesc.DepthStencilState.DepthFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_LESS;
        _opaquePSODesc.DepthStencilState.DepthWriteMask = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ALL;

        _opaquePSODesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
        _opaquePSODesc.NumRenderTargets = 1;
        _opaquePSODesc.RTVFormats._0 = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
        _opaquePSODesc.BlendState.RenderTarget._0.RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE.D3D12_COLOR_WRITE_ENABLE_ALL;
        _opaquePSODesc.SampleDesc.Count = 1;
        _opaquePSODesc.SampleMask = uint.MaxValue;

        fixed (D3D12_INPUT_ELEMENT_DESC* pInputElementDesc = _inputElementDesc) {
            _opaquePSODesc.InputLayout = new() { NumElements = (uint)_inputElementDesc.Length, pInputElementDescs = pInputElementDesc };

            _d3d12Device.CreateGraphicsPipelineState(_opaquePSODesc, out _opaquePSO);
        }
    }

    private unsafe void CreateTransparentPSO() {

        _transparentPSODesc = _opaquePSODesc;

        D3DCompileFromFile(
            "TransparentShader.hlsl",
            null,
            null,
            "VSMain",
            "vs_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var vertexShaderBlob,
            out var errorBlobVS).ThrowOnFailure();

        if (errorBlobVS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobVS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        D3DCompileFromFile(
            "TransparentShader.hlsl",
            null,
            null,
            "PSMain",
            "ps_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var pixelShaderBlob,
            out var errorBlobPS).ThrowOnFailure();

        if (errorBlobPS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobPS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        _transparentPSODesc.VS.pShaderBytecode = vertexShaderBlob.GetBufferPointer();
        _transparentPSODesc.VS.BytecodeLength = vertexShaderBlob.GetBufferSize();

        _transparentPSODesc.PS.pShaderBytecode = pixelShaderBlob.GetBufferPointer();
        _transparentPSODesc.PS.BytecodeLength = pixelShaderBlob.GetBufferSize();

        // 关闭背面剔除
        _transparentPSODesc.RasterizerState.CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_NONE;

        fixed (D3D12_INPUT_ELEMENT_DESC* pInputElementDesc = _inputElementDesc) {
            _transparentPSODesc.InputLayout = new() { NumElements = (uint)_inputElementDesc.Length, pInputElementDescs = pInputElementDesc };

            _d3d12Device.CreateGraphicsPipelineState(_transparentPSODesc, out _transparentPSO);
        }
    }

    private unsafe void CreateTranslucencePSO() {

        _translucencePSODesc = _opaquePSODesc;

        D3DCompileFromFile(
            "TranslucenceShader.hlsl",
            null,
            null,
            "VSMain",
            "vs_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var vertexShaderBlob,
            out var errorBlobVS).ThrowOnFailure();

        if (errorBlobVS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobVS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        D3DCompileFromFile(
            "TranslucenceShader.hlsl",
            null,
            null,
            "PSMain",
            "ps_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var pixelShaderBlob,
            out var errorBlobPS).ThrowOnFailure();

        if (errorBlobPS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobPS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        _translucencePSODesc.VS.pShaderBytecode = vertexShaderBlob.GetBufferPointer();
        _translucencePSODesc.VS.BytecodeLength = vertexShaderBlob.GetBufferSize();

        _translucencePSODesc.PS.pShaderBytecode = pixelShaderBlob.GetBufferPointer();
        _translucencePSODesc.PS.BytecodeLength = pixelShaderBlob.GetBufferSize();

        // 关闭背面剔除
        _translucencePSODesc.RasterizerState.CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_NONE;

        // 关闭深度写入，但仍然保留深度测试
        _translucencePSODesc.DepthStencilState.DepthWriteMask = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ZERO;

        // 结果色彩 = 上层色彩 * 上层色彩 alpha + 下层色彩 * (1 - 上层色彩 alpha)，这套公式仅适用于下层是不透明物体的情况
        // Src = 源色彩 = 上层色彩，Dest = 目标色彩 = 下层色彩
        // SrcA = 源色彩 Alpha 值，DstA = 目标色彩 Alpha 值
        // Result = Src * SrcA + Dest * (1 - SrcA)

        // 开启混合
        _translucencePSODesc.BlendState.RenderTarget._0.BlendEnable = true;

        // 下面三个选项控制 RGB 通道的混合，Alpha 通道与 RGB 通道的混合是分开的，这一点请留意！
        // Result = Src * SrcA + Dest * (1 - SrcA)
        _translucencePSODesc.BlendState.RenderTarget._0.SrcBlend = D3D12_BLEND.D3D12_BLEND_SRC_ALPHA;
        _translucencePSODesc.BlendState.RenderTarget._0.DestBlend = D3D12_BLEND.D3D12_BLEND_INV_SRC_ALPHA;
        _translucencePSODesc.BlendState.RenderTarget._0.BlendOp = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD;

        // 下面的三个选项控制 Alpha 通道的混合，Alpha 通道与 RGB 通道的混合是分开的，这一点请留意！
        // ResultA = SrcA * 1 + DstA * 0
        _translucencePSODesc.BlendState.RenderTarget._0.SrcBlendAlpha = D3D12_BLEND.D3D12_BLEND_ONE;
        _translucencePSODesc.BlendState.RenderTarget._0.DestBlendAlpha = D3D12_BLEND.D3D12_BLEND_ZERO;
        _translucencePSODesc.BlendState.RenderTarget._0.BlendOpAlpha = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD;

        fixed (D3D12_INPUT_ELEMENT_DESC* pInputElementDesc = _inputElementDesc) {
            _translucencePSODesc.InputLayout = new() { NumElements = (uint)_inputElementDesc.Length, pInputElementDescs = pInputElementDesc };

            _d3d12Device.CreateGraphicsPipelineState(_translucencePSODesc, out _translucencePSO);
        }
    }

    private unsafe void CreateWBOITAccumPSO() {
        var wboitAccumPSODesc = _opaquePSODesc;

        D3DCompileFromFile(
            "WBOITAccumulationShader.hlsl",
            null,
            null,
            "VSMain",
            "vs_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var vertexShaderBlob,
            out var errorBlobVS).ThrowOnFailure();

        if (errorBlobVS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobVS.GetBufferPointer());
            Debug.WriteLine($"WBOIT Accum VS Error: {errorMessage}");
        }

        D3DCompileFromFile(
            "WBOITAccumulationShader.hlsl",
            null,
            null,
            "PSMain",
            "ps_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var pixelShaderBlob,
            out var errorBlobPS).ThrowOnFailure();

        if (errorBlobPS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobPS.GetBufferPointer());
            Debug.WriteLine($"WBOIT Accum PS Error: {errorMessage}");
        }

        wboitAccumPSODesc.VS.pShaderBytecode = vertexShaderBlob.GetBufferPointer();
        wboitAccumPSODesc.VS.BytecodeLength = vertexShaderBlob.GetBufferSize();

        wboitAccumPSODesc.PS.pShaderBytecode = pixelShaderBlob.GetBufferPointer();
        wboitAccumPSODesc.PS.BytecodeLength = pixelShaderBlob.GetBufferSize();

        // 关闭背面剔除
        wboitAccumPSODesc.RasterizerState.CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_NONE;

        // 深度测试开，深度写入关
        wboitAccumPSODesc.DepthStencilState.DepthWriteMask = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ZERO;

        // MRT: 2 个渲染目标
        wboitAccumPSODesc.NumRenderTargets = 2;
        wboitAccumPSODesc.RTVFormats._0 = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT;  // 累积
        wboitAccumPSODesc.RTVFormats._1 = DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT;           // Reveal

        // 关键：启用独立混合，允许每个 RT 使用不同的混合状态
        wboitAccumPSODesc.BlendState.IndependentBlendEnable = true;

        // RT0 混合: 加法混合 (累积)
        // Result = Src * 1 + Dest * 1
        wboitAccumPSODesc.BlendState.RenderTarget._0.BlendEnable = true;
        wboitAccumPSODesc.BlendState.RenderTarget._0.SrcBlend = D3D12_BLEND.D3D12_BLEND_ONE;
        wboitAccumPSODesc.BlendState.RenderTarget._0.DestBlend = D3D12_BLEND.D3D12_BLEND_ONE;
        wboitAccumPSODesc.BlendState.RenderTarget._0.BlendOp = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD;
        wboitAccumPSODesc.BlendState.RenderTarget._0.SrcBlendAlpha = D3D12_BLEND.D3D12_BLEND_ONE;
        wboitAccumPSODesc.BlendState.RenderTarget._0.DestBlendAlpha = D3D12_BLEND.D3D12_BLEND_ONE;
        wboitAccumPSODesc.BlendState.RenderTarget._0.BlendOpAlpha = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD;

        // RT1 混合: 乘法混合 (reveal)
        // Result = Src * 0 + Dest * (1 - SrcAlpha) = Dest * (1 - alpha)
        wboitAccumPSODesc.BlendState.RenderTarget._1.BlendEnable = true;
        wboitAccumPSODesc.BlendState.RenderTarget._1.SrcBlend = D3D12_BLEND.D3D12_BLEND_ZERO;
        wboitAccumPSODesc.BlendState.RenderTarget._1.DestBlend = D3D12_BLEND.D3D12_BLEND_INV_SRC_COLOR;
        wboitAccumPSODesc.BlendState.RenderTarget._1.BlendOp = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD;
        wboitAccumPSODesc.BlendState.RenderTarget._1.SrcBlendAlpha = D3D12_BLEND.D3D12_BLEND_ZERO;
        wboitAccumPSODesc.BlendState.RenderTarget._1.DestBlendAlpha = D3D12_BLEND.D3D12_BLEND_INV_SRC_ALPHA;
        wboitAccumPSODesc.BlendState.RenderTarget._1.BlendOpAlpha = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD;
        wboitAccumPSODesc.BlendState.RenderTarget._1.RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE.D3D12_COLOR_WRITE_ENABLE_RED;

        fixed (D3D12_INPUT_ELEMENT_DESC* pInputElementDesc = _inputElementDesc) {
            wboitAccumPSODesc.InputLayout = new() { NumElements = (uint)_inputElementDesc.Length, pInputElementDescs = pInputElementDesc };

            _d3d12Device.CreateGraphicsPipelineState(wboitAccumPSODesc, out _wboitAccumPSO);
        }
    }

    private unsafe void CreateWBOITCompositePSO() {
        D3DCompileFromFile(
            "WBOITCompositeShader.hlsl",
            null,
            null,
            "VSMain",
            "vs_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var vertexShaderBlob,
            out var errorBlobVS).ThrowOnFailure();

        if (errorBlobVS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobVS.GetBufferPointer());
            Debug.WriteLine($"WBOIT Composite VS Error: {errorMessage}");
        }

        D3DCompileFromFile(
            "WBOITCompositeShader.hlsl",
            null,
            null,
            "PSMain",
            "ps_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var pixelShaderBlob,
            out var errorBlobPS).ThrowOnFailure();

        if (errorBlobPS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobPS.GetBufferPointer());
            Debug.WriteLine($"WBOIT Composite PS Error: {errorMessage}");
        }

        var wboitCompositePSODesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC() {
            pRootSignature = (ID3D12RootSignature_unmanaged*)_wboitCompositeRootSignature.Ptr,

            VS = new() {
                pShaderBytecode = vertexShaderBlob.GetBufferPointer(),
                BytecodeLength = vertexShaderBlob.GetBufferSize()
            },
            PS = new() {
                pShaderBytecode = pixelShaderBlob.GetBufferPointer(),
                BytecodeLength = pixelShaderBlob.GetBufferSize()
            },

            // 空输入布局 - 使用 SV_VertexID 生成顶点
            InputLayout = new() { NumElements = 0, pInputElementDescs = null },

            // 光栅化状态
            RasterizerState = new() {
                FillMode = D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID,
                CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_NONE,
                DepthClipEnable = true,
            },

            // 禁用深度测试
            DepthStencilState = new() {
                DepthEnable = false,
                DepthWriteMask = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ZERO,
            },

            // 混合状态: Result = Src * (1-SrcAlpha) + Dest * SrcAlpha
            BlendState = new() {
                RenderTarget = new() {
                    _0 = new() {
                        BlendEnable = true,
                        SrcBlend = D3D12_BLEND.D3D12_BLEND_INV_SRC_ALPHA,
                        DestBlend = D3D12_BLEND.D3D12_BLEND_SRC_ALPHA,
                        BlendOp = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD,
                        SrcBlendAlpha = D3D12_BLEND.D3D12_BLEND_ONE,
                        DestBlendAlpha = D3D12_BLEND.D3D12_BLEND_ZERO,
                        BlendOpAlpha = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD,
                        RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE.D3D12_COLOR_WRITE_ENABLE_ALL,
                    }
                }
            },

            // 单个渲染目标 (后备缓冲区)
            NumRenderTargets = 1,
            RTVFormats = new() { _0 = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },

            // 其他配置
            PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
            SampleDesc = new() { Count = 1 },
            SampleMask = uint.MaxValue,
        };

        _d3d12Device.CreateGraphicsPipelineState(wboitCompositePSODesc, out _wboitCompositePSO);
    }


    private unsafe void UpdateConstantBuffer() {
        Unsafe.AsRef<CBuffer>((void*)mvpBuffer).MVPMatrix = _firstCamera.MVPMatrix;
    }

    private static readonly float[] WBOITAccumClear = [0, 0, 0, 0];
    private static readonly float[] WBOITRevealClear = [1, 0, 0, 0];

    // WBOIT 资源屏障 (复用，只需设置 pResource)
    private D3D12_RESOURCE_BARRIER _wboitBarrierToSRV;   // RenderTarget -> ShaderResource
    private D3D12_RESOURCE_BARRIER _wboitBarrierToRTV;   // ShaderResource -> RenderTarget

    private unsafe void Render() {
        UpdateConstantBuffer();


        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _frameIndex = _dxgiSwapChain.GetCurrentBackBufferIndex();
        _rtvHandle.ptr += _frameIndex * _rtvDescriptorSize;


        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator);

        _beginBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_beginBarrier]);

        _commandList.SetGraphicsRootSignature(_rootSignature.Managed);

        _commandList.RSSetViewports([_viewPort]);
        _commandList.RSSetScissorRects([_scissorRect]);

        _commandList.OMSetRenderTargets(1, _rtvHandle, false, _dsvHandle);

        _commandList.ClearDepthStencilView(_dsvHandle, D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0);

        _commandList.ClearRenderTargetView(_rtvHandle, SkyBlue);

        _commandList.SetDescriptorHeaps([_srvHeap]);

        _commandList.SetGraphicsRootConstantBufferView(0, _cbvResource.GetGPUVirtualAddress());

        _commandList.IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        // === Pass 1: 不透明物体 ===
        _commandList.SetPipelineState(_opaquePSO);
        _modelManager.RenderOpaqueModel(_commandList);

        // === Pass 2: Alpha 测试物体 ===
        _commandList.SetPipelineState(_transparentPSO);
        _modelManager.RenderTransparentModel(_commandList);

        // === Pass 3: WBOIT 累积 Pass ===
        // 清除 WBOIT 目标
        _commandList.ClearRenderTargetView(_wboitAccumRtvHandle, WBOITAccumClear);
        _commandList.ClearRenderTargetView(_wboitRevealRtvHandle, WBOITRevealClear);

        // 设置 MRT: 累积 + reveal (两个 RTV 在堆中连续，使用 RTsSingleHandleToDescriptorRange=true)
        _commandList.OMSetRenderTargets(2, _wboitAccumRtvHandle, true, _dsvHandle);

        _commandList.SetPipelineState(_wboitAccumPSO);
        _modelManager.RenderTranslucenceModel(_commandList);

        // === Pass 4: WBOIT 合成 Pass ===
        // 资源屏障: RenderTarget -> ShaderResource
        _wboitBarrierToSRV.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_wboitAccumTarget.Ptr;
        _commandList.ResourceBarrier([_wboitBarrierToSRV]);
        _wboitBarrierToSRV.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_wboitRevealTarget.Ptr;
        _commandList.ResourceBarrier([_wboitBarrierToSRV]);

        // 切换到后备缓冲区
        _commandList.OMSetRenderTargets(1, _rtvHandle, false, default);

        // 设置合成根签名和 PSO
        _commandList.SetGraphicsRootSignature(_wboitCompositeRootSignature.Managed);
        _commandList.SetPipelineState(_wboitCompositePSO);

        // 绑定 WBOIT SRV 堆
        _commandList.SetDescriptorHeaps([_wboitSRVHeap]);
        _commandList.SetGraphicsRootDescriptorTable(0, _wboitSrvGpuHandle);

        // 绘制全屏三角形
        _commandList.DrawInstanced(3, 1, 0, 0);

        // 资源屏障: ShaderResource -> RenderTarget (为下一帧准备)
        _wboitBarrierToRTV.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_wboitAccumTarget.Ptr;
        _commandList.ResourceBarrier([_wboitBarrierToRTV]);
        _wboitBarrierToRTV.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_wboitRevealTarget.Ptr;
        _commandList.ResourceBarrier([_wboitBarrierToRTV]);

        _endBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_endBarrier]);

        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);

        _dxgiSwapChain.Present(1, 0);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void RenderLoop() {
        bool exit = false;
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

    private LRESULT CallBackFunc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
        switch (msg) {
            case WM_DESTROY:
                PostQuitMessage(0);
                break;

            case WM_CHAR:
                var ch = (char)wParam;
                switch (ch) {
                    case 'w':
                    case 'W':
                        _firstCamera.Walk(0.2f);
                        break;

                    case 's':
                    case 'S':
                        _firstCamera.Walk(-0.2f);
                        break;

                    case 'a':
                    case 'A':
                        _firstCamera.Strafe(0.2f);
                        break;

                    case 'd':
                    case 'D':
                        _firstCamera.Strafe(-0.2f);
                        break;
                }
                break;

            case WM_MOUSEMOVE:
                var mouse = (MODIFIERKEYS_FLAGS)wParam.Value;
                switch (mouse) {
                    case MODIFIERKEYS_FLAGS.MK_LBUTTON:
                        _firstCamera.CameraRotate();
                        break;
                    default:
                        _firstCamera.UpdateLastCursorPos();
                        break;
                }
                break;

            default:
                return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        return new(0);
    }

    internal static void Run(SafeHandle hInstance) {
        DX12Engine engine = new();
        engine.InitWindow(hInstance);
        engine.CreateDebugDevice();
        engine.CreateDevice();
        engine.CreateCommandComponents();
        engine.CreateRenderTarget();
        engine.CreateFenceAndBarrier();

        engine.CreateDSVHeap();
        engine.CreateDepthStencilBuffer();
        engine.CreateDSV();

        engine.CreateModelTextureResource();

        engine.CreateModelVertexAndIndexResource();

        engine.CreateCBVResource();

        // WBOIT 资源
        engine.CreateWBOITResources();

        engine.CreateRootSignature();
        engine.CreateWBOITCompositeRootSignature();

        engine.CreateInputLayout();
        engine.CreateOpaquePSO();
        engine.CreateTransparentPSO();
        engine.CreateTranslucencePSO();

        // WBOIT PSO
        engine.CreateWBOITAccumPSO();
        engine.CreateWBOITCompositePSO();

        engine.RenderLoop();
    }

}
