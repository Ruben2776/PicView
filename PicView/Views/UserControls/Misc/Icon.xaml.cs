using PicView.ConfigureSettings;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PicView.Views.UserControls.Misc
{
    /// <summary>
    /// Interaction logic for Logo.xaml
    /// </summary>
    public partial class Icon : UserControl
    {
        public Icon()
        {
            InitializeComponent();
        }

        public void ChangeColor()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, () =>
            {
                BaseBrush.Brush = new SolidColorBrush(ConfigColors.GetSecondaryAccentColor);
            });
        }
    }
}