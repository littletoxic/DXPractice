using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Assimp;

internal static partial class PInvoke {
    internal const string _AI_MATKEY_NAME_BASE = "?mat.name";

    internal static unsafe ref Scene ImportFileR(string pFile, uint pFlags) {
        return ref *ImportFile(pFile, pFlags);
    }
}

internal partial struct AssimpString {
    public override string ToString() =>
        Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<__CHAR_1024, byte>(ref data), (int)length));
}

internal partial struct Node {
    internal readonly unsafe PtrSpan<Node> Children => new(mChildren, mNumChildren);
}

internal partial struct Scene {
    internal readonly unsafe PtrSpan<Material> Materials => new(mMaterials, mNumMaterials);

    internal readonly unsafe ref Node RootNode => ref *mRootNode;

    internal readonly unsafe PtrSpan<Mesh> Meshes => new(mMeshes, mNumMeshes);
}

internal unsafe partial struct Mesh {
    internal readonly unsafe PtrSpan<Bone> Bones => new(mBones, mNumBones);
}

internal unsafe ref struct PtrSpan<T>(T** ptr, uint count) where T : unmanaged {
    public readonly int Length => (int)count;
    public readonly ref T this[int index] => ref *ptr[index];
    public readonly PtrSpanEnumerator<T> GetEnumerator() => new(ptr, count);
}

internal unsafe ref struct PtrSpanEnumerator<T>(T** ptr, uint count) where T : unmanaged {
    private int _index = -1;
    public readonly ref T Current => ref *ptr[_index];
    public bool MoveNext() => ++_index < (int)count;
}
