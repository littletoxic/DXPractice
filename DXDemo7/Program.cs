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

namespace DXDemo7;

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
    private const float AspectRatio = 4f / 3f;   // 投影窗口宽高比
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
        // 注意！我们这里移除了模型矩阵！每个模型会指定具体的模型矩阵！
        _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection); // 观察矩阵，世界空间 -> 观察空间
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(FovAngleY, AspectRatio, NearZ, FarZ); // 投影矩阵，观察空间 -> 齐次裁剪空间

        _eyePosition = new Vector3(4, 5, -4);
        _focusPosition = new Vector3(4, 3, 4);
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
}

internal struct Vertex {
    internal Vector4 Position;
    internal Vector2 TexCoordUV;
}

internal abstract class Model {
    protected ID3D12Resource _vertexResource;
    protected ID3D12Resource _modelMatrixResource;
    protected ID3D12Resource _indexResource;

    // 每个模型的 VBV 顶点信息描述符数组，数组每个元素占用一个输入槽，多槽输入可以加速 CPU-GPU 的传递
    // VertexBufferView[0] 描述每个顶点的顶点信息 (position 位置，texcoordUV 纹理 UV 坐标)
    // VertexBufferView[1] 描述每个顶点对应的模型矩阵，模型矩阵会在 IA 阶段拆分成四个行向量进行输入，之后在 VS 阶段重新组装成矩阵
    protected D3D12_VERTEX_BUFFER_VIEW[] _vertexBufferView = new D3D12_VERTEX_BUFFER_VIEW[2];

    // 每个模型的 IBV 顶点索引描述符，一个模型只有一个索引描述符
    protected D3D12_INDEX_BUFFER_VIEW _indexBufferView;

    protected FrozenSet<string> _textureNameSet;

    // 纹理名 - GPU 句柄映射表，用于索引纹理，设置根参数
    public IReadOnlyDictionary<string, D3D12_GPU_DESCRIPTOR_HANDLE> TextureGPUHandleMap { get; private set; }

    public void BuildTextureGPUHandleMap(IReadOnlyDictionary<string, D3D12_GPU_DESCRIPTOR_HANDLE> globalTextureGPUHandleMap) {
        TextureGPUHandleMap = globalTextureGPUHandleMap.Where(kv => _textureNameSet.Contains(kv.Key)).ToFrozenDictionary();
    }

    public Matrix4x4 ModelMatrix { get; set; } = Matrix4x4.Identity;

    public abstract void CreateResourceAndDescriptor(ID3D12Device4 d3d12Device);

    public abstract void DrawModel(ID3D12GraphicsCommandList commandList);

    protected static unsafe void MapWriteUnmap<T>(ID3D12Resource resource, params ReadOnlySpan<T> data) where T : unmanaged {
        resource.Map(0, null, out var transmitPointer);

        data.CopyTo(new(transmitPointer, data.Length));

        resource.Unmap(0, default(D3D12_RANGE?));
    }
}

internal abstract class SoildBlock : Model {
    protected static Vertex[] VertexArray = [
        // 正面
        new() { Position = new(0, 1, 0, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 1, 0, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 1) },

        // 背面
        new() { Position = new(1, 1, 1, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(0, 1, 1, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(0, 1) },

        // 左面
        new() { Position = new(0, 1, 1, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(0, 1, 0, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(0, 1) },

        // 右面
        new() { Position = new(1, 1, 0, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 1, 1, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(0, 1) },

        // 上面
        new() { Position = new(0, 1, 1, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 1, 1, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 1, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 1, 0, 1), TexCoordUV = new(0, 1) },

        // 下面
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(0, 1) },
    ];

    protected static uint[] IndexArray = [
        // 正面
        0, 1, 2, 0, 2, 3,
        // 背面
        4, 5, 6, 4, 6, 7,
        // 左面
        8, 9, 10, 8, 10, 11,
        // 右面
        12, 13, 14, 12, 14, 15,
        // 上面
        16, 17, 18, 16, 18, 19,
        // 下面
        20, 21, 22, 20, 22, 23,
    ];

    public override void CreateResourceAndDescriptor(ID3D12Device4 d3d12Device) {

        var uploadResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1, Quality = 0 },
        };

        var heapProperties = new D3D12_HEAP_PROPERTIES() {
            Type = D3D12_HEAP_TYPE_UPLOAD,
        };

        uploadResourceDesc.Width = (ulong)(VertexArray.Length * Unsafe.SizeOf<Vertex>());
        d3d12Device.CreateCommittedResource(
            heapProperties,
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _vertexResource);

        uploadResourceDesc.Width = (ulong)(VertexArray.Length * Unsafe.SizeOf<Matrix4x4>());
        d3d12Device.CreateCommittedResource(
            heapProperties,
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _modelMatrixResource);

        uploadResourceDesc.Width = (ulong)(IndexArray.Length * sizeof(uint));
        d3d12Device.CreateCommittedResource(
            heapProperties,
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _indexResource);

        MapWriteUnmap(_vertexResource, VertexArray);
        Span<Matrix4x4> matrixSpan = VertexArray.Length <= 128 ? stackalloc Matrix4x4[VertexArray.Length] : new Matrix4x4[VertexArray.Length];
        matrixSpan.Fill(ModelMatrix);
        MapWriteUnmap(_modelMatrixResource, matrixSpan);
        MapWriteUnmap(_indexResource, IndexArray);

        _vertexBufferView[0].BufferLocation = _vertexResource.GetGPUVirtualAddress();
        _vertexBufferView[0].SizeInBytes = (uint)(VertexArray.Length * Unsafe.SizeOf<Vertex>());
        _vertexBufferView[0].StrideInBytes = (uint)Unsafe.SizeOf<Vertex>();

        _vertexBufferView[1].BufferLocation = _modelMatrixResource.GetGPUVirtualAddress();
        _vertexBufferView[1].SizeInBytes = (uint)(VertexArray.Length * Unsafe.SizeOf<Matrix4x4>());
        _vertexBufferView[1].StrideInBytes = (uint)Unsafe.SizeOf<Matrix4x4>();

        _indexBufferView.BufferLocation = _indexResource.GetGPUVirtualAddress();
        _indexBufferView.SizeInBytes = (uint)(IndexArray.Length * sizeof(uint));
        _indexBufferView.Format = DXGI_FORMAT_R32_UINT;
    }
}

internal abstract class SoildStair : Model {

    protected static readonly Vertex[] VertexArray = [
        // 台阶底面
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(0, 1) },

        // 台阶背面
        new() { Position = new(1, 1, 1, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(0, 1, 1, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(0, 1) },

        // 台阶正面
        new() { Position = new(0, 0.5f, 0, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(1, 0.5f, 0, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 1) },

        new() { Position = new(0, 1, 0.5f, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 1, 0.5f, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0.5f, 0.5f, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(0, 0.5f, 0.5f, 1), TexCoordUV = new(0, 0.5f) },

        // 台阶顶面
        new() { Position = new(0, 0.5f, 0.5f, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(1, 0.5f, 0.5f, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(1, 0.5f, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0.5f, 0, 1), TexCoordUV = new(0, 1) },

        new() { Position = new(0, 1, 1, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 1, 1, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 1, 0.5f, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(0, 1, 0.5f, 1), TexCoordUV = new(0, 0.5f) },

        // 台阶左面
        new() { Position = new(0, 1, 1, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(0, 1, 0.5f, 1), TexCoordUV = new(0.5f, 0) },
        new() { Position = new(0, 0, 0.5f, 1), TexCoordUV = new(0.5f, 1) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(0, 1) },

        new() { Position = new(0, 0.5f, 0.5f, 1), TexCoordUV = new(0.5f, 0.5f) },
        new() { Position = new(0, 0.5f, 0, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 0.5f, 1), TexCoordUV = new(0.5f, 1) },

        // 台阶右面
        new() { Position = new(1, 1, 0.5f, 1), TexCoordUV = new(0.5f, 0) },
        new() { Position = new(1, 1, 1, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 0.5f, 1), TexCoordUV = new(0.5f, 1) },

        new() { Position = new(1, 0.5f, 0, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(1, 0.5f, 0.5f, 1), TexCoordUV = new(0.5f, 0.5f) },
        new() { Position = new(1, 0, 0.5f, 1), TexCoordUV = new(0.5f, 1) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(0, 1) },
    ];

    protected static readonly uint[] IndexArray = [
        // 台阶底面
        0, 1, 2, 0, 2, 3,
        // 台阶背面
        4, 5, 6, 4, 6, 7,
        // 台阶正面
        8, 9, 10, 8, 10, 11,
        12, 13, 14, 12, 14, 15,
        // 台阶顶面
        16, 17, 18, 16, 18, 19,
        20, 21, 22, 20, 22, 23,
        // 台阶左面
        24, 25, 26, 24, 26, 27,
        28, 29, 30, 28, 30, 31,
        // 台阶右面
        32, 33, 34, 32, 34, 35,
        36, 37, 38, 36, 38, 39,
    ];

    public override void CreateResourceAndDescriptor(ID3D12Device4 d3d12Device) {

        var uploadResourceDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Height = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1, Quality = 0 },
        };

        var heapProperties = new D3D12_HEAP_PROPERTIES() {
            Type = D3D12_HEAP_TYPE_UPLOAD,
        };

        uploadResourceDesc.Width = (ulong)(VertexArray.Length * Unsafe.SizeOf<Vertex>());
        d3d12Device.CreateCommittedResource(
            heapProperties,
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _vertexResource);

        uploadResourceDesc.Width = (ulong)(VertexArray.Length * Unsafe.SizeOf<Matrix4x4>());
        d3d12Device.CreateCommittedResource(
            heapProperties,
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _modelMatrixResource);

        uploadResourceDesc.Width = (ulong)(IndexArray.Length * sizeof(uint));
        d3d12Device.CreateCommittedResource(
            heapProperties,
            D3D12_HEAP_FLAG_NONE,
            uploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _indexResource);

        MapWriteUnmap(_vertexResource, VertexArray);
        Span<Matrix4x4> matrixSpan = VertexArray.Length <= 128 ? stackalloc Matrix4x4[VertexArray.Length] : new Matrix4x4[VertexArray.Length];
        matrixSpan.Fill(ModelMatrix);
        MapWriteUnmap(_modelMatrixResource, matrixSpan);
        MapWriteUnmap(_indexResource, IndexArray);

        _vertexBufferView[0].BufferLocation = _vertexResource.GetGPUVirtualAddress();
        _vertexBufferView[0].SizeInBytes = (uint)(VertexArray.Length * Unsafe.SizeOf<Vertex>());
        _vertexBufferView[0].StrideInBytes = (uint)Unsafe.SizeOf<Vertex>();

        _vertexBufferView[1].BufferLocation = _modelMatrixResource.GetGPUVirtualAddress();
        _vertexBufferView[1].SizeInBytes = (uint)(VertexArray.Length * Unsafe.SizeOf<Matrix4x4>());
        _vertexBufferView[1].StrideInBytes = (uint)Unsafe.SizeOf<Matrix4x4>();

        _indexBufferView.BufferLocation = _indexResource.GetGPUVirtualAddress();
        _indexBufferView.SizeInBytes = (uint)(IndexArray.Length * sizeof(uint));
        _indexBufferView.Format = DXGI_FORMAT_R32_UINT;
    }

}

internal sealed class Dirt : SoildBlock {

    public Dirt() {
        _textureNameSet = ["dirt"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["dirt"]);

        commandList.DrawIndexedInstanced((uint)IndexArray.Length, 1, 0, 0, 0);
    }
}

internal sealed class PlanksOak : SoildBlock {

    public PlanksOak() {
        _textureNameSet = ["planks_oak"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["planks_oak"]);

        commandList.DrawIndexedInstanced((uint)IndexArray.Length, 1, 0, 0, 0);
    }
}

internal sealed class Furnace : SoildBlock {

    public Furnace() {
        _textureNameSet = ["furnace_front_off", "furnace_side", "furnace_top"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        // 渲染上下面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["furnace_top"]);
        commandList.DrawIndexedInstanced(12, 1, 24, 0, 0);

        // 渲染左右背面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["furnace_side"]);
        commandList.DrawIndexedInstanced(18, 1, 6, 0, 0);

        // 渲染上下面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["furnace_front_off"]);
        commandList.DrawIndexedInstanced(6, 1, 0, 0, 0);
    }
}

internal sealed class CraftingTable : SoildBlock {

    public CraftingTable() {
        _textureNameSet = ["crafting_table_front", "crafting_table_side", "crafting_table_top"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        // 渲染上下面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["crafting_table_top"]);
        commandList.DrawIndexedInstanced(12, 1, 24, 0, 0);

        // 渲染左右背面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["crafting_table_side"]);
        commandList.DrawIndexedInstanced(18, 1, 6, 0, 0);

        // 渲染上下面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["crafting_table_front"]);
        commandList.DrawIndexedInstanced(6, 1, 0, 0, 0);
    }
}

internal sealed class LogOak : SoildBlock {

    public LogOak() {
        _textureNameSet = ["log_oak", "log_oak_top"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        // 渲染上下面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["log_oak_top"]);
        commandList.DrawIndexedInstanced(12, 1, 24, 0, 0);

        // 渲染左右正背面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["log_oak"]);
        commandList.DrawIndexedInstanced(24, 1, 0, 0, 0);

    }
}

internal sealed class Grass : SoildBlock {

    public Grass() {
        _textureNameSet = ["grass_side", "grass_top", "dirt"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        // 渲染上面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["grass_top"]);
        commandList.DrawIndexedInstanced(6, 1, 24, 0, 0);

        // 渲染下面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["dirt"]);
        commandList.DrawIndexedInstanced(6, 1, 30, 0, 0);

        // 渲染左右正背面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["grass_side"]);
        commandList.DrawIndexedInstanced(24, 1, 0, 0, 0);

    }
}

internal sealed class PlanksOakSoildStair : SoildStair {

    public PlanksOakSoildStair() {
        _textureNameSet = ["planks_oak"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["planks_oak"]);

        commandList.DrawIndexedInstanced((uint)IndexArray.Length, 1, 0, 0, 0);
    }
}

internal sealed class TextureMapInfo {
    public string TextureFilePath { get; set; }
    public ComPtr<ID3D12Resource> DefaultHeapTextureResource { get; set; }
    public ComPtr<ID3D12Resource> UploadHeapTextureResource { get; set; }
    public D3D12_GPU_DESCRIPTOR_HANDLE GPUHandle { get; set; }
    public D3D12_CPU_DESCRIPTOR_HANDLE CPUHandle { get; set; }
}

internal sealed class ModelManager {

    private readonly Dictionary<string, TextureMapInfo> _textureSRVMap = [];
    private readonly List<Model> _modelGroup = [];

    public IReadOnlyDictionary<string, TextureMapInfo> TextureSRVMap => _textureSRVMap;

    public ModelManager() {
        _textureSRVMap["dirt"] = new() { TextureFilePath = "resource/dirt.png" };
        _textureSRVMap["grass_top"] = new() { TextureFilePath = "resource/grass_top.png" };
        _textureSRVMap["grass_side"] = new() { TextureFilePath = "resource/grass_side.png" };
        _textureSRVMap["log_oak"] = new() { TextureFilePath = "resource/log_oak.png" };
        _textureSRVMap["log_oak_top"] = new() { TextureFilePath = "resource/log_oak_top.png" };
        _textureSRVMap["furnace_front_off"] = new() { TextureFilePath = "resource/furnace_front_off.png" };
        _textureSRVMap["furnace_side"] = new() { TextureFilePath = "resource/furnace_side.png" };
        _textureSRVMap["furnace_top"] = new() { TextureFilePath = "resource/furnace_top.png" };
        _textureSRVMap["crafting_table_front"] = new() { TextureFilePath = "resource/crafting_table_front.png" };
        _textureSRVMap["crafting_table_side"] = new() { TextureFilePath = "resource/crafting_table_side.png" };
        _textureSRVMap["crafting_table_top"] = new() { TextureFilePath = "resource/crafting_table_top.png" };
        _textureSRVMap["planks_oak"] = new() { TextureFilePath = "resource/planks_oak.png" };

    }

    public void CreateBlock() {
        // 两层泥土地基，y 是高度
        for (int x = 0; x < 10; x++) {
            for (int z = -4; z < 10; z++) {
                for (int y = -2; y < 0; y++) {
                    var dirt = new Dirt {
                        ModelMatrix = Matrix4x4.CreateTranslation(x, y, z)
                    };
                    _modelGroup.Add(dirt);
                }
            }
        }

        // 一层草方块地基
        for (int x = 0; x < 10; x++) {
            for (int z = -4; z < 10; z++) {
                var grass = new Grass {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 0, z)
                };
                _modelGroup.Add(grass);
            }
        }

        // 4x4 木板房基
        for (int x = 3; x < 7; x++) {
            for (int z = 3; z < 7; z++) {
                var plank = new PlanksOak() {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 2, z)
                };
                _modelGroup.Add(plank);
            }
        }

        // 8 柱原木 

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(3, y, 2)
            };
            _modelGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, y, 3)
            };
            _modelGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(6, y, 2)
            };
            _modelGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, y, 3)
            };
            _modelGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, y, 6)
            };
            _modelGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(6, y, 7)
            };
            _modelGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, y, 6)
            };
            _modelGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(3, y, 7)
            };
            _modelGroup.Add(logOak);
        }

        // 其他木板与门前台阶
        {
            var plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 2, 2)
            };
            _modelGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 2, 2)
            };
            _modelGroup.Add(plank);

            for (int y = 5; y < 7; y++) {
                for (int x = 4; x < 6; x++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(x, y, 2)
                    };
                    _modelGroup.Add(plank);
                }
            }

            for (int y = 2; y < 4; y++) {
                for (int z = 4; z < 6; z++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(2, y, z)
                    };
                    _modelGroup.Add(plank);
                }
            }

            for (int y = 2; y < 4; y++) {
                for (int x = 4; x < 6; x++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(x, y, 7)
                    };
                    _modelGroup.Add(plank);
                }
            }

            for (int y = 2; y < 4; y++) {
                for (int z = 4; z < 6; z++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(7, y, z)
                    };
                    _modelGroup.Add(plank);
                }
            }

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, 6, 4)
            };
            _modelGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, 6, 5)
            };
            _modelGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 6, 7)
            };
            _modelGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 6, 7)
            };
            _modelGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, 6, 4)
            };
            _modelGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, 6, 5)
            };
            _modelGroup.Add(plank);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 2, 1)
            };
            _modelGroup.Add(stair);

            stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 2, 1)
            };
            _modelGroup.Add(stair);

            stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 1, 0)
            };
            _modelGroup.Add(stair);

            stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 1, 0)
            };
            _modelGroup.Add(stair);
        }

        // 4x4 木板房顶
        for (int x = 3; x < 7; x++) {
            for (int z = 3; z < 7; z++) {
                var plank = new PlanksOak {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 6, z)
                };
                _modelGroup.Add(plank);
            }
        }

        // 屋顶

        // 第一层
        for (int x = 3; x < 7; x++) {
            var stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(x, 6, 1)
            };
            _modelGroup.Add(stair);
        }

        for (int x = 3; x < 7; x++) {
            // 旋转橡木台阶用的模型矩阵
            // 这里本来是可以不用 XMMatrixTranslation(-0.5, -0.5, -0.5) 平移到模型中心的
            // 因为作者本人 (我) 的设计失误，把模型坐标系原点建立在模型左下角了 (见上文的 VertexArray)
            // 导致还要先把原点平移到模型中心，旋转完再还原，增大计算量，这个是完全可以规避的
            // 读者可以自行修改 VertexArray，使方块以自身中心为原点建系，这样就可以直接 XMMatrixRotationY() 进行旋转了
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI);                                      // 平移中心后，再旋转，否则会出错 (旋转角度是弧度)
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);         // 旋转完再还原
            transform *= Matrix4x4.CreateTranslation(x, 6, 8);                  // 再平移到对应的坐标

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _modelGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI / 2.0f);            // 旋转 90°
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(1, 6, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _modelGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI + MathF.PI / 2.0f); // 旋转 270°
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(8, 6, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _modelGroup.Add(stair);
        }

        // 第二层
        for (int x = 3; x < 7; x++) {
            var stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(x, 7, 2)
            };
            _modelGroup.Add(stair);
        }

        for (int x = 3; x < 7; x++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI);
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(x, 7, 7);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _modelGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI / 2.0f);
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(2, 7, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _modelGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI + MathF.PI / 2.0f);
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(7, 7, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _modelGroup.Add(stair);
        }

        // 补上屋顶空位
        for (int x = 3; x < 7; x++) {
            for (int z = 3; z < 7; z++) {
                var plank = new PlanksOak {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 7, z)
                };
                _modelGroup.Add(plank);
            }
        }


        // 工作台和熔炉
        var craftTable = new CraftingTable {
            ModelMatrix = Matrix4x4.CreateTranslation(3, 3, 6)
        };
        _modelGroup.Add(craftTable);

        var furnace = new Furnace {
            ModelMatrix = Matrix4x4.CreateTranslation(4, 3, 6)
        };
        _modelGroup.Add(furnace);

        furnace = new Furnace {
            ModelMatrix = Matrix4x4.CreateTranslation(5, 3, 6)
        };
        _modelGroup.Add(furnace);

    }

    public void CreateModelResource(ID3D12Device4 d3d12Device) {
        var globalTextureGPUHandleMap = _textureSRVMap.ToDictionary(kv => kv.Key, kv => kv.Value.GPUHandle);
        foreach (var model in _modelGroup) {
            model.CreateResourceAndDescriptor(d3d12Device);
            model.BuildTextureGPUHandleMap(globalTextureGPUHandleMap);
        }
    }

    public void RenderAllModel(ID3D12GraphicsCommandList commandList) {
        foreach (var model in _modelGroup) {
            model.DrawModel(commandList);
        }
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
    private readonly DXGI_FORMAT _dsvFormat = DXGI_FORMAT_D24_UNORM_S8_UINT;
    private ID3D12Resource _depthStencilBuffer;

    private ID3D12DescriptorHeap _srvHeap;

    private readonly ModelManager _modelManager = new();

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

    private struct CBuffer {
        internal Matrix4x4 MVPMatrix;
    }
    private ID3D12Resource _cbvResource;
    private nint mvpBuffer;

    private ComPtr<ID3D12RootSignature> _rootSignature;

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

    private void CreateDSVHeap() {
        var dsvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = 1,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
        };

        _d3d12Device.CreateDescriptorHeap(dsvHeapDesc, out _dsvHeap);

        _dsvHandle = _dsvHeap.GetCPUDescriptorHandleForHeapStart();
    }

    private void CreateDepthStencilBuffer() {
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
            new D3D12_HEAP_PROPERTIES() {
                Type = D3D12_HEAP_TYPE_DEFAULT,
            },
            D3D12_HEAP_FLAG_NONE,
            dsvResourceDesc,
            D3D12_RESOURCE_STATE_DEPTH_WRITE,
            depthStencilBufferClearValue,
            out _depthStencilBuffer);
    }

    private void CreateDSV() {
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

    private void CreateSRVHeap() {
        var srvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = (uint)_modelManager.TextureSRVMap.Count,
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

    private void CreateUploadAndDefaultResource(TextureMapInfo info) {
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
        info.UploadHeapTextureResource = new(uploadTextureResource);


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
            Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
            Anonymous = new() { SubresourceIndex = 0 },
            pResource = (ID3D12Resource_unmanaged*)info.DefaultHeapTextureResource.Ptr,
        };

        var srcLocation = new D3D12_TEXTURE_COPY_LOCATION() {
            Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
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
            ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D,
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
        var srvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

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

    private unsafe void CreateRootSignature() {
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
        var inputElementDesc = stackalloc D3D12_INPUT_ELEMENT_DESC[6];

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

        var semanticNameMatrix = "MATRIX"u8;
        byte* pSemanticNameMatrix = stackalloc byte[semanticNameMatrix.Length + 1];
        semanticNameMatrix.CopyTo(new Span<byte>(pSemanticNameMatrix, semanticNameMatrix.Length));
        pSemanticNameMatrix[semanticNameMatrix.Length] = 0;

        for (uint i = 0; i < 4; i++) {
            inputElementDesc[2 + i] = new() {
                SemanticName = new(pSemanticNameMatrix),
                SemanticIndex = i,
                Format = DXGI_FORMAT_R32G32B32A32_FLOAT,
                InputSlot = 1,
                AlignedByteOffset = (uint)(i * Unsafe.SizeOf<Vector4>()),
                InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                InstanceDataStepRate = 0,
            };
        }

        inputLayoutDesc.NumElements = 6;
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

        psoDesc.VS.pShaderBytecode = vertexShaderBlob.GetBufferPointer();
        psoDesc.VS.BytecodeLength = vertexShaderBlob.GetBufferSize();

        psoDesc.PS.pShaderBytecode = pixelShaderBlob.GetBufferPointer();
        psoDesc.PS.BytecodeLength = pixelShaderBlob.GetBufferSize();

        psoDesc.RasterizerState.CullMode = D3D12_CULL_MODE_BACK;
        psoDesc.RasterizerState.FillMode = D3D12_FILL_MODE_SOLID;

        psoDesc.pRootSignature = (ID3D12RootSignature_unmanaged*)_rootSignature.Ptr;

        psoDesc.DSVFormat = _dsvFormat;
        psoDesc.DepthStencilState.DepthEnable = true;
        psoDesc.DepthStencilState.DepthFunc = D3D12_COMPARISON_FUNC_LESS;
        psoDesc.DepthStencilState.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;

        psoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
        psoDesc.NumRenderTargets = 1;
        psoDesc.RTVFormats._0 = DXGI_FORMAT_R8G8B8A8_UNORM;
        psoDesc.BlendState.RenderTarget._0.RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE_ALL;
        psoDesc.SampleDesc.Count = 1;
        psoDesc.SampleMask = uint.MaxValue;

        _d3d12Device.CreateGraphicsPipelineState(psoDesc, out _pipelineStateObject);
    }

    private unsafe void UpdateConstantBuffer() {
        Unsafe.AsRef<CBuffer>((void*)mvpBuffer).MVPMatrix = _firstCamera.MVPMatrix;
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

        _modelManager.RenderAllModel(_commandList);

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

        engine.CreateRootSignature();
        engine.CreatePSO();

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