using System.Windows;

namespace XamlAnimatedGif;

public delegate void AnimationErrorEventHandler(DependencyObject d, AnimationErrorEventArgs e);

public class AnimationErrorEventArgs(object source, Exception exception, AnimationErrorKind kind) : RoutedEventArgs(AnimationBehavior.ErrorEvent, source)
{
    public Exception Exception { get; } = exception;

    public AnimationErrorKind Kind { get; } = kind;
}

public enum AnimationErrorKind
{
    Loading,
    Rendering
}