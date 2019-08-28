using PicView.UserControls;
using System.Windows;
using static PicView.Fields;

namespace PicView
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
                Margin = new Thickness(0, 0, 152, 0)
            };

            mainWindow.bg.Children.Add(fileMenu);
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
                Margin = new Thickness(0, 0, 112, 0)
            };

            mainWindow.bg.Children.Add(imageSettingsMenu);
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
                Margin = new Thickness(30, 0, 0, 0)
            };

            mainWindow.bg.Children.Add(quickSettingsMenu);
        }

        /// <summary>
        /// Loads FunctionsMenu and adds it to the window
        /// </summary>
        internal static void LoadFunctionsMenu()
        {
            functionsMenu = new UserControls.Menus.FunctionsMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(70, 0, 0, 0)
            };

            mainWindow.bg.Children.Add(functionsMenu);
        }



        // Tooltip

        /// <summary>
        /// Loads TooltipStyle and adds it to the window
        /// </summary>
        internal static void LoadTooltipStyle()
        {
            sexyToolTip = new SexyToolTip
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden
            };

            mainWindow.bg.Children.Add(sexyToolTip);
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
        }
    }
}
