using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Update;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views.Main;

public partial class PrintPreviewView : UserControl
{
    public required IPlatformSpecificUpdate PlatformUpdate;

    public PrintPreviewView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {

            if (!Settings.Theme.Dark && !Settings.Theme.GlassTheme)
            {
                if (!Application.Current.TryGetResource("MainTextColor",
                        Application.Current.RequestedThemeVariant, out var textColor))
                {
                    return;
                }

                if (textColor is not Color color)
                {
                    return;
                }
            }
        };
    }

}