using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;
using PicView.Avalonia.Navigation;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;

public class PicBox : Control
{
    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<Image, IImage?>(nameof(Source));
    
    /// <summary>
    /// Gets or sets the image that will be displayed.
    /// </summary>
    [Content]
    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
    
    public static readonly AvaloniaProperty<ImageType> ImageTypeProperty =
        AvaloniaProperty.Register<AnimatedMenu, ImageType>(nameof(ImageType));

    public ImageType ImageType
    {
        get => (ImageType)(GetValue(ImageTypeProperty) ?? false);
        set => SetValue(ImageTypeProperty, value);
    }
    
    
    static PicBox()
    {
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<PicBox>(AutomationControlType.Image);
    }
    
    #region Rendering
    
    /// <summary>
    /// Renders the control.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);
        var source = Source;

        if (source == null || Bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var viewPort = new Rect(Bounds.Size);
        var sourceSize = source.Size;
        
        var scaleX = 1.0;
        var scaleY = 1.0;
        var scale = 1;
        var scaledSize = sourceSize * scale;
        var destRect = viewPort
            .CenterRect(new Rect(scaledSize))
            .Intersect(viewPort);
        var sourceRect = new Rect(sourceSize)
            .CenterRect(new Rect(destRect.Size / scale));

        // var isConstrainedWidth = !double.IsPositiveInfinity(viewPort.Width);
        // var isConstrainedHeight = !double.IsPositiveInfinity(viewPort.Height);
        //
        // Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
        // Size scaledSize = sourceSize * scale;
        // Rect destRect = viewPort
        //     .CenterRect(new Rect(scaledSize))
        //     .Intersect(viewPort);
        // Rect sourceRect = new Rect(sourceSize)
        //     .CenterRect(new Rect(destRect.Size / scale));
        
        // if (SettingsHelper.Settings.ImageScaling.StretchImage)
        // {
        //     UniformToFill(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);
        // }
        // else
        // {
        //     Uniform(panelWidth, panelHeight, elementWidth, elementHeight, skipTransitions);
        // }

        if (SettingsHelper.Settings.ImageScaling.StretchImage)
        {
            context.DrawImage(source, sourceRect, destRect);
        }
        else
        {
            context.DrawImage(source, sourceRect, destRect);
        }
    }
    
    #endregion
    
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new ImageAutomationPeer(this);
    }
}