﻿using System.Windows.Controls;

namespace PicView.Views.UserControls.Buttons;

public partial class ResizeButton : UserControl
{
    public ResizeButton()
    {
        InitializeComponent();

        //Loaded += delegate
        //{
        //    var IconBrush = (SolidColorBrush)Resources["IconBrush"];
        //    TheButton.PreviewMouseLeftButtonDown += delegate
        //    {
        //        ButtonMouseOverAnim(IconBrush, false, true);
        //        ButtonMouseOverAnim(ButtonBrush, false, true);
        //        AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
        //    };

        //    TheButton.MouseEnter += delegate
        //    {
        //        ButtonMouseOverAnim(IconBrush);
        //        AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
        //    };

        //    TheButton.MouseLeave += delegate
        //    {
        //        ButtonMouseLeaveAnim(IconBrush);
        //        AnimationHelper.MouseLeaveBgTexColor(ButtonBrush);
        //    };

        //    TheButton.Click += (_, _) => UpdateUIValues.ToggleQuickResize();
        //};
    }


}