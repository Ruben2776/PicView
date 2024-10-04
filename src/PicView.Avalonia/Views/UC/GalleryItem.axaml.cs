using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Exception = System.Exception;

namespace PicView.Avalonia.Views.UC
{
    public partial class GalleryItem : UserControl
    {
        public GalleryItem()
        {
            InitializeComponent();
            GalleryContextMenu.Opened += delegate
            {
                if (!Application.Current.TryGetResource("SecondaryAccentColor", Application.Current.RequestedThemeVariant, out var color))
                {
                    return;
                }

                try
                {
                    var secondaryAccentBrush = (SolidColorBrush)color;
                    ImageBorder.BorderBrush = secondaryAccentBrush;
                }
#if DEBUG
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
#else
        catch (Exception){}
#endif
            };
            GalleryContextMenu.Closed += delegate
            {
                ImageBorder.BorderBrush = Brushes.Transparent;
            };
        }
    }
}