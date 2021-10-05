using PicView.Animations;
using PicView.ChangeImage;
using PicView.SystemIntegration;
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

            if (Properties.Settings.Default.AutoFitWindow == false)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                if (Properties.Settings.Default.Width > 0)
                {
                    UILogic.Sizing.WindowSizing.SetLastWindowSize();
                }
            }

            InitializeComponent();

            Loaded += async delegate { await StartLoading.LoadedEventsAsync().ConfigureAwait(false); };
            ContentRendered += delegate { StartLoading.ContentRenderedEvent(); };
        }

        #region OnRenderSizeChanged override

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo == null)
            {
                return;
            }

            if (!sizeInfo.WidthChanged && !sizeInfo.HeightChanged)
            {
                return;
            }
            else if (Properties.Settings.Default.AutoFitWindow == false)
            {
                if (Properties.Settings.Default.FullscreenGalleryHorizontal == false)
                {
                    return;
                }
            }

            //Keep position when size has changed
            Top += ((sizeInfo.PreviousSize.Height / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Height / MonitorInfo.DpiScaling)) / 2;
            Left += ((sizeInfo.PreviousSize.Width / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Width / MonitorInfo.DpiScaling)) / 2;

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

                _ = FadeControls.FadeControlsAsync(true);
            }
            else if (Navigation.ClickArrowLeftClicked)
            {
                Point p = GetClickArrowLeft.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.ClickArrowLeftClicked = false;

                _ = FadeControls.FadeControlsAsync(true);
            }

            base.OnRenderSizeChanged(sizeInfo);
        }

        #endregion OnRenderSizeChanged override
    }
}