﻿using PicView.Core.Config;
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

        // InfoButton
        InfoButton.Click += delegate
        {
            ConfigureWindows.AboutWindow();
            Close_UserControls();
        };
        SetButtonIconMouseOverAnimations(
            InfoButton, InfoButtonBrush, (SolidColorBrush)Resources["AboutIcon"]);

        // Toggle Scroll
        ToggleScroll.IsChecked = SettingsHelper.Settings.Zoom.ScrollEnabled;
        ToggleScroll.Click += (s, x) => UpdateUIValues.SetScrolling(SettingsHelper.Settings.Zoom.ScrollEnabled);
        ToggleScrollBorder.MouseLeftButtonDown +=
            (_, _) => UpdateUIValues.SetScrolling(SettingsHelper.Settings.Zoom.ScrollEnabled);
        SetButtonIconMouseOverAnimations(ToggleScrollBorder, ToggleScrollBrush, ToggleScrollFill);

        // Toggle Fill
        ToggleFill.IsChecked = SettingsHelper.Settings.ImageScaling.StretchImage;
        ToggleFill.Click += async (_, _) => await UpdateUIValues.SetAutoFill().ConfigureAwait(false);
        ToggleFillBorder.MouseLeftButtonDown += async (_, _) => await UpdateUIValues.SetAutoFill().ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(ToggleFillBorder, ToggleFillBrush, ToggleFillFill);

        // Toggle Looping
        ToggleLooping.IsChecked = SettingsHelper.Settings.UIProperties.Looping;
        ToggleLooping.Click += (_, _) => UpdateUIValues.SetLooping();
        ToggleLoopingBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetLooping();
        SetButtonIconMouseOverAnimations(ToggleLoopingBorder, ToggleLoopBrush, ToggleLoopFill);

        // Set Fit
        SetFit.IsChecked = SettingsHelper.Settings.WindowProperties.AutoFit;
        SetFit.Click += async (_, _) => await UpdateUIValues.SetAutoFit().ConfigureAwait(false);
        SetFitBorder.MouseLeftButtonDown += async (_, _) => await UpdateUIValues.SetAutoFit().ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(SetFitBorder, SetFitBrush, SetFitFill);

        // Stay on top
        StayOnTop.IsChecked = SettingsHelper.Settings.WindowProperties.TopMost;
        StayOnTop.Click += (_, _) => UpdateUIValues.SetTopMost();
        StayOnTopBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetTopMost();
        SetButtonIconMouseOverAnimations(StayOnTopBorder, StayOnTopBrush, StayOnTopFill);

        // Search subdirectories
        SearchSubDir.IsChecked = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
        SearchSubDir.Click += async (_, _) =>
            await UpdateUIValues.ToggleIncludeSubdirectoriesAsync().ConfigureAwait(false);
        SearchSubDirBorder.MouseLeftButtonDown += async (_, _) =>
            await UpdateUIValues.ToggleIncludeSubdirectoriesAsync().ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(SearchSubDirBorder, SearchSubDirBrush, SearchSubDirFill);

        UpdateLanguage();
    }

    internal void UpdateLanguage()
    {
        SettingsButton.ToolTip = TranslationHelper.GetTranslation("ShowAllSettingsWindow");
        SettingsButtonTextBlock.ToolTip = TranslationHelper.GetTranslation("ShowAllSettingsWindow");
        SettingsButtonTextBlock.Text = TranslationHelper.GetTranslation("Settings");
        SearchSubDirBorderTextBlock.Text = TranslationHelper.GetTranslation("SearchSubdirectory");
        StayOnTopBorderTextBlock.Text = TranslationHelper.GetTranslation("StayTopMost");
        SetFitTextBlock.Text = TranslationHelper.GetTranslation("AutoFitWindow");
        ToggleLoopingTextBlock.Text = TranslationHelper.GetTranslation("ToggleLooping");
        ToggleFillTextBlock.Text = TranslationHelper.GetTranslation("StretchImage");
        ToggleScrollTextBlock.Text = TranslationHelper.GetTranslation("ToggleScroll");
        InfoButton.ToolTip = TranslationHelper.GetTranslation("ShowInfoWindow");
        InfoButtonTextBlock.Text = TranslationHelper.GetTranslation("About");
    }
}