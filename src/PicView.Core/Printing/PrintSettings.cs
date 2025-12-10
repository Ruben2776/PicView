using R3;

namespace PicView.Core.Printing
{
    public class PrintSettings
    {
        /// <summary>
        /// Path to the image to print.
        /// <remarks>Should point to a temporary file of a common format</remarks>
        /// </summary>
        public BindableReactiveProperty<string?> ImagePath { get; } = new();
        public BindableReactiveProperty<string?> PrinterName { get; } = new();
        public BindableReactiveProperty<string?> PaperSize { get; } = new();
        public BindableReactiveProperty<int> Orientation { get; } = new((int)Orientations.Portrait);
        public BindableReactiveProperty<int> ScaleMode { get; } = new((int)ScaleModes.Fit);
        public BindableReactiveProperty<int> ColorMode { get; } = new((int)ColorModes.Auto);
        public BindableReactiveProperty<int> Copies { get; } = new(1);
        public BindableReactiveProperty<int> MarginTop { get; } = new(0);
        public BindableReactiveProperty<int> MarginBottom { get; } = new(0);
        public BindableReactiveProperty<int> MarginLeft { get; } = new(0);
        public BindableReactiveProperty<int> MarginRight { get; } = new(0);


        public static int HundredthsInchToMm(int hundredthsInch) => (int)Math.Round(hundredthsInch * 0.254);

        public static int MmToHundredthsInch(double mm) => (int)Math.Round(mm / 0.254);

    }

    public enum ScaleModes
    {
        Fit,
        Fill,
        Stretch,
        Center
    }

    public enum ColorModes
    {
        Auto,
        Color,
        BlackAndWhite
    }

    public enum Orientations
    {
        Portrait,
        Landscape
    }
}
