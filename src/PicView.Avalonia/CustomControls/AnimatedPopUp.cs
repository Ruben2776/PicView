using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.Animations;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.CustomControls;

[TemplatePart("PART_Overlay", typeof(Panel))]
[TemplatePart("PART_Border", typeof(Border))]
public class AnimatedPopUp : ContentControl
{
    public static readonly AvaloniaProperty<bool> ClickingOutsideClosesProperty =
        AvaloniaProperty.Register<AnimatedPopUp, bool>(nameof(ClickingOutsideCloses));

    private Border? _partBorder;
    private Panel? _partOverlay;

    private const double AnimSpeed = 0.3;
    protected AnimatedPopUp()
    {
        Loaded += async delegate { await AnimatedOpening(); };
    }

    public event EventHandler<KeyEventArgs> KeyChanged;

    public bool ClickingOutsideCloses
    {
        get => (bool)GetValue(ClickingOutsideClosesProperty)!;
        set => SetValue(ClickingOutsideClosesProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(AnimatedPopUp);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _partOverlay = e.NameScope.Find<Panel>("PART_Overlay");
        _partBorder = e.NameScope.Find<Border>("PART_Border");

        _partOverlay?.Opacity = 0;
        _partBorder?.Opacity = 0;

        ApplyGlassThemeBackground();

        if (_partOverlay != null)
        {
            _partOverlay.PointerPressed += async (_, _) => await OnOverlayPointerPressed();
        }
    }

    private void ApplyGlassThemeBackground()
    {
        if (!Settings.Theme.GlassTheme || _partBorder == null)
        {
            return;
        }

        if (Application.Current.TryGetResource("MenuBackgroundColor",
                Application.Current.RequestedThemeVariant, out var bgColor)
            && bgColor is Color color)
        {
            _partBorder.Background = new SolidColorBrush(color);
        }
    }

    private async Task OnOverlayPointerPressed()
    {
        if (!ClickingOutsideCloses)
        {
            return;
        }

        if (_partBorder is { IsPointerOver: false })
        {
            await AnimatedClosing();
        }
    }

    public async Task AnimatedOpening()
    {
        IsHitTestVisible = true;
        IsVisible = true;
        
        const int fromX = 50;
        const int fromY = 100;
        const int toX = 0;
        const int toY = 0;
        DialogManager.IsDialogOpen = true;
        var fadeIn = AnimationsHelper.OpacityAnimation(0, 1, AnimSpeed);
        var centering = AnimationsHelper.CenteringAnimation(fromX, fromY, toX, toY, AnimSpeed);
        await Task.WhenAll(
            fadeIn.RunAsync(_partOverlay),
            fadeIn.RunAsync(_partBorder),
            centering.RunAsync(_partBorder)
        );
    }

    public async Task AnimatedClosing(bool remove = true)
    {
        const int fromX = 0;
        const int fromY = 0;
        const int toX = 50;
        const int toY = 100;
        DialogManager.IsDialogOpen = false;
        var fadeIn = AnimationsHelper.OpacityAnimation(1, 0, AnimSpeed);
        var centering = AnimationsHelper.CenteringAnimation(fromX, fromY, toX, toY, AnimSpeed);
        await Task.WhenAll(
            fadeIn.RunAsync(_partOverlay),
            fadeIn.RunAsync(_partBorder),
            centering.RunAsync(_partBorder)
        );
        if (remove)
        {
            UIHelper.GetMainView.MainGrid.Children.Remove(this);
        }
        else
        {
            IsHitTestVisible = false;
            IsVisible = false;
        }
    }

    // ReSharper disable once UnusedMember.Global
    public void KeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape)
        {
            _ = AnimatedClosing();
        }
        else
        {
            KeyChanged(this, e);
        }
    }
}