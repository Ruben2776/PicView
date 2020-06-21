using PicView.SystemIntegration;
using PicView.UI.Loading;
using System.Windows;
using static PicView.Library.Fields;
using static PicView.UI.Sizing.WindowLogic;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += delegate { StartLoading.PreStart(); };
            ContentRendered += async delegate { await StartLoading.Start().ConfigureAwait(false); };
        }

        #region OnRenderSizeChanged override

        protected override void OnRenderSizeChanged(SizeChangedInfo size)
        {
            if (size == null)
            {
                return;
            }

            if (!SetWindowBehaviour || !size.WidthChanged && !size.HeightChanged)
            {
                return;
            }

            //Keep position when size has changed
            Top += (size.PreviousSize.Height - size.NewSize.Height) / 2;
            Left += (size.PreviousSize.Width - size.NewSize.Width) / 2;

            // Move cursor after resize when the button has been pressed
            if (RightbuttonClicked)
            {
                Point p = RightButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RighButton
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                RightbuttonClicked = false;
            }
            else if (LeftbuttonClicked)
            {
                Point p = LeftButton.PointToScreen(new Point(50, 10));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                LeftbuttonClicked = false;
            }
            else if (ClickArrowRightClicked)
            {
                Point p = clickArrowRight.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                ClickArrowRightClicked = false;
            }
            else if (ClickArrowLeftClicked)
            {
                Point p = clickArrowLeft.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                ClickArrowLeftClicked = false;
            }

            base.OnRenderSizeChanged(size);
        }

        #endregion OnRenderSizeChanged override
    }
}