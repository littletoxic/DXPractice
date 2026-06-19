using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32;

internal static partial class PInvoke {
    internal static uint D3D12_ENCODE_SHADER_4_COMPONENT_MAPPING(uint Src0, uint Src1, uint Src2, uint Src3) =>
        (Src0 & D3D12_SHADER_COMPONENT_MAPPING_MASK)
        | ((Src1 & D3D12_SHADER_COMPONENT_MAPPING_MASK) << (int)D3D12_SHADER_COMPONENT_MAPPING_SHIFT)
        | ((Src2 & D3D12_SHADER_COMPONENT_MAPPING_MASK) << (int)(D3D12_SHADER_COMPONENT_MAPPING_SHIFT * 2))
        | ((Src3 & D3D12_SHADER_COMPONENT_MAPPING_MASK) << (int)(D3D12_SHADER_COMPONENT_MAPPING_SHIFT * 3))
        | D3D12_SHADER_COMPONENT_MAPPING_ALWAYS_SET_BIT_AVOIDING_ZEROMEM_MISTAKES;

    internal static unsafe PCSTR AllocatePCSTR(string str) => (PCSTR)(byte*)Marshal.StringToHGlobalAnsi(str);
}
