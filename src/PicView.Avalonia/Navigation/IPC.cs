#if DEBUG
using System.Diagnostics;
#endif
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Core.DebugTools;
using PicView.Core.ProcessHandling;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// Provides Inter-Process Communication (IPC) functionality using named pipes.
/// Allows multiple instances of the application to communicate, 
/// enabling the transfer of commands or data between them.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class IPC
{
    /// <summary>
    /// The default name for the named pipe used by the application.
    /// This pipe is used to facilitate communication between instances of the application.
    /// </summary>
    private const string PipeName = "PicViewPipe";

    private static bool? _isRunning;
    
    public static void SendWithArgs(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        if (Settings.UIProperties.OpenInSameWindow && ProcessHelper.CheckIfAnotherInstanceIsRunning())
        {
            RetrySendingArgs(args);
        }
    }
    

    private static void RetrySendingArgs(string[] args)
    {
        if (args.Length > 1)
        {
            Task.Run(async () =>
            {
                var retries = 0;
                while (!await SendArgumentToRunningInstance(args[1]))
                {
                    await Task.Delay(1000);
                    if (++retries > 20)
                    {
                        break;
                    }
                }

                Environment.Exit(0);
            });
        }
    }

    /// <summary>
    /// Sends an argument to a running instance of the application via the specified named pipe.
    /// If a running instance is detected, the argument (e.g., a file path) is passed to it for processing.
    /// </summary>
    /// <param name="arg">The argument to send to the running instance, such as a file path.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the argument is sent successfully.</returns>
    /// <remarks>
    /// This method tries to connect to an existing instance of the application using a named pipe. If successful, 
    /// it sends the argument. In case of a timeout or other exceptions, these errors are caught and logged in debug mode.
    /// This can be used to pass new command-line arguments to a running instance instead of starting a new instance.
    /// </remarks>
    public static async Task<bool> SendArgumentToRunningInstance(string arg)
    {
        await using var pipeClient = new NamedPipeClientStream(PipeName);
        try
        {
            // Try to connect to the running instance
            await pipeClient.ConnectAsync(2750).ConfigureAwait(false);

            // Send the argument
            await using var writer = new StreamWriter(pipeClient);
            await writer.WriteLineAsync(arg).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Log the timeout if in debug mode
#if DEBUG
            Trace.WriteLine($"{nameof(SendArgumentToRunningInstance)} timeout");
#endif
            return false;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(IPC), nameof(SendArgumentToRunningInstance), ex);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Starts a named pipe server to listen for incoming arguments from other instances of the application.
    /// Processes incoming arguments (e.g., file paths) by instructing the main view model to open the specified picture.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The method runs indefinitely to handle multiple connections.</returns>
    /// <remarks>
    /// This method runs continuously, waiting for incoming connections on the specified named pipe. When a connection is made, 
    /// it reads the incoming arguments and processes them. The arguments can include file paths or commands, 
    /// and they are passed to the main view model to update the UI accordingly.
    /// </remarks>
    public static async Task StartListeningForArguments()
    {
        if (_isRunning.HasValue && !_isRunning.Value)
        {
            _isRunning = true;
            return;
        }
        
        _isRunning = true;
        do
        {
            try
            {
                await using var pipeServer = new NamedPipeServerStream(PipeName);
                
                // Wait for a connection from another instance
                await pipeServer.WaitForConnectionAsync();

                using var reader = new StreamReader(pipeServer);

                // Read and process incoming arguments
                while (await reader.ReadLineAsync() is { } line)
                {
                    if (!_isRunning.Value)
                    {
                        // Setting to open in same window turned off, start new process instead
                        ProcessHelper.StartNewProcess(line);
                        return;
                    }
                    // Log the received argument if in debug mode
#if DEBUG
                    Trace.WriteLine("Received argument: " + line);
#endif

                    var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
                    if (core is null)
                    {
                        return;
                    }
                    // Need to stop taskbar progress if it's running
                    // Otherwise the new taskbar progress will not be updated
                    core.PlatformService.StopTaskbarProgress();
                    var desktop = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
                    var activeWindow = core.MainWindows.ActiveWindow.CurrentValue;
                    var tabs = activeWindow.WindowTabs;
                    var tab = tabs.ActiveTab.CurrentValue;
                    activeWindow.IsLoadingIndicatorShown.Value = true;
                    if (!tab.IsInitialized)
                    {
                        var mainWindow = await Dispatcher.UIThread.InvokeAsync(() => desktop?.MainWindow as MainWindow);
                        if (mainWindow is null)
                        {
                            return;
                        }
                        await QuickLoad.QuickLoadAsync(mainWindow, core, line, continueFromLeftOff: false).ConfigureAwait(false);
                    }
                    else
                    {
                        await tabs.LoadFromStringAsync(line).ConfigureAwait(false);
                    }
                    activeWindow.IsLoadingIndicatorShown.Value = false;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        desktop?.MainWindow.Activate();
                    });
                }
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(IPC), nameof(StartListeningForArguments), ex);
                return;
            }
        }
        while (true); // Continuously listen for incoming connections
    }

    public static void StopListening()
    {
        _isRunning = false;
    }
}
