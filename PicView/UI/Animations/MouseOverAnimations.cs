using System.Windows.Media;
using static PicView.UI.ConfigColors;

namespace PicView.UI.Animations
{
    internal static class MouseOverAnimations
    {
        /*

            Adds MouseOver events for the given elements with the AnimationHelper.
            Changes color depending on the users settings.

        */

        #region logo

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

        #endregion logo

        #region 1x

        internal static void PreviewMouseButtonDownAnim(Brush brush, bool alpha = false)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(brush, alpha);
        }

        internal static void ButtonMouseOverAnim(Brush brush, bool transparent = false, bool alpha = false)
        {
            if (transparent)
            {
                AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, brush, alpha);
            }
            else
            {
                AnimationHelper.MouseOverColorEvent(
                    mainColor.A,
                    mainColor.R,
                    mainColor.G,
                    mainColor.B,
                    brush,
                    alpha
                );
            }
        }

        internal static void ButtonMouseLeaveAnim(Brush brush, bool transparent = false, bool alpha = false)
        {
            if (transparent)
            {
                AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, brush, alpha);
            }
            else
            {
                AnimationHelper.MouseLeaveColorEvent(
                    mainColor.A,
                    mainColor.R,
                    mainColor.G,
                    mainColor.B,
                    brush,
                    alpha
                );
            }
        }

        internal static void ButtonMouseLeaveAnimBgColor(Brush brush, bool alpha = false)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                brush,
                alpha
            );
        }

        #endregion 1x

        #region 2x

        internal static void PreviewMouseButtonDownAnim(Brush brush, Brush brush2, bool alpha = false)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(brush, alpha);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(brush2, alpha);
        }

        internal static void ButtonMouseOverAnim(Brush brush, Brush brush2, bool transparent = false, bool alpha = false)
        {
            ButtonMouseOverAnim(brush, transparent, alpha);
            ButtonMouseOverAnim(brush2, transparent, alpha);
        }

        internal static void ButtonMouseLeaveAnim(Brush brush, Brush brush2, bool transparent = false, bool alpha = false)
        {
            ButtonMouseLeaveAnim(brush, transparent, alpha);
            ButtonMouseLeaveAnim(brush2, transparent, alpha);
        }

        #endregion 2x

        #region 3x

        internal static void PreviewMouseButtonDownAnim(Brush brush, Brush brush2, Brush brush3, bool alpha = false)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(brush, alpha);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(brush2, alpha);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(brush3, alpha);
        }

        internal static void ButtonMouseOverAnim(Brush brush, Brush brush2, Brush brush3, bool transparent = false, bool alpha = false)
        {
            ButtonMouseOverAnim(brush, transparent, alpha);
            ButtonMouseOverAnim(brush2, transparent, alpha);
            ButtonMouseOverAnim(brush3, transparent, alpha);
        }

        internal static void ButtonMouseLeaveAnim(Brush brush, Brush brush2, Brush brush3, bool transparent = false, bool alpha = false)
        {
            ButtonMouseLeaveAnim(brush, transparent, alpha);
            ButtonMouseLeaveAnim(brush2, transparent, alpha);
            ButtonMouseLeaveAnim(brush3, transparent, alpha);
        }

        #endregion 3x
    }
}