using PicView.UILogic;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using static PicView.SystemIntegration.NativeMethods;

namespace PicView.Views.Windows
{
    /// <summary>
    /// Interaction logic for FileAssociationsWindow.xaml
    /// </summary>
    public partial class FileAssociationsWindow : Window
    {
        public FileAssociationsWindow()
        {
            Title = Application.Current.Resources["SetAsDefualt"] + " - PicView";
            Width = ConfigureWindows.GetSettingsWindow.ActualWidth;
            Height = ConfigureWindows.GetSettingsWindow.ActualHeight;
            InitializeComponent();

            ContentRendered += delegate
            {
                // Hide from alt tab
                var helper = new WindowInteropHelper(this).Handle;
                _ = SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

                TitleBar.MouseLeftButtonDown += (_, _) => DragMove();
                LocationChanged += delegate // Move parent window as well
                {
                    ConfigureWindows.GetSettingsWindow.Top = Top;
                    ConfigureWindows.GetSettingsWindow.Left = Left;
                };
                CloseButton.TheButton.Click += (_, _) => HideLogic();             

                RasterFormatsCheck.Checked += delegate 
                {
                    jpg.IsChecked = png.IsChecked = bmp.IsChecked = ico.IsChecked = gif.IsChecked =
                    webp.IsChecked = jfif.IsChecked = tiff.IsChecked = ppm.IsChecked = wbmp.IsChecked = true;
                };
                RasterFormatsCheck.Unchecked += delegate
                {
                    jpg.IsChecked = png.IsChecked = bmp.IsChecked = ico.IsChecked = gif.IsChecked =
                    webp.IsChecked = jfif.IsChecked = tiff.IsChecked = ppm.IsChecked = wbmp.IsChecked = false;
                };

                PhotoshopCheck.Checked += delegate
                {
                    psd.IsChecked = psb.IsChecked = true;
                };
                PhotoshopCheck.Unchecked += delegate
                {
                    psd.IsChecked = psb.IsChecked = false;
                };

                VectorCheck.Checked += delegate
                {
                    svg.IsChecked = true;
                };
                VectorCheck.Unchecked += delegate
                {
                    svg.IsChecked = false;
                };

                RawCameraCheck.Checked += delegate
                {
                    threefr.IsChecked = arw.IsChecked = cr2.IsChecked = crw.IsChecked = dcr.IsChecked =
                    dng.IsChecked = erf.IsChecked = kdc.IsChecked = mef.IsChecked = mdc.IsChecked =
                    mos.IsChecked = mrw.IsChecked = nef.IsChecked = nrw.IsChecked = orf.IsChecked =
                    pef.IsChecked = raf.IsChecked = raw.IsChecked = rw2.IsChecked = srf.IsChecked =
                    x3f.IsChecked = true;
                };
                RawCameraCheck.Unchecked += delegate
                {
                    threefr.IsChecked = arw.IsChecked = cr2.IsChecked = crw.IsChecked = dcr.IsChecked =
                    dng.IsChecked = erf.IsChecked = kdc.IsChecked = mef.IsChecked = mdc.IsChecked =
                    mos.IsChecked = mrw.IsChecked = nef.IsChecked = nrw.IsChecked = orf.IsChecked =
                    pef.IsChecked = raf.IsChecked = raw.IsChecked = rw2.IsChecked = srf.IsChecked =
                    x3f.IsChecked = false;
                };

                OtherCheck.Checked += delegate
                {
                    cut.IsChecked = exr.IsChecked = emf.IsChecked = dib.IsChecked = hdr.IsChecked = heic.IsChecked =
                    pcx.IsChecked = pgm.IsChecked = wmf.IsChecked = wpg.IsChecked = xbm.IsChecked = xpm.IsChecked = true;
                };
                OtherCheck.Unchecked += delegate
                {
                    cut.IsChecked = exr.IsChecked = emf.IsChecked = dib.IsChecked = hdr.IsChecked = heic.IsChecked =
                    pcx.IsChecked = pgm.IsChecked = wmf.IsChecked = wpg.IsChecked = xbm.IsChecked = xpm.IsChecked = false;
                };
            };      

            KeyDown += (_, e) => 
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    HideLogic();
                }
            };
        }

        internal void HideLogic()
        {
            var da = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(.3)
            };

            da.Completed += delegate
            {
                Hide();
            };

            ConfigureWindows.GetSettingsWindow.Effect = null;
            BeginAnimation(OpacityProperty, da);
            ConfigureWindows.GetSettingsWindow.Focus();
        }
    }
}
