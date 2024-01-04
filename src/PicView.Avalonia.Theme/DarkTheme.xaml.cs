using System;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace PicView.Avalonia.Theme;

public class DarkTheme : Styles
{
    public DarkTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}