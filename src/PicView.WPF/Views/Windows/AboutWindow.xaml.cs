using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using PicView.WPF.Animations;
using PicView.WPF.ConfigureSettings;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;

namespace PicView.WPF.Views.Windows
{
    public partial class AboutWindow
    {
        private readonly double startHeight;
        private readonly double extendedHeight;

        internal UserControls.Misc.ShortcutList? ShortcutList;

        public AboutWindow()
        {
            InitializeComponent();

            extendedHeight = 750;
            startHeight = Height;

            ContentRendered += Window_ContentRendered;

            MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
            if (!double.IsNaN(Width)) return; // Fixes if user opens window when loading from startup
            WindowSizing.MonitorInfo = MonitorSize.GetMonitorSize(this);
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;
            MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
        }

        private void Window_ContentRendered(object? sender, EventArgs e)
        {
            WindowBlur.EnableBlur(this);

            UpdateLanguage(GetFileVersionInfo());
            Owner = null; // Remove owner, so that minimizing main-window will not minimize this
            ShortcutList = new UserControls.Misc.ShortcutList
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Container.Children.Add(ShortcutList);

            var credits = new UserControls.Misc.Credits
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Container.Children.Add(credits);

            Deactivated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, ExpandBorder, false);
            Activated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, ExpandBorder, true);

            MouseLeftButtonDown += (_, mouseButtonEventArgs) =>
            {
                if (mouseButtonEventArgs.LeftButton == MouseButtonState.Pressed) DragMove();
            };

            // ExpandButton
            ExpandButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(chevronDownBrush);
            ExpandButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ExpandButtonBg);
            ExpandButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(chevronDownBrush);
            ExpandButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ExpandButtonBg);

            ExpandButton.Click += (_, _) =>
                UIHelper.ExtendOrCollapse(Height, startHeight, extendedHeight, this, Scroller, xGeo);

            PreviewMouseWheel += (_, mouseWheelEventArgs) => // Collapse when scrolling down
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (mouseWheelEventArgs.Delta < 0 && Height == startHeight)
                {
                    UIHelper.ExtendOrCollapse(Height, startHeight, extendedHeight, this, Scroller, xGeo);
                }
            };

            KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(Scroller, e, this);
            Scroller.MouseWheel += (_, e) => GenericWindowShortcuts.Window_MouseWheel(Scroller, e);
            TitleBar.MouseLeftButtonDown += (_, _) => DragMove();

            CloseButton.TheButton.Click += delegate { Hide(); };
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            var color = SettingsHelper.Settings.Theme.Dark ? Colors.White : (Color)Application.Current.Resources["MainColor"];

            GitHub.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, GitHubBrush);
            };
            GitHub.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, GitHubBrush);
            };

            License.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, LicenseBrush);
            };
            License.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, LicenseBrush);
            };

            PicViewSite.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, PicViewBrush);
            };
            PicViewSite.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, PicViewBrush);
            };

            UpdateButton.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, UpdateText);
            };
            UpdateButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(UpdateBrush); };
            UpdateButton.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, UpdateText);
            };
            UpdateButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(UpdateBrush); };

            UpdateButton.MouseLeftButtonDown += delegate
            {
                Update.UpdateHelper.Update(this);
            };
        }

        public void UpdateLanguage()
        {
            ShortcutList?.UpdateLanguage();
            UpdateLanguage(GetFileVersionInfo());
        }

        private void UpdateLanguage(FileVersionInfo fvi)
        {
            Title = TitleText.Text = TranslationHelper.GetTranslation("InfoWindowTitle");
            appVersion.Text = TranslationHelper.GetTranslation("Version") + " " + fvi.FileVersion;

            GitHub.Text = TranslationHelper.GetTranslation("GithubRepo");
            GitHub.ToolTip = TranslationHelper.GetTranslation("ViewLicenseFile");

            UpdateButtonLabel.Content = TranslationHelper.GetTranslation("CheckForUpdates");
        }

        private FileVersionInfo GetFileVersionInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi;
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
}