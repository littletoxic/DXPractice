using System.Collections.Frozen;
using Windows.Win32.Graphics.Dxgi.Common;

namespace DXDemo8WBOIT;

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
