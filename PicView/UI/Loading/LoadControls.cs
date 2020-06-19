using PicView.UI.UserControls;
using System.Diagnostics;
using System.Windows;
using static PicView.Library.Fields;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Loading
{
    internal static class LoadControls
    {
        // Load controls

        /// <summary>
        /// Loads ClickArrow and adds it to the window
        /// </summary>
        internal static void LoadClickArrow(bool right)
        {
            if (right)
            {
                clickArrowRight = new ClickArrow(true)
                {
                    Focusable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                mainWindow.bg.Children.Add(clickArrowRight);
            }
            else
            {
                clickArrowLeft = new ClickArrow(false)
                {
                    Focusable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                mainWindow.bg.Children.Add(clickArrowLeft);

#if DEBUG
                Trace.WriteLine("LoadClickArrow loaded " + right);
#endif
            }
        }

        /// <summary>
        /// Loads x2 and adds it to the window
        /// </summary>
        internal static void Loadx2()
        {
            x2 = new X2()
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            mainWindow.bg.Children.Add(x2);

#if DEBUG
            Trace.WriteLine("Loadx2 loaded ");
#endif
        }

        /// <summary>
        /// Loads Minus and adds it to the window
        /// </summary>
        internal static void LoadMinus()
        {
            minus = new Minus()
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 50, 0)
            };

            mainWindow.bg.Children.Add(minus);

#if DEBUG
            Trace.WriteLine("LoadMinus loaded ");
#endif
        }

        /// <summary>
        /// Loads GalleryShortcut and adds it to the window
        /// </summary>
        internal static void LoadGalleryShortcut()
        {
            galleryShortcut = new GalleryShortcut()
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Bottom,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            mainWindow.bg.Children.Add(galleryShortcut);

#if DEBUG
            Trace.WriteLine("LoadGalleryShortcut loaded ");
#endif
        }

        /// <summary>
        /// Loads FileMenu and adds it to the window
        /// </summary>
        internal static void LoadFileMenu()
        {
            fileMenu = new FileMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 188, 0)
            };

            mainWindow.bg.Children.Add(fileMenu);

#if DEBUG
            Trace.WriteLine("LoadFileMenu loaded ");
#endif
        }

        /// <summary>
        /// Loads ImageSettingsMenu and adds it to the window
        /// </summary>
        internal static void LoadImageSettingsMenu()
        {
            imageSettingsMenu = new ImageSettings
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 95, 0)
            };

            mainWindow.bg.Children.Add(imageSettingsMenu);

#if DEBUG
            Trace.WriteLine("LoadImageSettingsMenu loaded ");
#endif
        }

        /// <summary>
        /// Loads QuickSettingsMenu and adds it to the window
        /// </summary>
        internal static void LoadQuickSettingsMenu()
        {
            quickSettingsMenu = new QuickSettingsMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(52, 0, 0, 0)
            };

            quickSettingsMenu.SetFit.IsChecked = Properties.Settings.Default.AutoFitWindow;

            mainWindow.bg.Children.Add(quickSettingsMenu);

#if DEBUG
            Trace.WriteLine("LoadQuickSettingsMenu loaded ");
#endif
        }

        /// <summary>
        /// Loads FunctionsMenu and adds it to the window
        /// </summary>
        internal static void LoadToolsAndEffectsMenu()
        {
            toolsAndEffectsMenu = new ToolsAndEffectsMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(87, 0, 0, 0)
            };

            mainWindow.bg.Children.Add(toolsAndEffectsMenu);

#if DEBUG
            Trace.WriteLine("LoadFunctionsMenu loaded ");
#endif
        }

        // Tooltip

        /// <summary>
        /// Loads TooltipStyle and adds it to the window
        /// </summary>
        internal static void LoadTooltipStyle()
        {
            toolTipMessage = new ToolTipMessage
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden
            };

            mainWindow.bg.Children.Add(toolTipMessage);

#if DEBUG
            Trace.WriteLine("LoadTooltipStyle loaded ");
#endif
        }

        /// <summary>
        /// Loads AutoScrollSign and adds it to the window
        /// </summary>
        internal static void LoadAutoScrollSign()
        {
            autoScrollSign = new AutoScrollSign
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                Width = 20,
                Height = 35
            };

            mainWindow.topLayer.Children.Add(autoScrollSign);

#if DEBUG
            Trace.WriteLine("LoadAutoScrollSign loaded ");
#endif
        }


        /// <summary>
        /// Loads Croppingtool and adds it to the window
        /// </summary>
        internal static void LoadCroppingTool()
        {
            cropppingTool = new CroppingTool
            {
            };

            mainWindow.bg.Children.Add(cropppingTool);

#if DEBUG
            Trace.WriteLine("cropppingTool loaded ");
#endif
        }


    }
}