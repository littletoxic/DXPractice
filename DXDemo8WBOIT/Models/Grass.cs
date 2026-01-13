using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

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
