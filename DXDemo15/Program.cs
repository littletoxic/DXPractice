// https://blog.csdn.net/DGAF2198588973/article/details/147233643

using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing;
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

namespace DXDemo15;

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

internal static class CallBackWrapper {

    internal static Func<HWND, uint, WPARAM, LPARAM, LRESULT> BrokerFunc { get; set; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    internal static LRESULT CallBackFunc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
        return BrokerFunc(hwnd, msg, wParam, lParam);
    }
}

internal sealed class Camera {
    private Vector3 _eyePosition;    // 摄像机在世界空间下的位置
    private Vector3 _focusPosition;  // 摄像机在世界空间下观察的焦点位置
    private Vector3 _upDirection;    // 世界空间垂直向上的向量

    // 摄像机观察方向的单位向量，用于前后移动
    private Vector3 _viewDirection;

    // 焦距，摄像机原点与焦点的距离
    private readonly float _focalLength;

    // 摄像机向右方向的单位向量，用于左右移动
    private Vector3 _rightDirection;

    private Point _lastCursorPoint;         // 上一次鼠标的位置

    private const float FovAngleY = MathF.PI / 4.0f;   // 垂直视场角
    private const float AspectRatio = 16f / 9f;   // 投影窗口宽高比
    private const float NearZ = 0.1f;            // 近平面到原点的距离
    private const float FarZ = 1000f;            // 远平面到原点的距离

    // 观察矩阵，注意前两个参数是点，第三个参数才是向量
    private Matrix4x4 _viewMatrix;
    // 投影矩阵(注意近平面和远平面距离不能 <= 0!)
    private Matrix4x4 _projectionMatrix;

    internal Matrix4x4 MVPMatrix {
        get {
            _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection);
            return _viewMatrix * _projectionMatrix; // MVP 矩阵
        }
    }

    internal Camera() {
        _eyePosition = new Vector3(4, 4, 2);
        _focusPosition = new Vector3(0, 0, 0);
        _upDirection = new Vector3(0, 1, 0);

        // 注意！我们这里移除了模型矩阵！每个模型会指定具体的模型矩阵！
        _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection); // 观察矩阵，世界空间 -> 观察空间
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(FovAngleY, AspectRatio, NearZ, FarZ); // 投影矩阵，观察空间 -> 齐次裁剪空间

        _viewDirection = Vector3.Normalize(_focusPosition - _eyePosition);
        _focalLength = Vector3.Distance(_focusPosition, _eyePosition);
        _rightDirection = Vector3.Normalize(Vector3.Cross(_viewDirection, _upDirection));
    }

    // 摄像机前后移动，参数 Stride 是移动速度 (步长)，正数向前移动，负数向后移动
    internal void Walk(float stride) {
        _eyePosition += stride * _viewDirection;
        _focusPosition += stride * _viewDirection;
    }

    // 摄像机左右移动，参数 Stride 是移动速度 (步长)，正数向左移动，负数向右移动
    internal void Strafe(float stride) {
        _eyePosition += stride * _rightDirection;
        _focusPosition += stride * _rightDirection;
    }

    // 鼠标在屏幕空间 y 轴上移动，相当于摄像机以向右的向量 RightDirection 向上向下旋转，人眼往上下看
    private void RotateByY(float angleY) {
        var r = Matrix4x4.CreateFromAxisAngle(_rightDirection, -angleY);

        _upDirection = Vector3.TransformNormal(_upDirection, r);
        _viewDirection = Vector3.TransformNormal(_viewDirection, r);

        _focusPosition = _eyePosition + _focalLength * _viewDirection;
    }

    // 鼠标在屏幕空间 x 轴上移动，相当于摄像机绕世界空间的 y 轴向左向右旋转，人眼往左右看
    private void RotateByX(float angleX) {
        var r = Matrix4x4.CreateRotationY(angleX);

        _upDirection = Vector3.TransformNormal(_upDirection, r);
        _viewDirection = Vector3.TransformNormal(_viewDirection, r);
        _rightDirection = Vector3.TransformNormal(_rightDirection, r);

        _focusPosition = _eyePosition + _focalLength * _viewDirection;
    }

    internal void UpdateLastCursorPos() {
        GetCursorPos(out _lastCursorPoint);
    }

    // 当鼠标左键长按并移动时，旋转摄像机视角
    internal void CameraRotate() {
        GetCursorPos(out var currentCursorPoint);

        float deltaX = currentCursorPoint.X - _lastCursorPoint.X;
        float deltaY = currentCursorPoint.Y - _lastCursorPoint.Y;

        float angleX = deltaX * (MathF.PI / 180.0f) * 0.25f;
        float angleY = deltaY * (MathF.PI / 180.0f) * 0.25f;

        RotateByY(angleY);
        RotateByX(angleX);

        UpdateLastCursorPos();
    }
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

    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;

    private static readonly D3D12_HEAP_PROPERTIES UploadHeapDesc = new() {
        Type = D3D12_HEAP_TYPE_UPLOAD,
    };

    private static readonly D3D12_HEAP_PROPERTIES DefaultHeapDesc = new() {
        Type = D3D12_HEAP_TYPE_DEFAULT,
    };

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
    // DXGI_FORMAT_D24_UNORM_S8_UINT    (每个像素占用四个字节 32 位，24 位无符号归一化浮点数留作深度值，8 位整数留作模板值)
    // DXGI_FORMAT_D32_FLOAT_S8X24_UINT    (每个像素占用八个字节 64 位，32 位浮点数留作深度值，8 位整数留作模板值，其余 24 位保留不使用)
    // DXGI_FORMAT_D16_UNORM            (每个像素占用两个字节 16 位，16 位无符号归一化浮点数留作深度值，范围 [0,1]，不使用模板)
    // DXGI_FORMAT_D32_FLOAT            (每个像素占用四个字节 32 位，32 位浮点数留作深度值，不使用模板)
    // 这里我们选择最常用的格式 DXGI_FORMAT_D24_UNORM_S8_UINT
    private const DXGI_FORMAT _dsvFormat = DXGI_FORMAT_D24_UNORM_S8_UINT;
    private ID3D12Resource _depthStencilBuffer;

    private struct CBuffer {
        internal Matrix4x4 MVPMatrix;
    }
    private ID3D12Resource _cbvResource;
    private nint _mvpBuffer;

    private IWICImagingFactory _wicFactory;
    private IWICBitmapDecoder _wicBitmapDecoder;
    private IWICBitmapFrameDecode _wicBitmapDecoderFrame;
    private IWICFormatConverter _wicFormatConverter;
    private DXGI_FORMAT _textureFormat = DXGI_FORMAT_UNKNOWN;

    internal sealed record class Texture(string TextureName, string FilePath) {

        internal IWICBitmapSource WICBitmapSource { get; set; }
    }

    private readonly Texture[] _textureGroup = [
        new("蓝冰", "resource/ice_packed.png"),
        new("圆石", "resource/cobblestone.png"),
        new("绿宝石块", "resource/emerald_block.png"),
        new("熔炉正面", "resource/furnace_front_off.png"),
        new("熔炉侧面", "resource/furnace_side.png"),
        new("熔炉顶面", "resource/furnace_top.png"),
        new("金矿", "resource/gold_ore.png"),
        new("金块", "resource/gold_block.png"),
        new("音符盒", "resource/noteblock.png"),
        new("活塞底面", "resource/piston_bottom.png"),
        new("活塞侧面", "resource/piston_side.png"),
        new("活塞顶面", "resource/piston_top_normal.png"),
        new("红石块", "resource/redstone_block.png"),
        new("红石灯激活状态", "resource/redstone_lamp_on.png"),
        new("TNT底面", "resource/tnt_bottom.png"),
        new("TNT侧面", "resource/tnt_side.png"),
        new("TNT顶面", "resource/tnt_top.png"),
        new("基岩", "resource/bedrock.png"),
        new("书架", "resource/bookshelf.png"),
        new("命令方块", "resource/command_block.png"),
        new("工作台正面", "resource/crafting_table_front.png"),
        new("工作台侧面", "resource/crafting_table_side.png"),
        new("工作台顶面", "resource/crafting_table_top.png"),
        new("水平发射器正面", "resource/dispenser_front_horizontal.png"),
        new("垂直发射器顶面", "resource/dispenser_front_vertical.png"),
        new("水平投掷器正面", "resource/dropper_front_horizontal.png"),
        new("垂直投掷器顶面", "resource/dropper_front_vertical.png"),
        new("绿宝石原矿", "resource/emerald_ore.png"),
        new("玻璃", "resource/glass.png"),
        new("萤石", "resource/glowstone.png"),
        new("铁矿", "resource/iron_ore.png"),
        new("橡木原木侧面", "resource/log_oak.png"),
        new("橡木原木顶面", "resource/log_oak_top.png"),
        new("橡木木板", "resource/planks_oak.png"),
        new("沙子", "resource/sand.png"),
        new("石砖", "resource/stonebrick.png"),
        new("平滑石", "resource/stone_slab_top.png"),
        new("石英块底面", "resource/quartz_block_bottom.png"),
        new("石英块侧面", "resource/quartz_block_side.png"),
        new("石英块顶面", "resource/quartz_block_top.png")
    ];

    private uint _textureWidth;
    private uint _textureHeight;
    private uint _bitsPerPixel;

    private uint _bytesPerRowSize;
    private ulong _textureSize;
    private uint _uploadResourceRowSize;
    private ulong _uploadSubResourceSize;

    private ulong _uploadArrayElementSize;
    private ulong _uploadResourceSize;

    private ComPtr<ID3D12Resource> _textureArrayDefaultResource;
    private ComPtr<ID3D12Resource> _textureArrayUploadResource;

    private ID3D12DescriptorHeap _srvHeap;
    private D3D12_CPU_DESCRIPTOR_HANDLE _srvTextureArrayCPUHandle;
    private D3D12_GPU_DESCRIPTOR_HANDLE _srvTextureArrayGPUHandle;

    [InlineArray(6)]
    [CollectionBuilder(typeof(Buffer6Builder), nameof(Buffer6Builder.Create))]
    internal struct Buffer6<T> where T : unmanaged {
        private T _element0;
    }

    internal static class Buffer6Builder {
        internal static Buffer6<T> Create<T>(ReadOnlySpan<T> values) where T : unmanaged {
            var buffer = default(Buffer6<T>);
            values.CopyTo(buffer);
            return buffer;
        }
    }

    // 六个立方体面对应的纹理在 Texture Array 中的位置
    // 数组索引 0-5 分别对应右面 (+X)，左面 (-X)，前面 (+Z)，后面 (-Z)，上面 (+Y)，下面 (-Y)
    internal readonly record struct CubeFace(Buffer6<uint> FaceTextureInArrayIndex);

    private readonly CubeFace[] _blockCubeTextureIndexGroup = [
        new([0, 0, 0, 0, 0, 0]),        // 蓝冰
        new([1, 1, 1, 1, 1, 1]),        // 圆石
        new([2, 2, 2, 2, 2, 2]),        // 绿宝石块
        new([3, 4, 4, 4, 5, 5]),        // 熔炉
        new([6, 6, 6, 6, 6, 6]),        // 金矿
        new([7, 7, 7, 7, 7, 7]),        // 金块
        new([8, 8, 8, 8, 8, 8]),        // 音符盒
        new([10, 10, 10, 10, 11, 9]),   // 活塞
        new([12, 12, 12, 12, 12, 12]),  // 红石块
        new([13, 13, 13, 13, 13, 13]),  // 红石灯
        new([15, 15, 15, 15, 16, 14]),  // TNT
        new([17, 17, 17, 17, 17, 17]),  // 基岩
        new([18, 18, 18, 18, 33, 33]),  // 书架
        new([19, 19, 19, 19, 19, 19]),  // 命令方块
        new([20, 20, 21, 21, 22, 22]),  // 工作台
        new([23, 9, 9, 9, 9, 9]),       // 水平发射器
        new([9, 9, 9, 9, 24, 9]),       // 垂直发射器
        new([25, 9, 9, 9, 9, 9]),       // 水平投掷器
        new([9, 9, 9, 9, 26, 9]),       // 垂直投掷器
        new([27, 27, 27, 27, 27, 27]),  // 绿宝石原矿
        new([28, 28, 28, 28, 28, 28]),  // 玻璃
        new([29, 29, 29, 29, 29, 29]),  // 萤石
        new([30, 30, 30, 30, 30, 30]),  // 铁矿
        new([31, 31, 31, 31, 32, 32]),  // 橡木原木
        new([31, 31, 31, 31, 31, 31]),  // 橡树木
        new([33, 33, 33, 33, 33, 33]),  // 橡木木板
        new([34, 34, 34, 34, 34, 34]),  // 沙子
        new([35, 35, 35, 35, 35, 35]),  // 石砖
        new([36, 36, 36, 36, 36, 36]),  // 平滑石
        new([38, 38, 38, 38, 39, 37]),  // 石英块
    ];

    private ID3D12Resource _structuredBufferUploadResource;
    private ID3D12Resource _structuredBufferDefaultResource;

    private ComPtr<ID3D12RootSignature> _rootSignature;

    private static readonly PCSTR Position = CreatePCSTR("POSITION");
    private static readonly PCSTR TexCoord = CreatePCSTR("TEXCOORD");
    private static readonly PCSTR FaceIndex = CreatePCSTR("FACEINDEX");
    private static readonly PCSTR BlockOffset = CreatePCSTR("BLOCKOFFSET");
    private static readonly PCSTR BlockType = CreatePCSTR("BLOCKTYPE");

    private ID3D12PipelineState _renderBlockPSO;

    private readonly D3D12_VERTEX_BUFFER_VIEW[] _vertexBufferView = new D3D12_VERTEX_BUFFER_VIEW[2];
    private D3D12_INDEX_BUFFER_VIEW _indexBufferView;

    internal readonly record struct Vertex(Vector4 Position, Vector2 TexCoordUV, uint FaceIndex);

    private readonly Vertex[] _perBlockVertexData = [
        // 右面 (+X)
        new(new(1, 1, -1, 1), new(0, 0), 0),
        new(new(1, 1, 1, 1), new(1, 0), 0),
        new(new(1, -1, 1, 1), new(1, 1), 0),
        new(new(1, -1, -1, 1), new(0, 1), 0),

        // 左面 (-X)
        new(new(-1, 1, 1, 1), new(0, 0), 1),
        new(new(-1, 1, -1, 1), new(1, 0), 1),
        new(new(-1, -1, -1, 1), new(1, 1), 1),
        new(new(-1, -1, 1, 1), new(0, 1), 1),

        // 前面 (+Z)
        new(new(1, 1, 1, 1), new(0, 0), 2),
        new(new(-1, 1, 1, 1), new(1, 0), 2),
        new(new(-1, -1, 1, 1), new(1, 1), 2),
        new(new(1, -1, 1, 1), new(0, 1), 2),

        // 后面 (-Z)
        new(new(-1, 1, -1, 1), new(0, 0), 3),
        new(new(1, 1, -1, 1), new(1, 0), 3),
        new(new(1, -1, -1, 1), new(1, 1), 3),
        new(new(-1, -1, -1, 1), new(0, 1), 3),

        // 上面 (+Y)
        new(new(-1, 1, -1, 1), new(0, 0), 4),
        new(new(-1, 1, 1, 1), new(1, 0), 4),
        new(new(1, 1, 1, 1), new(1, 1), 4),
        new(new(1, 1, -1, 1), new(0, 1), 4),

        // 下面 (-Y)
        new(new(1, -1, -1, 1), new(0, 0), 5),
        new(new(1, -1, 1, 1), new(1, 0), 5),
        new(new(-1, -1, 1, 1), new(1, 1), 5),
        new(new(-1, -1, -1, 1), new(0, 1), 5),
    ];

    private readonly uint[] _perBlockIndexData = [
        0, 1, 2, 0, 2, 3,          // 右面
        4, 5, 6, 4, 6, 7,          // 左面
        8, 9, 10, 8, 10, 11,       // 前面
        12, 13, 14, 12, 14, 15,    // 后面
        16, 17, 18, 16, 18, 19,    // 上面
        20, 21, 22, 20, 22, 23     // 下面
    ];

    private ID3D12Resource _blockVertexResource;
    private ID3D12Resource _blockIndexResource;
    private ID3D12Resource _blockInstanceResource;

    internal readonly record struct BlockInstance(Vector3 BlockOffset, uint BlockType);

    private readonly List<BlockInstance> _blockGroup = [];

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
                if (D3D12CreateDevice(_dxgiAdapter, level, out _d3d12Device).Succeeded) {
                    var adapter = _dxgiAdapter.GetDesc();
                    Debug.WriteLine($"当前使用的显卡：{adapter.Description}");
                    return true;
                }
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

    private void STEP06_CreateFenceAndBarrier() {
        _renderEvent = CreateEvent(null, false, false, null);

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

    private void STEP07_CreateDSVHeap() {
        var dsvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 1,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
        };

        _d3d12Device.CreateDescriptorHeap(dsvHeapDesc, out _dsvHeap);

        _dsvHandle = _dsvHeap.GetCPUDescriptorHandleForHeapStart();
    }

    private void STEP08_CreateDepthStencilBuffer() {
        var dsvResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Format = _dsvFormat,
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Width = WindowWidth,
            Height = WindowHeight,
            MipLevels = 1,
            DepthOrArraySize = 1,
            SampleDesc = new() { Count = 1, Quality = 0 },
            Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL
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
            DefaultHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            dsvResourceDesc,
            D3D12_RESOURCE_STATE_DEPTH_WRITE,
            depthStencilBufferClearValue,
            out _depthStencilBuffer);
    }

    private void STEP09_CreateDSV() {
        var dsvViewDesc = new D3D12_DEPTH_STENCIL_VIEW_DESC() {
            Format = _dsvFormat,
            ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D,
            Flags = D3D12_DSV_FLAG_NONE,
        };

        _d3d12Device.CreateDepthStencilView(
            _depthStencilBuffer,
            dsvViewDesc,
            _dsvHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T CeilToMultiple<T>(T value, T multiple) where T : IBinaryInteger<T>, IUnsignedNumber<T> {
        // assumes `multiple` is power-of-two (true for D3D12_TEXTURE_DATA_PITCH_ALIGNMENT)
        Debug.Assert(T.IsPow2(multiple));
        return (value + multiple - T.One) & ~(multiple - T.One);
    }

    private unsafe void STEP10_CreateCameraCBVResource() {
        ulong cBufferSize = CeilToMultiple((uint)Unsafe.SizeOf<CBuffer>(), D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT);

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
            UploadHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            cbvResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _cbvResource);

        _cbvResource.Map(0, null, out var cbvPointer);
        _mvpBuffer = (nint)cbvPointer;
    }

    private bool STEP11_LoadTextureGroup() {
        if (_wicFactory == null) {
            CoCreateInstance(
                CLSID_WICImagingFactory2,
                null,
                CLSCTX.CLSCTX_INPROC_SERVER,
                out _wicFactory).ThrowOnFailure();
        }


        for (int i = 0; i < _textureGroup.Length; i++) {
            try {
                _wicBitmapDecoder = _wicFactory.CreateDecoderFromFilename(
                   _textureGroup[i].FilePath,
                   null,
                   GENERIC_ACCESS_RIGHTS.GENERIC_READ,
                   WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
            } catch (Exception ex) {
                MessageBox(default, ex.Message, "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
                return false;
            }

            _wicBitmapDecoder.GetFrame(0, out _wicBitmapDecoderFrame);

            _wicBitmapDecoderFrame.GetPixelFormat(out var pixelFormat);

            if (!DX12TextureHelper.GetTargetPixelFormat(pixelFormat, out var targetFormat)) {
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

            _textureGroup[i].WICBitmapSource = _wicFormatConverter;
        }

        return true;
    }

    // 获取纹理数组的各种属性，以第一个元素为准，后面的元素这些属性基本上是一样的 (?)
    private void STEP12_GetTextureArrayElementsProperties() {
        _textureGroup[0].WICBitmapSource.GetPixelFormat(out var wicPixelFormat);
        _textureFormat = DX12TextureHelper.GetDXGIFormatFromPixelFormat(wicPixelFormat);

        _wicFactory.CreateComponentInfo(wicPixelFormat, out var componentInfo);
        var pixelInfo = componentInfo as IWICPixelFormatInfo;
        pixelInfo.GetBitsPerPixel(out _bitsPerPixel);

        _textureGroup[0].WICBitmapSource.GetSize(out _textureWidth, out _textureHeight);

        _bytesPerRowSize = (_textureWidth * _bitsPerPixel + 7) / 8;
        _textureSize = _bytesPerRowSize * _textureHeight;

        _uploadResourceRowSize = CeilToMultiple(_bytesPerRowSize, D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);
        _uploadSubResourceSize = _uploadResourceRowSize * (_textureHeight - 1) + _bytesPerRowSize;

        _uploadArrayElementSize = CeilToMultiple(_uploadSubResourceSize, D3D12_TEXTURE_DATA_PLACEMENT_ALIGNMENT);
        _uploadResourceSize = _uploadArrayElementSize * ((uint)_textureGroup.Length - 1) + _uploadSubResourceSize;

    }

    private void STEP13_CreateTextureArrayResource() {

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
            UploadHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out var uploadResource);
        _textureArrayUploadResource = new(uploadResource);


        var defaultResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            DepthOrArraySize = (ushort)_textureGroup.Length,
            Width = _textureWidth,
            Height = _textureHeight,
            Format = _textureFormat,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource<ID3D12Resource>(
            DefaultHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            defaultResourceDesc,
            D3D12_RESOURCE_STATE_COPY_DEST,
            null,
            out var defaultResource);
        _textureArrayDefaultResource = new(defaultResource);
    }

    private unsafe void STEP14_CopyTextureArrayToDefaultResource() {
        var textureData = ArrayPool<byte>.Shared.Rent((int)_textureSize);

        _textureArrayUploadResource.Managed.Map(0, null, out var transferPointer);

        for (int i = 0; i < _textureGroup.Length; i++) {

            _textureGroup[i].WICBitmapSource.CopyPixels(default, _bytesPerRowSize, textureData);

            int rowBytes = (int)_bytesPerRowSize;
            for (int j = 0; j < _textureHeight; j++) {
                var srcRow = textureData.AsSpan().Slice(j * rowBytes, rowBytes);
                var dstRow = new Span<byte>((byte*)transferPointer + (long)i * (long)_uploadArrayElementSize + j * (long)_uploadResourceRowSize, rowBytes);
                srcRow.CopyTo(dstRow);
            }

            _textureGroup[i].WICBitmapSource = null;
        }
        _textureArrayUploadResource.Managed.Unmap(0, default(D3D12_RANGE?));
        ArrayPool<byte>.Shared.Return(textureData);


        var placedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT[_textureGroup.Length];
        var defaultResourceDesc = _textureArrayDefaultResource.Managed.GetDesc();

        _d3d12Device.GetCopyableFootprints(
            defaultResourceDesc,
            0,
            0,
            placedFootprint);

        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);

        for (int i = 0; i < _textureGroup.Length; i++) {
            var dstLocation = new D3D12_TEXTURE_COPY_LOCATION() {
                Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                Anonymous = new() { SubresourceIndex = (uint)i },
                pResource = (ID3D12Resource_unmanaged*)_textureArrayDefaultResource.Ptr,
            };

            var srcLocation = new D3D12_TEXTURE_COPY_LOCATION() {
                Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                Anonymous = new() { PlacedFootprint = placedFootprint[i] },
                pResource = (ID3D12Resource_unmanaged*)_textureArrayUploadResource.Ptr,
            };

            _commandList.CopyTextureRegion(dstLocation, 0, 0, 0, srcLocation, default(D3D12_BOX?));
        }

        var barrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
            Anonymous = new() {
                Transition = new() {
                    pResource = (ID3D12Resource_unmanaged*)_textureArrayDefaultResource.Ptr,
                    StateBefore = D3D12_RESOURCE_STATE_COPY_DEST,
                    StateAfter = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
                }
            }
        };
        _commandList.ResourceBarrier([barrier]);

        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);

        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);

        WaitForSingleObject(_renderEvent, INFINITE);

    }


    private void STEP15_CreateSRVHeap() {
        var srvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 1,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
            Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
        };

        _d3d12Device.CreateDescriptorHeap(srvHeapDesc, out _srvHeap);
    }

    private void STEP16_CreateTextureArraySRV() {

        var srvDescriptorDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC() {
            ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2DARRAY,
            Format = _textureFormat,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Anonymous = new() {
                Texture2DArray = new() {
                    FirstArraySlice = 0,
                    ArraySize = (uint)_textureGroup.Length,
                    MipLevels = 1
                }
            },
        };

        _srvTextureArrayCPUHandle = _srvHeap.GetCPUDescriptorHandleForHeapStart();
        _srvTextureArrayGPUHandle = _srvHeap.GetGPUDescriptorHandleForHeapStart();

        _d3d12Device.CreateShaderResourceView(_textureArrayDefaultResource.Managed, srvDescriptorDesc, _srvTextureArrayCPUHandle);
    }

    private void STEP17_CreateStructuredBufferResource() {

        var structuredBufferUploadDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(_blockCubeTextureIndexGroup.Length * Unsafe.SizeOf<CubeFace>()),
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            UploadHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            structuredBufferUploadDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _structuredBufferUploadResource);

        var structuredBufferDefaultDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(_blockCubeTextureIndexGroup.Length * Unsafe.SizeOf<CubeFace>()),
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            DefaultHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            structuredBufferDefaultDesc,
            D3D12_RESOURCE_STATE_COPY_DEST,
            null,
            out _structuredBufferDefaultResource);
    }

    private unsafe void STEP18_CopyStructuredBufferToDefaultResource() {
        _structuredBufferUploadResource.Map(0, null, out var transferPointer);

        var cubeFaceDataSpan = new Span<CubeFace>(transferPointer, _blockCubeTextureIndexGroup.Length);

        _blockCubeTextureIndexGroup.AsSpan().CopyTo(cubeFaceDataSpan);

        _structuredBufferUploadResource.Unmap(0, default(D3D12_RANGE?));

        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);

        _commandList.CopyBufferRegion(_structuredBufferDefaultResource, 0, _structuredBufferUploadResource, 0, (ulong)(Unsafe.SizeOf<CubeFace>() * _blockCubeTextureIndexGroup.Length));

        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);

        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);

        // 下一个等待就是 RenderLoop 的 MsgWaitForMultipleObjects，不需要用 WaitForSingleObject 了
        // 这里再用一次 WaitForSingleObject 就会使事件变成无信号 (CreateEvent 第二个参数)
        // 导致在 MsgWaitForMultipleObjects 那里卡死，永远返回 1，窗口白屏，完全进不去 case 0 渲染函数
    }

    private void StartCommandRecord() {
        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);
    }

    private void StartCommandExecute() {
        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }


    private unsafe void STEP19_CreateRootSignature() {

        // 根参数 + 静态采样器列表
        // Para 0: (Type = Root Descriptor,  2 DWORD)  (b0, space0) CBV 根描述符，用于 MVP 缓冲
        // Para 1: (Type = Root Descriptor,  2 DWORD)  (t0, space0) SRV 根描述符，用于结构化缓冲区
        // Para 2: (Type = Descriptor Table, 1 DWORD)  (t1, space0) SRV 描述符表，用于纹理数组
        // 
        // Sampler 0: (Type = Static Sampler) (s0, space0) 静态采样器 (邻近点过滤)，用于纹理数组采样

        var rootParameters = stackalloc D3D12_ROOT_PARAMETER[3];

        // 把更新频率高的根参数放前面，低的放后面，可以优化性能 (微软官方文档建议)
        // 因为 DirectX API 能对根签名进行 Version Control 版本控制，在根签名越前面的根参数，访问速度更快

        var cbvRootDescriptorDesc = new D3D12_ROOT_DESCRIPTOR() {
            ShaderRegister = 0,
            RegisterSpace = 0,
        };

        rootParameters[0] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            Anonymous = new() { Descriptor = cbvRootDescriptorDesc },
        };


        var srvRootDescriptorDesc = new D3D12_ROOT_DESCRIPTOR() {
            ShaderRegister = 0,
            RegisterSpace = 0,
        };

        rootParameters[1] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_SRV,
            Anonymous = new() { Descriptor = srvRootDescriptorDesc },
        };


        var srvDescriptorRangeDesc = new D3D12_DESCRIPTOR_RANGE() {
            RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
            NumDescriptors = 1,
            BaseShaderRegister = 1,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0,
        };

        var rootDescriptorTableDesc = new D3D12_ROOT_DESCRIPTOR_TABLE() {
            NumDescriptorRanges = 1,
            pDescriptorRanges = &srvDescriptorRangeDesc,
        };

        rootParameters[2] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            Anonymous = new() { DescriptorTable = rootDescriptorTableDesc },
        };


        var staticSamplerDesc = new D3D12_STATIC_SAMPLER_DESC() {
            ShaderRegister = 0,
            RegisterSpace = 0,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
            Filter = D3D12_FILTER_MIN_MAG_MIP_POINT,
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
            NumParameters = 3,
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
            out var errorBlob);

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

    private unsafe void STEP20_CreatePSO() {
        var psoDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC();

        var inputLayoutDesc = new D3D12_INPUT_LAYOUT_DESC();
        var inputElementDesc = stackalloc D3D12_INPUT_ELEMENT_DESC[5];

        inputElementDesc[0] = new() {
            SemanticName = Position,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32B32A32_FLOAT,
            InputSlot = 0,
            AlignedByteOffset = 0,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        inputElementDesc[1] = new() {
            SemanticName = TexCoord,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32_FLOAT,
            InputSlot = 0,
            AlignedByteOffset = 16,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        inputElementDesc[2] = new() {
            SemanticName = FaceIndex,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32_UINT,
            InputSlot = 0,
            AlignedByteOffset = 24,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };


        inputElementDesc[3] = new() {
            SemanticName = BlockOffset,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32B32_FLOAT,
            InputSlot = 1,
            AlignedByteOffset = 0,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_INSTANCE_DATA,
            InstanceDataStepRate = 1,
        };

        inputElementDesc[4] = new() {
            SemanticName = BlockType,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32_UINT,
            InputSlot = 1,
            AlignedByteOffset = 12,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_INSTANCE_DATA,
            InstanceDataStepRate = 1,
        };

        inputLayoutDesc.NumElements = 5;
        inputLayoutDesc.pInputElementDescs = inputElementDesc;
        psoDesc.InputLayout = inputLayoutDesc;


        D3DCompileFromFile(
            "RenderShader.hlsl",
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
            out var errorBlobVS);

        if (errorBlobVS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobVS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        D3DCompileFromFile(
            "RenderShader.hlsl",
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
            out var errorBlobPS);

        if (errorBlobPS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobPS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        psoDesc.VS.pShaderBytecode = vertexShaderBlob.GetBufferPointer();
        psoDesc.VS.BytecodeLength = vertexShaderBlob.GetBufferSize();
        psoDesc.PS.pShaderBytecode = pixelShaderBlob.GetBufferPointer();
        psoDesc.PS.BytecodeLength = pixelShaderBlob.GetBufferSize();

        psoDesc.RasterizerState.CullMode = D3D12_CULL_MODE_NONE;
        psoDesc.RasterizerState.FillMode = D3D12_FILL_MODE_SOLID;

        psoDesc.pRootSignature = (ID3D12RootSignature_unmanaged*)_rootSignature.Ptr;

        psoDesc.DSVFormat = _dsvFormat;
        psoDesc.DepthStencilState.DepthEnable = true;
        psoDesc.DepthStencilState.DepthFunc = D3D12_COMPARISON_FUNC_LESS;
        psoDesc.DepthStencilState.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;

        psoDesc.BlendState.RenderTarget._0.BlendEnable = true;

        psoDesc.BlendState.RenderTarget._0.SrcBlend = D3D12_BLEND_SRC_ALPHA;
        psoDesc.BlendState.RenderTarget._0.DestBlend = D3D12_BLEND_INV_SRC_ALPHA;
        psoDesc.BlendState.RenderTarget._0.BlendOp = D3D12_BLEND_OP_ADD;

        psoDesc.BlendState.RenderTarget._0.SrcBlendAlpha = D3D12_BLEND_ONE;
        psoDesc.BlendState.RenderTarget._0.DestBlendAlpha = D3D12_BLEND_ZERO;
        psoDesc.BlendState.RenderTarget._0.BlendOpAlpha = D3D12_BLEND_OP_ADD;


        psoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
        psoDesc.NumRenderTargets = 1;
        psoDesc.RTVFormats._0 = DXGI_FORMAT_R8G8B8A8_UNORM;
        psoDesc.BlendState.RenderTarget._0.RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE_ALL;
        psoDesc.SampleDesc.Count = 1;
        psoDesc.SampleMask = uint.MaxValue;

        _d3d12Device.CreateGraphicsPipelineState(psoDesc, out _renderBlockPSO);
    }

    private unsafe void STEP21_CreatePerVertexAndIndexBuffer() {

        var vertexResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(_perBlockVertexData.Length * Unsafe.SizeOf<Vertex>()),
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            UploadHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            vertexResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _blockVertexResource);


        var indexResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(_perBlockIndexData.Length * sizeof(uint)),
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            UploadHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            indexResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _blockIndexResource);


        _blockVertexResource.Map(0, null, out var vertexPointer);
        _perBlockVertexData.AsSpan().CopyTo(new(vertexPointer, _perBlockVertexData.Length));
        _blockVertexResource.Unmap(0, default(D3D12_RANGE?));

        _blockIndexResource.Map(0, null, out var indexPointer);
        _perBlockIndexData.AsSpan().CopyTo(new(indexPointer, _perBlockIndexData.Length));
        _blockIndexResource.Unmap(0, default(D3D12_RANGE?));


        _vertexBufferView[0] = new() {
            BufferLocation = _blockVertexResource.GetGPUVirtualAddress(),
            StrideInBytes = (uint)Unsafe.SizeOf<Vertex>(),
            SizeInBytes = (uint)(_perBlockVertexData.Length * Unsafe.SizeOf<Vertex>()),
        };

        _indexBufferView = new() {
            BufferLocation = _blockIndexResource.GetGPUVirtualAddress(),
            Format = DXGI_FORMAT_R32_UINT,
            SizeInBytes = (uint)(_perBlockIndexData.Length * sizeof(uint)),
        };
    }

    private unsafe void STEP22_CreatePerInstanceBuffer() {

        var random = new Random();

        _blockGroup.EnsureCapacity(5 * 9 * 25);

        for (int y = -12; y <= 12; y += 6)
            for (int z = -24; z <= 24; z += 6)
                for (int x = 0; x < 25; x++) {
                    float blockX = 2 * x - 25;
                    float blockY = y + 2 * (x % 3) - 2;
                    float blockZ = z;
                    uint blockTypeIndex = (uint)random.Next(_blockCubeTextureIndexGroup.Length);

                    _blockGroup.Add(new(new(blockX, blockY, blockZ), blockTypeIndex));
                }


        var instanceResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(_blockGroup.Count * Unsafe.SizeOf<BlockInstance>()),
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            UploadHeapDesc,
            D3D12_HEAP_FLAG_NONE,
            instanceResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _blockInstanceResource);

        _blockInstanceResource.Map(0, null, out var instancePointer);
        CollectionsMarshal.AsSpan(_blockGroup).CopyTo(new(instancePointer, _blockGroup.Count));
        _blockInstanceResource.Unmap(0, default(D3D12_RANGE?));


        _vertexBufferView[1] = new() {
            BufferLocation = _blockInstanceResource.GetGPUVirtualAddress(),
            StrideInBytes = (uint)Unsafe.SizeOf<BlockInstance>(),
            SizeInBytes = (uint)(_blockGroup.Count * Unsafe.SizeOf<BlockInstance>()),
        };
    }

    private unsafe void UpdateConstantBuffer() {
        Unsafe.AsRef<CBuffer>((void*)_mvpBuffer).MVPMatrix = _firstCamera.MVPMatrix;
    }

    private unsafe void Render() {
        UpdateConstantBuffer();


        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _frameIndex = _dxgiSwapChain.GetCurrentBackBufferIndex();
        _rtvHandle.ptr += _frameIndex * _rtvDescriptorSize;


        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator);

        _beginBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_beginBarrier]);

        _commandList.RSSetViewports([_viewPort]);
        _commandList.RSSetScissorRects([_scissorRect]);

        _commandList.OMSetRenderTargets(1, _rtvHandle, false, _dsvHandle);
        _commandList.ClearDepthStencilView(_dsvHandle, D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0);
        _commandList.ClearRenderTargetView(_rtvHandle, SkyBlue);

        _commandList.SetGraphicsRootSignature(_rootSignature.Managed);
        _commandList.SetPipelineState(_renderBlockPSO);

        _commandList.SetGraphicsRootConstantBufferView(0, _cbvResource.GetGPUVirtualAddress());
        _commandList.SetGraphicsRootShaderResourceView(1, _structuredBufferDefaultResource.GetGPUVirtualAddress());
        _commandList.SetDescriptorHeaps([_srvHeap]);
        _commandList.SetGraphicsRootDescriptorTable(2, _srvTextureArrayGPUHandle);

        _commandList.IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        _commandList.IASetVertexBuffers(0, _vertexBufferView);
        _commandList.IASetIndexBuffer(_indexBufferView);
        _commandList.DrawIndexedInstanced((uint)_perBlockIndexData.Length, (uint)_blockGroup.Count, 0, 0, 0);

        _endBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_endBarrier]);

        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);

        _dxgiSwapChain.Present(1, 0);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void STEP23_RenderLoop() {
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
                var ch = (char)(nuint)wParam;
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
        engine.STEP01_InitWindow(hInstance);
        engine.STEP02_CreateDebugDevice();
        engine.STEP03_CreateDevice();
        engine.STEP04_CreateCommandComponents();
        engine.STEP05_CreateRenderTarget();
        engine.STEP06_CreateFenceAndBarrier();
        engine.STEP07_CreateDSVHeap();
        engine.STEP08_CreateDepthStencilBuffer();
        engine.STEP09_CreateDSV();
        engine.STEP10_CreateCameraCBVResource();

        engine.STEP11_LoadTextureGroup();
        engine.STEP12_GetTextureArrayElementsProperties();
        engine.STEP13_CreateTextureArrayResource();
        engine.STEP14_CopyTextureArrayToDefaultResource();
        engine.STEP15_CreateSRVHeap();
        engine.STEP16_CreateTextureArraySRV();

        engine.STEP17_CreateStructuredBufferResource();
        engine.STEP18_CopyStructuredBufferToDefaultResource();

        engine.STEP19_CreateRootSignature();
        engine.STEP20_CreatePSO();
        engine.STEP21_CreatePerVertexAndIndexBuffer();
        engine.STEP22_CreatePerInstanceBuffer();

        engine.STEP23_RenderLoop();
    }

}

internal static class Program {

    [STAThread]
    static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }
}