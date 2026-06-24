using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct2D;
using Windows.Win32.Graphics.Direct3D12;

namespace Windows.Win32;

public static partial class PInvoke {
    public static uint D3D12_ENCODE_SHADER_4_COMPONENT_MAPPING(uint Src0, uint Src1, uint Src2, uint Src3) =>
        (Src0 & D3D12_SHADER_COMPONENT_MAPPING_MASK)
        | ((Src1 & D3D12_SHADER_COMPONENT_MAPPING_MASK) << (int)D3D12_SHADER_COMPONENT_MAPPING_SHIFT)
        | ((Src2 & D3D12_SHADER_COMPONENT_MAPPING_MASK) << (int)(D3D12_SHADER_COMPONENT_MAPPING_SHIFT * 2))
        | ((Src3 & D3D12_SHADER_COMPONENT_MAPPING_MASK) << (int)(D3D12_SHADER_COMPONENT_MAPPING_SHIFT * 3))
        | D3D12_SHADER_COMPONENT_MAPPING_ALWAYS_SET_BIT_AVOIDING_ZEROMEM_MISTAKES;

    public static unsafe PCSTR AllocatePCSTR(string str) => (PCSTR)(byte*)Marshal.StringToHGlobalAnsi(str);

    public static HRESULT D2D1CreateFactory<T>(D2D1_FACTORY_TYPE factoryType, [Optional] D2D1_FACTORY_OPTIONS? pFactoryOptions, out T device) where T : class, ID2D1Factory {
        var hr = D2D1CreateFactory(factoryType, typeof(T).GUID, pFactoryOptions, out var result);
        device = (T)result;
        return hr;
    }
}

public static class Extensions {
    public static unsafe void SetGraphicsRoot32BitConstants<T>(this ID3D12GraphicsCommandList commandList, uint RootParameterIndex, ReadOnlySpan<T> SrcData, uint DestOffsetIn32BitValues) where T : unmanaged {
        fixed (T* pSrcData = SrcData) {
            commandList.SetGraphicsRoot32BitConstants(RootParameterIndex, (uint)((SrcData.Length * Unsafe.SizeOf<T>() + sizeof(uint) - 1) / sizeof(uint)), pSrcData, DestOffsetIn32BitValues);
        }
    }
}
