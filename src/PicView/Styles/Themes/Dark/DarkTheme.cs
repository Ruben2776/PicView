using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using AvaloniaStyles = Avalonia.Styling.Styles;

namespace PicView.Styles.Themes
{
    internal class DarkTheme : AvaloniaStyles
    {
        public DarkTheme() => AvaloniaXamlLoader.Load(this);
    }
}