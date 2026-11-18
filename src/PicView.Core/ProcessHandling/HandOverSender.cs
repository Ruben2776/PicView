using System.IO.Pipes;
using System.Text;
using Microsoft.Win32.SafeHandles;
using PicView.Core.DebugTools;

namespace PicView.Core.ProcessHandling;

/// <summary>
/// The starting PicView's side of a hand-over: it sends its Source to the running PicView, waits for it to answer that
/// it is ready, and confirms. The running PicView opens the Source only once it is confirmed.
/// </summary>
public static class HandOverSender
{
    /// <summary>
    /// Hands <paramref name="source"/> over to the PicView listening on <paramref name="pipeName"/>.
    /// </summary>
    /// <param name="source">The Source this PicView was started with, or null when it was started with none.</param>
    /// <param name="connectTimeout">How long to wait for a PicView to listen. Zero tries once and fails at once when
    /// none does, which is what a start with no other PicView running costs.</param>
    /// <param name="acknowledgeTimeout">How long to wait for the listening PicView to answer.</param>
    /// <param name="connected">Called with the pipe once connected, before anything is sent.</param>
    /// <returns>True only when the running PicView answered that it is ready and was told to go ahead. False for
    /// anything else: no PicView listening, no answer in time, a refusal, a broken pipe, or a Source the one-line
    /// message cannot carry. The running PicView then opens nothing, and the caller starts normally.</returns>
    public static async Task<bool> TrySendAsync(string pipeName, string? source, TimeSpan connectTimeout,
        TimeSpan acknowledgeTimeout, Action<SafePipeHandle>? connected = null)
    {
        if (!HandOverMessage.CanCarry(source))
        {
            return false;
        }

        try
        {
            var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await using (pipe.ConfigureAwait(false))
            {
                // Synchronous, so no PicView listening costs one failed attempt and no thread
                pipe.Connect((int)connectTimeout.TotalMilliseconds);
                connected?.Invoke(pipe.SafePipeHandle);

                using var timeout = new CancellationTokenSource(acknowledgeTimeout);
                var line = Encoding.UTF8.GetBytes(HandOverMessage.Write(source) + "\n");
                await pipe.WriteAsync(line, timeout.Token).ConfigureAwait(false);
                await pipe.FlushAsync(timeout.Token).ConfigureAwait(false);

                using var reader = new StreamReader(pipe, Encoding.UTF8, false, 256, leaveOpen: true);
                var answer = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
                if (!string.Equals(answer, HandOverMessage.Ready, StringComparison.Ordinal))
                {
                    return false;
                }

                // Not under the timeout: once this is written the running PicView opens the Source, so a write that
                // went through must never be reported as one that did not
                var confirm = Encoding.UTF8.GetBytes(HandOverMessage.Confirmed + "\n");
                await pipe.WriteAsync(confirm, CancellationToken.None).ConfigureAwait(false);
                await pipe.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                return true;
            }
        }
        catch (TimeoutException)
        {
            // No PicView is listening, which is how every first start goes
            return false;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(HandOverSender), nameof(TrySendAsync), e);
            return false;
        }
    }
}
