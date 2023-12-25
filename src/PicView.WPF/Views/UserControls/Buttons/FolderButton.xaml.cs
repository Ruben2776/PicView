using System.Windows.Controls;
using PicView.WPF.Animations;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class FolderButton : UserControl
{
    public FolderButton()
    {
        InitializeComponent();

        // FileMenuButton
        FileMenuButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(FolderFill);
        FileMenuButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(FolderFill);
    }

    internal void BackgroundEvents()
    {
        FileMenuButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(FileMenuBg);
        FileMenuButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(FileMenuBg);
        FileMenuButton.Click += Toggle_open_menu;
    }
}