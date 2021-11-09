using System.Windows;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for StartUpUC.xaml
    /// </summary>
    public partial class StartUpUC : UserControl
    {
        public StartUpUC()
        {
            InitializeComponent();

            SelectFile.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(folderBrush1);
                PreviewMouseButtonDownAnim(folderBrush2);
                PreviewMouseButtonDownAnim(selectBrush);
            };

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

            SelectFile.Click += async (_, _) => await FileHandling.Open_Save.OpenAsync().ConfigureAwait(false);


            OpenLastFileButton.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(lastBrush1);
                PreviewMouseButtonDownAnim(lastBrush2);
                PreviewMouseButtonDownAnim(lastBrush);
            };

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

            OpenLastFileButton.Click += async (_, _) => await ChangeImage.History.OpenLastFileAsync().ConfigureAwait(false);


            PasteButton.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(pasteBrush);
                PreviewMouseButtonDownAnim(pasteTxt);
            };

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

            PasteButton.Click += (_, _) => FileHandling.Copy_Paste.Paste();
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
