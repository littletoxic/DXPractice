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

    internal readonly unsafe ReadOnlySpan<uint> Meshes => new(mMeshes, (int)mNumMeshes);

}

internal partial struct Scene {
    internal readonly unsafe PtrSpan<Material> Materials => new(mMaterials, mNumMaterials);

    internal readonly unsafe ref Node RootNode => ref *mRootNode;

    internal readonly unsafe PtrSpan<Mesh> Meshes => new(mMeshes, mNumMeshes);
}

internal partial struct Mesh {
    internal readonly unsafe PtrSpan<Bone> Bones => new(mBones, mNumBones);

    internal unsafe bool HasTextureCoords(uint index) {
        if (index >= AI_MAX_NUMBER_OF_TEXTURECOORDS) {
            return false;
        }
        return mTextureCoords[index] != null && mNumVertices > 0;
    }

    internal readonly unsafe ReadOnlySpan<AssimpVector3D> Vertices => new(mVertices, (int)mNumVertices);

    internal readonly unsafe ReadOnlySpan<Face> Faces => new(mFaces, (int)mNumFaces);

    internal readonly unsafe ReadOnlySpan<AssimpVector3D> TextureCoords(uint index) {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, AI_MAX_NUMBER_OF_TEXTURECOORDS);
        return new(mTextureCoords[index], (int)mNumVertices);
    }

    internal unsafe bool HasVertexColors(uint index) {
        if (index >= AI_MAX_NUMBER_OF_COLOR_SETS) {
            return false;
        }
        return mColors[index] != null && mNumVertices > 0;
    }

    internal readonly unsafe ReadOnlySpan<Color4D> Colors(uint index) {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, AI_MAX_NUMBER_OF_COLOR_SETS);
        return new(mColors[index], (int)mNumVertices);
    }

    internal readonly unsafe bool HasBones() => mBones != null && mNumBones > 0;
}

internal partial struct Face {
    internal readonly unsafe ReadOnlySpan<uint> Indices => new(mIndices, (int)mNumIndices);
}

internal partial struct Bone {
    internal readonly unsafe ReadOnlySpan<VertexWeight> Weights => new(mWeights, (int)mNumWeights);
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

internal partial struct P__AssimpVector3D_8 {
    public unsafe AssimpVector3D* this[uint index] => ((AssimpVector3D**)Unsafe.AsPointer(ref this))[index];
}

internal partial struct P__Color4D_8 {
    public unsafe Color4D* this[uint index] => ((Color4D**)Unsafe.AsPointer(ref this))[index];
}

internal static class Extensions {
    extension(Unsafe) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref T AsRef<T>(nint source) where T : allows ref struct => ref Unsafe.AsRef<T>((void*)source);
    }
}

internal partial struct AssimpMatrix4x4 {
    public static implicit operator Matrix4x4(AssimpMatrix4x4 m) => Unsafe.As<AssimpMatrix4x4, Matrix4x4>(ref m);

}

internal partial struct AssimpVector3D {
    public static implicit operator Vector3(AssimpVector3D v) => Unsafe.As<AssimpVector3D, Vector3>(ref v);
}

internal partial struct Color4D {
    public static implicit operator Vector4(Color4D c) => Unsafe.As<Color4D, Vector4>(ref c);
}