using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace PicView.WPF.ProcessHandling
{
    internal static class IPCHelper
    {
        public static async Task<bool> SendArgumentToRunningInstance(string arg, string pipeName)
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
            }
            return true;
        }

        public static bool IsListening { get; private set; }

        public static async Task StartListeningForArguments(string pipeName)
        {
            IsListening = true;
            while (IsListening) // Continue listening for new connections
            {
                await using var pipeServer = new NamedPipeServerStream(pipeName);

                try
                {
                    await pipeServer.WaitForConnectionAsync().ConfigureAwait(false);

                    using var reader = new StreamReader(pipeServer);

                    // Read and process incoming arguments
                    while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
                    {
                        // Process the incoming argument as needed
#if DEBUG
                        Trace.WriteLine("Received argument: " + line);
#endif
                        await LoadPic.LoadPicFromStringAsync(line).ConfigureAwait(false);
                        await ConfigureWindows.GetMainWindow?.Dispatcher?.InvokeAsync(() =>
                        {
                            ConfigureWindows.GetMainWindow.Focus();
                        });
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

        public static void StopListening()
        {
            IsListening = false;
        }
    }
}