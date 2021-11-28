using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using AvaloniaStyles = Avalonia.Styling.Styles;

namespace PicView.Styles.Themes
{
    internal class FluentDark : AvaloniaStyles
    {
        public FluentDark() => AvaloniaXamlLoader.Load(this);
    }
}