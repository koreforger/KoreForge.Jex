using BenchmarkDotNet.Running;
using KoreForge.Jex.Benchmarks;

// Run all benchmarks
BenchmarkRunner.Run<JexBenchmarks>();
BenchmarkRunner.Run<NestedJsonBenchmarks>();
