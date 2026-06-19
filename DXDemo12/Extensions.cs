using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D12;

namespace Windows.Win32;

internal static partial class PInvoke {
    internal static unsafe PCSTR AllocatePCSTR(string str) => (PCSTR)(byte*)Marshal.StringToHGlobalAnsi(str);
}

internal static class Extensions {
    // ID3D12GraphicsCommandList 实例扩展
    internal static unsafe void SetGraphicsRoot32BitConstants<T>(this ID3D12GraphicsCommandList commandList, uint RootParameterIndex, ReadOnlySpan<T> SrcData, uint DestOffsetIn32BitValues) where T : unmanaged {
        fixed (T* pSrcData = SrcData) {
            commandList.SetGraphicsRoot32BitConstants(RootParameterIndex, (uint)((SrcData.Length * Unsafe.SizeOf<T>() + sizeof(uint) - 1) / sizeof(uint)), pSrcData, DestOffsetIn32BitValues);
        }
    }
}
