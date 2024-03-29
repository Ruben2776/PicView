﻿using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.Animations;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class CloseButton
{
    public CloseButton()
    {
        InitializeComponent();

        Loaded += delegate
        {
            MouseEnter += (_, _) => ButtonMouseOverAnim(CloseButtonBrush, true);
            MouseLeave += (_, _) => ButtonMouseLeaveAnim(CloseButtonBrush, true);

            if (!SettingsHelper.Settings.Theme.Dark)
            {
                AnimationHelper.LightThemeMouseEvent(this, IconBrush);
            }

            ToolTip = TranslationHelper.GetTranslation("Close");
        };
    }
}