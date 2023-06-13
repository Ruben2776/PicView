﻿using PicView.Animations;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using System.Windows;

namespace PicView.Views.UserControls.Buttons;


public partial class X2
{
    public X2()
    {
        InitializeComponent();
        MouseLeftButtonDown += (_, _) =>
        {
            if (Settings.Default.FullscreenGallery)
            {
                SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
            }
            else if (GalleryFunctions.IsGalleryOpen)
            {
                GalleryToggle.CloseHorizontalGallery();
            }
            else if (Settings.Default.ShowInterface == false || Settings.Default.Fullscreen)
            {
                if (UC.GetPicGallery is null or { IsVisible: false })
                {
                    SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                }
            }
        };

        MouseEnter += (_, _) =>
        {
            MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
        };

        MouseLeave += (_, _) =>
        {
            MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
        };
    }
}