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

        /// <summary>
        /// Sets every setting to the one in <paramref name="other"/>, keeping these properties, so what follows them
        /// sees the change.
        /// </summary>
        public void CopyFrom(PrintSettings other)
        {
            Printer.Value = other.Printer.Value;
            PaperSize.Value = other.PaperSize.Value;
            Orientation.Value = other.Orientation.Value;
            ScaleMode.Value = other.ScaleMode.Value;
            ColorMode.Value = other.ColorMode.Value;
            Copies.Value = other.Copies.Value;
            MarginTop.Value = other.MarginTop.Value;
            MarginBottom.Value = other.MarginBottom.Value;
            MarginLeft.Value = other.MarginLeft.Value;
            MarginRight.Value = other.MarginRight.Value;
        }

        /// <summary>
        /// Whether every setting is the one in <paramref name="other"/>. Printers are the same when their
        /// <see cref="Printing.Printer.Id"/> is, whatever language names them.
        /// </summary>
        public bool Matches(PrintSettings other) =>
            string.Equals(Printer.Value?.Id, other.Printer.Value?.Id, StringComparison.Ordinal) &&
            string.Equals(PaperSize.Value, other.PaperSize.Value, StringComparison.Ordinal) &&
            Orientation.Value == other.Orientation.Value &&
            ScaleMode.Value == other.ScaleMode.Value &&
            ColorMode.Value == other.ColorMode.Value &&
            Copies.Value == other.Copies.Value &&
            MarginTop.Value == other.MarginTop.Value &&
            MarginBottom.Value == other.MarginBottom.Value &&
            MarginLeft.Value == other.MarginLeft.Value &&
            MarginRight.Value == other.MarginRight.Value;
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
