using PicView.Animations;
using System.Windows.Controls;
using static PicView.UILogic.UC;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for FolderButton.xaml
    /// </summary>
    public partial class FolderButton : UserControl
    {
        public FolderButton()
        {
            InitializeComponent();

            // FileMenuButton
            FileMenuButton.PreviewMouseLeftButtonDown += (_, _) => MouseOverAnimations.PreviewMouseButtonDownAnim(FolderFill);
            FileMenuButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(FolderFill);
            FileMenuButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(FileMenuBg);
            FileMenuButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(FolderFill);
            FileMenuButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(FileMenuBg);
            FileMenuButton.Click += Toggle_open_menu;
        }
    }
}
