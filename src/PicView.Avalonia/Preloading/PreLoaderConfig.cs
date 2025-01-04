namespace PicView.Avalonia.Preloading;

public class PreLoaderConfig
{
    public static int PositiveIterations => 6;
    public static int NegativeIterations => 4;
    public static int MaxCount => PositiveIterations + NegativeIterations + 2;
    public int MaxParallelism { get; } = Math.Max(1, Environment.ProcessorCount - 3);
}