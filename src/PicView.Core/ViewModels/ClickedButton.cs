namespace PicView.Core.ViewModels;

/// <summary>
/// The navigation or rotate button last clicked in a window, kept in <see cref="MainWindowViewModel.ClickedButton"/>
/// until the window resizes, so an AutoFit window can put the cursor back on it.
/// </summary>
public enum ClickedButton
{
    None,
    BottomBarPrevious,
    BottomBarNext,
    BottomBarRotateLeft,
    BottomBarRotateRight,
    HoverbarPrevious,
    HoverbarNext,
    HoverbarRotateLeft,
    HoverbarRotateRight,
    ClickArrowLeft,
    ClickArrowRight
}
