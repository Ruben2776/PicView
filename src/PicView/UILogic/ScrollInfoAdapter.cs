using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UILogic
{
    public class SmoothScrollViewer : ScrollViewer
    {
        public SmoothScrollViewer()
        {
            Loaded += ScrollViewer_Loaded;
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ScrollInfo = new ScrollInfoAdapter(ScrollInfo);
            }
            catch (Exception)
            {
                //
            }
        }
    }

    public class ScrollInfoAdapter : UIElement, IScrollInfo
    {
        internal const double _scrollLineDelta = 16.0;
        internal const double _mouseWheelDelta = 48.0;
        private readonly IScrollInfo _child;
        private double _computedHorizontalOffset;
        private double _computedVerticalOffset;

        public ScrollInfoAdapter(IScrollInfo child)
        {
            _child = child;
        }

        public bool CanVerticallyScroll
        {
            get =>
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                _child is not null && _child.CanVerticallyScroll;
            set
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (_child is null)
                {
                    return;
                }

                _child.CanVerticallyScroll = value;
            }
        }

        public bool CanHorizontallyScroll
        {
            get =>
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                _child is not null && _child.CanHorizontallyScroll;
            set
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (_child is null)
                {
                    return;
                }

                _child.CanHorizontallyScroll = value;
            }
        }

        public double ExtentWidth => _child.ExtentWidth;

        public double ExtentHeight => _child.ExtentHeight;

        public double ViewportWidth => _child.ViewportWidth;

        public double ViewportHeight => _child.ViewportHeight;

        public double HorizontalOffset
        {
            get
            {
                try
                {
                    return _child.HorizontalOffset;
                }
                catch (Exception)
                {
                    //
                }

                return 0;
            }
        }

        public double VerticalOffset => _child.VerticalOffset;

        public ScrollViewer ScrollOwner
        {
            get => _child.ScrollOwner;
            set => _child.ScrollOwner = value;
        }

        public void LineUp()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.LineUp();
            }
            else
            {
                VerticalScroll(_computedVerticalOffset - _scrollLineDelta);
            }
        }

        public void LineDown()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.LineDown();
            }
            else
            {
                VerticalScroll(_computedVerticalOffset + _scrollLineDelta);
            }
        }

        public void LineLeft()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.LineLeft();
            }
            else
            {
                HorizontalScroll(_computedHorizontalOffset - _scrollLineDelta);
            }
        }

        public void LineRight()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.LineRight();
            }
            else
            {
                HorizontalScroll(_computedHorizontalOffset + _scrollLineDelta);
            }
        }

        public void MouseWheelUp()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.MouseWheelUp();
            }
            else
            {
                VerticalScroll(_computedVerticalOffset - _mouseWheelDelta);
            }
        }

        public void MouseWheelDown()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.MouseWheelDown();
            }
            else
            {
                VerticalScroll(_computedVerticalOffset + _mouseWheelDelta);
            }
        }

        public void MouseWheelLeft()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.MouseWheelLeft();
            }
            else
            {
                HorizontalScroll(_computedHorizontalOffset - _mouseWheelDelta);
            }
        }

        public void MouseWheelRight()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.MouseWheelRight();
            }
            else
            {
                HorizontalScroll(_computedHorizontalOffset + _mouseWheelDelta);
            }
        }

        public void PageUp()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.PageUp();
            }
            else
            {
                VerticalScroll(_computedVerticalOffset - ViewportHeight);
            }
        }

        public void PageDown()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.PageDown();
            }
            else
            {
                VerticalScroll(_computedVerticalOffset + ViewportHeight);
            }
        }

        public void PageLeft()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.PageLeft();
            }
            else
            {
                HorizontalScroll(_computedHorizontalOffset - ViewportWidth);
            }
        }

        public void PageRight()
        {
            if (_child.ScrollOwner.CanContentScroll)
            {
                _child.PageRight();
            }
            else
            {
                HorizontalScroll(_computedHorizontalOffset + ViewportWidth);
            }
        }

        public void SetHorizontalOffset(double offset)
        {
            try
            {
                if (_child is null)
                    return;
                if (_child.ScrollOwner.CanContentScroll)
                {
                    _child.SetHorizontalOffset(offset);
                }
                else
                {
                    _computedHorizontalOffset = offset;
                    Animate(HorizontalScrollOffsetProperty, offset);
                }
            }
            catch (Exception)
            {
                //
            }
        }

        public void SetVerticalOffset(double offset)
        {
            try
            {
                if (_child is null)
                    return;
                if (_child.ScrollOwner.CanContentScroll)
                {
                    _child.SetVerticalOffset(offset);
                }
                else
                {
                    _computedVerticalOffset = offset;
                    Animate(VerticalScrollOffsetProperty, offset);
                }
            }
            catch (Exception)
            {
                //
            }
        }

        #region not exposed methods

        private void Animate(DependencyProperty property, double targetValue)
        {
            try
            {
                //make a smooth animation that starts and ends slowly
                var keyFramesAnimation = new DoubleAnimationUsingKeyFrames
                {
                    Duration = TimeSpan.FromSeconds(.8)
                };
                keyFramesAnimation.KeyFrames.Add(
                    new SplineDoubleKeyFrame(
                        targetValue,
                        KeyTime.FromTimeSpan(TimeSpan.FromSeconds(.8)),
                        new KeySpline(0.5, 0.0, 0.5, 1.0)
                    )
                );

                BeginAnimation(property, keyFramesAnimation);
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine(exception);
#endif
            }
        }

        private void VerticalScroll(double val)
        {
            try
            {
                if (!(Math.Abs(_computedVerticalOffset - ValidateVerticalOffset(val)) >
                      0.1)) //prevent restart of animation in case of frequent event fire
                {
                    return;
                }

                _computedVerticalOffset = ValidateVerticalOffset(val);
                Animate(VerticalScrollOffsetProperty, _computedVerticalOffset);
            }
            catch (Exception)
            {
                //
            }
        }

        private void HorizontalScroll(double val)
        {
            try
            {
                if (!(Math.Abs(_computedHorizontalOffset - ValidateHorizontalOffset(val)) >
                      0.1)) //prevent restart of animation in case of frequent event fire
                {
                    return;
                }

                _computedHorizontalOffset = ValidateHorizontalOffset(val);
                Animate(HorizontalScrollOffsetProperty, _computedHorizontalOffset);
            }
            catch (Exception)
            {
                //
            }
        }

        private double ValidateVerticalOffset(double verticalOffset)
        {
            if (verticalOffset < 0)
            {
                return 0;
            }

            return verticalOffset > _child.ScrollOwner.ScrollableHeight
                ? _child.ScrollOwner.ScrollableHeight
                : verticalOffset;
        }

        private double ValidateHorizontalOffset(double horizontalOffset)
        {
            if (horizontalOffset < 0)
            {
                return 0;
            }

            return horizontalOffset > _child.ScrollOwner.ScrollableWidth
                ? _child.ScrollOwner.ScrollableWidth
                : horizontalOffset;
        }

        #endregion not exposed methods

        #region helper dependency properties as scrollbars are not animatable by default

        internal double VerticalScrollOffset
        {
            get => (double)GetValue(VerticalScrollOffsetProperty);
            set => SetValue(VerticalScrollOffsetProperty, value);
        }

        internal static readonly DependencyProperty VerticalScrollOffsetProperty =
            DependencyProperty.Register("VerticalScrollOffset", typeof(double), typeof(ScrollInfoAdapter),
                new PropertyMetadata(0.0, OnVerticalScrollOffsetChanged));

        private static void OnVerticalScrollOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var smoothScrollViewer = (ScrollInfoAdapter)d;
            smoothScrollViewer._child.SetVerticalOffset((double)e.NewValue);
        }

        internal double HorizontalScrollOffset
        {
            get
            {
                try
                {
                    return (double)GetValue(HorizontalScrollOffsetProperty);
                }
                catch (Exception)
                {
                    //
                }

                return 0;
            }
            set
            {
                try
                {
                    SetValue(HorizontalScrollOffsetProperty, value);
                }
                catch (Exception)
                {
                    //
                }
            }
        }

        internal static readonly DependencyProperty HorizontalScrollOffsetProperty =
            DependencyProperty.Register("HorizontalScrollOffset", typeof(double), typeof(ScrollInfoAdapter),
                new PropertyMetadata(0.0, OnHorizontalScrollOffsetChanged));

        private static void OnHorizontalScrollOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var smoothScrollViewer = (ScrollInfoAdapter)d;
                smoothScrollViewer._child.SetHorizontalOffset((double)e.NewValue);
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine(exception);
#endif
            }
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            throw new NotImplementedException();
        }

        #endregion helper dependency properties as scrollbars are not animatable by default
    }
}