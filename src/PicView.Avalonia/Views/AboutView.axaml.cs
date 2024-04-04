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
            AppVersion.Text = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                VersionHelper.GetFileVersionInfo().FileVersion :
                GetType().Assembly.GetName().Version.ToString();

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