using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;

[TemplatePart("PART_ScrollViewer", typeof(AutoScrollViewer))]
public class GalleryListBox : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    private AutoScrollViewer? _autoScrollViewer;
    
    public GalleryListBox()
    {
        SelectionMode = SelectionMode.Single;
        AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, PreviewKeyDownEvent, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, PreviewKeyUpEvent, RoutingStrategies.Tunnel);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _autoScrollViewer = e.NameScope.Find<AutoScrollViewer>("PART_ScrollViewer");
    }

    #region Functions

    public IEnumerable<Control?> GetVisibleItems()
    {
        return Items.Cast<Control?>().Where(IsControlVisible);
    }
    
    private bool IsControlVisible(Control? child)
    {
        if (child is null)
        {
            return false;
        }
        var parentBounds = new Rect(Bounds.Size);
        var childBounds = child.Bounds.TransformToAABB(child.TransformToVisual(this)!.Value);

        return parentBounds.Intersects(childBounds);
    }

    public void ScrollToCenterOfItem(GalleryItem galleryItem)
    {
        var visibleItems = GetVisibleItems();
        
        var array = visibleItems as GalleryItem[] ?? visibleItems.ToArray();
        var visibleItemsCount = array.Length;
        if (visibleItemsCount == 0)
        {
            return;
        }
        
        var averageItemWidth = array.Sum(item => item.Bounds.Width);
        averageItemWidth /= visibleItemsCount;
        
        var selectedScrollTo = galleryItem.TranslatePoint(new Point(), ItemsPanelRoot);
        
        if (!selectedScrollTo.HasValue)
        {
            return;
        }
        
        // ReSharper disable once PossibleLossOfFraction
        var x = selectedScrollTo.Value.X - (visibleItemsCount + 1) / 2 * averageItemWidth + averageItemWidth / 2;
        
        _autoScrollViewer.Offset = new Vector(x, _autoScrollViewer.Offset.Y);
    }
    
    #endregion

    private void PreviewPointerPressedEvent(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // Disable right click selection
        e.Handled = true;
    }
    
    private async Task PreviewKeyDownEvent(object? sender, KeyEventArgs e)
    {
        // Prevent control from hijacking keys
        await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false); 
        e.Handled = true;
    }
    
    private void PreviewKeyUpEvent(object? sender, KeyEventArgs e)
    {
        // Prevent control from hijacking keys
        MainKeyboardShortcuts.MainWindow_KeysUp(e); 
        e.Handled = true;
    }
    
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        e.Handled = true;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS already has horizontal scrolling for touchpad
            return;
        }

        const int speed = 34;

        if (e.Delta.Y > 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                _autoScrollViewer.Offset -= new Vector(speed, 0);
            }
            else
            {
                _autoScrollViewer.Offset -= new Vector(-speed, 0);
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                _autoScrollViewer.Offset -= new Vector(-speed, 0);
            }
            else
            {
                _autoScrollViewer.Offset -= new Vector(speed, 0);
            }
        }
    }
}
