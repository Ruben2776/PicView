using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace PicView.Avalonia.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TaskBarToggleButton.IsVisible = false;
        }
    }
}