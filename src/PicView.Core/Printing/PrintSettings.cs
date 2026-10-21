using R3;

namespace PicView.Core.Printing
{
    /// <summary>
    /// The Printer, paper, orientation, Scale Mode, color mode, copies and margins of a Print Job. Margins are in
    /// millimeters.
    /// </summary>
    public class PrintSettings
    {
        public BindableReactiveProperty<Printer?> Printer { get; } = new();
        public BindableReactiveProperty<string?> PaperSize { get; } = new();
        public BindableReactiveProperty<int> Orientation { get; } = new((int)Orientations.Portrait);
        public BindableReactiveProperty<int> ScaleMode { get; } = new((int)ScaleModes.Fit);
        public BindableReactiveProperty<int> ColorMode { get; } = new((int)ColorModes.Auto);
        public BindableReactiveProperty<int> Copies { get; } = new(1);
        public BindableReactiveProperty<int> MarginTop { get; } = new(0);
        public BindableReactiveProperty<int> MarginBottom { get; } = new(0);
        public BindableReactiveProperty<int> MarginLeft { get; } = new(0);
        public BindableReactiveProperty<int> MarginRight { get; } = new(0);
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
