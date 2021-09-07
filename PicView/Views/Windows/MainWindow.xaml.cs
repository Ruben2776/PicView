using PicView.ChangeImage;
using PicView.SystemIntegration;
using PicView.UILogic.Animations;
using PicView.UILogic.Loading;
using System.Windows;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.UC;

namespace PicView.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (Properties.Settings.Default.DarkTheme == false)
            {
                ConfigureSettings.ConfigColors.ChangeToLightTheme();
            }

            InitializeComponent();
            Loaded += delegate { StartLoading.LoadedEvemtAsync(); };
            ContentRendered += delegate { StartLoading.ContentRenderedEvent(); };
        }

        #region OnRenderSizeChanged override

        protected override void OnRenderSizeChanged(SizeChangedInfo size)
        {
            if (size == null)
            {
                return;
            }

            if (!size.WidthChanged && !size.HeightChanged)
            {
                return;
            }
            else if (!AutoFitWindow)
            {
                if (Properties.Settings.Default.FullscreenGallery == false)
                {
                    return;
                }
            }

            //Keep position when size has changed
            Top += ((size.PreviousSize.Height / MonitorInfo.DpiScaling) - (size.NewSize.Height / MonitorInfo.DpiScaling)) / 2;
            Left += ((size.PreviousSize.Width / MonitorInfo.DpiScaling) - (size.NewSize.Width / MonitorInfo.DpiScaling)) / 2;

            // Move cursor after resize when the button has been pressed
            if (Navigation.RightbuttonClicked)
            {
                Point p = RightButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RighButton
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.RightbuttonClicked = false;
            }
            else if (Navigation.LeftbuttonClicked)
            {
                Point p = LeftButton.PointToScreen(new Point(50, 10));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.LeftbuttonClicked = false;
            }
            else if (Navigation.ClickArrowRightClicked)
            {
                Point p = GetClickArrowRight.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.ClickArrowRightClicked = false;

                FadeControls.FadeControlsAsync(true);
            }
            else if (Navigation.ClickArrowLeftClicked)
            {
                Point p = GetClickArrowLeft.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.ClickArrowLeftClicked = false;

                FadeControls.FadeControlsAsync(true);
            }

            base.OnRenderSizeChanged(size);
        }

        #endregion OnRenderSizeChanged override
    }
}