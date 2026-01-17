using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

internal abstract class SoildBlock : Model {
    protected static readonly Vertex[] VertexArray = [
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

    protected static readonly uint[] IndexArray = [
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
