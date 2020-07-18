using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// Interaction logic for fileMenu.xaml
    /// </summary>
    public partial class FileMenu : UserControl
    {
        public FileMenu()
        {
            InitializeComponent();

            PasteButton.PreviewMouseLeftButtonDown += delegate { UC.Close_UserControls(); };

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