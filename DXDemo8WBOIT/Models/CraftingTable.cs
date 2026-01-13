using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

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
