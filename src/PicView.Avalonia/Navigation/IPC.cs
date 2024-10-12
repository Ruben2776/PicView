using System.Diagnostics;
using System.IO.Pipes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// Provides Inter-Process Communication (IPC) functionality using named pipes.
/// </summary>
internal static class IPC
{
    /// <summary>
    /// The default name for the named pipe used by the application.
    /// </summary>
    internal const string PipeName = "PicViewPipe";

    /// <summary>
    /// Sends an argument to a running instance of the application through the specified named pipe.
    /// </summary>
    /// <param name="arg">The argument to send to the running instance.</param>
    /// <param name="pipeName">The name of the pipe to connect to.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the operation completes successfully.</returns>
    /// <remarks>
    /// This method attempts to connect to a running instance of the application via the provided pipe and sends an argument. 
    /// If the connection fails due to a timeout or other exceptions, they are caught and logged in debug mode.
    /// </remarks>
    internal static async Task<bool> SendArgumentToRunningInstance(string arg, string pipeName)
    {
        await using var pipeClient = new NamedPipeClientStream(pipeName);
        try
        {
            await pipeClient.ConnectAsync(2750).ConfigureAwait(false);

            await using var writer = new StreamWriter(pipeClient);
            await writer.WriteLineAsync(arg).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(SendArgumentToRunningInstance)} timeout");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(SendArgumentToRunningInstance)} exception: \n{ex}");
#endif
        }
        return true;
    }

    /// <summary>
    /// Starts listening for incoming arguments from other instances of the application through the specified named pipe.
    /// </summary>
    /// <param name="pipeName">The name of the pipe to listen on.</param>
    /// <param name="vm">The main view model to handle the received arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method continuously listens for incoming connections on the specified named pipe. 
    /// Upon receiving a connection, it reads arguments and processes them by loading the specified picture in the application's view model.
    /// </remarks>
    internal static async Task StartListeningForArguments(string pipeName, MainViewModel vm)
    {
        while (true) // Continue listening for new connections
        {
            await using var pipeServer = new NamedPipeServerStream(pipeName);

            try
            {
                await pipeServer.WaitForConnectionAsync();

                using var reader = new StreamReader(pipeServer);

                // Read and process incoming arguments
                while (await reader.ReadLineAsync() is { } line)
                {
                    // Process the incoming argument as needed
#if DEBUG
                    Trace.WriteLine("Received argument: " + line);
#endif
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            return;
                        }
                        desktop.MainWindow?.Activate();
                    });
                    await NavigationHelper.LoadPicFromStringAsync(line, vm).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(StartListeningForArguments)} exception: \n{ex}");
#endif
            }
        }
    }
}
