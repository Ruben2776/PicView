using PicView.Core.BatchResize;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// One Batch Thumbnail as the user edits it.
/// </summary>
public sealed class BatchThumbnailRow : IDisposable
{
    public BatchThumbnailRow(int number, BatchThumbnail thumbnail, IReadOnlyList<Choice<ThumbnailSize>> sizeChoices)
    {
        Number = number;
        SizeChoices = sizeChoices;
        Subfolder = new BindableReactiveProperty<string>(thumbnail.Subfolder);
        Size = new BindableReactiveProperty<ThumbnailSize>(thumbnail.Size);
        Value = new BindableReactiveProperty<double>(thumbnail.Value);
    }

    /// <summary>
    /// Its place among the job's Batch Thumbnails, starting at 1.
    /// </summary>
    public int Number { get; }

    public IReadOnlyList<Choice<ThumbnailSize>> SizeChoices { get; }

    public BindableReactiveProperty<string> Subfolder { get; }

    public BindableReactiveProperty<ThumbnailSize> Size { get; }

    public BindableReactiveProperty<double> Value { get; }

    public BatchThumbnail ToThumbnail() => new(Subfolder.Value, Size.Value, Value.Value);

    public void Dispose()
    {
        Subfolder.Dispose();
        Size.Dispose();
        Value.Dispose();
    }
}
