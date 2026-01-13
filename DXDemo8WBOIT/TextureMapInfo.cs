using Windows.Win32;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT;

internal sealed class TextureMapInfo {
    public string TextureFilePath { get; set; }
    public ComPtr<ID3D12Resource> DefaultHeapTextureResource { get; set; }
    public ComPtr<ID3D12Resource> UploadHeapTextureResource { get; set; }
    public D3D12_GPU_DESCRIPTOR_HANDLE GPUHandle { get; set; }
    public D3D12_CPU_DESCRIPTOR_HANDLE CPUHandle { get; set; }
}
