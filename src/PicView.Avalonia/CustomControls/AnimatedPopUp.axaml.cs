using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace PicView.Avalonia.CustomControls;

public class AnimatedPopUp : ContentControl
{
    public AnimatedPopUp()
    {
        // Make a new underlay control
        _mUnderlayControl = new Border
        {
            Background = Brushes.Black,
            Opacity = 0,
            ZIndex = 9
        };

        // On press, close popup
        _mUnderlayControl.PointerPressed += (_, _) =>
        {
            Open = false;
        };
        
        // Make a new dispatch timer
        _mAnimationTimer = new DispatcherTimer
        {
            // Set the timer to run 60 times a second
            Interval = _mFrameRate 
        };

        _mSizingTimer = new Timer((t) =>
        {
            // If we have already calculated the size...
            if (_mSizeFound)
                // No longer accept new sizes
                return;

            // We have now found our desired size
            _mSizeFound = true;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Update the desired size
                UpdateDesiredSize();
                
                // Update animation
                UpdateAnimation();
            });
        });
        
        // Callback on every tick
        _mAnimationTimer.Tick += (s, e) => AnimationTick();
    }
    
    #region Private Members

    /// <summary>
    /// The underlay control for closing this popup
    /// </summary>
    private readonly Control _mUnderlayControl;

    /// <summary>
    /// Indicates if this is the first time we are animating
    /// </summary>
    private bool _mFirstAnimation = true;

    /// <summary>
    /// Indicates if we have captured the opacity value yet
    /// </summary>
    private bool _mOpacityCaptured = false;
    
    /// <summary>
    /// Store the controls original Opacity value at startup
    /// </summary>
    private double _mOriginalOpacity;
    
    /// <summary>
    /// The speed of the animation in FPS
    /// </summary>
    private readonly TimeSpan _mFrameRate = TimeSpan.FromSeconds(1 / 60.0);
    
    // Calculate total ticks that make up the animation time
    private int MTotalTicks => (int)(_animationTime.TotalSeconds / _mFrameRate.TotalSeconds);

    /// <summary>
    /// Store the controls desired size
    /// </summary>
    private Size _mDesiredSize;

    /// <summary>
    /// A flag for when we are animating
    /// </summary>
    private bool _mAnimating;

    /// <summary>
    /// Keeps track of if we have found the desired 100% width/height auto size
    /// </summary>
    private bool _mSizeFound;

    /// <summary>
    /// The animation UI timer
    /// </summary>
    private readonly DispatcherTimer _mAnimationTimer;

    /// <summary>
    /// The timeout timer to detect when auto-sizing has finished firing
    /// </summary>
    private readonly Timer _mSizingTimer;

    /// <summary>
    /// The current position in the animation
    /// </summary>
    private int _mAnimationCurrentTick;
    
    #endregion
    
    #region Public Properties

    /// <summary>
    /// Indicates if the control is currently opened
    /// </summary>
    public bool IsOpened => _mAnimationCurrentTick >= MTotalTicks;

    #region Open
    
    private bool _open;

    public static readonly DirectProperty<AnimatedPopUp, bool> OpenProperty = AvaloniaProperty.RegisterDirect<AnimatedPopUp, bool>(
        nameof(Open), o => o.Open, (o, v) => o.Open = v);

    /// <summary>
    /// Property to set whether the control should be open or closed
    /// </summary>
    public bool Open
    {
        get => _open;
        set
        {
            // If the value has not changed...
            if (value == _open)
                // Do nothing
                return;
            
            // If we are opening...
            if (value)
            {
                // If the parent is a grid...
                if (Parent is Grid grid)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Set grid row/column span
                        if (grid.RowDefinitions?.Count > 0)
                            _mUnderlayControl.SetValue(Grid.RowSpanProperty, grid.RowDefinitions.Count);
                    
                        if (grid.ColumnDefinitions?.Count > 0)
                            _mUnderlayControl.SetValue(Grid.ColumnSpanProperty, grid.ColumnDefinitions.Count);
                    
                        // Insert the underlay control
                        if (!grid.Children.Contains(_mUnderlayControl))
                            grid.Children.Insert(0, _mUnderlayControl);
                    });
                }
            }
            // If closing...
            else
            {
                // If the control is currently fully open...
                if (IsOpened)
                    // Update desired size
                    UpdateDesiredSize();
            }
            
            // Update animation
            UpdateAnimation();

            // Raise the property changed event
            SetAndRaise(OpenProperty, ref _open, value);
        }
    }
    
    #endregion
    
    #region Animation Time

    private TimeSpan _animationTime = TimeSpan.FromSeconds(3);

    public static readonly DirectProperty<AnimatedPopUp, TimeSpan> AnimationTimeProperty = AvaloniaProperty.RegisterDirect<AnimatedPopUp, TimeSpan>(
        nameof(AnimationTime), o => o.AnimationTime, (o, v) => o.AnimationTime = v);

    public TimeSpan AnimationTime
    {
        get => _animationTime;
        set => SetAndRaise(AnimationTimeProperty, ref _animationTime, value);
    }
    
    #endregion
    
    #region Animate Opacity

    private bool _animateOpacity = true;

    public static readonly DirectProperty<AnimatedPopUp, bool> AnimateOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopUp, bool>(
        nameof(AnimateOpacity), o => o.AnimateOpacity, (o, v) => o.AnimateOpacity = v);

    public bool AnimateOpacity
    {
        get => _animateOpacity;
        set => SetAndRaise(AnimateOpacityProperty, ref _animateOpacity, value);
    }
    
    #endregion
    
    #region Underlay Opacity
    
    private double _underlayOpacity = 0.2;

    public static readonly DirectProperty<AnimatedPopUp, double> UnderlayOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopUp, double>(
        "UnderlayOpacity", o => o.UnderlayOpacity, (o, v) => o.UnderlayOpacity = v);

    public double UnderlayOpacity
    {
        get => _underlayOpacity;
        set => SetAndRaise(UnderlayOpacityProperty, ref _underlayOpacity, value);
    }
    
    #endregion
    
    #endregion
    
    
    #region Private Methods

    /// <summary>
    /// Updates the animation desired size based on the current visuals desired size
    /// </summary>
    private void UpdateDesiredSize() => _mDesiredSize = DesiredSize - Margin;   

    /// <summary>
    /// Calculate and start any new required animations
    /// </summary>
    private void UpdateAnimation()
    {
        // Do nothing if we still haven't found our initial size
        if (!_mSizeFound)
            return;
        
        // Start the animation thread again
        _mAnimationTimer.Start();
    }

    /// <summary>
    /// Should be called when an open or close transition has complete
    /// </summary>
    private void AnimationComplete()
    {
        // If open...
        if (_open)
        {
            // Set size to desired size
            Width = double.NaN;
            Height = double.NaN;

            // Make sure opacity is set to original value
            Opacity = _mOriginalOpacity;
        }
        // If closed...
        else
        {
            // Set size to 0
            Width = 0;
            Height = 0;
            
            // If the parent is a grid...
            if (Parent is Grid grid)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Reset opacity
                    _mUnderlayControl.Opacity = 0;

                    // Remove underlay
                    grid.Children.Remove(_mUnderlayControl);
                });
            }
        }
    }
    
    /// <summary>
    /// Update controls sizes based on the next tick of an animation
    /// </summary>
    private void AnimationTick()
    {
        // If this is the first call after calculating the desired size...
        if (_mFirstAnimation)
        {
            // Clear the flag
            _mFirstAnimation = false;

            // Stop this animation timer
            _mAnimationTimer.Stop();
            
            // Reset opacity
            Opacity = _mOriginalOpacity;
            
            // Set the final size
            AnimationComplete();

            // Do on this tick
            return;
        }

        // If we have reached the end of our animation...
        if ((_open && _mAnimationCurrentTick >= MTotalTicks) ||
            (!_open && _mAnimationCurrentTick == 0))
        {
            // Stop this animation timer
            _mAnimationTimer.Stop();
            
            // Set the final size
            AnimationComplete();
            
            // Clear animating flag
            _mAnimating = false;
            
            // Break out of code
            return;
        }

        // Set animating flag
        _mAnimating = true;
        
        // Move the tick in the right direction
        _mAnimationCurrentTick += _open ? 1 : -1;

        // Get percentage of the way through the current animation
        var percentageAnimated = (float)_mAnimationCurrentTick / MTotalTicks;
        
        // Make an animation easing
        var quadraticEasing = new QuadraticEaseIn();
        var linearEasing = new LinearEasing();
        
        // Calculate final width and height
        var finalWidth = _mDesiredSize.Width * quadraticEasing.Ease(percentageAnimated);
        var finalHeight = _mDesiredSize.Height * quadraticEasing.Ease(percentageAnimated);

        // Do our animation
        Width = finalWidth;
        Height = finalHeight;
        
        // Animate opacity
        if (AnimateOpacity)
            Opacity = _mOriginalOpacity * linearEasing.Ease(percentageAnimated);

        // Animate underlay
        _mUnderlayControl.Opacity = _underlayOpacity * quadraticEasing.Ease(percentageAnimated);
    }
    
    #endregion
    
    public override void Render(DrawingContext context)
    {
        // If we have not yet found the desired size...
        if (!_mSizeFound)
        {
            // If we have not yet captured the opacity
            if (!_mOpacityCaptured)
            {
                // Set flag to true
                _mOpacityCaptured = true;
                
                // Remember original controls opacity
                _mOriginalOpacity = Opacity;

                // Hide control
                Dispatcher.UIThread.InvokeAsync(() =>  Opacity = 0);
            }

            _mSizingTimer.Change(100, int.MaxValue);
        }

        base.Render(context);
    }
}
