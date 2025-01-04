using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.Animations;
using PicView.Avalonia.UI;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;

[TemplatePart("PART_Overlay", typeof(Panel))]
[TemplatePart("PART_Border", typeof(Border))]
public class AnimatedPopUp : ContentControl
{
    public static readonly AvaloniaProperty<bool> ClickingOutSideClosesProperty =
        AvaloniaProperty.Register<CopyButton, bool>(nameof(ClickingOutSideCloses));

    private Border? _partBorder;
    private Panel? _partOverlay;

    protected AnimatedPopUp()
    {
        Loaded += async delegate { await AnimatedOpening(); };
    }

    public bool ClickingOutSideCloses
    {
        get => (bool)GetValue(ClickingOutSideClosesProperty)!;
        set => SetValue(ClickingOutSideClosesProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(AnimatedPopUp);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _partOverlay = e.NameScope.Find<Panel>("PART_Overlay");
        _partBorder = e.NameScope.Find<Border>("PART_Border");

        _partOverlay.Opacity = 0;
        _partBorder.Opacity = 0;

        if (SettingsHelper.Settings.Theme.GlassTheme)
        {
            if (Application.Current.TryGetResource("MenuBackgroundColor",
                    Application.Current.RequestedThemeVariant, out var bgColor))
            {
                if (bgColor is Color color)
                {
                    _partBorder.Background = new SolidColorBrush(color);
                }
            }
        }

        // Handle click outside to close
        _partOverlay.PointerPressed += async delegate
        {
            if (!ClickingOutSideCloses)
            {
                return;
            }

            if (!_partBorder.IsPointerOver)
            {
                await AnimatedClosing();
            }
        };
    }

    public async Task AnimatedOpening()
    {
        UIHelper.IsDialogOpen = true;
        var fadeIn = AnimationsHelper.OpacityAnimation(0, 1, 0.3);
        var centering = AnimationsHelper.CenteringAnimation(50, 100, 0, 0, 0.3);
        await Task.WhenAll(fadeIn.RunAsync(_partOverlay), fadeIn.RunAsync(_partBorder),
            centering.RunAsync(_partBorder));
    }

    public async Task AnimatedClosing()
    {
        UIHelper.IsDialogOpen = false;
        var fadeIn = AnimationsHelper.OpacityAnimation(1, 0, 0.3);
        var centering = AnimationsHelper.CenteringAnimation(0, 0, 50, 100, 0.3);
        await Task.WhenAll(fadeIn.RunAsync(_partOverlay), fadeIn.RunAsync(_partBorder),
            centering.RunAsync(_partBorder));
        UIHelper.GetMainView.MainGrid.Children.Remove(this);
    }

    public void KeyDownHandler(object? sender, KeyEventArgs e)
    {
        RaiseEvent(e);
    }
}