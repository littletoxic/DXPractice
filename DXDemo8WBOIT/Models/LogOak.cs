using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

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
