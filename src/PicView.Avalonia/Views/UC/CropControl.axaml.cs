using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Crop;

namespace PicView.Avalonia.Views.UC;

public partial class CropControl : UserControl
{
    private readonly CropKeyboardManager? _keyboardManager;
    private readonly CropDragHandler? _dragHandler;
    private readonly CropLayoutManager? _layoutManager;
    private readonly CropResizeHandler? _resizeHandler;

    public CropControl()
    {
        InitializeComponent();
        _keyboardManager = new CropKeyboardManager(this);
        _dragHandler = new CropDragHandler(this);
        _resizeHandler = new CropResizeHandler(this);
        _layoutManager = new CropLayoutManager(this);

        Loaded += OnControlLoaded;
    }

    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        InitLoaded();
    }

    private void InitLoaded()
    {
        InitializeResizeHandlers();
        _layoutManager.InitializeLayout();
            
        MainRectangle.PointerPressed += _dragHandler.OnDragStart;
        MainRectangle.PointerReleased += _dragHandler.OnDragEnd;
        MainRectangle.PointerMoved += _dragHandler.OnDragMove;
        MainRectangle.PointerMoved += (_, _) => _layoutManager.UpdateLayout();

        TopLeftButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        TopRightButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        BottomLeftButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        BottomRightButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        LeftMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        RightMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        TopMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        BottomMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);

        TopLeftButton.PointerReleased += _resizeHandler.OnResizeEnd;
        TopRightButton.PointerReleased += _resizeHandler.OnResizeEnd;
        BottomLeftButton.PointerReleased += _resizeHandler.OnResizeEnd;
        BottomRightButton.PointerReleased += _resizeHandler.OnResizeEnd;
        LeftMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;
        RightMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;
        TopMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;
        BottomMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;
            
        LostFocus += OnControlLostFocus;
    }

    private void OnControlLostFocus(object? sender, RoutedEventArgs e)
    {
        _dragHandler.Reset();
        _resizeHandler.Reset();
    }

    private void InitializeResizeHandlers()
    {
        var resizeControls = new Dictionary<Border, CropResizeMode>
        {
            { TopLeftButton, CropResizeMode.TopLeft },
            { TopRightButton, CropResizeMode.TopRight },
            { BottomLeftButton, CropResizeMode.BottomLeft },
            { BottomRightButton, CropResizeMode.BottomRight },
            { LeftMiddleButton, CropResizeMode.Left },
            { RightMiddleButton, CropResizeMode.Right },
            { TopMiddleButton, CropResizeMode.Top },
            { BottomMiddleButton, CropResizeMode.Bottom }
        };

        foreach (var control in resizeControls)
        {
            control.Key.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
            control.Key.PointerMoved += (s, e) => _resizeHandler.OnResizeMove(s, e, control.Value);
            control.Key.PointerMoved += (s, e) => _layoutManager.UpdateLayout();
            control.Key.PointerReleased += _resizeHandler.OnResizeEnd;
        }
    }

    public async Task KeyDownHandler(object? sender, KeyEventArgs e)
    {
        await _keyboardManager.KeyDownHandler(e).ConfigureAwait(false);
    }
}