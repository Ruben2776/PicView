using System.Windows.Media;
using System.Windows.Input;
using static PicView.Fields;


namespace PicView
{
    internal static class MouseOverAnimations
    {
        #region MouseOver Button Events

        /*

            Adds MouseOver events for the given elements with the AnimationHelper.
            Changes color depending on the users settings.

        */

        // Logo Mouse Over
        //internal static void LogoMouseOver(object sender, MouseEventArgs e)
        //{
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, pBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, cBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, vBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iiBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, eBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, wBrush, false);
        //}

        //internal static void LogoMouseLeave(object sender, MouseEventArgs e)
        //{
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, pBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, cBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, vBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iiBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, eBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, wBrush, false);
        //}

        //internal static void LogoMouseButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(pBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(cBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(vBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iiBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(eBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(wBrush, false);
        //}

        internal static void ButtonMouseOver(Brush brush)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, brush, false);
        }

        internal static void ButtonMouseButtonDown(Brush brush)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(brush, false);
        }

        internal static void ButtonMouseLeave(Brush brush)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, brush, false);
        }

        // LeftButton
        internal static void LeftButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.LeftArrowFill,
                false
            );
        }

        internal static void LeftButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.LeftArrowFill, false);
        }

        internal static void LeftButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.LeftArrowFill,
                false
            );
        }

        // RightButton
        internal static void RightButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.RightArrowFill,
                false
            );
        }

        internal static void RightButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.RightArrowFill, false);
        }

        internal static void RightButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.RightArrowFill,
                false
            );
        }

        // OpenMenuButton
        internal static void OpenMenuButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.FolderFill,
                false
            );
        }

        internal static void OpenMenuButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.FolderFill, false);
        }

        internal static void OpenMenuButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.FolderFill,
                false
            );
        }

        // ImageButton
        internal static void ImageButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath1Fill,
                false
            );
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath2Fill,
                false
            );
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath3Fill,
                false
            );
            //AnimationHelper.MouseEnterColorEvent(
            //    mainColor.A,
            //    mainColor.R,
            //    mainColor.G,
            //    mainColor.B,
            //    ImagePath4Fill,
            //    false
            //);
        }

        internal static void ImageButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.ImagePath1Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.ImagePath2Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.ImagePath3Fill, false);
            //AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath4Fill, false);
        }

        internal static void ImageButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath1Fill,
                false
            );
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath2Fill,
                false
            );
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath3Fill,
                false
            );
            //AnimationHelper.MouseLeaveColorEvent(
            //    mainColor.A,
            //    mainColor.R,
            //    mainColor.G,
            //    mainColor.B,
            //    ImagePath4Fill,
            //    false
            //);
        }

        // SettingsButton
        internal static void SettingsButtonButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.SettingsButtonFill,
                false
            );
        }

        internal static void SettingsButtonButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.SettingsButtonFill, false);
        }

        internal static void SettingsButtonButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.SettingsButtonFill,
                false
            );
        }

        // InfoButton
        internal static void InfoButtonButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.InfoButtonFill,
                false
            );
        }

        internal static void InfoButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.InfoButtonFill, false);
        }

        internal static void InfoButtonButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.InfoButtonFill,
                false
            );
        }

        #endregion MouseOver Button Events
    }
}
