using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using PicView.WPF.ChangeImage;

namespace PicView.WPF.ProcessHandling
{
    internal static class IPCHelper
    {
        public static async Task SendArgumentToRunningInstance(string arg, string pipeName)
        {
            await using var pipeClient = new NamedPipeClientStream(pipeName);
            try
            {
                await pipeClient.ConnectAsync(5000).ConfigureAwait(false);

                await using var writer = new StreamWriter(pipeClient);
                await writer.WriteLineAsync(arg).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Handle connection or write error
            }
        }

        public static async Task StartListeningForArguments(string pipeName)
        {
            while (true) // Continue listening for new connections
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
                    }
                }
                catch (Exception)
                {
                    // Handle connection or read error
                }
            }
        }
    }
}