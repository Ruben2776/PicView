using PicView.Core.Config;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.UILogic;
using System.Windows.Media;
using PicView.Core.Localization;
using static PicView.WPF.Animations.MouseOverAnimations;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.Views.UserControls.Menus;

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
        SettingsButton.ToolTip = TranslationHelper.GetTranslation("ShowAllSettingsWindow");
        SettingsButtonTextBlock.ToolTip = TranslationHelper.GetTranslation("ShowAllSettingsWindow");
        SettingsButtonTextBlock.Text = TranslationHelper.GetTranslation("Settings");

        // InfoButton
        InfoButton.Click += delegate
        {
            ConfigureWindows.AboutWindow();
            Close_UserControls();
        };
        SetButtonIconMouseOverAnimations(
            InfoButton, InfoButtonBrush, (SolidColorBrush)Resources["AboutIcon"]);
        InfoButton.ToolTip = TranslationHelper.GetTranslation("ShowInfoWindow");
        InfoButtonTextBlock.Text = TranslationHelper.GetTranslation("About");

        // Toggle Scroll
        ToggleScroll.IsChecked = SettingsHelper.Settings.Zoom.ScrollEnabled;
        ToggleScroll.Click += (s, x) => UpdateUIValues.SetScrolling(SettingsHelper.Settings.Zoom.ScrollEnabled);
        ToggleScrollBorder.MouseLeftButtonDown +=
            (_, _) => UpdateUIValues.SetScrolling(SettingsHelper.Settings.Zoom.ScrollEnabled);
        SetButtonIconMouseOverAnimations(ToggleScrollBorder, ToggleScrollBrush, ToggleScrollFill);
        ToggleScrollTextBlock.Text = TranslationHelper.GetTranslation("ToggleScroll");

        // Toggle Fill
        ToggleFill.IsChecked = SettingsHelper.Settings.ImageScaling.StretchImage;
        ToggleFill.Click += UpdateUIValues.SetAutoFill;
        ToggleFillBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFill;
        SetButtonIconMouseOverAnimations(ToggleFillBorder, ToggleFillBrush, ToggleFillFill);
        ToggleFillTextBlock.Text = TranslationHelper.GetTranslation("StretchImage");

        // Toggle Looping
        ToggleLooping.IsChecked = SettingsHelper.Settings.UIProperties.Looping;
        ToggleLooping.Click += (_, _) => UpdateUIValues.SetLooping();
        ToggleLoopingBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetLooping();
        SetButtonIconMouseOverAnimations(ToggleLoopingBorder, ToggleLoopBrush, ToggleLoopFill);
        ToggleLoopingTextBlock.Text = TranslationHelper.GetTranslation("ToggleLooping");

        // Set Fit
        SetFit.IsChecked = SettingsHelper.Settings.WindowProperties.AutoFit;
        SetFit.Click += UpdateUIValues.SetAutoFit;
        SetFitBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFit;
        SetButtonIconMouseOverAnimations(SetFitBorder, SetFitBrush, SetFitFill);
        SetFitTextBlock.Text = TranslationHelper.GetTranslation("AutoFitWindow");

        // Stay on top
        StayOnTop.IsChecked = SettingsHelper.Settings.WindowProperties.TopMost;
        StayOnTop.Click += (_, _) => UpdateUIValues.SetTopMost();
        StayOnTopBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetTopMost();
        SetButtonIconMouseOverAnimations(StayOnTopBorder, StayOnTopBrush, StayOnTopFill);
        StayOnTopBorderTextBlock.Text = TranslationHelper.GetTranslation("StayTopMost");

        // Search subdirectories
        SearchSubDir.IsChecked = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
        SearchSubDir.Click += async (_, _) =>
            await UpdateUIValues.ToggleIncludeSubdirectoriesAsync().ConfigureAwait(false);
        SearchSubDirBorder.MouseLeftButtonDown += async (_, _) =>
            await UpdateUIValues.ToggleIncludeSubdirectoriesAsync().ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(SearchSubDirBorder, SearchSubDirBrush, SearchSubDirFill);
        SearchSubDirBorderTextBlock.Text = TranslationHelper.GetTranslation("SearchSubdirectory");
    }
}