using System.Windows;

namespace XamlAnimatedGif;

public delegate void AnimationCompletedEventHandler(DependencyObject d, AnimationCompletedEventArgs e);

public class AnimationCompletedEventArgs(object source) : RoutedEventArgs(AnimationBehavior.AnimationCompletedEvent, source);