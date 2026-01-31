// https://blog.csdn.net/DGAF2198588973/article/details/155771199

// 鸣谢原作者大大: close2animation (https://sketchfab.com/close2animation)
// 模型项目地址: https://sketchfab.com/3d-models/kurumi-model-f776c64883414c71b9441b8c45342533

using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Assimp;
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

namespace DXDemo11;

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
    private float _focalLength;

    // 摄像机向右方向的单位向量，用于左右移动
    private Vector3 _rightDirection;

    private Point _lastCursorPoint;         // 上一次鼠标的位置

    private const float FovAngleY = MathF.PI / 4.0f;   // 垂直视场角
    private const float AspectRatio = 16f / 9f;   // 投影窗口宽高比
    private const float NearZ = 0.1f;            // 近平面到原点的距离
    private const float FarZ = 1000f;            // 远平面到原点的距离

    // 模型矩阵，模型空间 -> 世界空间
    private Matrix4x4 _modelMatrix;
    // 观察矩阵，注意前两个参数是点，第三个参数才是向量
    private Matrix4x4 _viewMatrix;
    // 投影矩阵(注意近平面和远平面距离不能 <= 0!)
    private Matrix4x4 _projectionMatrix;

    internal Matrix4x4 MVPMatrix {
        get {
            _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection);
            return _modelMatrix * _viewMatrix * _projectionMatrix; // MVP 矩阵
        }
    }

    internal Camera() {
        // 模型矩阵，这里设置成单位矩阵，是因为模型导入的时候已经是 y 轴朝上的了，无需再进行旋转
        _modelMatrix = Matrix4x4.Identity;
        _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection); // 观察矩阵，世界空间 -> 观察空间
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(FovAngleY, AspectRatio, NearZ, FarZ); // 投影矩阵，观察空间 -> 齐次裁剪空间

        _eyePosition = new Vector3(1, 1, 1);
        _focusPosition = new Vector3(0, 0, 0);
        _upDirection = new Vector3(0, 1, 0);

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

    // 设置摄像机位置
    internal void SetEyePosition(Vector3 pos) {
        _eyePosition = pos;

        // 改变位置后，观察向量、焦距、右方向向量也要改变，否则会发生视角瞬移
        _viewDirection = Vector3.Normalize(_focusPosition - _eyePosition);
        _focalLength = Vector3.Distance(_focusPosition, _eyePosition);
        _rightDirection = Vector3.Normalize(Vector3.Cross(_viewDirection, _upDirection));
    }

    // 设置摄像机焦点
    internal void SetFocusPosition(Vector3 pos) {
        _focusPosition = pos;

        // 改变焦点后，观察向量、焦距、右方向向量也要改变，否则会发生视角瞬移
        _viewDirection = Vector3.Normalize(_focusPosition - _eyePosition);
        _focalLength = Vector3.Distance(_focusPosition, _eyePosition);
        _rightDirection = Vector3.Normalize(Vector3.Cross(_viewDirection, _upDirection));
    }
}

[InlineArray(4)]
internal struct Buffer4<T> where T : unmanaged {
    private T _element0;
}

[InlineArray(512)]
internal struct Buffer512<T> where T : unmanaged {
    private T _element0;
}

internal struct Vertex {
    internal Vector4 Position;
    internal Vector2 TexCoordUV;
    internal Vector4 Color;
    internal Buffer4<int> BoneIndices;
    internal Buffer4<float> BoneWeights;
}

internal sealed class Material {
    internal string FilePath;
    internal TextureType Type;
    internal ComPtr<ID3D12Resource> UploadTexture;
    internal ComPtr<ID3D12Resource> DefaultTexture;

    internal D3D12_CPU_DESCRIPTOR_HANDLE CPUHandle;
    internal D3D12_GPU_DESCRIPTOR_HANDLE GPUHandle;
}

internal struct Mesh {
    internal int MaterialIndex;

    internal int VertexGroupOffset;
    internal uint VertexCount;
    internal uint IndexGroupOffset;
    internal uint IndexCount;
}

internal struct AABB {
    internal float MinBoundsX;
    internal float MinBoundsY;
    internal float MinBoundsZ;
    internal float MaxBoundsX;
    internal float MaxBoundsY;
    internal float MaxBoundsZ;
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
    private readonly DXGI_FORMAT _dsvFormat = DXGI_FORMAT_D24_UNORM_S8_UINT;
    private ID3D12Resource _depthStencilBuffer;

    private const string ModelFileName = "kurumi/scene.gltf";
    private const string ModelTextureFilePath = "kurumi/";
    private nint _modelScene;

    // 导入模型使用的标志
    // ConvertToLeftHanded: Assimp 导入的模型是以 OpenGL 的右手坐标系为基础的，将模型转换成 DirectX 的左手坐标系
    // Triangulate：模型设计师可能使用多边形对模型进行建模的，对于用多边形建模的模型，将它们都转换成基于三角形建模
    // FixInfacingNormals：建模软件都是双面显示的，所以设计师不会在意顶点绕序方向，部分面会被剔除无法正常显示，需要翻转过来
    // LimitBoneWeights: 限制网格的骨骼权重最多为 4 个，其余权重无需处理
    // GenBoundBoxes: 对每个网格，都生成一个 AABB 体积盒
    // JoinIdenticalVertices: 将位置相同的顶点合并为一个顶点，从而减少模型的顶点数量，优化内存使用和提升渲染效率。
    private const uint ModelImportFlag = (uint)(PostProcessSteps.ConvertToLeftHanded | PostProcessSteps.Triangulate | PostProcessSteps.FixInfacingNormals |
        PostProcessSteps.LimitBoneWeights | PostProcessSteps.GenBoundingBoxes | PostProcessSteps.JoinIdenticalVertices);

    private readonly List<Material> _materialGroup = [];

    private readonly Dictionary<string, int> _boneNodeIndexGroup = [];

    private readonly List<Matrix4x4> _boneNodeTransformGroup = [];

    private readonly List<Vertex> _vertexGroup = [];
    private readonly List<int> _vertexWeightsCountGroup = [];
    private readonly List<uint> _indexGroup = [];

    private readonly List<Mesh> _meshGroup = [];

    private AABB _modelBoundingBox;

    private ID3D12DescriptorHeap _srvHeap;

    private IWICImagingFactory _wicFactory;
    private IWICBitmapDecoder _wicBitmapDecoder;
    private IWICBitmapFrameDecode _wicBitmapDecoderFrame;
    private IWICFormatConverter _wicFormatConverter;
    private IWICBitmapSource _wicBitmapSource;
    private DXGI_FORMAT _textureFormat = DXGI_FORMAT_UNKNOWN;
    private uint _textureWidth;
    private uint _textureHeight;
    private uint _bitsPerPixel;

    private uint _bytesPerRowSize;
    private uint _textureSize;
    private uint _uploadResourceRowSize;
    private uint _uploadResourceSize;

    private ID3D12Resource _uploadVertexResource;
    private ID3D12Resource _uploadIndexResource;
    private ID3D12Resource _defaultVertexResource;
    private ID3D12Resource _defaultIndexResource;

    private D3D12_VERTEX_BUFFER_VIEW _vertexBufferView;
    private D3D12_INDEX_BUFFER_VIEW _indexBufferView;

    private struct CBuffer {
        internal Matrix4x4 MVPMatrix;
        internal Buffer512<Matrix4x4> BoneTransformMatrix;
    }
    private ID3D12Resource _cbvResource;
    private nint mvpBuffer;

    private ComPtr<ID3D12RootSignature> _rootSignature;

    private static readonly PCSTR Position = CreatePCSTR("POSITION");
    private static readonly PCSTR TexCoord = CreatePCSTR("TEXCOORD");
    private static readonly PCSTR Color = CreatePCSTR("COLOR");
    private static readonly PCSTR BlendIndices = CreatePCSTR("BLENDINDICES");
    private static readonly PCSTR BlendWeight = CreatePCSTR("BLENDWEIGHT");

    private ID3D12PipelineState _pipelineStateObject;

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

    private unsafe void STEP1_InitWindow(SafeHandle hInstance) {
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
    private void STEP2_CreateDebugDevice() {
        // [STAThread] attribute on Main method handles this
        //CoInitialize();

        D3D12GetDebugInterface(out _d3d12DebugDevice).ThrowOnFailure();
        _d3d12DebugDevice.EnableDebugLayer();

        _dxgiCreateFactoryFlag = DXGI_CREATE_FACTORY_DEBUG;
    }

    private bool STEP3_CreateDevice() {
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

    private void STEP4_CreateCommandComponents() {
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

    private void STEP5_CreateRenderTarget() {
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

    private void STEP6_CreateFenceAndBarrier() {
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

    private void STEP7_CreateDSVHeap() {
        var dsvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 1,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
        };

        _d3d12Device.CreateDescriptorHeap(dsvHeapDesc, out _dsvHeap);

        _dsvHandle = _dsvHeap.GetCPUDescriptorHandleForHeapStart();
    }

    private void STEP8_CreateDepthStencilBuffer() {
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
            Anonymous = new() {
                DepthStencil = new() {
                    Depth = 1.0f,
                    Stencil = 0,
                }
            }
        };

        _d3d12Device.CreateCommittedResource(
            new D3D12_HEAP_PROPERTIES() {
                Type = D3D12_HEAP_TYPE_DEFAULT,
            },
            D3D12_HEAP_FLAG_NONE,
            dsvResourceDesc,
            D3D12_RESOURCE_STATE_DEPTH_WRITE,
            depthStencilBufferClearValue,
            out _depthStencilBuffer);
    }

    private void STEP9_CreateDSV() {
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

    private unsafe bool STEP10_OpenModelFile() {
        _modelScene = (IntPtr)ImportFile(ModelFileName, ModelImportFlag);
        ref var modelScene = ref Unsafe.AsRef<Scene>(_modelScene);

        if (Unsafe.IsNullRef(ref modelScene) || (modelScene.mFlags & AI_SCENE_FLAGS_INCOMPLETE) != 0 || modelScene.mRootNode == null) {
            var assimpErrorMsg = GetErrorString();
            var errorMsg = $"载入文件 {ModelFileName} 失败！错误原因：{assimpErrorMsg}";

            MessageBox(default, errorMsg, "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            return false;
        }

        return true;
    }

    private void STEP11_AddModelMaterials() {
        ref var modelScene = ref Unsafe.AsRef<Scene>(_modelScene);

        foreach (ref var material in modelScene.Materials) {

            if (GetMaterialTexture(material, TextureType.EMISSIVE, 0, out var textureFilePath) == ReturnCode.SUCCESS) {
                var mt = new Material() {
                    FilePath = ModelTextureFilePath + textureFilePath,
                    Type = TextureType.EMISSIVE,
                };
                _materialGroup.Add(mt);
            } else if (GetMaterialTexture(material, TextureType.DIFFUSE, 0, out textureFilePath) == ReturnCode.SUCCESS) {
                var mt = new Material() {
                    FilePath = ModelTextureFilePath + textureFilePath,
                    Type = TextureType.DIFFUSE,
                };
                _materialGroup.Add(mt);
            } else {
                var mt = new Material() {
                    Type = TextureType.NONE,
                };
                _materialGroup.Add(mt);
            }
        }
    }

    private void CalcModelNodeMatrix(ref Node node, Matrix4x4 parentNodeTransform) {

        var currentTransformMatrix = (Matrix4x4)node.mTransformation;

        currentTransformMatrix = parentNodeTransform * currentTransformMatrix;

        var boneName = node.mName.ToString();

        _boneNodeTransformGroup.Add(currentTransformMatrix);
        _boneNodeIndexGroup[boneName] = _boneNodeTransformGroup.Count - 1;

        // 注意！有些网格没有绑定到骨骼上，但是它们属于某个骨骼节点，仍然是需要进行骨骼变换的，否则会出错
        // 没有骨骼的下文 mNumBones 会是 0，需要特殊处理，这里要先添加并更新节点影响网格对应的名字 _Mesh_i
        // 在 Assimp 中，如果网格没有绑定到骨骼上，一个网格只会对应一个节点，所以下文没有骨骼的网格会用 _Mesh_i 索引
        foreach (var meshIndex in node.Meshes) {
            _boneNodeIndexGroup[$"_Mesh_{meshIndex}"] = _boneNodeTransformGroup.Count - 1;
        }

        foreach (ref var childNode in node.Children) {
            CalcModelNodeMatrix(ref childNode, currentTransformMatrix);
        }
    }

    private void STEP12_AddModelData() {
        ref var modelScene = ref Unsafe.AsRef<Scene>(_modelScene);

        var modelMatrix = Matrix4x4.Identity;

        CalcModelNodeMatrix(ref modelScene.RootNode, modelMatrix);

        int currentMeshVertexGroupOffset = 0;
        uint currentMeshIndexGroupOffset = 0;

        for (int i = 0; i < modelScene.mNumMeshes; i++) {
            ref var mesh = ref modelScene.Meshes[i];

            if (mesh.mNumVertices == 0)
                continue;

            for (int j = 0; j < mesh.mNumVertices; j++) {
                var newVertex = new Vertex {
                    Position = new(mesh.Vertices[j], 1.0f)
                };

                // 新节点纹理 UV，如果有就添加，没有就默认 (-1, -1)
                // 注意这个 0 指的是第 0 号 UV 通道 (详情请见 UE5 文档: UV 通道)
                // 对于同一个顶点，不同的 UV 通道可以有不同的 UV 坐标，常用于光照，但我们这里不涉及，直接获取第 0 号纹理 UV 即可
                if (mesh.HasTextureCoords(0)) {
                    newVertex.TexCoordUV = new(mesh.TextureCoords(0)[j].x, mesh.TextureCoords(0)[j].y);
                } else {
                    newVertex.TexCoordUV = new(-1.0f, -1.0f); // 默认纹理 UV 坐标，Pixel Shader 会进行处理
                }

                if (mesh.HasVertexColors(0)) {
                    newVertex.Color = mesh.Colors(0)[j];
                } else {
                    newVertex.Color = new(1.0f, 1.0f, 1.0f, 1.0f);
                }

                _vertexWeightsCountGroup.Add(0);

                _vertexGroup.Add(newVertex);
            }

            for (int j = 0; j < mesh.mNumFaces; j++) {
                _indexGroup.Add(mesh.Faces[j].Indices[0]);
                _indexGroup.Add(mesh.Faces[j].Indices[1]);
                _indexGroup.Add(mesh.Faces[j].Indices[2]);
            }

            if (mesh.HasBones()) {

                foreach (ref var currentBone in mesh.Bones) {

                    int boneIndex = _boneNodeIndexGroup[currentBone.mName.ToString()];

                    var meshToBoneSpaceMatrix = (Matrix4x4)currentBone.mOffsetMatrix;

                    var finalSkinnedMeshIndex = _boneNodeTransformGroup.Count;
                    _boneNodeIndexGroup[$"_Mesh_{i}_To_Bone_{boneIndex}"] = finalSkinnedMeshIndex;

                    meshToBoneSpaceMatrix = _boneNodeTransformGroup[boneIndex] * meshToBoneSpaceMatrix;

                    _boneNodeTransformGroup.Add(meshToBoneSpaceMatrix);

                    foreach (var vertexWeight in currentBone.Weights) {
                        var vertexId = currentMeshVertexGroupOffset + vertexWeight.mVertexId;
                        var weight = vertexWeight.mWeight;
                        var weightsCount = _vertexWeightsCountGroup[(int)vertexId];

                        var verticesSpan = CollectionsMarshal.AsSpan(_vertexGroup);
                        verticesSpan[(int)vertexId].BoneIndices[weightsCount] = finalSkinnedMeshIndex;
                        verticesSpan[(int)vertexId].BoneWeights[weightsCount] = weight;
                        _vertexWeightsCountGroup[(int)vertexId]++;
                    }
                }

            } else {
                var boneIndex = _boneNodeIndexGroup[$"_Mesh_{i}"];

                for (int j = 0; j < mesh.mNumVertices; j++) {
                    var vertexId = currentMeshVertexGroupOffset + j;
                    var weight = 1.0f;
                    var weightsCount = _vertexWeightsCountGroup[vertexId];

                    var verticesSpan = CollectionsMarshal.AsSpan(_vertexGroup);
                    verticesSpan[vertexId].BoneIndices[weightsCount] = boneIndex;
                    verticesSpan[vertexId].BoneWeights[weightsCount] = weight;
                    _vertexWeightsCountGroup[vertexId]++;
                }
            }

            var newMesh = new Mesh() {
                MaterialIndex = (int)mesh.mMaterialIndex,

                VertexGroupOffset = currentMeshVertexGroupOffset,
                VertexCount = mesh.mNumVertices,
                IndexGroupOffset = currentMeshIndexGroupOffset,
                IndexCount = mesh.mNumFaces * 3,
            };

            currentMeshVertexGroupOffset += (int)mesh.mNumVertices;
            currentMeshIndexGroupOffset += mesh.mNumFaces * 3;

            _meshGroup.Add(newMesh);

        }

        // 读取所有骨骼、网格数据完成后，对 BoneNode_TransformGroup 里面的所有矩阵进行转置，不转会渲染错误
        // 因为在 shader 中我们指定了 row_major 让 GPU 按行读取矩阵，但从 Assimp 获取并变换的矩阵是列主序的
        // 我们需要使用 XMMatrixTranspose 转置这些矩阵，让这些矩阵变成行主序
        for (int i = 0; i < _boneNodeTransformGroup.Count; i++) {
            _boneNodeTransformGroup[i] = Matrix4x4.Transpose(_boneNodeTransformGroup[i]);
        }
    }

    private void STEP13_CalcModelBoundingBox() {
        ref var modelScene = ref Unsafe.AsRef<Scene>(_modelScene);

        // 初始化包围盒
        _modelBoundingBox = new() {
            MinBoundsX = float.MaxValue,
            MinBoundsY = float.MaxValue,
            MinBoundsZ = float.MaxValue,

            MaxBoundsX = float.MinValue,
            MaxBoundsY = float.MinValue,
            MaxBoundsZ = float.MinValue,
        };

        foreach (ref var mesh in modelScene.Meshes) {

            _modelBoundingBox.MinBoundsX = MathF.Min(_modelBoundingBox.MinBoundsX, mesh.mAABB.mMin.x);
            _modelBoundingBox.MinBoundsY = MathF.Min(_modelBoundingBox.MinBoundsY, mesh.mAABB.mMin.y);
            _modelBoundingBox.MinBoundsZ = MathF.Min(_modelBoundingBox.MinBoundsZ, mesh.mAABB.mMin.z);

            _modelBoundingBox.MaxBoundsX = MathF.Max(_modelBoundingBox.MaxBoundsX, mesh.mAABB.mMax.x);
            _modelBoundingBox.MaxBoundsY = MathF.Max(_modelBoundingBox.MaxBoundsY, mesh.mAABB.mMax.y);
            _modelBoundingBox.MaxBoundsZ = MathF.Max(_modelBoundingBox.MaxBoundsZ, mesh.mAABB.mMax.z);
        }

        var centerPoint = new Vector3(
            (_modelBoundingBox.MinBoundsX + _modelBoundingBox.MaxBoundsX) / 2.0f,
            (_modelBoundingBox.MinBoundsY + _modelBoundingBox.MaxBoundsY) / 2.0f,
            (_modelBoundingBox.MinBoundsZ + _modelBoundingBox.MaxBoundsZ) / 2.0f);

        var radiusX = (_modelBoundingBox.MaxBoundsX - _modelBoundingBox.MinBoundsX) / 2.0f;
        var radiusY = (_modelBoundingBox.MaxBoundsY - _modelBoundingBox.MinBoundsY) / 2.0f;
        var radiusZ = (_modelBoundingBox.MaxBoundsZ - _modelBoundingBox.MinBoundsZ) / 2.0f;

        var radius = MathF.Sqrt(radiusX * radiusX + radiusY * radiusY + radiusZ * radiusZ) / 2.0f;

        _firstCamera.SetFocusPosition(centerPoint);
        _firstCamera.SetEyePosition(centerPoint with { Z = centerPoint.Z - radius });
    }

    private void CreateSRVHeap() {
        var srvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = (uint)_materialGroup.Count,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
            Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
        };

        _d3d12Device.CreateDescriptorHeap(srvHeapDesc, out _srvHeap);
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

    private void CreateUploadAndDefaultResource(int index) {
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
        _materialGroup[index].UploadTexture = new(uploadTextureResource);


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
        _materialGroup[index].DefaultTexture = new(defaultTextureResource);
    }

    private unsafe void CopyTextureDataToDefaultResource(int index) {
        var textureData = ArrayPool<byte>.Shared.Rent((int)_textureSize);

        _wicBitmapSource.CopyPixels(default, _bytesPerRowSize, textureData);

        _materialGroup[index].UploadTexture.Managed.Map(0, null, out var transferPointer);

        int rowBytes = (int)_bytesPerRowSize;
        ReadOnlySpan<byte> allSrcData = textureData;
        byte* dstBasePtr = (byte*)transferPointer;
        for (int i = 0; i < _textureHeight; i++) {
            var srcRow = allSrcData.Slice(i * rowBytes, rowBytes);
            var dstRow = new Span<byte>(dstBasePtr + i * _uploadResourceRowSize, rowBytes);
            srcRow.CopyTo(dstRow);
        }

        _materialGroup[index].UploadTexture.Managed.Unmap(0, default(D3D12_RANGE?));

        ArrayPool<byte>.Shared.Return(textureData);

        var placedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT();
        var defaultResourceDesc = _materialGroup[index].DefaultTexture.Managed.GetDesc();

        _d3d12Device.GetCopyableFootprints(
            defaultResourceDesc,
            0,
            0,
            new(ref placedFootprint));

        var dstLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
            Anonymous = new() { SubresourceIndex = 0 },
            pResource = (ID3D12Resource_unmanaged*)_materialGroup[index].DefaultTexture.Ptr,
        };

        var srcLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
            Anonymous = new() { PlacedFootprint = placedFootprint },
            pResource = (ID3D12Resource_unmanaged*)_materialGroup[index].UploadTexture.Ptr,
        };

        _commandList.CopyTextureRegion(dstLocation, 0, 0, 0, srcLocation, default(D3D12_BOX?));
    }

    private void CreateDefaultTexture(int index) {

        var uploadResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = 512,
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
        _materialGroup[index].UploadTexture = new(uploadTextureResource);


        // 注意！默认纹理的纹理格式要选 DXGI_FORMAT_R8G8B8A8_UNORM！
        _textureFormat = DXGI_FORMAT_R8G8B8A8_UNORM;

        var defaultResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Width = 2,
            Height = 2,
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
        _materialGroup[index].DefaultTexture = new(defaultTextureResource);
    }

    private unsafe void CopyDefaultTextureToDefaultResource(int index) {
        Span<byte> defaultTextureData = stackalloc byte[2 * 2 * 4];

        for (int i = 0; i < 2 * 2; i++) {
            defaultTextureData[i * 4 + 0] = 255; // R
            defaultTextureData[i * 4 + 1] = 255; // G
            defaultTextureData[i * 4 + 2] = 255; // B
            defaultTextureData[i * 4 + 3] = 128; // A
        }

        _materialGroup[index].UploadTexture.Managed.Map(0, null, out var transferPointer);

        byte* dstBasePtr = (byte*)transferPointer;
        for (int i = 0; i < 2; i++) {
            var srcRow = defaultTextureData.Slice(i * 8, 8);
            var dstRow = new Span<byte>(dstBasePtr + i * 256, 8);
            srcRow.CopyTo(dstRow);
        }

        _materialGroup[index].UploadTexture.Managed.Unmap(0, default(D3D12_RANGE?));

        var placedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT();
        var defaultResourceDesc = _materialGroup[index].DefaultTexture.Managed.GetDesc();

        _d3d12Device.GetCopyableFootprints(
            defaultResourceDesc,
            0,
            0,
            new(ref placedFootprint));

        var dstLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
            Anonymous = new() { SubresourceIndex = 0 },
            pResource = (ID3D12Resource_unmanaged*)_materialGroup[index].DefaultTexture.Ptr,
        };

        var srcLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
            Anonymous = new() { PlacedFootprint = placedFootprint },
            pResource = (ID3D12Resource_unmanaged*)_materialGroup[index].UploadTexture.Ptr,
        };

        _commandList.CopyTextureRegion(dstLocation, 0, 0, 0, srcLocation, default(D3D12_BOX?));
    }

    private void CreateSRV(
        int index,
        D3D12_CPU_DESCRIPTOR_HANDLE cpuHandle,
        D3D12_GPU_DESCRIPTOR_HANDLE gpuHandle) {

        var srvDescriptorDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC() {
            ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D,
            Format = _textureFormat,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Anonymous = new() { Texture2D = new() { MipLevels = 1 } },
        };

        _d3d12Device.CreateShaderResourceView(_materialGroup[index].DefaultTexture.Managed, srvDescriptorDesc, cpuHandle);

        _materialGroup[index].CPUHandle = cpuHandle;
        _materialGroup[index].GPUHandle = gpuHandle;
    }

    private void StartCommandExecute() {
        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void STEP14_CreateModelTextureResource() {
        CreateSRVHeap();

        var currentCPUHandle = _srvHeap.GetCPUDescriptorHandleForHeapStart();
        var currentGPUHandle = _srvHeap.GetGPUDescriptorHandleForHeapStart();
        var srvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

        StartCommandRecord();

        for (var i = 0; i < _materialGroup.Count; i++) {
            if (_materialGroup[i].Type != TextureType.NONE) {
                LoadTextureFromFile(_materialGroup[i].FilePath);
                CreateUploadAndDefaultResource(i);
                CopyTextureDataToDefaultResource(i);
            } else {
                CreateDefaultTexture(i);
                CopyDefaultTextureToDefaultResource(i);
            }

            CreateSRV(i, currentCPUHandle, currentGPUHandle);

            currentCPUHandle.ptr += srvDescriptorSize;
            currentGPUHandle.ptr += srvDescriptorSize;
        }

        StartCommandExecute();


        // 让主线程强制等待命令队列的完成，WaitForSingleObject 会阻塞当前线程，直到 event 有信号，或达到指定时间
        // 因为我们复制完资源，还要执行复制顶点到默认堆的命令，这个需要重置命令分配器
        // 重置命令分配器的要求是：命令队列必须执行完分配器的命令，否则会发生资源竞争
        // 而 CPU，GPU 两者恰好是异步执行的，也就是说 CommandQueue->ExecuteCommandLists() 后 CPU 端仍然会继续执行
        // 所以我们在这里要阻塞 Main 函数所在的主线程，同步 CPU 与 GPU 的执行
        // 然而这里用 WaitForSingleObject 阻塞主线程并不是一个好的选择，第二个参数 INFINITE 设定等待时间无限，更是一种不好的做法
        // 如果要执行的命令数量多，执行耗时长，主线程一阻塞，而绑定窗口的消息回调恰好是在主线程上执行的，那么程序就会不幸的 gg 了
        // 后面的教程我们会将程序进行多线程优化，避免使用 WaitForSingleObject 同步，而是改用 Render 里的 MsgWaitForMultipleObjects
        WaitForSingleObject(_renderEvent, INFINITE);
    }

    private void STEP15_CreateMeshResourceAndDescriptor() {

        // 如果两边 (上传堆和默认堆) 的用途都是 buffer 线性缓冲，不需要再进行 256 字节对齐了

        var vertexResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Format = DXGI_FORMAT_UNKNOWN,
            Width = (ulong)(Unsafe.SizeOf<Vertex>() * _vertexGroup.Count),
            Height = 1,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1, Quality = 0 },
        };

        var indexResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Format = DXGI_FORMAT_UNKNOWN,
            Width = (ulong)(sizeof(uint) * _indexGroup.Count),
            Height = 1,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1, Quality = 0 },
        };

        var uploadHeapProperties = new D3D12_HEAP_PROPERTIES() {
            Type = D3D12_HEAP_TYPE_UPLOAD,
        };

        var defaultHeapProperties = new D3D12_HEAP_PROPERTIES() {
            Type = D3D12_HEAP_TYPE_DEFAULT,
        };


        _d3d12Device.CreateCommittedResource(
            uploadHeapProperties,
            D3D12_HEAP_FLAG_NONE,
            vertexResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _uploadVertexResource);

        _d3d12Device.CreateCommittedResource(
            defaultHeapProperties,
            D3D12_HEAP_FLAG_NONE,
            vertexResourceDesc,
            D3D12_RESOURCE_STATE_COPY_DEST,
            null,
            out _defaultVertexResource);

        _d3d12Device.CreateCommittedResource(
            uploadHeapProperties,
            D3D12_HEAP_FLAG_NONE,
            indexResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _uploadIndexResource);

        _d3d12Device.CreateCommittedResource(
            defaultHeapProperties,
            D3D12_HEAP_FLAG_NONE,
            indexResourceDesc,
            D3D12_RESOURCE_STATE_COPY_DEST,
            null,
            out _defaultIndexResource);


        _vertexBufferView.BufferLocation = _defaultVertexResource.GetGPUVirtualAddress();
        _vertexBufferView.SizeInBytes = (uint)vertexResourceDesc.Width;
        _vertexBufferView.StrideInBytes = (uint)Unsafe.SizeOf<Vertex>();

        _indexBufferView.BufferLocation = _defaultIndexResource.GetGPUVirtualAddress();
        _indexBufferView.SizeInBytes = (uint)indexResourceDesc.Width;
        _indexBufferView.Format = DXGI_FORMAT_R32_UINT;
    }

    private unsafe void STEP16_CopyMeshToUploadResource() {

        _uploadVertexResource.Map(0, null, out var vertexPointer);
        CollectionsMarshal.AsSpan(_vertexGroup).CopyTo(new Span<Vertex>(vertexPointer, _vertexGroup.Count));
        _uploadVertexResource.Unmap(0, default(D3D12_RANGE?));

        _uploadIndexResource.Map(0, null, out var indexPointer);
        CollectionsMarshal.AsSpan(_indexGroup).CopyTo(new Span<uint>(indexPointer, _indexGroup.Count));
        _uploadIndexResource.Unmap(0, default(D3D12_RANGE?));
    }

    private void STEP17_CopyMeshToDefaultResource() {
        StartCommandRecord();

        _commandList.CopyBufferRegion(
            _defaultVertexResource,
            0,
            _uploadVertexResource,
            0,
            (ulong)(Unsafe.SizeOf<Vertex>() * _vertexGroup.Count));
        _commandList.CopyBufferRegion(
            _defaultIndexResource,
            0,
            _uploadIndexResource,
            0,
            (ulong)(sizeof(uint) * _indexGroup.Count));

        StartCommandExecute();

        WaitForSingleObject(_renderEvent, INFINITE);
    }

    private unsafe void STEP18_CreateCBVResource() {
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
        mvpBuffer = (nint)cbvPointer;
    }

    private unsafe void STEP19_CreateRootSignature() {
        var rootParameters = stackalloc D3D12_ROOT_PARAMETER[2];

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

        rootParameters[1] = new D3D12_ROOT_PARAMETER() {
            ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
            ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            Anonymous = new() { DescriptorTable = rootDescriptorTableDesc },
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

        if (errorBlob is not null) {
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
            SemanticName = Color,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32B32A32_FLOAT,
            InputSlot = 0,
            AlignedByteOffset = 24,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        inputElementDesc[3] = new() {
            SemanticName = BlendIndices,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32B32A32_UINT,
            InputSlot = 0,
            AlignedByteOffset = 40,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        inputElementDesc[4] = new() {
            SemanticName = BlendWeight,
            SemanticIndex = 0,
            Format = DXGI_FORMAT_R32G32B32A32_FLOAT,
            InputSlot = 0,
            AlignedByteOffset = 56,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        inputLayoutDesc.NumElements = 5;
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

        psoDesc.DSVFormat = _dsvFormat;
        psoDesc.DepthStencilState.DepthEnable = true;
        psoDesc.DepthStencilState.DepthFunc = D3D12_COMPARISON_FUNC_LESS;
        psoDesc.DepthStencilState.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;

        psoDesc.BlendState.RenderTarget._0.BlendEnable = true; // 开启混合，因为模型用到了自发光和默认纹理

        // 下面三个选项控制 RGB 通道的混合，Alpha 通道与 RGB 通道的混合是分开的，这一点请留意！
        // Result = Src * SrcA + Dest * (1 - SrcA)
        psoDesc.BlendState.RenderTarget._0.SrcBlend = D3D12_BLEND_SRC_ALPHA;
        psoDesc.BlendState.RenderTarget._0.DestBlend = D3D12_BLEND_INV_SRC_ALPHA;
        psoDesc.BlendState.RenderTarget._0.BlendOp = D3D12_BLEND_OP_ADD;

        // 下面的三个选项控制 Alpha 通道的混合，Alpha 通道与 RGB 通道的混合是分开的，这一点请留意！
        // ResultA = SrcA * 1 + DstA * 0
        psoDesc.BlendState.RenderTarget._0.SrcBlendAlpha = D3D12_BLEND_ONE;
        psoDesc.BlendState.RenderTarget._0.DestBlendAlpha = D3D12_BLEND_ZERO;
        psoDesc.BlendState.RenderTarget._0.BlendOpAlpha = D3D12_BLEND_OP_ADD;


        psoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
        psoDesc.NumRenderTargets = 1;
        psoDesc.RTVFormats._0 = DXGI_FORMAT_R8G8B8A8_UNORM;
        psoDesc.BlendState.RenderTarget._0.RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE_ALL;
        psoDesc.SampleDesc.Count = 1;
        psoDesc.SampleMask = uint.MaxValue;

        _d3d12Device.CreateGraphicsPipelineState(psoDesc, out _pipelineStateObject);
    }

    private void UpdateConstantBuffer() {
        ref var buffer = ref Unsafe.AsRef<CBuffer>(mvpBuffer);
        buffer.MVPMatrix = _firstCamera.MVPMatrix;
        CollectionsMarshal.AsSpan(_boneNodeTransformGroup).CopyTo(buffer.BoneTransformMatrix);
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

        _commandList.SetGraphicsRootSignature(_rootSignature.Managed);
        _commandList.SetPipelineState(_pipelineStateObject);

        _commandList.RSSetViewports([_viewPort]);
        _commandList.RSSetScissorRects([_scissorRect]);

        _commandList.OMSetRenderTargets(1, _rtvHandle, false, _dsvHandle);

        _commandList.ClearDepthStencilView(_dsvHandle, D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0);

        _commandList.ClearRenderTargetView(_rtvHandle, SkyBlue);

        _commandList.SetDescriptorHeaps([_srvHeap]);

        _commandList.SetGraphicsRootConstantBufferView(0, _cbvResource.GetGPUVirtualAddress());

        _commandList.IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);


        _commandList.IASetVertexBuffers(0, [_vertexBufferView]);
        _commandList.IASetIndexBuffer(_indexBufferView);

        foreach (var mesh in _meshGroup) {
            _commandList.SetGraphicsRootDescriptorTable(1, _materialGroup[mesh.MaterialIndex].GPUHandle);
            _commandList.DrawIndexedInstanced(mesh.IndexCount, 1, mesh.IndexGroupOffset, mesh.VertexGroupOffset, 0);
        }

        _endBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_endBarrier]);

        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);

        _dxgiSwapChain.Present(1, 0);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void STEP21_RenderLoop() {
        bool exit = false;
        Render();
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
        engine.STEP1_InitWindow(hInstance);
        engine.STEP2_CreateDebugDevice();
        engine.STEP3_CreateDevice();
        engine.STEP4_CreateCommandComponents();
        engine.STEP5_CreateRenderTarget();
        engine.STEP6_CreateFenceAndBarrier();
        engine.STEP7_CreateDSVHeap();
        engine.STEP8_CreateDepthStencilBuffer();
        engine.STEP9_CreateDSV();

        engine.STEP10_OpenModelFile();
        engine.STEP11_AddModelMaterials();
        engine.STEP12_AddModelData();
        engine.STEP13_CalcModelBoundingBox();

        engine.STEP14_CreateModelTextureResource();

        engine.STEP15_CreateMeshResourceAndDescriptor();
        engine.STEP16_CopyMeshToUploadResource();
        engine.STEP17_CopyMeshToDefaultResource();

        engine.STEP18_CreateCBVResource();
        engine.STEP19_CreateRootSignature();
        engine.STEP20_CreatePSO();

        engine.STEP21_RenderLoop();
    }

}

internal static class Program {

    [STAThread]
    static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }
}