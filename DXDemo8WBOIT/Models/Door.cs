using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

internal class Door : Model {

    protected static readonly Vertex[] VertexArray = [
        // 门上部分
        new() { Position = new(0, 2, 0, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 2, 0, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 1, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 1, 0, 1), TexCoordUV = new(0, 1) },

        new() { Position = new(1, 2, 0.2f, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(0, 2, 0.2f, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(0, 1, 0.2f, 1), TexCoordUV = new(0, 1) },
        new() { Position = new(1, 1, 0.2f, 1), TexCoordUV = new(1, 1) },

        // 门下部分
        new() { Position = new(0, 1, 0, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(1, 1, 0, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 1) },

        new() { Position = new(1, 1, 0.2f, 1), TexCoordUV = new(0, 0) },
        new() { Position = new(0, 1, 0.2f, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(0, 0, 0.2f, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 0.2f, 1), TexCoordUV = new(0, 1) },

        // 门隙部分
        new() { Position = new(0, 2, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(1, 2, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(1, 2, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0, 2, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(1, 0, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0, 0, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0, 2, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0, 2, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0, 0, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(1, 2, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(1, 2, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(1, 0, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        // 上部分门隙 (左侧装饰)
        new() { Position = new(0.1875f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.1875f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.1875f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.4375f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.1875f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.1875f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.1875f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.1875f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.4375f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.4375f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        // 上部分门隙 (右侧装饰)
        new() { Position = new(0.5625f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.5625f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.5625f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.8125f, 1.5f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.5f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.3125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.3125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.5625f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.5625f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.5625f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.5625f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },

        new() { Position = new(0.8125f, 1.8125f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.8125f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.625f, 0, 1), TexCoordUV = new(0.1875f, 0.4375f) },
        new() { Position = new(0.8125f, 1.625f, 0.2f, 1), TexCoordUV = new(0.1875f, 0.4375f) },
    ];

    protected static readonly uint[] IndexArray = [
        // 门上部分
        0, 1, 2, 0, 2, 3,
        4, 5, 6, 4, 6, 7,

        // 门下部分
        8, 9, 10, 8, 10, 11,
        12, 13, 14, 12, 14, 15,

        // 门隙部分
        16, 17, 18, 16, 18, 19,
        20, 21, 22, 20, 22, 23,
        24, 25, 26, 24, 26, 27,
        28, 29, 30, 28, 30, 31,

        // 上部分门隙
        32, 33, 34, 32, 34, 35,
        36, 37, 38, 36, 38, 39,
        40, 41, 42, 40, 42, 43,
        44, 45, 46, 44, 46, 47,

        48, 49, 50, 48, 50, 51,
        52, 53, 54, 52, 54, 55,
        56, 57, 58, 56, 58, 59,
        60, 61, 62, 60, 62, 63,

        64, 65, 66, 64, 66, 67,
        68, 69, 70, 68, 70, 71,
        72, 73, 74, 72, 74, 75,
        76, 77, 78, 76, 78, 79,

        80, 81, 82, 80, 82, 83,
        84, 85, 86, 84, 86, 87,
        88, 89, 90, 88, 90, 91,
        92, 93, 94, 92, 94, 95
    ];

    public Door() {
        _textureNameSet = ["door_wood_lower", "door_wood_upper"];
    }

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

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        // 门上部分
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["door_wood_upper"]);
        commandList.DrawIndexedInstanced(12, 1, 0, 0, 0);

        // 门下部分
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["door_wood_lower"]);
        commandList.DrawIndexedInstanced(12, 1, 12, 0, 0);

        // 门隙部分
        commandList.DrawIndexedInstanced(24, 1, 24, 0, 0);

        // 上门隙部分
        commandList.DrawIndexedInstanced(96, 1, 48, 0, 0);
    }
}
