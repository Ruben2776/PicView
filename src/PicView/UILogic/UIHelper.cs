using PicView.Animations;
using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.Editing;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic.Sizing;
using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Shortcuts.MainKeyboardShortcuts;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.UC;

namespace PicView.UILogic
{
    /// <summary>
    /// Provides helper methods for UI-related tasks.
    /// </summary>
// ReSharper disable once InconsistentNaming
    internal static class UIHelper
    {
        private static async Task<bool> CheckModifierFunctionAsync()
        {
            if (CtrlDown)
            {
                if (CurrentKey == Key.S)
                {
                    await OpenSave.SaveFilesAsync().ConfigureAwait(false);
                    return true;
                }

                if (CurrentKey == Key.C)
                {
                    if (ShiftDown)
                    {
                        CopyPaste.CopyBitmap();
                    }
                    else if (AltDown)
                    {
                        CopyPaste.CopyFilePath();
                    }
                    else
                    {
                        if (GetMainWindow.MainImage.Effect != null)
                            CopyPaste.CopyBitmap();
                        else
                            CopyPaste.CopyFile();
                    }
                    return true;
                }

                if (CurrentKey == Key.V)
                {
                    await CopyPaste.PasteAsync().ConfigureAwait(false);
                    return true;
                }

                if (CurrentKey == Key.O)
                {
                    await OpenSave.OpenAsync().ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }

        #region UI functions

        internal static async Task Close()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (UserControls_Open())
                {
                    Close_UserControls();
                }
                else if (GalleryFunctions.IsGalleryOpen)
                {
                    GalleryToggle.CloseCurrentGallery();
                }
                else if (Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                {
                    Slideshow.StopSlideshow();
                }
                else if (OpenSave.IsDialogOpen)
                {
                    OpenSave.IsDialogOpen = false;
                }
                else if (ColorPicking.IsRunning)
                {
                    ColorPicking.StopRunning(false);
                }
                else if (GetEffectsWindow is { IsVisible: true })
                {
                    GetEffectsWindow.Hide();
                }
                else if (GetImageInfoWindow is { IsVisible: true })
                {
                    GetImageInfoWindow.Hide();
                }
                else if (GetAboutWindow is { IsVisible: true })
                {
                    GetAboutWindow.Hide();
                }
                else if (GetSettingsWindow is { IsVisible: true })
                {
                    GetSettingsWindow.Hide();
                }
                else if (Settings.Default.Fullscreen)
                {
                    WindowSizing.Fullscreen_Restore(false);
                }
                else if (GetQuickResize is not null && GetQuickResize.Opacity > 0)
                {
                    GetQuickResize.Hide();
                }
                else if (!MainContextMenu.IsVisible)
                {
                    if (GetCroppingTool is { IsVisible: true })
                    {
                        return;
                    }

                    SystemCommands.CloseWindow(GetMainWindow);
                }
            });
        }

        #region Navigation and scrolling

        internal static async Task Next()
        {
            // exit if browsing horizontal PicGallery
            if (GalleryFunctions.IsGalleryOpen)
            {
                if (IsKeyHeldDown)
                {
                    // Disable animations when key is held down
                    GetPicGallery.Scroller.CanContentScroll = true;
                }

                GalleryNavigation.NavigateGallery(GalleryNavigation.Direction.Right);
                return;
            }
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }

            // Go to first if Ctrl held down
            if (CtrlDown && !IsKeyHeldDown)
            {
                await Navigation.GoToNextImage(NavigateTo.Last).ConfigureAwait(false);
            }
            else if (ShiftDown)
            {
                await Navigation.GoToNextFolder(true).ConfigureAwait(false);
            }
            else
            {
                await Navigation.GoToNextImage(NavigateTo.Next, IsKeyHeldDown).ConfigureAwait(false);
            }
        }

        internal static async Task Prev()
        {
            // exit if browsing horizontal PicGallery
            if (GalleryFunctions.IsGalleryOpen)
            {
                if (IsKeyHeldDown)
                {
                    // Disable animations when key is held down
                    GetPicGallery.Scroller.CanContentScroll = true;
                }

                GalleryNavigation.NavigateGallery(GalleryNavigation.Direction.Left);
                return;
            }
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }

            // Go to first if Ctrl held down
            if (CtrlDown && !IsKeyHeldDown)
            {
                await Navigation.GoToNextImage(NavigateTo.First).ConfigureAwait(false);
            }
            else if (ShiftDown)
            {
                await Navigation.GoToNextFolder(false).ConfigureAwait(false);
            }
            else
            {
                await Navigation.GoToNextImage(NavigateTo.Previous, IsKeyHeldDown).ConfigureAwait(false);
            }
        }

        internal static async Task Up()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility ==
                    Visibility.Visible)
                {
                    GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                }
                else if (GalleryFunctions.IsGalleryOpen && GetPicGallery != null)
                {
                    GalleryNavigation.NavigateGallery(GalleryNavigation.Direction.Up);
                }
                else
                {
                    Rotation.Rotate(IsKeyHeldDown, false);
                }
            });
        }

        internal static async Task Down()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility ==
                    Visibility.Visible)
                {
                    GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                }
                else if (GalleryFunctions.IsGalleryOpen && GetPicGallery != null)
                {
                    GalleryNavigation.NavigateGallery(GalleryNavigation.Direction.Up);
                }
                else
                {
                    Rotation.Rotate(IsKeyHeldDown, false);
                }
            });
        }

        internal static async Task ScrollUp()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (GetPicGallery != null && GalleryFunctions.IsGalleryOpen)
                {
                    GalleryNavigation.ScrollGallery(true, CtrlDown, ShiftDown, true);
                }
                else
                {
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility ==
                        Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }
                }
            });
        }

        internal static async Task ScrollDown()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (GetPicGallery != null && GalleryFunctions.IsGalleryOpen)
                {
                    GalleryNavigation.ScrollGallery(false, CtrlDown, ShiftDown, true);
                }
                else
                {
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility ==
                        Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                    }
                }
            });
        }

        internal static async Task ToggleScroll()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(UpdateUIValues.SetScrolling);
        }

        #endregion Navigation and scrolling

        #region Windows

        internal static async Task AboutWindow()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(ConfigureWindows.AboutWindow);
        }

        internal static async Task EffectsWindow()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(ConfigureWindows.EffectsWindow);
        }

        internal static async Task ImageInfoWindow()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(ConfigureWindows.ImageInfoWindow);
        }

        internal static async Task ResizeWindow()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(ConfigureWindows.ResizeWindow);
        }

        internal static async Task SettingsWindow()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(ConfigureWindows.SettingsWindow);
        }

        #endregion Windows

        #region Misc

        internal static async Task ToggleBackground()
        {
            var check = await CheckModifierFunctionAsync().ConfigureAwait(false);
            if (check)
            {
                return;
            }
            await GetMainWindow.Dispatcher.InvokeAsync(ConfigColors.ChangeBackground);
        }

        #endregion Misc

        #endregion UI functions

        #region Extending Controls

        /// <summary>
        /// Expands or collapses a <see cref="ScrollViewer"/> control.
        /// </summary>
        /// <param name="height">The current height of the <see cref="ScrollViewer"/> control.</param>
        /// <param name="startHeight">The starting height of the <see cref="ScrollViewer"/> control.</param>
        /// <param name="extendedHeight">The expanded height of the <see cref="ScrollViewer"/> control.</param>
        /// <param name="frameworkElement">The parent control or window <see cref="FrameworkElement"/> control.</param>
        /// <param name="scrollViewer">The <see cref="ScrollViewer"/> control to expand or collapse.</param>
        /// <param name="geometryDrawing">The <see cref="GeometryDrawing"/> object to modify.</param>
        internal static void ExtendOrCollapse(double height, double startHeight, double extendedHeight,
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

        #endregion Extending Controls
    }
}