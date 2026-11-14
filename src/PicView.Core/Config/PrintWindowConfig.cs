namespace PicView.Core.Config;

public sealed class PrintWindowConfig() : RememberedWindowConfig<PrintWindowConfig.PrintWindowProperties>("PrintWindow.json")
{
    public class PrintWindowProperties : RememberedWindowProperties
    {
        public PrintWindowProperties()
        {
            Width = 850;
            Height = 495;
        }

        public string? PrinterName { get; set; }
        public string? PaperSize { get; set; }
        public int? Orientation { get; set; }
        public int? ScaleMode { get; set; }
        public int? ColorMode { get; set; }
        public int? Copies { get; set; }
        public int? MarginTop { get; set; }
        public int? MarginBottom { get; set; }
        public int? MarginLeft { get; set; }
        public int? MarginRight { get; set; }
    }
}
