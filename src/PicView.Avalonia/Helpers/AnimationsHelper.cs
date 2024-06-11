using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace PicView.Avalonia.Helpers;
public static class AnimationsHelper
{
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
                },
            }
        };
    }
    
    public static Animation OpacityAnimation(double from, double to, double speed)
    {
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
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
                },
            }
        };
    }
    
    public static Animation FlipAnimation(object from, object to, double speed)
    {
        var scale = ScaleTransform.ScaleXProperty;
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
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
                            Property = scale,
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
                            Property = scale,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                },
            }
        };
    }
    
    public static Animation ZoomAnimation(double initialZoom, double zoomValue, double oldX, double oldY, double newX, double newY, TimeSpan duration)
    {
        return new Animation
        {
            Duration = duration,
            FillMode = FillMode.Forward,
            Easing = new LinearEasing(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, initialZoom),
                        new Setter(ScaleTransform.ScaleYProperty, initialZoom),
                        new Setter(TranslateTransform.XProperty, oldX),
                        new Setter(TranslateTransform.YProperty, oldY)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, zoomValue),
                        new Setter(ScaleTransform.ScaleYProperty, zoomValue),
                        new Setter(TranslateTransform.XProperty, newX),
                        new Setter(TranslateTransform.YProperty, newY)
                    }
                }
            }
        };
    }
    
}
        
