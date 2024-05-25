using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Layout;
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
}
