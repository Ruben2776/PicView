namespace PicView.Core.Preloading;

public static class PreLoaderConfig
{
    public static int PositiveIterations => Settings.Navigation.PositiveIterations;
    public static int NegativeIterations => Settings.Navigation.NegativeIterations;
    
    /// Total items to preload forward and backward, +2 to account for the current active image and a potential side-by-side image!
    public static int MaxCount => PositiveIterations + NegativeIterations + 2;
    
    /// Leave a few cores for the UI thread and other system processes to ensure responsiveness.
    public static int MaxParallelism { get; } = Environment.ProcessorCount - 1;
}