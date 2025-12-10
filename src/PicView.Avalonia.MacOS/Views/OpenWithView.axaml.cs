using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.MacOS.AppLauncher;
using PicView.Core.MacOS.FileAssociation;

namespace PicView.Avalonia.MacOS.Views;

public partial class OpenWithView : Window
{
    private bool _isLaunchingApp;
    
    private string? _filePath;

    public OpenWithView()
    {
        Start();
    }
    
    public OpenWithView(string path)
    {
        _filePath = path;
        Start();
    }
    
    public void Start()
    {
        InitializeComponent();
        if (!Settings.Theme.Dark && !Settings.Theme.GlassTheme)
        {
            MainPanel.Background = UIHelper.GetMenuBackgroundColor();
        }
        Loaded += OnLoaded;
        
        // Close when window loses focus (more standard behavior)
        Deactivated += OnDeactivated;
        
        // Handle key presses (optional: close on Escape)
        KeyDown += OnKeyDown;
        
        // Ensure the window can receive focus
        Focusable = true;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_filePath is null)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            _filePath = vm.PicViewer?.FileInfo?.CurrentValue.FullName;
        }


        // Focus the window
        Focus();
        
        CancelButton.Click += (_, _) => (VisualRoot as Window)?.Close();

        Task.Run(async () =>
        {
            var apps = await GetAssociatedFiles.GetAssociatedFilesAsync(_filePath);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var app in apps)
                {
                    var btn = new Button
                    {
                        Classes = { "altHover" },
                        Width = 300,
                        Padding = new Thickness(0, 5, 0, 5),
                        Content =
                            new TextBlock
                            {
                                Text = app.Name,
                                VerticalAlignment = VerticalAlignment.Center,
                                Classes = { "txt" },
                                MaxWidth = 250,
                            }
                    };

                    // Add click handler to launch the app with the file and close window
                    btn.Click += async (_, _) =>
                    {
                        _isLaunchingApp = true; // Prevent deactivated event from closing
                        await AppLauncher.LaunchAppWithFileAsync(app.Path, _filePath);
                        Close(); // Close the window after launching the app
                    };

                    ParentPanel.Children.Add(btn);
                }
                SpinWaiter.IsVisible = false;
            });
        });
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Only close if we're not in the process of launching an app
        if (!_isLaunchingApp)
        {
            Close();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Close window on Escape key
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    // Override to ensure proper cleanup
    protected override void OnClosed(EventArgs e)
    {
        Deactivated -= OnDeactivated;
        KeyDown -= OnKeyDown;
        base.OnClosed(e);
    }
}