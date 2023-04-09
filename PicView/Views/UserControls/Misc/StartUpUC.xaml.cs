using Microsoft.VisualBasic.Logging;
using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.FileHandling;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static PicView.Animations.MouseOverAnimations;
using Color = System.Windows.Media.Color;

namespace PicView.Views.UserControls.Misc
{
    /// <summary>
    /// Interaction logic for StartUpUC.xaml
    /// </summary>
    public partial class StartUpUC : UserControl
    {
        public StartUpUC()
        {
            InitializeComponent();

            SelectFile.MouseEnter += delegate
            {
                ButtonMouseOverAnim(folderBrush1);
                ButtonMouseOverAnim(folderBrush2);
                ButtonMouseOverAnim(selectBrush);
            };

            SelectFile.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(folderBrush1);
                ButtonMouseLeaveAnim(folderBrush2);
                ButtonMouseLeaveAnim(selectBrush);
            };

            SelectFile.Click += async (_, _) => await Open_Save.OpenAsync().ConfigureAwait(false);

            OpenLastFileButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(lastBrush1);
                ButtonMouseOverAnim(lastBrush2);
                ButtonMouseOverAnim(lastBrush);
            };

            OpenLastFileButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(lastBrush1);
                ButtonMouseLeaveAnim(lastBrush2);
                ButtonMouseLeaveAnim(lastBrush);
            };

            OpenLastFileButton.Click += async (_, _) => await Navigation.GetFileHistory.OpenLastFileAsync().ConfigureAwait(false);

            PasteButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(pasteBrush);
                ButtonMouseOverAnim(pasteTxt);
            };

            PasteButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(pasteBrush);
                ButtonMouseLeaveAnim(pasteTxt);
            };

            PasteButton.Click += async (_, _) => await Copy_Paste.PasteAsync().ConfigureAwait(false);

            if (Properties.Settings.Default.DarkTheme == false)
            {
                Background = new SolidColorBrush(Color.FromArgb(230,255, 255, 255));
            }
        }

        public void ToggleMenu()
        {
            if (buttons.IsVisible) { buttons.Visibility = Visibility.Collapsed; }
            else { buttons.Visibility = Visibility.Visible; }
        }

        public void ResponsiveSize(double width)
        {
            if (width < 1265)
            {
                Logo.Width = 350;
                buttons.Margin = new Thickness(0, 0, 0, 16);
                buttons.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else if (width > 1265)
            {
                Logo.Width = double.NaN;
                buttons.Margin = new Thickness(0, 220, 25, 16);
                buttons.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        public void ChangeColor()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, () =>
            {
                AccentBrush.Brush = new SolidColorBrush(ConfigColors.GetSecondaryAccentColor);
            });
        }
    }
}