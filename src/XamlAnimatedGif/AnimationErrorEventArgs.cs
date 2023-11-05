using System;
using System.Windows;

namespace XamlAnimatedGif
{
    public delegate void AnimationErrorEventHandler(DependencyObject d, AnimationErrorEventArgs e);

    public class AnimationErrorEventArgs : RoutedEventArgs
    {
        public AnimationErrorEventArgs(object source, Exception exception, AnimationErrorKind kind)
            : base(AnimationBehavior.ErrorEvent, source)
        {
            Exception = exception;
            Kind = kind;
        }

        public Exception Exception { get; }

        public AnimationErrorKind Kind { get; }
    }

    public enum AnimationErrorKind
    {
        Loading,
        Rendering
    }
}
