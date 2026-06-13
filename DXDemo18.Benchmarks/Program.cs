using BenchmarkDotNet.Running;
using DXDemo18.Benchmarks;

CollisionValidation.EnsureVariantsAgree();
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
