using System.Windows.Media;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.Properties;
using PicView.WPF.UILogic;
using static PicView.WPF.Animations.MouseOverAnimations;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.Views.UserControls.Menus
{
    public partial class QuickSettingsMenu
    {
        public QuickSettingsMenu()
        {
            InitializeComponent();

            // SettingsButton
            SettingsButton.Click += delegate
            {
                ConfigureWindows.SettingsWindow();
                Close_UserControls();
            };
            SetButtonIconMouseOverAnimations(
                SettingsButtonBorder, SettingsButtonBrush, (SolidColorBrush)Resources["SettingsIcon"]);

            // InfoButton
            InfoButton.Click += delegate
            {
                ConfigureWindows.AboutWindow();
                Close_UserControls();
            };
            SetButtonIconMouseOverAnimations(
                InfoButton, InfoButtonBrush, (SolidColorBrush)Resources["AboutIcon"]);

            // Toggle Scroll
            ToggleScroll.IsChecked = Settings.Default.ScrollEnabled;
            ToggleScroll.Click += (s, x) => UpdateUIValues.SetScrolling(Settings.Default.ScrollEnabled);
            ToggleScrollBorder.MouseLeftButtonDown +=
                (_, _) => UpdateUIValues.SetScrolling(Settings.Default.ScrollEnabled);
            SetButtonIconMouseOverAnimations(ToggleScrollBorder, ToggleScrollBrush, ToggleScrollFill);

            // Toggle Fill
            ToggleFill.IsChecked = Settings.Default.FillImage;
            ToggleFill.Click += UpdateUIValues.SetAutoFill;
            ToggleFillBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFill;
            SetButtonIconMouseOverAnimations(ToggleFillBorder, ToggleFillBrush, ToggleFillFill);

            // Toggle Looping
            ToggleLooping.IsChecked = Settings.Default.Looping;
            ToggleLooping.Click += (_, _) => UpdateUIValues.SetLooping();
            ToggleLoopingBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetLooping();
            SetButtonIconMouseOverAnimations(ToggleLoopingBorder, ToggleLoopBrush, ToggleLoopFill);

            // Set Fit
            SetFit.IsChecked = Settings.Default.AutoFitWindow;
            SetFit.Click += UpdateUIValues.SetAutoFit;
            SetFitBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFit;
            SetButtonIconMouseOverAnimations(SetFitBorder, SetFitBrush, SetFitFill);

            // Stay on top
            StayOnTop.IsChecked = Settings.Default.TopMost;
            StayOnTop.Click += (_, _) => UpdateUIValues.SetTopMost();
            StayOnTopBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetTopMost();
            SetButtonIconMouseOverAnimations(StayOnTopBorder, StayOnTopBrush, StayOnTopFill);

            // Search subdirectories
            SearchSubDir.IsChecked = Settings.Default.IncludeSubDirectories;
            SearchSubDir.Click += async (_, _) =>
                await UpdateUIValues.ToggleIncludeSubdirectoriesAsync().ConfigureAwait(false);
            SearchSubDirBorder.MouseLeftButtonDown += async (_, _) =>
                await UpdateUIValues.ToggleIncludeSubdirectoriesAsync().ConfigureAwait(false);
            SetButtonIconMouseOverAnimations(SearchSubDirBorder, SearchSubDirBrush, SearchSubDirFill);
        }
    }
}