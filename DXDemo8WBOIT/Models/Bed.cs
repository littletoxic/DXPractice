using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

internal sealed class Bed : Model {
    private static readonly Vertex[] VertexArray = [
        // 床脚正面
        new() { Position = new(0, 0.5f, 0, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(1, 0.5f, 0, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 1) },

        // 床头正面
        new() { Position = new(1, 0.5f, 2, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(0, 0.5f, 2, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(0, 0, 2, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 2, 1), TexCoordUV = new(0, 1) },

        // 床顶
        new() { Position = new(0, 0.5f, 2, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0.5f, 2, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0.5f, 1, 1), TexCoordUV = new(0, 1) },
        new() { Position = new(0, 0.5f, 1, 1), TexCoordUV = new(0, 0) },

        new() { Position = new(0, 0.5f, 1, 1), TexCoordUV = new(1, 0) },
        new() { Position = new(1, 0.5f, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0.5f, 0, 1), TexCoordUV = new(0, 1) },
        new() { Position = new(0, 0.5f, 0, 1), TexCoordUV = new(0, 0) },

        // 床头一侧
        new() { Position = new(0, 0.5f, 2, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(0, 0.5f, 1, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(0, 1) },
        new() { Position = new(0, 0, 2, 1), TexCoordUV = new(1, 1) },

        new() { Position = new(1, 0.5f, 1, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(1, 0.5f, 2, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(1, 0, 2, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(0, 1) },

        // 床脚一侧
        new() { Position = new(0, 0.5f, 1, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(0, 0.5f, 0, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(0, 0, 0, 1), TexCoordUV = new(0, 1) },
        new() { Position = new(0, 0, 1, 1), TexCoordUV = new(1, 1) },

        new() { Position = new(1, 0.5f, 0, 1), TexCoordUV = new(0, 0.5f) },
        new() { Position = new(1, 0.5f, 1, 1), TexCoordUV = new(1, 0.5f) },
        new() { Position = new(1, 0, 1, 1), TexCoordUV = new(1, 1) },
        new() { Position = new(1, 0, 0, 1), TexCoordUV = new(0, 1) },
    ];

    private static readonly uint[] IndexArray = [
        // 床脚正面
        0, 1, 2, 0, 2, 3,

        // 床头正面
        4, 5, 6, 4, 6, 7,

        // 床顶
        8, 9, 10, 8, 10, 11,
        12, 13, 14, 12, 14, 15,

        // 床头一侧
        16, 17, 18, 16, 18, 19,
        20, 21, 22, 20, 22, 23,

        // 床脚一侧
        24, 25, 26, 24, 26, 27,
        28, 29, 30, 28, 30, 31
    ];

    public Bed() {
        _textureNameSet = ["bed_feet_end", "bed_feet_side", "bed_feet_top", "bed_head_top", "bed_head_side", "bed_head_end"];
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

        // 床脚正面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["bed_feet_end"]);
        commandList.DrawIndexedInstanced(6, 1, 0, 0, 0);

        // 床头正面
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["bed_head_end"]);
        commandList.DrawIndexedInstanced(6, 1, 6, 0, 0);

        // 床顶
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["bed_head_top"]);
        commandList.DrawIndexedInstanced(6, 1, 12, 0, 0);
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["bed_feet_top"]);
        commandList.DrawIndexedInstanced(6, 1, 18, 0, 0);

        // 床头一侧
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["bed_head_side"]);
        commandList.DrawIndexedInstanced(12, 1, 24, 0, 0);

        // 床脚一侧
        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["bed_feet_side"]);
        commandList.DrawIndexedInstanced(12, 1, 36, 0, 0);
    }
}
