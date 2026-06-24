using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace DXDemo18.Benchmarks;

internal enum ContainmentType {
    DISJOINT = 0,
    INTERSECTS = 1,
    CONTAINS = 2,
}

internal readonly record struct CollisionBounds(Vector3 Center, Vector3 Extents);

internal readonly record struct CollisionRay(Vector3 Origin, Vector3 Direction);

internal static class OriginalScalarBoundingBox {
    private const float RayEpsilon = 1e-6f;

    internal static ContainmentType Contains(CollisionBounds bounds, Vector3 point) =>
        MathF.Abs(point.X - bounds.Center.X) <= bounds.Extents.X &&
        MathF.Abs(point.Y - bounds.Center.Y) <= bounds.Extents.Y &&
        MathF.Abs(point.Z - bounds.Center.Z) <= bounds.Extents.Z
            ? ContainmentType.CONTAINS
            : ContainmentType.DISJOINT;

    internal static bool Intersects(CollisionBounds bounds, Vector3 origin, Vector3 direction, out float distance) {
        var min = bounds.Center - bounds.Extents;
        var max = bounds.Center + bounds.Extents;
        var tMin = 0.0f;
        var tMax = float.MaxValue;

        if (!IntersectSlab(origin.X, direction.X, min.X, max.X, ref tMin, ref tMax) ||
            !IntersectSlab(origin.Y, direction.Y, min.Y, max.Y, ref tMin, ref tMax) ||
            !IntersectSlab(origin.Z, direction.Z, min.Z, max.Z, ref tMin, ref tMax)) {
            distance = 0.0f;
            return false;
        }

        distance = tMin;
        return true;
    }

    private static bool IntersectSlab(float origin, float direction, float min, float max, ref float tMin, ref float tMax) {
        if (MathF.Abs(direction) < RayEpsilon)
            return origin >= min && origin <= max;

        var t1 = (min - origin) / direction;
        var t2 = (max - origin) / direction;

        if (t1 > t2)
            (t1, t2) = (t2, t1);

        tMin = MathF.Max(tMin, t1);
        tMax = MathF.Min(tMax, t2);

        return tMin <= tMax;
    }
}

internal static class CurrentVector3BoundingBox {
    private const float RayEpsilon = 1e-20f;

    internal static ContainmentType Contains(CollisionBounds bounds, Vector3 point) =>
        Vector3.LessThanOrEqualAll(Vector3.Abs(point - bounds.Center), bounds.Extents)
            ? ContainmentType.CONTAINS
            : ContainmentType.DISJOINT;

    internal static bool Intersects(CollisionBounds bounds, Vector3 origin, Vector3 direction, out float distance) {
        Debug.Assert(MathF.Abs(direction.LengthSquared() - 1.0f) < 1e-4f);

        var axisDotOrigin = bounds.Center - origin;

        if (IsSeparatedParallelAxis(axisDotOrigin.X, direction.X, bounds.Extents.X) ||
            IsSeparatedParallelAxis(axisDotOrigin.Y, direction.Y, bounds.Extents.Y) ||
            IsSeparatedParallelAxis(axisDotOrigin.Z, direction.Z, bounds.Extents.Z)) {
            distance = 0.0f;
            return false;
        }

        var inverseDirection = new Vector3(
            GetInverseDirectionOrZero(direction.X),
            GetInverseDirectionOrZero(direction.Y),
            GetInverseDirectionOrZero(direction.Z));

        var t1 = (axisDotOrigin - bounds.Extents) * inverseDirection;
        var t2 = (axisDotOrigin + bounds.Extents) * inverseDirection;
        var tNear = Vector3.Min(t1, t2);
        var tFar = Vector3.Max(t1, t2);

        IgnoreParallelAxis(direction.X, ref tNear.X, ref tFar.X);
        IgnoreParallelAxis(direction.Y, ref tNear.Y, ref tFar.Y);
        IgnoreParallelAxis(direction.Z, ref tNear.Z, ref tFar.Z);

        var tMin = MaxComponent(tNear);
        var tMax = MinComponent(tFar);

        if (tMin > tMax || tMax < 0.0f) {
            distance = 0.0f;
            return false;
        }

        distance = tMin;
        return true;
    }

    private static bool IsParallel(float axisDotDirection) => MathF.Abs(axisDotDirection) <= RayEpsilon;

    private static bool IsSeparatedParallelAxis(float axisDotOrigin, float axisDotDirection, float extent) =>
        IsParallel(axisDotDirection) && MathF.Abs(axisDotOrigin) > extent;

    private static float GetInverseDirectionOrZero(float axisDotDirection) =>
        IsParallel(axisDotDirection) ? 0.0f : 1.0f / axisDotDirection;

    private static void IgnoreParallelAxis(float axisDotDirection, ref float tNear, ref float tFar) {
        if (!IsParallel(axisDotDirection))
            return;

        tNear = -float.MaxValue;
        tFar = float.MaxValue;
    }

    private static float MaxComponent(Vector3 vector) => MathF.Max(vector.X, MathF.Max(vector.Y, vector.Z));

    private static float MinComponent(Vector3 vector) => MathF.Min(vector.X, MathF.Min(vector.Y, vector.Z));
}

internal static class DxMathLikeBoundingBox {
    private const byte XIndex = 0;
    private const byte YIndex = 1;
    private const byte ZIndex = 2;

    private static readonly Vector3 RayEpsilon = new(1e-20f);
    private static readonly Vector3 FloatMin = new(-float.MaxValue);
    private static readonly Vector3 FloatMax = new(float.MaxValue);

    internal static ContainmentType Contains(CollisionBounds bounds, Vector3 point) =>
        Vector3.AllWhereAllBitsSet(Vector3.LessThanOrEqual(Vector3.Abs(point - bounds.Center), bounds.Extents))
            ? ContainmentType.CONTAINS
            : ContainmentType.DISJOINT;

    internal static bool Intersects(CollisionBounds bounds, Vector3 origin, Vector3 direction, out float distance) {
        Debug.Assert(MathF.Abs(direction.LengthSquared() - 1.0f) < 1e-4f);

        var axisDotOrigin = bounds.Center - origin;
        var axisDotDirection = direction;

        var isParallel = Vector3.LessThanOrEqual(Vector3.Abs(axisDotDirection), RayEpsilon);
        var inverseAxisDotDirection = Vector3.One / axisDotDirection;

        var t1 = (axisDotOrigin - bounds.Extents) * inverseAxisDotDirection;
        var t2 = (axisDotOrigin + bounds.Extents) * inverseAxisDotDirection;

        var tMinVector = Vector3.ConditionalSelect(isParallel, FloatMin, Vector3.Min(t1, t2));
        var tMaxVector = Vector3.ConditionalSelect(isParallel, FloatMax, Vector3.Max(t1, t2));

        tMinVector = Vector3.Max(tMinVector, SplatY(tMinVector));
        tMinVector = Vector3.Max(tMinVector, SplatZ(tMinVector));
        tMaxVector = Vector3.Min(tMaxVector, SplatY(tMaxVector));
        tMaxVector = Vector3.Min(tMaxVector, SplatZ(tMaxVector));

        var tMinSplat = SplatX(tMinVector);
        var tMaxSplat = SplatX(tMaxVector);

        var noIntersection = Vector3.GreaterThan(tMinSplat, tMaxSplat);
        noIntersection = Vector3.BitwiseOr(noIntersection, Vector3.LessThan(tMaxSplat, Vector3.Zero));

        var parallelOverlap = Vector3.LessThanOrEqual(Vector3.Abs(axisDotOrigin), bounds.Extents);
        noIntersection = Vector3.BitwiseOr(noIntersection, Vector3.AndNot(isParallel, parallelOverlap));

        if (!Vector3.AnyWhereAllBitsSet(noIntersection)) {
            distance = tMinVector.X;
            return true;
        }

        distance = 0.0f;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SplatX(Vector3 vector) => Vector3.Shuffle(vector, XIndex, XIndex, XIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SplatY(Vector3 vector) => Vector3.Shuffle(vector, YIndex, YIndex, YIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SplatZ(Vector3 vector) => Vector3.Shuffle(vector, ZIndex, ZIndex, ZIndex);
}

internal static class Vector128BoundingBox {
    private const byte SplatXControl = 0x00;
    private const byte SplatYControl = 0x55;
    private const byte SplatZControl = 0xAA;

    private static readonly Vector128<float> SignMask = Vector128.Create(-0.0f);
    private static readonly Vector128<float> RayEpsilon = Vector128.Create(1e-20f);
    private static readonly Vector128<float> FloatMin = Vector128.Create(-float.MaxValue);
    private static readonly Vector128<float> FloatMax = Vector128.Create(float.MaxValue);

    internal static ContainmentType Contains(CollisionBounds bounds, Vector3 point) {
        if (!Sse.IsSupported)
            return DxMathLikeBoundingBox.Contains(bounds, point);

        var offset = Abs(Create(point - bounds.Center));
        var extents = Create(bounds.Extents);
        var containsMask = Sse.MoveMask(Sse.CompareLessThanOrEqual(offset, extents));

        return (containsMask & 0b111) == 0b111
            ? ContainmentType.CONTAINS
            : ContainmentType.DISJOINT;
    }

    internal static bool Intersects(CollisionBounds bounds, Vector3 origin, Vector3 direction, out float distance) {
        if (!Sse.IsSupported)
            return DxMathLikeBoundingBox.Intersects(bounds, origin, direction, out distance);

        Debug.Assert(MathF.Abs(direction.LengthSquared() - 1.0f) < 1e-4f);

        var axisDotOrigin = Create(bounds.Center - origin);
        var axisDotDirection = Create(direction);
        var extents = Create(bounds.Extents);

        var isParallel = Sse.CompareLessThanOrEqual(Abs(axisDotDirection), RayEpsilon);
        var inverseAxisDotDirection = Sse.Divide(Vector128<float>.One, axisDotDirection);

        var t1 = Sse.Multiply(Sse.Subtract(axisDotOrigin, extents), inverseAxisDotDirection);
        var t2 = Sse.Multiply(Sse.Add(axisDotOrigin, extents), inverseAxisDotDirection);

        var tMinVector = Select(isParallel, FloatMin, Sse.Min(t1, t2));
        var tMaxVector = Select(isParallel, FloatMax, Sse.Max(t1, t2));

        tMinVector = Sse.Max(tMinVector, SplatY(tMinVector));
        tMinVector = Sse.Max(tMinVector, SplatZ(tMinVector));
        tMaxVector = Sse.Min(tMaxVector, SplatY(tMaxVector));
        tMaxVector = Sse.Min(tMaxVector, SplatZ(tMaxVector));

        var tMinSplat = SplatX(tMinVector);
        var tMaxSplat = SplatX(tMaxVector);

        var noIntersection = Sse.CompareGreaterThan(tMinSplat, tMaxSplat);
        noIntersection = Sse.Or(noIntersection, Sse.CompareLessThan(tMaxSplat, Vector128<float>.Zero));

        var parallelOverlap = Sse.CompareLessThanOrEqual(Abs(axisDotOrigin), extents);
        noIntersection = Sse.Or(noIntersection, Sse.AndNot(parallelOverlap, isParallel));

        if ((Sse.MoveMask(noIntersection) & 0b111) == 0) {
            distance = tMinVector.GetElement(0);
            return true;
        }

        distance = 0.0f;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> Create(Vector3 vector, float w = 0.0f) =>
        Vector128.Create(vector.X, vector.Y, vector.Z, w);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> Abs(Vector128<float> value) => Sse.AndNot(SignMask, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> Select(Vector128<float> mask, Vector128<float> trueValue, Vector128<float> falseValue) =>
        Sse.Or(Sse.And(mask, trueValue), Sse.AndNot(mask, falseValue));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> SplatX(Vector128<float> vector) => Sse.Shuffle(vector, vector, SplatXControl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> SplatY(Vector128<float> vector) => Sse.Shuffle(vector, vector, SplatYControl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> SplatZ(Vector128<float> vector) => Sse.Shuffle(vector, vector, SplatZControl);
}

internal static class Avx256BoundingBox {
    private const byte SplatXControl = 0x00;
    private const byte SplatYControl = 0x55;
    private const byte SplatZControl = 0xAA;

    private static readonly Vector256<float> SignMask = Vector256.Create(-0.0f);
    private static readonly Vector256<float> RayEpsilon = Vector256.Create(1e-20f);
    private static readonly Vector256<float> FloatMin = Vector256.Create(-float.MaxValue);
    private static readonly Vector256<float> FloatMax = Vector256.Create(float.MaxValue);

    internal static ContainmentType Contains(CollisionBounds bounds, Vector3 point) {
        if (!Avx.IsSupported)
            return Vector128BoundingBox.Contains(bounds, point);

        var offset = Abs(Create(point - bounds.Center));
        var extents = Create(bounds.Extents);
        var containsMask = Avx.MoveMask(Avx.CompareLessThanOrEqual(offset, extents));

        return (containsMask & 0b111) == 0b111
            ? ContainmentType.CONTAINS
            : ContainmentType.DISJOINT;
    }

    internal static bool Intersects(CollisionBounds bounds, Vector3 origin, Vector3 direction, out float distance) {
        if (!Avx.IsSupported)
            return Vector128BoundingBox.Intersects(bounds, origin, direction, out distance);

        Debug.Assert(MathF.Abs(direction.LengthSquared() - 1.0f) < 1e-4f);

        var axisDotOrigin = Create(bounds.Center - origin);
        var axisDotDirection = Create(direction);
        var extents = Create(bounds.Extents);

        var isParallel = Avx.CompareLessThanOrEqual(Abs(axisDotDirection), RayEpsilon);
        var inverseAxisDotDirection = Avx.Divide(Vector256<float>.One, axisDotDirection);

        var t1 = Avx.Multiply(Avx.Subtract(axisDotOrigin, extents), inverseAxisDotDirection);
        var t2 = Avx.Multiply(Avx.Add(axisDotOrigin, extents), inverseAxisDotDirection);

        var tMinVector = Select(isParallel, FloatMin, Avx.Min(t1, t2));
        var tMaxVector = Select(isParallel, FloatMax, Avx.Max(t1, t2));

        tMinVector = Avx.Max(tMinVector, SplatY(tMinVector));
        tMinVector = Avx.Max(tMinVector, SplatZ(tMinVector));
        tMaxVector = Avx.Min(tMaxVector, SplatY(tMaxVector));
        tMaxVector = Avx.Min(tMaxVector, SplatZ(tMaxVector));

        var tMinSplat = SplatX(tMinVector);
        var tMaxSplat = SplatX(tMaxVector);

        var noIntersection = Avx.CompareGreaterThan(tMinSplat, tMaxSplat);
        noIntersection = Avx.Or(noIntersection, Avx.CompareLessThan(tMaxSplat, Vector256<float>.Zero));

        var parallelOverlap = Avx.CompareLessThanOrEqual(Abs(axisDotOrigin), extents);
        noIntersection = Avx.Or(noIntersection, Avx.AndNot(parallelOverlap, isParallel));

        if ((Avx.MoveMask(noIntersection) & 0b111) == 0) {
            distance = tMinVector.GetElement(0);
            return true;
        }

        distance = 0.0f;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> Create(Vector3 vector, float rest = 0.0f) =>
        Vector256.Create(vector.X, vector.Y, vector.Z, rest, rest, rest, rest, rest);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> Abs(Vector256<float> value) => Avx.AndNot(SignMask, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> Select(Vector256<float> mask, Vector256<float> trueValue, Vector256<float> falseValue) =>
        Avx.Or(Avx.And(mask, trueValue), Avx.AndNot(mask, falseValue));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> SplatX(Vector256<float> vector) => Avx.Permute(vector, SplatXControl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> SplatY(Vector256<float> vector) => Avx.Permute(vector, SplatYControl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> SplatZ(Vector256<float> vector) => Avx.Permute(vector, SplatZControl);
}
