using BenchmarkDotNet.Running;
using PicView.Benchmarks;
using PicView.Benchmarks.Gallery;
using PicView.Benchmarks.Images;
using PicView.Benchmarks.StartupBenchmarks;
using PicView.Benchmarks.StringBenchmarks;
using PicView.Benchmarks.Svg;

/*

dotnet run -c Release --project src/PicView.Benchmarks

 */

// BenchmarkRunner.Run<EvictingDictionaryBenchmark>();
// BenchmarkRunner.Run<ImageBenchmarks>();
//BenchmarkRunner.Run<PreloadingBenchmark>();
//BenchmarkRunner.Run<TranslationBenchmarks>();
BenchmarkRunner.Run<GalleryLoadBenchmarks>();
//BenchmarkRunner.Run<SvgLoadBenchmarks>();
//BenchmarkRunner.Run<LanguageBenchmark>();
//BenchmarkRunner.Run<FileSizeBenchmark>();
//BenchmarkRunner.Run<ConfigBenchmark>();
//BenchmarkRunner.Run<ImageCacheBenchmarks>();
//BenchmarkRunner.Run<AspectRatioBenchmark>();