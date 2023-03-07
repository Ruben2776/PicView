using PicView.ChangeImage;
using PicView.FileHandling;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            if (width < 900)
            {
                logo.Width = 247;
                buttons.Margin = new Thickness(0, 0, 0, 0);
                buttons.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else if (width > 900)
            {
                logo.Width = double.NaN;
                buttons.Margin = new Thickness(0, 181, 25, 16);
                buttons.VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }
}