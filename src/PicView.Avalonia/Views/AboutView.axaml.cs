using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Core.Config;
using System.Runtime.InteropServices;

namespace PicView.Avalonia.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        Loaded += (sender, e) =>
        {
            // TODO: Add version check when ready for release
            // AppVersion.Text = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            //     VersionHelper.GetFileVersionInfo().FileVersion :
            //     GetType().Assembly.GetName().Version.ToString();
            AppVersion.Text = "Avalonia Beta Preview 1";

            // TODO Check if https://github.com/NetSparkleUpdater/NetSparkle is a good choice for auto-updates

            KofiImage.PointerEntered += (_, _) =>
            {
                if (!TryGetResource("kofi_button_redDrawingImage", ThemeVariant.Default, out var redDrawing))
                {
                    return;
                }

                if (redDrawing is DrawingImage drawingImage)
                {
                    KofiImage.Source = drawingImage;
                }
            };
            KofiImage.PointerExited += (_, _) =>
            {
                if (!TryGetResource("kofi_button_strokeDrawingImage", ThemeVariant.Default, out var drawing))
                {
                    return;
                }

                if (drawing is DrawingImage drawingImage)
                {
                    KofiImage.Source = drawingImage;
                }
            };
        };
    }
}