namespace PicView.Core.Models;

public class BatchLogEntry
{
    public string FileName { get; set; } = string.Empty;
    public string OldSize { get; set; } = string.Empty;
    public string NewSize { get; set; } = string.Empty;

    public override string ToString()
    {
        var fn = FileName.AsSpan();
        var os = OldSize.AsSpan();
        var ns = NewSize.AsSpan();

        var length = fn.Length + 1 + os.Length + 4 + ns.Length;

        return string.Create(length, (FileName, OldSize, NewSize), static (span, state) =>
        {
            var (fileName, oldSize, newSize) = state;

            var fnSpan = fileName.AsSpan();
            fnSpan.CopyTo(span);
            var pos = fnSpan.Length;

            span[pos++] = ' ';

            var osSpan = oldSize.AsSpan();
            osSpan.CopyTo(span[pos..]);
            pos += osSpan.Length;

            " -> ".CopyTo(span[pos..]);
            pos += 4;

            var nsSpan = newSize.AsSpan();
            nsSpan.CopyTo(span[pos..]);
        });
    }
}
