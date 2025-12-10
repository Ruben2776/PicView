using BenchmarkDotNet.Running;
using PicView.Benchmarks.StartupBenchmarks;

/*

dotnet run -c Release --project src/PicView.Benchmarks

 */

// BenchmarkRunner.Run<EvictingDictionaryBenchmark>();
// BenchmarkRunner.Run<ImageBenchmarks>();
//BenchmarkRunner.Run<PreloadingBenchmark>();
// BenchmarkRunner.Run<TranslationBenchmarks>();
//BenchmarkRunner.Run<LanguageBenchmark>();
//BenchmarkRunner.Run<FileSizeBenchmark>();
BenchmarkRunner.Run<ConfigBenchmark>();