namespace PicView.Core.ViewModels;

/// <summary>
/// What changed in what a <see cref="TabViewModel"/> shows, published through <see cref="TabViewModel.Shown"/>.
/// </summary>
public enum ShownChange
{
    /// <summary>
    /// A new image, or a new page of the shown multi-page TIFF, is shown.
    /// </summary>
    Image,

    /// <summary>
    /// The shown image is the same object; its file or its pixel size changed on disk.
    /// </summary>
    File
}
