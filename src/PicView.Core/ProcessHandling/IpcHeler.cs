using System.Diagnostics;
using System.IO.Pipes;
using PicView.Core.Navigation;

namespace PicView.Core.ProcessHandling;

/// <summary>
/// Provides inter-process communication (IPC) helper methods.
/// </summary>
public static class IpcHelper
{
    /// <summary>
    /// Sends an argument to a running instance through a named pipe.
    /// </summary>
    /// <param name="arg">The argument to be sent.</param>
    /// <param name="pipeName">The name of the named pipe.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains <c>true</c> if the argument is successfully sent; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method attempts to connect to a named pipe with the specified name and send the argument.
    /// If the connection times out or an exception occurs, it returns <c>false</c>.
    /// </remarks>
    internal static async Task<bool> SendArgumentToRunningInstance(string arg, string pipeName)
    {
        await using var pipeClient = new NamedPipeClientStream(pipeName);
        try
        {
            await pipeClient.ConnectAsync(750).ConfigureAwait(false);

            await using var writer = new StreamWriter(pipeClient);
            await writer.WriteLineAsync(arg).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(SendArgumentToRunningInstance)} exception: \n{ex}");
#endif
            return false;
        }
        return true;
    }

    /// <summary>
    /// Starts listening for incoming arguments through a named pipe.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe.</param>
    /// <param name="imageLoading">An instance of <see cref="IImageLoading"/> to process the incoming arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method runs an infinite loop to continuously listen for new connections on the specified named pipe.
    /// When a connection is established, it reads arguments from the pipe and processes them using the provided <see cref="IImageLoading"/> instance.
    /// </remarks>
    public static async Task StartListeningForArguments(string pipeName, IImageLoading imageLoading)
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
                    await imageLoading.LoadPicFromStringAsync(line).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(StartListeningForArguments)} exception: \n{ex}");
#endif
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }
}

