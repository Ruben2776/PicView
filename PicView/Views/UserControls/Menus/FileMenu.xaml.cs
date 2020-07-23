using PicView.UILogic.Animations;
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

            // OpenBorder
            OpenBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(OpenBorderFill); };
            OpenBorder.MouseEnter += delegate { ButtonMouseOverAnim(OpenBorderFill); };
            OpenBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(OpenBorderBrush); };
            OpenBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(OpenBorderFill); };
            OpenBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(OpenBorderBrush); };

            // PrintBorder
            PrintBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PrintFill); };
            PrintBorder.MouseEnter += delegate { ButtonMouseOverAnim(PrintFill); };
            PrintBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(PrintBrush); };
            PrintBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(PrintFill); };
            PrintBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(PrintBrush); };

            // SaveBorder
            SaveBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SaveFill); };
            SaveBorder.MouseEnter += delegate { ButtonMouseOverAnim(SaveFill); };
            SaveBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SaveBrush); };
            SaveBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(SaveFill); };
            SaveBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SaveBrush); };

            // FileLocationBorder
            FileLocationBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FileLocationFill); };
            FileLocationBorder.MouseEnter += delegate { ButtonMouseOverAnim(FileLocationFill); };
            FileLocationBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(FileLocationBrush); };
            FileLocationBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FileLocationFill); };
            FileLocationBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(FileLocationBrush); };
        }
    }
}