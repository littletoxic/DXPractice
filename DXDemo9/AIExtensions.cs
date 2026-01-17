using System.Runtime.CompilerServices;
using System.Text;

namespace Assimp;

internal partial struct aiString {
    public unsafe override string ToString() {
        return Encoding.UTF8.GetString((byte*)Unsafe.AsPointer(ref data), (int)length);
    }
}