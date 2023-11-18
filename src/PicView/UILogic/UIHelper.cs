using PicView.Animations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.UILogic
{
    /// <summary>
    /// Provides helper methods for UI-related tasks.
    /// </summary>
// ReSharper disable once InconsistentNaming
    internal static class UIHelper
    {
        /// <summary>
        /// Expands or collapses a <see cref="ScrollViewer"/> control.
        /// </summary>
        /// <param name="height">The current height of the <see cref="ScrollViewer"/> control.</param>
        /// <param name="startHeight">The starting height of the <see cref="ScrollViewer"/> control.</param>
        /// <param name="extendedHeight">The expanded height of the <see cref="ScrollViewer"/> control.</param>
        /// <param name="frameworkElement">The parent control or window <see cref="FrameworkElement"/> control.</param>
        /// <param name="scrollViewer">The <see cref="ScrollViewer"/> control to expand or collapse.</param>
        /// <param name="geometryDrawing">The <see cref="GeometryDrawing"/> object to modify.</param>
        public static void ExtendOrCollapse(double height, double startHeight, double extendedHeight,
            FrameworkElement frameworkElement, ScrollViewer scrollViewer, GeometryDrawing geometryDrawing)
        {
            double from, to;
            bool expanded;
            if (Math.Abs(height - startHeight) < .1)
            {
                from = startHeight;
                to = extendedHeight;
                expanded = false;
            }
            else
            {
                to = startHeight;
                from = extendedHeight;
                expanded = true;
            }

            AnimationHelper.AnimateHeight(frameworkElement, from, to, expanded);

            if (expanded)
            {
                Collapse(scrollViewer, geometryDrawing);
            }
            else
            {
                Extend(scrollViewer, geometryDrawing);
            }
        }

        /// <summary>
        /// Expands a <see cref="ScrollViewer"/> control.
        /// </summary>
        /// <param name="scrollViewer">The <see cref="ScrollViewer"/> control to expand.</param>
        /// <param name="geometryDrawing">The <see cref="GeometryDrawing"/> object to modify.</param>
        private static void Extend(ScrollViewer scrollViewer, GeometryDrawing geometryDrawing)
        {
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            geometryDrawing.Geometry =
                Geometry.Parse(
                    "F1 M512,512z M0,0z M414,321.94L274.22,158.82A24,24,0,0,0,237.78,158.82L98,321.94C84.66,337.51,95.72,361.56,116.22,361.56L395.82,361.56C416.32,361.56,427.38,337.51,414,321.94z");
        }

        /// <summary>
        /// Collapses a <see cref="ScrollViewer"/> control.
        /// </summary>
        /// <param name="scrollViewer">The <see cref="ScrollViewer"/> control to collapse.</param>
        /// <param name="geometryDrawing">The <see cref="GeometryDrawing"/> object to modify.</param>
        private static void Collapse(ScrollViewer scrollViewer, GeometryDrawing geometryDrawing)
        {
            scrollViewer.ScrollToTop();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            geometryDrawing.Geometry = Geometry.Parse(
                "F1 M512,512z M0,0z M98,190.06L237.78,353.18A24,24,0,0,0,274.22,353.18L414,190.06C427.34,174.49,416.28,150.44,395.78,150.44L116.18,150.44C95.6799999999999,150.44,84.6199999999999,174.49,97.9999999999999,190.06z");
        }
    }
}