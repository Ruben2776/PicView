using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class FileMenu : AnimatedMenu
{
    public FileMenu()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (Settings.Theme.GlassTheme)
            {
                if (Application.Current.TryGetResource("NoisyTexture",
                        ThemeVariant.Dark, out var texture))
                {
                    var brush = texture as ImageBrush;
                    MainBorder.Background = brush;
                    DownArrow.Fill = brush;
                    DownArrow.StrokeThickness = 0;
                }
            }

            if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
            {
                if (!Settings.Theme.GlassTheme)
                {
                    TopBorder.Background = Brushes.White;
                }

                NewWindowButton.Classes.Remove("altHover");
                NewWindowButton.Classes.Add("hover");

                PasteButton.Classes.Remove("altHover");
                PasteButton.Classes.Add("hover");

                SaveAsButton.Classes.Remove("altHover");
                SaveAsButton.Classes.Add("hover");

                ShowInFolderButton.Classes.Remove("altHover");
                ShowInFolderButton.Classes.Add("hover");

                OpenWithButton.Classes.Remove("altHover");
                OpenWithButton.Classes.Add("hover");

                OpenButton.Classes.Remove("altHover");
                OpenButton.Classes.Add("hover");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                PrintButton.IsEnabled = false;
            }
        };
    }
}