using PicView.Animations;
using PicView.Properties;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;

namespace PicView.Views.UserControls.Misc;

public partial class Credits
{
    public Credits()
    {
        InitializeComponent();
        Loaded += delegate
        {
            var color = Settings.Default.DarkTheme
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