// https://blog.csdn.net/DGAF2198588973/article/details/145391595

using System.Buffers;
using System.Collections.Frozen;
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
using Windows.Win32.UI.WindowsAndMessaging;

namespace DXDemo5;

internal static class DX12TextureHelper {

    private static readonly FrozenDictionary<Guid, DXGI_FORMAT> WicToDxgiFormat = FrozenDictionary.ToFrozenDictionary<Guid, DXGI_FORMAT>([
        new(GUID_WICPixelFormat128bppRGBAFloat, DXGI_FORMAT_R32G32B32A32_FLOAT),
        new(GUID_WICPixelFormat64bppRGBAHalf, DXGI_FORMAT_R16G16B16A16_FLOAT),
        new(GUID_WICPixelFormat64bppRGBA, DXGI_FORMAT_R16G16B16A16_UNORM),
        new(GUID_WICPixelFormat32bppRGBA, DXGI_FORMAT_R8G8B8A8_UNORM),
        new(GUID_WICPixelFormat32bppBGRA, DXGI_FORMAT_B8G8R8A8_UNORM),
        new(GUID_WICPixelFormat32bppBGR, DXGI_FORMAT_B8G8R8X8_UNORM),
        new(GUID_WICPixelFormat32bppRGBA1010102XR, DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM),
        new(GUID_WICPixelFormat32bppRGBA1010102, DXGI_FORMAT_R10G10B10A2_UNORM),
        new(GUID_WICPixelFormat16bppBGRA5551, DXGI_FORMAT_B5G5R5A1_UNORM),
        new(GUID_WICPixelFormat16bppBGR565, DXGI_FORMAT_B5G6R5_UNORM),
        new(GUID_WICPixelFormat32bppGrayFloat, DXGI_FORMAT_R32_FLOAT),
        new(GUID_WICPixelFormat16bppGrayHalf, DXGI_FORMAT_R16_FLOAT),
        new(GUID_WICPixelFormat16bppGray, DXGI_FORMAT_R16_UNORM),
        new(GUID_WICPixelFormat8bppGray, DXGI_FORMAT_R8_UNORM),
        new(GUID_WICPixelFormat8bppAlpha, DXGI_FORMAT_A8_UNORM)
    ]);

    private static readonly FrozenDictionary<Guid, Guid> WicConvert = FrozenDictionary.ToFrozenDictionary<Guid, Guid>([
        new(GUID_WICPixelFormatBlackWhite, GUID_WICPixelFormat8bppGray),
        new(GUID_WICPixelFormat1bppIndexed, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat2bppIndexed, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat4bppIndexed, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat8bppIndexed, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat2bppGray, GUID_WICPixelFormat8bppGray),
        new(GUID_WICPixelFormat4bppGray, GUID_WICPixelFormat8bppGray),
        new(GUID_WICPixelFormat16bppGrayFixedPoint, GUID_WICPixelFormat16bppGrayHalf),
        new(GUID_WICPixelFormat32bppGrayFixedPoint, GUID_WICPixelFormat32bppGrayFloat),
        new(GUID_WICPixelFormat16bppBGR555, GUID_WICPixelFormat16bppBGRA5551),
        new(GUID_WICPixelFormat32bppBGR101010, GUID_WICPixelFormat32bppRGBA1010102),
        new(GUID_WICPixelFormat24bppBGR, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat24bppRGB, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat32bppPBGRA, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat32bppPRGBA, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat48bppRGB, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat48bppBGR, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat64bppBGRA, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat64bppPRGBA, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat64bppPBGRA, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat48bppRGBFixedPoint, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat48bppBGRFixedPoint, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat64bppRGBAFixedPoint, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat64bppBGRAFixedPoint, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat64bppRGBFixedPoint, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat48bppRGBHalf, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat64bppRGBHalf, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat128bppPRGBAFloat, GUID_WICPixelFormat128bppRGBAFloat),
        new(GUID_WICPixelFormat128bppRGBFloat, GUID_WICPixelFormat128bppRGBAFloat),
        new(GUID_WICPixelFormat128bppRGBAFixedPoint, GUID_WICPixelFormat128bppRGBAFloat),
        new(GUID_WICPixelFormat128bppRGBFixedPoint, GUID_WICPixelFormat128bppRGBAFloat),
        new(GUID_WICPixelFormat32bppRGBE, GUID_WICPixelFormat128bppRGBAFloat),
        new(GUID_WICPixelFormat32bppCMYK, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat64bppCMYK, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat40bppCMYKAlpha, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat80bppCMYKAlpha, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat32bppRGB, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat64bppRGB, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat64bppPRGBAHalf, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat128bppRGBAFloat, GUID_WICPixelFormat128bppRGBAFloat),
        new(GUID_WICPixelFormat64bppRGBAHalf, GUID_WICPixelFormat64bppRGBAHalf),
        new(GUID_WICPixelFormat64bppRGBA, GUID_WICPixelFormat64bppRGBA),
        new(GUID_WICPixelFormat32bppRGBA, GUID_WICPixelFormat32bppRGBA),
        new(GUID_WICPixelFormat32bppBGRA, GUID_WICPixelFormat32bppBGRA),
        new(GUID_WICPixelFormat32bppBGR, GUID_WICPixelFormat32bppBGR),
        new(GUID_WICPixelFormat32bppRGBA1010102XR, GUID_WICPixelFormat32bppRGBA1010102XR),
        new(GUID_WICPixelFormat32bppRGBA1010102, GUID_WICPixelFormat32bppRGBA1010102),
        new(GUID_WICPixelFormat16bppBGRA5551, GUID_WICPixelFormat16bppBGRA5551),
        new(GUID_WICPixelFormat16bppBGR565, GUID_WICPixelFormat16bppBGR565),
        new(GUID_WICPixelFormat32bppGrayFloat, GUID_WICPixelFormat32bppGrayFloat),
        new(GUID_WICPixelFormat16bppGrayHalf, GUID_WICPixelFormat16bppGrayHalf),
        new(GUID_WICPixelFormat16bppGray, GUID_WICPixelFormat16bppGray),
        new(GUID_WICPixelFormat8bppGray, GUID_WICPixelFormat8bppGray),
        new(GUID_WICPixelFormat8bppAlpha, GUID_WICPixelFormat8bppAlpha)
    ]);

    // 查表确定兼容的最接近格式是哪个
    internal static bool GetTargetPixelFormat(Guid sourceFormat, out Guid targetFormat) => WicConvert.TryGetValue(sourceFormat, out targetFormat);

    internal static DXGI_FORMAT GetDXGIFormatFromPixelFormat(Guid pixelFormat) => WicToDxgiFormat.TryGetValue(pixelFormat, out var format) ? format : DXGI_FORMAT_UNKNOWN;
}

internal sealed class DX12Engine {

    private const int FrameCount = 3;
    private static readonly float[] SkyBlue = [0.529411793f, 0.807843208f, 0.921568692f, 1f];

    // DX12 支持的所有功能版本，你的显卡最低需要支持 11
    private static readonly D3D_FEATURE_LEVEL[] DX12SupportLevels = [
        D3D_FEATURE_LEVEL_12_2,        // 12.2
        D3D_FEATURE_LEVEL_12_1,        // 12.1
        D3D_FEATURE_LEVEL_12_0,        // 12
        D3D_FEATURE_LEVEL_11_1,        // 11.1
        D3D_FEATURE_LEVEL_11_0         // 11
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

    private const string TextureFilename = "diamond_ore.png";
    private IWICImagingFactory _wicFactory;
    private IWICBitmapDecoder _wicBitmapDecoder;
    private IWICBitmapFrameDecode _wicBitmapDecoderFrame;
    private IWICFormatConverter _wicFormatConverter;
    private IWICBitmapSource _wicBitmapSource;
    private DXGI_FORMAT _textureFormat = DXGI_FORMAT_UNKNOWN;
    private uint _textureWidth;
    private uint _textureHeight;
    private uint _bitsPerPixel;

    private ID3D12DescriptorHeap _srvHeap;

    private uint _bytesPerRowSize;
    private uint _textureSize;
    private uint _uploadResourceRowSize;
    private uint _uploadResourceSize;
    private ComPtr<ID3D12Resource> _uploadTextureResource;
    private ComPtr<ID3D12Resource> _defaultTextureResource;

    private D3D12_CPU_DESCRIPTOR_HANDLE _srvCPUHandle;
    private D3D12_GPU_DESCRIPTOR_HANDLE _srvGPUHandle;

    private struct CBuffer {
        internal Matrix4x4 MVPMatrix;
    }
    private ID3D12Resource _cbvResource;
    private nint _mvpBuffer;

    private ComPtr<ID3D12RootSignature> _rootSignature;

    private ID3D12PipelineState _pipelineStateObject;

    private struct Vertex {
        internal Vector4 Position;
        internal Vector2 TexCoordUV;
    }
    private ID3D12Resource _vertexResource;
    private D3D12_VERTEX_BUFFER_VIEW _vertexBufferView;

    private ID3D12Resource _indexResource;
    private D3D12_INDEX_BUFFER_VIEW _indexBufferView;

    private static readonly Vector4 EyePosition = new(4, 3, 4, 1);
    private static readonly Vector4 FocusPosition = new(0, 1, 1, 1);
    private static readonly Vector4 UpDirection = new(0, 1, 0, 0);
    private Matrix4x4 _modelMatrix;
    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;

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
            lpfnWndProc = &WindowProc,
            lpszClassName = pClassName,
        };

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
            hInstance,
            null);

        ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
    }

    [Conditional("DEBUG")]
    private void CreateDebugDevice() {
        // [STAThread] attribute on Main method handles this
        //CoInitialize();

        D3D12GetDebugInterface(out _d3d12DebugDevice).ThrowOnFailure();
        _d3d12DebugDevice.EnableDebugLayer();

        _dxgiCreateFactoryFlag = DXGI_CREATE_FACTORY_DEBUG;
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

    private void CreateRenderTarget() {
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
        _dxgiSwapChain = tempSwapChain as IDXGISwapChain3;

        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _rtvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(type);

        _renderTargets = new ComPtr<ID3D12Resource>[FrameCount];
        for (uint i = 0; i < FrameCount; i++) {
            _dxgiSwapChain.GetBuffer<ID3D12Resource>(i, out var resource);
            _renderTargets[i] = new(resource);

            _d3d12Device.CreateRenderTargetView(_renderTargets[i].Managed, default(D3D12_RENDER_TARGET_VIEW_DESC?), DestDescriptor: _rtvHandle);

            _rtvHandle.ptr += _rtvDescriptorSize;
        }
    }

    private void CreateFenceAndBarrier() {
        _renderEvent = CreateEvent(null, false, true, null);

        _d3d12Device.CreateFence(0, D3D12_FENCE_FLAG_NONE, out _fence);

        // 设置资源屏障
        // _beginBarrier 起始屏障：Present 呈现状态 -> Render Target 渲染目标状态
        _beginBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _beginBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATE_PRESENT;
        _beginBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATE_RENDER_TARGET;

        // _endBarrier 终止屏障：Render Target 渲染目标状态 -> Present 呈现状态
        _endBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _endBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET;
        _endBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATE_PRESENT;
    }

    private bool LoadTextureFromFile() {
        if (_wicFactory == null) {
            CoCreateInstance(
                CLSID_WICImagingFactory2,
                null,
                CLSCTX.CLSCTX_SERVER,
                out _wicFactory).ThrowOnFailure();
        }

        try {
            _wicBitmapDecoder = _wicFactory.CreateDecoderFromFilename(
               TextureFilename,
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

    private void CreateSRVHeap() {
        var srvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 1,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
            Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
        };

        _d3d12Device.CreateDescriptorHeap(srvHeapDesc, out _srvHeap);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CeilToMultiple(uint value, uint multiple) {
        // assumes `multiple` is power-of-two (true for D3D12_TEXTURE_DATA_PITCH_ALIGNMENT)
        return (value + multiple - 1) & ~(multiple - 1);
    }

    private void CreateUploadAndDefaultResource() {
        _bytesPerRowSize = (_textureWidth * _bitsPerPixel + 7) / 8;

        _textureSize = _bytesPerRowSize * _textureHeight;

        _uploadResourceRowSize = CeilToMultiple(_bytesPerRowSize, D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);

        _uploadResourceSize = _uploadResourceRowSize * (_textureHeight - 1) + _bytesPerRowSize;


        var uploadResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = _uploadResourceSize,
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource<ID3D12Resource>(
            new() {
                Type = D3D12_HEAP_TYPE_UPLOAD,
            },
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out var uploadTextureResource);
        _uploadTextureResource = new(uploadTextureResource);


        var defaultResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Width = _textureWidth,
            Height = _textureHeight,
            Format = _textureFormat,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource<ID3D12Resource>(
            new() {
                Type = D3D12_HEAP_TYPE_DEFAULT,
            },
            D3D12_HEAP_FLAG_NONE,
            defaultResourceDesc,
            D3D12_RESOURCE_STATE_COPY_DEST,
            null,
            out var defaultTextureResource);
        _defaultTextureResource = new(defaultTextureResource);
    }

    private unsafe void CopyTextureDataToDefaultResource() {
        var textureData = ArrayPool<byte>.Shared.Rent((int)_textureSize);

        _wicBitmapSource.CopyPixels(default, _bytesPerRowSize, textureData);

        _uploadTextureResource.Managed.Map(0, null, out var transferPointer);

        int rowBytes = (int)_bytesPerRowSize;
        ReadOnlySpan<byte> allSrcData = textureData;
        byte* dstBasePtr = (byte*)transferPointer;
        for (int i = 0; i < _textureHeight; i++) {
            var srcRow = allSrcData.Slice(i * rowBytes, rowBytes);
            var dstRow = new Span<byte>(dstBasePtr + i * _uploadResourceRowSize, rowBytes);
            srcRow.CopyTo(dstRow);
        }

        _uploadTextureResource.Managed.Unmap(0, default(D3D12_RANGE?));

        ArrayPool<byte>.Shared.Return(textureData);

        var placedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT();
        var defaultResourceDesc = _defaultTextureResource.Managed.GetDesc();

        _d3d12Device.GetCopyableFootprints(
            defaultResourceDesc,
            0,
            0,
            new(ref placedFootprint));

        var dstLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
            Anonymous = new() { SubresourceIndex = 0 },
            pResource = (ID3D12Resource_unmanaged*)_defaultTextureResource.Ptr,
        };

        var srcLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
            Anonymous = new() { PlacedFootprint = placedFootprint },
            pResource = (ID3D12Resource_unmanaged*)_uploadTextureResource.Ptr,
        };

        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);

        _commandList.CopyTextureRegion(dstLocation, 0, 0, 0, srcLocation, default(D3D12_BOX?));
        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void CreateSRV() {
        var srvDescriptorDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC() {
            ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D,
            Format = _textureFormat,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Anonymous = new() { Texture2D = new() { MipLevels = 1 } },
        };

        _srvCPUHandle = _srvHeap.GetCPUDescriptorHandleForHeapStart();

        _d3d12Device.CreateShaderResourceView(_defaultTextureResource.Managed, srvDescriptorDesc, _srvCPUHandle);

        _srvGPUHandle = _srvHeap.GetGPUDescriptorHandleForHeapStart();
    }

    private unsafe void CreateCBVResource() {
        uint cBufferSize = CeilToMultiple((uint)Unsafe.SizeOf<CBuffer>(), D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT);

        var cbvResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = cBufferSize,
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            new() {
                Type = D3D12_HEAP_TYPE_UPLOAD,
            },
            D3D12_HEAP_FLAG_NONE,
            cbvResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _cbvResource);

        _cbvResource.Map(0, null, out var cbvPointer);
        _mvpBuffer = (nint)cbvPointer;
    }

    private unsafe void CreateRootSignature() {
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER[2];


        var srvDescriptorDesc = new D3D12_DESCRIPTOR_RANGE() {
            RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
            NumDescriptors = 1,
            BaseShaderRegister = 0,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0,
        };

        var rootDescriptorTableDesc = new D3D12_ROOT_DESCRIPTOR_TABLE() {
            NumDescriptorRanges = 1,
            pDescriptorRanges = &srvDescriptorDesc,
        };

        rootParameters[0] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            Anonymous = new() { DescriptorTable = rootDescriptorTableDesc },
        };


        var cbvRootDescriptor = new D3D12_ROOT_DESCRIPTOR() {
            ShaderRegister = 0,
            RegisterSpace = 0,
        };

        rootParameters[1] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            Anonymous = new() { Descriptor = cbvRootDescriptor },
        };

        var staticSamplerDesc = new D3D12_STATIC_SAMPLER_DESC() {
            ShaderRegister = 0,
            RegisterSpace = 0,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
            Filter = D3D12_FILTER_COMPARISON_MIN_MAG_MIP_POINT,
            AddressU = D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            AddressV = D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            AddressW = D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            MinLOD = 0.0f,
            MaxLOD = D3D12_FLOAT32_MAX,
            MipLODBias = 0,
            MaxAnisotropy = 1,
            ComparisonFunc = D3D12_COMPARISON_FUNC_NEVER,
        };


        var rootSignatureDesc = new D3D12_ROOT_SIGNATURE_DESC() {
            NumParameters = 2,
            pParameters = rootParameters,
            NumStaticSamplers = 1,
            pStaticSamplers = &staticSamplerDesc,
            // 根签名标志，可以设置渲染管线不同阶段下的输入参数状态。注意这里！我们要从 IA 阶段输入顶点数据，所以要通过根签名，设置渲染管线允许从 IA 阶段读入数据
            Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT
        };

        D3D12SerializeRootSignature(
            rootSignatureDesc,
            D3D_ROOT_SIGNATURE_VERSION_1_0,
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

    private unsafe void CreatePSO() {
        var psoDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC();

        var inputLayoutDesc = new D3D12_INPUT_LAYOUT_DESC();
        var inputElementDesc = stackalloc D3D12_INPUT_ELEMENT_DESC[2];

        var semanticNamePosition = "POSITION"u8;
        byte* pSemanticNamePosition = stackalloc byte[semanticNamePosition.Length + 1];
        semanticNamePosition.CopyTo(new Span<byte>(pSemanticNamePosition, semanticNamePosition.Length));
        pSemanticNamePosition[semanticNamePosition.Length] = 0;

        inputElementDesc[0] = new() {
            SemanticName = new(pSemanticNamePosition),
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32B32A32_FLOAT,
            InputSlot = 0,
            AlignedByteOffset = 0,
            // 输入流类型，一种是我们现在用的 D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA 逐顶点输入流,还有一种叫逐实例输入流，后面再学
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        var semanticNameTexCoord = "TEXCOORD"u8;
        byte* pSemanticNameTexCoord = stackalloc byte[semanticNameTexCoord.Length + 1];
        semanticNameTexCoord.CopyTo(new Span<byte>(pSemanticNameTexCoord, semanticNameTexCoord.Length));
        pSemanticNameTexCoord[semanticNameTexCoord.Length] = 0;

        inputElementDesc[1] = new() {
            SemanticName = new(pSemanticNameTexCoord),
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32_FLOAT,
            InputSlot = 0,
            // 在输入槽中的偏移，因为 position 与 color 在同一输入槽(0号输入槽)
            // position 是 float4，有 4 个 float ，每个 float 占 4 个字节，所以要偏移 4*4=16 个字节，这样才能确定 color 参数的位置，不然装配的时候会覆盖原先 position 的数据
            AlignedByteOffset = 16,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        inputLayoutDesc.NumElements = 2;
        inputLayoutDesc.pInputElementDescs = inputElementDesc;
        psoDesc.InputLayout = inputLayoutDesc;

        D3DCompileFromFile(
            "shader.hlsl",
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
            "shader.hlsl",
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

        psoDesc.VS = new() {
            pShaderBytecode = vertexShaderBlob.GetBufferPointer(),
            BytecodeLength = vertexShaderBlob.GetBufferSize(),
        };
        psoDesc.PS = new() {
            pShaderBytecode = pixelShaderBlob.GetBufferPointer(),
            BytecodeLength = pixelShaderBlob.GetBufferSize(),
        };

        psoDesc.RasterizerState.CullMode = D3D12_CULL_MODE_BACK;
        psoDesc.RasterizerState.FillMode = D3D12_FILL_MODE_SOLID;

        psoDesc.pRootSignature = (ID3D12RootSignature_unmanaged*)_rootSignature.Ptr;

        psoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
        psoDesc.NumRenderTargets = 1;
        psoDesc.RTVFormats._0 = DXGI_FORMAT_R8G8B8A8_UNORM;
        psoDesc.BlendState.RenderTarget._0.RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE_ALL;
        psoDesc.SampleDesc.Count = 1;
        psoDesc.SampleMask = uint.MaxValue;

        _d3d12Device.CreateGraphicsPipelineState(psoDesc, out _pipelineStateObject);
    }

    private unsafe void CreateVertexResource() {
        ReadOnlySpan<Vertex> vertices = [
            // 正面
            new() { Position = new(0, 2, 0, 1), TexCoordUV = new(0, 0) },
            new() { Position = new(2, 2, 0, 1), TexCoordUV = new(1, 0) },
            new() { Position = new(2, 0, 0, 1), TexCoordUV = new(1, 1) },
            new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 1) },

            // 背面
            new() { Position = new(2, 2, 2, 1), TexCoordUV = new(0, 0) },
            new() { Position = new(0, 2, 2, 1), TexCoordUV = new(1, 0) },
            new() { Position = new(0, 0, 2, 1), TexCoordUV = new(1, 1) },
            new() { Position = new(2, 0, 2, 1), TexCoordUV = new(0, 1) },

            // 左面
            new() { Position = new(0, 2, 2, 1), TexCoordUV = new(0, 0) },
            new() { Position = new(0, 2, 0, 1), TexCoordUV = new(1, 0) },
            new() { Position = new(0, 0, 0, 1), TexCoordUV = new(1, 1) },
            new() { Position = new(0, 0, 2, 1), TexCoordUV = new(0, 1) },

            // 右面
            new() { Position = new(2, 2, 0, 1), TexCoordUV = new(0, 0) },
            new() { Position = new(2, 2, 2, 1), TexCoordUV = new(1, 0) },
            new() { Position = new(2, 0, 2, 1), TexCoordUV = new(1, 1) },
            new() { Position = new(2, 0, 0, 1), TexCoordUV = new(0, 1) },

            // 上面
            new() { Position = new(0, 2, 2, 1), TexCoordUV = new(0, 0) },
            new() { Position = new(2, 2, 2, 1), TexCoordUV = new(1, 0) },
            new() { Position = new(2, 2, 0, 1), TexCoordUV = new(1, 1) },
            new() { Position = new(0, 2, 0, 1), TexCoordUV = new(0, 1) },

            // 下面
            new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 0) },
            new() { Position = new(2, 0, 0, 1), TexCoordUV = new(1, 0) },
            new() { Position = new(2, 0, 2, 1), TexCoordUV = new(1, 1) },
            new() { Position = new(0, 0, 2, 1), TexCoordUV = new(0, 1) },
        ];

        var vertexDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(Unsafe.SizeOf<Vertex>() * vertices.Length),
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            new() {
                Type = D3D12_HEAP_TYPE_UPLOAD,
            },
            D3D12_HEAP_FLAG_NONE,
            vertexDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _vertexResource);

        _vertexResource.Map(0, null, out var transferPointer);
        vertices.CopyTo(new(transferPointer, vertices.Length));
        _vertexResource.Unmap(0, default(D3D12_RANGE?));

        _vertexBufferView.BufferLocation = _vertexResource.GetGPUVirtualAddress();
        _vertexBufferView.SizeInBytes = (uint)(Unsafe.SizeOf<Vertex>() * vertices.Length);
        _vertexBufferView.StrideInBytes = (uint)Unsafe.SizeOf<Vertex>();
    }

    private unsafe void CreateIndexResource() {
        ReadOnlySpan<uint> indexArray = [
            // 正面
            0,1,2,0,2,3,
            // 背面
            4,5,6,4,6,7,
            // 左面
            8,9,10,8,10,11,
            // 右面
            12,13,14,12,14,15,
            // 上面
            16,17,18,16,18,19,
            // 下面
            20,21,22,20,22,23,
        ];

        var indexResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(sizeof(uint) * indexArray.Length),
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            new() {
                Type = D3D12_HEAP_TYPE_UPLOAD,
            },
            D3D12_HEAP_FLAG_NONE,
            indexResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _indexResource);


        _indexResource.Map(0, null, out var transferPointer);
        indexArray.CopyTo(new(transferPointer, indexArray.Length));
        _indexResource.Unmap(0, default(D3D12_RANGE?));

        _indexBufferView.BufferLocation = _indexResource.GetGPUVirtualAddress();
        _indexBufferView.SizeInBytes = (uint)(sizeof(uint) * indexArray.Length);
        _indexBufferView.Format = DXGI_FORMAT_R32_UINT;
    }

    private unsafe void UpdateConstantBuffer() {
        _modelMatrix = Matrix4x4.CreateRotationY(30.0f);
        _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(EyePosition.AsVector3(), FocusPosition.AsVector3(), UpDirection.AsVector3());
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 4.0f, 4.0f / 3, 0.1f, 1000);

        Unsafe.AsRef<CBuffer>((void*)_mvpBuffer).MVPMatrix = _modelMatrix * _viewMatrix * _projectionMatrix;
    }

    private unsafe void Render() {
        UpdateConstantBuffer();


        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _frameIndex = _dxgiSwapChain.GetCurrentBackBufferIndex();
        _rtvHandle.ptr += _frameIndex * _rtvDescriptorSize;


        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);

        _beginBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_beginBarrier]);

        _commandList.SetGraphicsRootSignature(_rootSignature.Managed);
        _commandList.SetPipelineState(_pipelineStateObject);

        _commandList.RSSetViewports([_viewPort]);
        _commandList.RSSetScissorRects([_scissorRect]);

        _commandList.OMSetRenderTargets(1, _rtvHandle, false, null);

        _commandList.ClearRenderTargetView(_rtvHandle, SkyBlue, 0, null);

        _commandList.SetDescriptorHeaps([_srvHeap]);
        _commandList.SetGraphicsRootDescriptorTable(0, _srvGPUHandle);

        _commandList.SetGraphicsRootConstantBufferView(1, _cbvResource.GetGPUVirtualAddress());

        _commandList.IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        _commandList.IASetVertexBuffers(0, [_vertexBufferView]);

        _commandList.IASetIndexBuffer(_indexBufferView);

        _commandList.DrawIndexedInstanced(36, 1, 0, 0, 0);


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

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT WindowProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
        switch (msg) {
            case WM_DESTROY:
                PostQuitMessage(0);
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

        engine.LoadTextureFromFile();
        engine.CreateSRVHeap();
        engine.CreateUploadAndDefaultResource();
        engine.CopyTextureDataToDefaultResource();
        engine.CreateSRV();

        engine.CreateCBVResource();

        engine.CreateRootSignature();
        engine.CreatePSO();

        engine.CreateVertexResource();
        engine.CreateIndexResource();

        engine.RenderLoop();
    }

}

internal static class Program {

    [STAThread]
    static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }
}