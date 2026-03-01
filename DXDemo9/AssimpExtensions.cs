using System.Collections;
using System.Numerics;
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
    internal readonly PtrSpan<Bone> Bones => new(mBones, mNumBones);
}

internal unsafe ref struct PtrSpan<T>(T** ptr, uint count) where T : unmanaged {
    public readonly int Length => (int)count;
    public readonly ref T this[int index] => ref *ptr[index];
    public readonly PtrSpanEnumerator GetEnumerator() => new(this);


    internal ref struct PtrSpanEnumerator(PtrSpan<T> ptrSpan) : IEnumerator<T> {
        private readonly PtrSpan<T> _ptrSpan = ptrSpan;
        private int _index = -1;

        public bool MoveNext() => ++_index < _ptrSpan.Length;

        public readonly ref T Current => ref _ptrSpan[_index];

        readonly T IEnumerator<T>.Current => Current;
        readonly object IEnumerator.Current => Current;

        public readonly void Dispose() { }
        public void Reset() => _index = -1;
    }

}

internal partial struct AssimpVector3D {
    public static implicit operator Vector3(AssimpVector3D v) => Unsafe.As<AssimpVector3D, Vector3>(ref v);
}