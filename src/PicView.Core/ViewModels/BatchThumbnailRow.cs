using PicView.Core.BatchResize;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// One Batch Thumbnail as the user edits it.
/// </summary>
public sealed class BatchThumbnailRow(
    int number,
    BatchThumbnail thumbnail,
    IReadOnlyList<Choice<ThumbnailSize>> sizeChoices)
    : IDisposable
{
    /// <summary>
    /// Its place among the job's Batch Thumbnails, starting at 1.
    /// </summary>
    public int Number { get; } = number;

    public IReadOnlyList<Choice<ThumbnailSize>> SizeChoices { get; } = sizeChoices;

    public BindableReactiveProperty<string> Subfolder { get; } = new(thumbnail.Subfolder, StringComparer.Ordinal);

    public BindableReactiveProperty<ThumbnailSize> Size { get; } = new(thumbnail.Size);

    public BindableReactiveProperty<double> Value { get; } = new(thumbnail.Value);

    public BatchThumbnail ToThumbnail() => new(Subfolder.Value, Size.Value, Value.Value);

    public void Dispose()
    {
        Subfolder.Dispose();
        Size.Dispose();
        Value.Dispose();
    }
}
