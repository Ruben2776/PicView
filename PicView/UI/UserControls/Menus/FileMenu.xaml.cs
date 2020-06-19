using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
{
    /// <summary>
    /// Interaction logic for fileMenu.xaml
    /// </summary>
    public partial class FileMenu : UserControl
    {
        public FileMenu()
        {
            InitializeComponent();

            PasteButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PasteButtonBrush);
            PasteButton.MouseEnter += (s, x) => ButtonMouseOverAnim(PasteButtonBrush, true);
            PasteButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PasteButtonBrush, false);

            CopyButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(CopyButtonBrush);
            CopyButton.MouseEnter += (s, x) => ButtonMouseOverAnim(CopyButtonBrush, true);
            CopyButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(CopyButtonBrush, false);

            ReloadButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ReloadButtonBrush);
            ReloadButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ReloadButtonBrush, true);
            ReloadButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ReloadButtonBrush, false);

            RecycleButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RecycleButtonBrush);
            RecycleButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RecycleButtonBrush, true);
            RecycleButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RecycleButtonBrush, true);

            OpenBorder.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(OpenBorderBrush);
            OpenBorder.MouseEnter += (s, x) => ButtonMouseOverAnim(OpenBorderBrush, true);
            OpenBorder.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(OpenBorderBrush);

            PrintBorder.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PrintBorderBrush);
            PrintBorder.MouseEnter += (s, x) => ButtonMouseOverAnim(PrintBorderBrush, true);
            PrintBorder.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PrintBorderBrush);

            SaveBorder.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SaveBorderBrush);
            SaveBorder.MouseEnter += (s, x) => ButtonMouseOverAnim(SaveBorderBrush, true);
            SaveBorder.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SaveBorderBrush);

            FileLocationBorder.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(FileLocationBorderBrush);
            FileLocationBorder.MouseEnter += (s, x) => ButtonMouseOverAnim(FileLocationBorderBrush, true);
            FileLocationBorder.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(FileLocationBorderBrush);
        }
    }
}
