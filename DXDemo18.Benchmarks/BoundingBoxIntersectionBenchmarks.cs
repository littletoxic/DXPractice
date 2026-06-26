using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace DXDemo18.Benchmarks;

[SimpleJob(RuntimeMoniker.NativeAot10_0)]
[MemoryDiagnoser]
public class BoundingBoxIntersectionBenchmarks {
    private const int OperationCount = 16_384;

    private CollisionBounds[] _bounds = [];
    private CollisionRay[] _rays = [];

    [GlobalSetup]
    public void Setup() => (_bounds, _rays) = CollisionData.Create(OperationCount);

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationCount)]
    public float OriginalScalar() => RunOriginalScalar();

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public float CurrentVector3() => RunCurrentVector3();

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public float DxMathLikeVector3() => RunDxMathLikeVector3();

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public float Vector128Intrinsics() => RunVector128Intrinsics();

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public float Avx256Intrinsics() => RunAvx256Intrinsics();

    private float RunOriginalScalar() {
        var checksum = 0.0f;

        for (var i = 0; i < OperationCount; i++) {
            var bounds = _bounds[i];
            var ray = _rays[i];

            if (OriginalScalarBoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
                continue;

            if (OriginalScalarBoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance))
                checksum += distance;
        }

        return checksum;
    }

    private float RunCurrentVector3() {
        var checksum = 0.0f;

        for (var i = 0; i < OperationCount; i++) {
            var bounds = _bounds[i];
            var ray = _rays[i];

            if (CurrentVector3BoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
                continue;

            if (CurrentVector3BoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance))
                checksum += distance;
        }

        return checksum;
    }

    private float RunDxMathLikeVector3() {
        var checksum = 0.0f;

        for (var i = 0; i < OperationCount; i++) {
            var bounds = _bounds[i];
            var ray = _rays[i];

            if (DxMathLikeBoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
                continue;

            if (DxMathLikeBoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance))
                checksum += distance;
        }

        return checksum;
    }

    private float RunVector128Intrinsics() {
        var checksum = 0.0f;

        for (var i = 0; i < OperationCount; i++) {
            var bounds = _bounds[i];
            var ray = _rays[i];

            if (Vector128BoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
                continue;

            if (Vector128BoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance))
                checksum += distance;
        }

        return checksum;
    }

    private float RunAvx256Intrinsics() {
        var checksum = 0.0f;

        for (var i = 0; i < OperationCount; i++) {
            var bounds = _bounds[i];
            var ray = _rays[i];

            if (Avx256BoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
                continue;

            if (Avx256BoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance))
                checksum += distance;
        }

        return checksum;
    }
}

internal static class CollisionValidation {
    internal static void EnsureVariantsAgree() {
        var (bounds, rays) = CollisionData.Create(16_384);

        for (var i = 0; i < bounds.Length; i++) {
            var original = EvaluateOriginal(bounds[i], rays[i]);
            var current = EvaluateCurrent(bounds[i], rays[i]);
            var dxMathLike = EvaluateDxMathLike(bounds[i], rays[i]);
            var vector128 = EvaluateVector128(bounds[i], rays[i]);
            var avx256 = EvaluateAvx256(bounds[i], rays[i]);

            if (!Matches(original, current) ||
                !Matches(original, dxMathLike) ||
                !Matches(original, vector128) ||
                !Matches(original, avx256)) {
                throw new InvalidOperationException($"Collision benchmark variants disagree at input {i}.");
            }
        }
    }

    private static CollisionResult EvaluateOriginal(CollisionBounds bounds, CollisionRay ray) {
        if (OriginalScalarBoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
            return new(false, 0.0f);

        return new(
            OriginalScalarBoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance),
            distance);
    }

    private static CollisionResult EvaluateCurrent(CollisionBounds bounds, CollisionRay ray) {
        if (CurrentVector3BoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
            return new(false, 0.0f);

        return new(
            CurrentVector3BoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance),
            distance);
    }

    private static CollisionResult EvaluateDxMathLike(CollisionBounds bounds, CollisionRay ray) {
        if (DxMathLikeBoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
            return new(false, 0.0f);

        return new(
            DxMathLikeBoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance),
            distance);
    }

    private static CollisionResult EvaluateVector128(CollisionBounds bounds, CollisionRay ray) {
        if (Vector128BoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
            return new(false, 0.0f);

        return new(
            Vector128BoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance),
            distance);
    }

    private static CollisionResult EvaluateAvx256(CollisionBounds bounds, CollisionRay ray) {
        if (Avx256BoundingBox.Contains(bounds, ray.Origin) == ContainmentType.CONTAINS)
            return new(false, 0.0f);

        return new(
            Avx256BoundingBox.Intersects(bounds, ray.Origin, ray.Direction, out var distance),
            distance);
    }

    private static bool Matches(CollisionResult left, CollisionResult right) {
        if (left.Intersects != right.Intersects)
            return false;

        return !left.Intersects || MathF.Abs(left.Distance - right.Distance) <= 1e-4f;
    }

    private readonly record struct CollisionResult(bool Intersects, float Distance);
}

internal static class CollisionData {
    internal static (CollisionBounds[] Bounds, CollisionRay[] Rays) Create(int count) {
        var bounds = new CollisionBounds[count];
        var rays = new CollisionRay[count];

        for (var i = 0; i < count; i++) {
            var center = new Vector3(
                ((i % 64) - 32) * 2.0f,
                ((i / 64 % 16) - 8) * 2.0f,
                ((i / 1024 % 64) - 32) * 2.0f);
            var extents = new Vector3(1.0f);

            bounds[i] = new(center, extents);
            rays[i] = CreateRay(i, center);
        }

        return (bounds, rays);
    }

    private static CollisionRay CreateRay(int index, Vector3 center) => (index & 7) switch {
        0 => new(center - new Vector3(16.0f, 0.0f, 0.0f), Vector3.UnitX),
        1 => CreateRayToward(center, Vector3.Normalize(new Vector3(1.0f, 0.35f, -0.2f)), 18.0f),
        2 => new(center + new Vector3(3.0f, -16.0f, 0.0f), Vector3.UnitY),
        3 => new(center + new Vector3(0.0f, 3.0f, -16.0f), Vector3.UnitZ),
        4 => new(center, Vector3.Normalize(new Vector3(0.2f, 0.9f, 0.1f))),
        5 => CreateRayToward(center, Vector3.Normalize(new Vector3(-0.6f, 0.45f, 0.65f)), 24.0f),
        6 => new(center + new Vector3(8.0f, 8.0f, 8.0f), Vector3.Normalize(new Vector3(0.8f, 0.1f, 0.2f))),
        _ => new(center + new Vector3(-3.0f, 0.0f, 12.0f), -Vector3.UnitZ),
    };

    private static CollisionRay CreateRayToward(Vector3 center, Vector3 direction, float distance) =>
        new(center - direction * distance, direction);
}
