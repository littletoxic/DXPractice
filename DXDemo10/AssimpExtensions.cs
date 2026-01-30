using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Assimp;

internal static partial class PInvoke {
    internal const string _AI_MATKEY_NAME_BASE = "?mat.name";
}

internal partial struct AssimpString {
    public override string ToString() =>
        Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<__CHAR_1024, byte>(ref data), (int)length));
}

internal partial struct Mesh {
    public bool HasTextureCoords(uint index) {
        if (index >= AI_MAX_NUMBER_OF_TEXTURECOORDS) {
            return false;
        }
        return !Unsafe.IsNullRef(ref mTextureCoords[index]) && mNumVertices > 0;
    }


}

internal partial struct P__AssimpVector3D_8 {
    public unsafe ref AssimpVector3D this[uint index] => ref *((AssimpVector3D**)Unsafe.AsPointer(ref this))[index];
}