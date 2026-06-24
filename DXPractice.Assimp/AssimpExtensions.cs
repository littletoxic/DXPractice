using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Assimp.PInvoke;

namespace Assimp;

public static partial class PInvoke {
    public const string _AI_MATKEY_NAME_BASE = "?mat.name";
    public const string _AI_MATKEY_COLOR_DIFFUSE_BASE = "$clr.diffuse";
    public const string _AI_MATKEY_COLOR_SPECULAR_BASE = "$clr.specular";
    public const string _AI_MATKEY_COLOR_EMISSIVE_BASE = "$clr.emissive";
    public const string _AI_MATKEY_TWOSIDED_BASE = "$mat.twosided";
    public const string _AI_MATKEY_SHININESS_BASE = "$mat.shininess";

    public static unsafe ref Scene ImportFileR(string pFile, uint pFlags) => ref *ImportFile(pFile, pFlags);

    public static ReturnCode GetMaterialFloat(in Material pMat, string pKey, uint type, uint index, out float value) {
        Unsafe.SkipInit(out value);
        return GetMaterialFloatArray(pMat, pKey, type, index, new(ref value));
    }

    public static ReturnCode GetMaterialInteger(in Material pMat, string pKey, uint type, uint index, out int value) {
        Unsafe.SkipInit(out value);
        return GetMaterialIntegerArray(pMat, pKey, type, index, new(ref value));
    }
}

public partial struct AssimpString {
    public override string ToString() =>
        Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<__CHAR_1024, byte>(ref data), (int)length));
}

public partial struct Node {
    public readonly unsafe PtrSpan<Node> Children => new(mChildren, mNumChildren);

    public readonly unsafe ReadOnlySpan<uint> Meshes => new(mMeshes, (int)mNumMeshes);
}

public partial struct Scene {
    public readonly unsafe PtrSpan<Material> Materials => new(mMaterials, mNumMaterials);

    public readonly unsafe ref Node RootNode => ref *mRootNode;

    public readonly unsafe PtrSpan<Mesh> Meshes => new(mMeshes, mNumMeshes);

    public readonly unsafe PtrSpan<Animation> Animations => new(mAnimations, mNumAnimations);

    public readonly unsafe bool HasAnimations() => mAnimations != null && mNumAnimations > 0;
}

public partial struct Mesh {
    public readonly unsafe PtrSpan<Bone> Bones => new(mBones, mNumBones);

    public readonly unsafe bool HasTextureCoords(uint index) {
        if (index >= AI_MAX_NUMBER_OF_TEXTURECOORDS) {
            return false;
        }
        return mTextureCoords[index] != null && mNumVertices > 0;
    }

    public readonly unsafe ReadOnlySpan<AssimpVector3D> Vertices => new(mVertices, (int)mNumVertices);

    public readonly unsafe ReadOnlySpan<Face> Faces => new(mFaces, (int)mNumFaces);

    public readonly unsafe ReadOnlySpan<AssimpVector3D> TextureCoords(uint index) {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, AI_MAX_NUMBER_OF_TEXTURECOORDS);
        return new(mTextureCoords[index], (int)mNumVertices);
    }

    public readonly unsafe bool HasVertexColors(uint index) {
        if (index >= AI_MAX_NUMBER_OF_COLOR_SETS) {
            return false;
        }
        return mColors[index] != null && mNumVertices > 0;
    }

    public readonly unsafe ReadOnlySpan<Color4D> Colors(uint index) {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, AI_MAX_NUMBER_OF_COLOR_SETS);
        return new(mColors[index], (int)mNumVertices);
    }

    public readonly unsafe bool HasBones() => mBones != null && mNumBones > 0;

    public readonly unsafe ReadOnlySpan<AssimpVector3D> Normals => new(mNormals, (int)mNumVertices);
}

public partial struct Face {
    public readonly unsafe ReadOnlySpan<uint> Indices => new(mIndices, (int)mNumIndices);
}

public partial struct Bone {
    public readonly unsafe ReadOnlySpan<VertexWeight> Weights => new(mWeights, (int)mNumWeights);
}

public readonly unsafe ref struct PtrSpan<T>(T** source, uint count) where T : unmanaged {
    public int Length => (int)count;
    public ref T this[int index] => ref *source[index];
    public PtrSpanEnumerator GetEnumerator() => new(this);

    public ref struct PtrSpanEnumerator(PtrSpan<T> ptrSpan) : IEnumerator<T> {
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

public partial struct P__AssimpVector3D_8 {
    public unsafe AssimpVector3D* this[uint index] => ((AssimpVector3D**)Unsafe.AsPointer(ref this))[index];
}

public partial struct P__Color4D_8 {
    public unsafe Color4D* this[uint index] => ((Color4D**)Unsafe.AsPointer(ref this))[index];
}

public static class Extensions {
    extension(Unsafe) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T AsRef<T>(nint source) where T : allows ref struct => ref Unsafe.AsRef<T>((void*)source);
    }
}

public partial struct AssimpMatrix4x4 {
    public static implicit operator Matrix4x4(AssimpMatrix4x4 m) => Unsafe.BitCast<AssimpMatrix4x4, Matrix4x4>(m);
}

public partial struct AssimpVector3D {
    public static implicit operator Vector3(AssimpVector3D v) => Unsafe.BitCast<AssimpVector3D, Vector3>(v);
}

public partial struct Color4D {
    public static implicit operator Vector4(Color4D c) => Unsafe.BitCast<Color4D, Vector4>(c);
}

public partial struct NodeAnim {
    public readonly unsafe ReadOnlySpan<VectorKey> PositionKeys => new(mPositionKeys, (int)mNumPositionKeys);

    public readonly unsafe ReadOnlySpan<VectorKey> ScalingKeys => new(mScalingKeys, (int)mNumScalingKeys);

    public readonly unsafe ReadOnlySpan<QuatKey> RotationKeys => new(mRotationKeys, (int)mNumRotationKeys);
}

public partial struct AssimpQuaternion {
    public static implicit operator Quaternion(AssimpQuaternion q) => new(q.x, q.y, q.z, q.w);
}

public partial struct Animation {
    public readonly unsafe PtrSpan<NodeAnim> Channels => new(mChannels, mNumChannels);
}
