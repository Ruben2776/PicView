using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.UI;

/// <summary>
///     Handles fade-in and fade-out animation for a button (or button group) based on pointer proximity.
/// </summary>
public class HoverFadeButtonHandler
{
    private readonly Control? _childButton;
    private readonly Control _mainButton;
    private readonly MainViewModel _vm;
    private CancellationTokenSource? _fadeCts;

    /// <summary>
    ///     Initializes the hover fade logic for a button or button group.
    /// </summary>
    /// <param name="mainButton">The main button or parent control.</param>
    /// <param name="vm">The ViewModel for context (navigation, settings, etc).</param>
    /// <param name="childButton">Optional child button (e.g., an icon inside the button).</param>
    public HoverFadeButtonHandler(Control mainButton, MainViewModel vm, Control? childButton = null)
    {
        _mainButton = mainButton ?? throw new ArgumentNullException(nameof(mainButton));
        _childButton = childButton;
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));

        AttachEvents();
    }

    /// <summary>
    ///     Duration of fade-in and fade-out in seconds.
    /// </summary>
    private static double FadeInDuration => 0.3;
    private static double FadeOutDuration => 0.45;

    private void AttachEvents()
    {
        _mainButton.PointerEntered += OnPointerEntered;
        _mainButton.PointerExited += OnPointerExited;
        if (_childButton == null)
        {
            return;
        }

        _childButton.PointerEntered += OnPointerEntered;
        _childButton.PointerExited += OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!ShouldShowButton())
        {
            SetOpacity(0);
            return;
        }

        FadeTo(1, FadeInDuration);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Captured != null) // Don't fade out when captured
        {
            return;
        }
        
        // Delay fade-out to ensure pointer is truly outside both parent and child
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(30); // short delay to allow pointer transitions
            if (!IsPointerOver())
            {
                FadeTo(0, FadeOutDuration);
            }
        });
    }

    private bool ShouldShowButton()
    {
        // You may want to extend this with more checks
        if (!Settings.UIProperties.ShowAltInterfaceButtons || GalleryFunctions.IsFullGalleryOpen)
        {
            return false;
        }

        if (_childButton != null && !NavigationManager.CanNavigate(_vm))
        {
            return false;
        }

        return _childButton == null || NavigationManager.GetCount > 1;
    }

    /// <summary>
    ///     Checks if the pointer is over the main button or the child button.
    /// </summary>
    private bool IsPointerOver()
    {
        if (_mainButton.IsPointerOver)
        {
            return true;
        }

        return _childButton?.IsPointerOver == true;
    }

    /// <summary>
    ///     Fades the button(s) from their current opacity to the target value.
    /// </summary>
    private void FadeTo(double targetOpacity, double durationSeconds)
    {
        _fadeCts?.Cancel();
        var cts = new CancellationTokenSource();
        _fadeCts = cts;

        var controls = _childButton != null ? new[] { _mainButton, _childButton } : new[] { _mainButton };

        foreach (var ctrl in controls)
        {
            // Run animation for each control
            _ = AnimateOpacityAsync(ctrl, targetOpacity, durationSeconds, cts.Token);
        }
    }

    private async Task AnimateOpacityAsync(Control control, double targetOpacity, double durationSeconds,
        CancellationToken token)
    {
        if (control == UIHelper.GetHoverBar)
        {
            // Fix instances where hover bar is visible, but shouldn't be
            // TODO: find a cleaner solution
            if (!_vm.HoverbarViewModel.IsHoverbarVisible.Value)
            {
                control.IsVisible = false;
                return;
            }
        }
        var from = control.Opacity;
        if (Math.Abs(from - targetOpacity) < 0.01)
        {
            return;
        }

        var anim = AnimationsHelper.OpacityAnimation(from, targetOpacity, durationSeconds);
        try
        {
            await anim.RunAsync(control, token);
            // After fade out, ensure fully hidden (in case animation didn't complete)
            if (Math.Abs(targetOpacity) < 0.01)
            {
                control.Opacity = 0;
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void SetOpacity(double opacity)
    {
        _mainButton.Opacity = opacity;
        _childButton?.Opacity = opacity;
    }
}