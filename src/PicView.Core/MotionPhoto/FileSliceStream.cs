namespace PicView.Core.MotionPhoto;

/// <summary>
/// A read-only, seekable window over a region of a file. Used to hand the embedded video
/// portion of a motion photo to consumers (e.g. libvlc) without copying it into memory first.
/// Reads and seeks are clamped to the slice; the underlying file is kept open until disposal.
/// </summary>
public sealed class FileSliceStream : Stream
{
    private readonly FileStream _inner;
    private readonly long _offset;
    private readonly long _length;

    public FileSliceStream(string path, long offset, long length)
    {
        _inner = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        _offset = offset;
        _length = Math.Min(length, Math.Max(0, _inner.Length - offset));
        _inner.Position = offset;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _inner.Position - _offset;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var absolute = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        var clamped = Math.Clamp(absolute, 0, _length);
        _inner.Position = _offset + clamped;
        return clamped;
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, ClampToRemaining(count));

    public override int Read(Span<byte> buffer) =>
        _inner.Read(buffer.Slice(0, ClampToRemaining(buffer.Length)));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(buffer.Slice(0, ClampToRemaining(buffer.Length)), cancellationToken);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _inner.ReadAsync(buffer.AsMemory(offset, ClampToRemaining(count)), cancellationToken).AsTask();

    public override void Flush()
    {
        // Read-only; nothing to flush.
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    private int ClampToRemaining(int count)
    {
        var remaining = _length - Position;
        return (int)Math.Max(0, Math.Min(count, remaining));
    }
}
