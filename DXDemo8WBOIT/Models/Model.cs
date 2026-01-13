using System.Collections.Frozen;
using System.Numerics;
using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

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
