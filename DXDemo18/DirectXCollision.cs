using System.Numerics;

namespace DXDemo18;

internal enum ContainmentType {
    DISJOINT = 0,
    INTERSECTS = 1,
    CONTAINS = 2,
}

internal struct BoundingBox {
    private const float RayEpsilon = 1e-6f;

    internal Vector3 Center;
    internal Vector3 Extents;

    public BoundingBox() {
        Center = Vector3.Zero;
        Extents = Vector3.One;
    }

    internal BoundingBox(Vector3 center, Vector3 extents) {
        Center = center;
        Extents = extents;
    }

    internal readonly ContainmentType Contains(Vector3 point) => Vector3.LessThanOrEqualAll(Vector3.Abs(point - Center), Extents)
            ? ContainmentType.CONTAINS
            : ContainmentType.DISJOINT;

    internal readonly bool Intersects(Vector3 origin, Vector3 direction, out float distance) {
        var min = Center - Extents;
        var max = Center + Extents;
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
