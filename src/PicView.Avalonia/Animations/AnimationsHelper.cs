using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Animations;

/// <summary>
/// Provides helper methods for creating and running animations within the Avalonia UI.
/// </summary>
public static class AnimationsHelper
{
    /// <summary>
    /// Creates a height animation for a <see cref="Layoutable"/> control.
    /// </summary>
    /// <param name="from">The starting height value.</param>
    /// <param name="to">The ending height value.</param>
    /// <param name="speed">The duration of the animation in seconds.</param>
    /// <returns>An <see cref="Animation"/> that animates the height property.</returns>
    public static Animation HeightAnimation(double from, double to, double speed)
    {
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
            Easing = new SplineEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Layoutable.HeightProperty,
                            Value = from
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Layoutable.HeightProperty,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                }
            }
        };
    }

    /// <summary>
    /// Creates a width animation for a <see cref="Layoutable"/> control.
    /// </summary>
    /// <param name="from">The starting width value.</param>
    /// <param name="to">The ending width value.</param>
    /// <param name="speed">The duration of the animation in seconds.</param>
    /// <returns>An <see cref="Animation"/> that animates the width property.</returns>
    public static Animation WidthAnimation(double from, double to, double speed)
    {
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
            Easing = new SplineEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Layoutable.WidthProperty,
                            Value = from
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Layoutable.WidthProperty,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                }
            }
        };
    }

    /// <summary>
    /// Creates an opacity animation for a <see cref="Visual"/> control.
    /// </summary>
    /// <param name="from">The starting opacity value.</param>
    /// <param name="to">The ending opacity value.</param>
    /// <param name="speed">The duration of the animation in seconds.</param>
    /// <returns>An <see cref="Animation"/> that animates the opacity property.</returns>
    public static Animation OpacityAnimation(double from, double to, double speed) =>
        OpacityAnimation(from, to, TimeSpan.FromSeconds(speed));

    /// <summary>
    /// Creates an opacity animation for a <see cref="Visual"/> control.
    /// </summary>
    /// <param name="from">The starting opacity value.</param>
    /// <param name="to">The ending opacity value.</param>
    /// <param name="timeSpan">The duration of the animation as a <see cref="TimeSpan"/>.</param>
    /// <returns>An <see cref="Animation"/> that animates the opacity property.</returns>
    public static Animation OpacityAnimation(double from, double to, TimeSpan timeSpan)
    {
        return new Animation
        {
            Duration = timeSpan,
            Easing = new LinearEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Visual.OpacityProperty,
                            Value = from
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Visual.OpacityProperty,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                }
            }
        };
    }

    /// <summary>
    /// Displays a brief animation to indicate a clipboard operation occurred.
    /// Fades a semi-transparent rectangle in and out to provide visual feedback.
    /// </summary>
    public static async Task CopyAnimation(MainWindow mainWindow)
    {
        const double speed = 0.2;
        const double opacity = 0.4;
        var startOpacityAnimation = OpacityAnimation(0, opacity, speed);
        var endOpacityAnimation = OpacityAnimation(opacity, 0, speed);
        Rectangle? rectangle = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            rectangle = new Rectangle
            {
                Width = mainWindow.UIHelper.GetMainView.Width,
                Height = mainWindow.UIHelper.GetMainView.Height,
                Opacity = 0,
                Fill = Brushes.Black,
                IsHitTestVisible = false
            };
            mainWindow.UIHelper.GetMainView.MainPanel.Children.Add(rectangle);
        });

        await startOpacityAnimation.RunAsync(rectangle).ConfigureAwait(true);
        await endOpacityAnimation.RunAsync(rectangle).ConfigureAwait(true);
        await Task.Delay(200).ConfigureAwait(true);

        await Dispatcher.UIThread.InvokeAsync(() => { mainWindow.UIHelper.GetMainView.MainPanel.Children.Remove(rectangle); });
    }

    /// <summary>
    /// Creates an animation to move a <see cref="TranslateTransform"/> from a start position to an end position.
    /// </summary>
    /// <param name="fromX">The starting X position.</param>
    /// <param name="fromY">The starting Y position.</param>
    /// <param name="toX">The ending X position.</param>
    /// <param name="toY">The ending Y position.</param>
    /// <param name="speed">The duration of the animation in seconds.</param>
    /// <returns>An <see cref="Animation"/> that animates the X and Y properties of a <see cref="TranslateTransform"/>.</returns>
    public static Animation CenteringAnimation(double fromX, double fromY, double toX, double toY, double speed) =>
        CenteringAnimation(fromX, fromY, toX, toY, TimeSpan.FromSeconds(speed));

    /// <summary>
    /// Creates an animation to move a <see cref="TranslateTransform"/> from a start position to an end position.
    /// </summary>
    /// <param name="fromX">The starting X position.</param>
    /// <param name="fromY">The starting Y position.</param>
    /// <param name="toX">The ending X position.</param>
    /// <param name="toY">The ending Y position.</param>
    /// <param name="duration">The duration of the animation as a <see cref="TimeSpan"/>.</param>
    /// <returns>An <see cref="Animation"/> that animates the X and Y properties of a <see cref="TranslateTransform"/>.</returns>
    public static Animation CenteringAnimation(double fromX, double fromY, double toX, double toY, TimeSpan duration)
    {
        return new Animation
        {
            Duration = duration,
            Easing = new SplineEasing(), // Using SplineEasing for smooth motion
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = TranslateTransform.XProperty,
                            Value = fromX
                        },
                        new Setter
                        {
                            Property = TranslateTransform.YProperty,
                            Value = fromY
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = TranslateTransform.XProperty,
                            Value = toX
                        },
                        new Setter
                        {
                            Property = TranslateTransform.YProperty,
                            Value = toY
                        }
                    },
                    Cue = new Cue(1d)
                }
            }
        };
    }
}