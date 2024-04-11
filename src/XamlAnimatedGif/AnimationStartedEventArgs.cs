using System.Windows;

namespace XamlAnimatedGif;

public delegate void AnimationStartedEventHandler(DependencyObject d, AnimationStartedEventArgs e);

public class AnimationStartedEventArgs(object source) : RoutedEventArgs(AnimationBehavior.AnimationStartedEvent, source);