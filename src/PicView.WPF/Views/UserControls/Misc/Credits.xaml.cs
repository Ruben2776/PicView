using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using PicView.WPF.Animations;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.WPF.Views.UserControls.Misc;

public partial class Credits
{
    public Credits()
    {
        InitializeComponent();
        Loaded += delegate
        {
            CreditsLabel.Content = TranslationHelper.GetTranslation("Credits");
            IconsUsedLabel.Content = TranslationHelper.GetTranslation("IconsUsed");

            var color = SettingsHelper.Settings.Theme.Dark
                ? Colors.White
                : (Color)Application.Current.Resources["MainColor"];

            Iconic.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, IconicBrush);
            };
            Iconic.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, IconicBrush);
            };

            Ionic.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, IonicBrush);
            };
            Ionic.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, IonicBrush);
            };

            FontAwesome.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, FontAwesomeBrush);
            };
            FontAwesome.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, FontAwesomeBrush);
            };

            zondicons.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, zondiconsBrush);
            };
            zondicons.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, zondiconsBrush);
            };

            freepik.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, FreepikBrush);
            };
            freepik.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, FreepikBrush);
            };

            XamlAnimatedGif.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, XamlAnimatedGifBrush);
            };
            XamlAnimatedGif.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, XamlAnimatedGifBrush);
            };

            ImageMagick.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, ImageMagickBrush);
            };
            ImageMagick.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, ImageMagickBrush);
            };
        };
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var ps = new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(ps);
    }
}