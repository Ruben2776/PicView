﻿using PicView.Core.Config;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.UILogic.Sizing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PicView.Core.ImageTransformations;

namespace PicView.WPF.UILogic.TransformImage;

/// <summary>
/// Contains static methods to manipulate the rotation and flipping of the displayed image.
/// </summary>
internal static class Rotation
{
    /// <summary>
    /// Used to determine if the image has been flipped
    /// </summary>
    internal static bool IsFlipped { get; set; }

    /// <summary>
    /// Used to get rotation by degrees
    /// </summary>
    internal static double RotationAngle { get; set; }

    /// <summary>
    /// Rotates the image and moves the cursor to the new position.
    /// </summary>
    /// <param name="up">If true, rotates the image up (clockwise); otherwise, rotates it down (counterclockwise).</param>
    /// <param name="uIElement">The UIElement in which the image is displayed.</param>
    internal static async Task RotateAndMoveCursor(bool up, UIElement uIElement)
    {
        Rotate(false, up);
        if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
        {
            return;
        }

        // Move cursor after rotating
        await Task.Delay(50)
            .ConfigureAwait(true); // Delay it, so that the move takes place after window has resized
        var p = uIElement.PointToScreen(new Point(25, 25));
        PicView.Windows.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
    }

    /// <summary>
    /// Rotates the image.
    /// </summary>
    /// <param name="keyDown">If true, the rotation is triggered by a key down event; otherwise, it is triggered by a key up event.</param>
    /// <param name="up">If true, rotates the image up (clockwise); otherwise, rotates it down (counterclockwise).</param>
    internal static void Rotate(bool keyDown, bool up)
    {
        if (keyDown)
        {
            Rotate(up ? RotationAngle + 1 : RotationAngle - 1);
        }
        else
        {
            if (RotationHelper.IsValidRotation(RotationAngle))
            {
                Rotate(RotationHelper.Rotate(RotationAngle, up));
            }
            else
            {
                Rotate(RotationHelper.NextRotationAngle(RotationAngle, up));
            }
        }
    }

    /// <summary>
    /// Rotates the image.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <param name="animate">If true, animates the rotation; otherwise, performs the rotation instantly.</param>
    internal static void Rotate(double degrees, bool animate = false)
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Source == null)
        {
            return;
        }

        if (animate)
        {
            var animatedRotation = new DoubleAnimation
            {
                From = RotationAngle,
                To = degrees,
                Duration = TimeSpan.FromSeconds(.7),
            };
            animatedRotation.Completed += (_, _) =>
            {
                DoRotation();
            };
            ConfigureWindows.GetMainWindow.MainImage.LayoutTransform.BeginAnimation(RotateTransform.AngleProperty, animatedRotation);
        }
        else
        {
            DoRotation();
        }

        return;

        void DoRotation()
        {
            var rt = new RotateTransform { Angle = RotationAngle = degrees };

            ScaleImage.FitImage(ConfigureWindows.GetMainWindow.MainImage.Source.Width,
                ConfigureWindows.GetMainWindow.MainImage.Source.Height);

            // If it's flipped, keep it flipped when rotating
            if (IsFlipped)
            {
                var tg = new TransformGroup();
                var flip = new ScaleTransform { ScaleX = -1 };
                tg.Children.Add(flip);
                tg.Children.Add(rt);
                ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = tg;
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = rt;
            }
        }
    }

    /// <summary>
    /// Flips the image
    /// </summary>
    internal static void Flip()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Source == null)
        {
            return;
        }

        var rt = new RotateTransform();
        var flip = new ScaleTransform();
        var tg = new TransformGroup();

        if (!IsFlipped)
        {
            flip.ScaleX = -1;
            IsFlipped = true;
        }
        else
        {
            flip.ScaleX = +1;
            IsFlipped = false;
        }

        rt.Angle = RotationAngle;
        tg.Children.Add(flip);
        tg.Children.Add(rt);
        ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = tg;

        UpdateUIValues.ChangeFlipButton();
    }
}