using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT.Models;

internal sealed class GlassLightBlue : SoildBlock {
    public GlassLightBlue() {
        _textureNameSet = ["glass_light_blue"];
    }

    public override void DrawModel(ID3D12GraphicsCommandList commandList) {
        commandList.IASetIndexBuffer(_indexBufferView);
        commandList.IASetVertexBuffers(0, _vertexBufferView);

        commandList.SetGraphicsRootDescriptorTable(1, TextureGPUHandleMap["glass_light_blue"]);

        commandList.DrawIndexedInstanced((uint)IndexArray.Length, 1, 0, 0, 0);
    }
}
