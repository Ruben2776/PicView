using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Models;
using PicView.Core.Calculations;
using PicView.Core.Config;

namespace PicView.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += async delegate
            {
                await LoadSettings.StartLoadingAsync().ConfigureAwait(false);
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    var skBitmap = await Core.ImageDecoding.ImageDecoder.GetSKBitmapAsync(new FileInfo(args[1])).ConfigureAwait(false);
                    var avaloniaPic = skBitmap.ToAvaloniaImage();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var size = ImageSizeCalculationHelper.GetImageSize(
                            width: avaloniaPic.Size.Width,
                            height: avaloniaPic.Size.Height,
                            monitorWidth: 2560,
                            monitorHeight: 1440,
                            rotationAngle: 0,
                            stretch: SettingsHelper.Settings.ImageScaling.StretchImage,
                            padding: 0,
                            dpiScaling: 1,
                            fullscreen: SettingsHelper.Settings.ImageScaling.StretchImage,
                            uiTopSize: 0,
                            uiBottomSize: 0,
                            galleryHeight: SettingsHelper.Settings.Gallery.IsBottomGalleryShown
                                ? SettingsHelper.Settings.Gallery.BottomGalleryItemSize
                                : 0,
                            autoFit: SettingsHelper.Settings.WindowProperties.AutoFit,
                            containerWidth: Width,
                            containerHeight: Height,
                            SettingsHelper.Settings.Zoom.ScrollEnabled
                        );
                        MainImage.Source = avaloniaPic;
                        MainImage.Width = size.Width;
                        MainImage.Height = size.Height;
                    });
                }
            };
        }
    }
}