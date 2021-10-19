using PicView.Animations;
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
                PreviewMouseButtonDownAnim(selectBrush);
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

            OpenLastFileButton.Click += async (_, _) => await FileHandling.Open_Save.OpenAsync().ConfigureAwait(false);


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

            PasteButton.Click += async (_, _) => await FileHandling.Copy_Paste.PasteAsync().ConfigureAwait(false);
        }
    }
}
