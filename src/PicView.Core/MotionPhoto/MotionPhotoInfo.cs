namespace PicView.Core.MotionPhoto;

/// <summary>
/// Identifies how the motion photo video is stored.
/// </summary>
public enum MotionPhotoSource
{
    /// <summary>Video is embedded at the end of the image file, located via XMP metadata.</summary>
    EmbeddedXmp,

    /// <summary>Video is embedded after a "MotionPhoto_Data" trailer marker (legacy Samsung format).</summary>
    SamsungTrailer,

    /// <summary>Video is stored in a same-named sidecar file (.mov/.mp4) next to the image.</summary>
    Sidecar,

    /// <summary>Image and video are stored inside a .livp zip container (Apple Live Photo export).</summary>
    LivpContainer,
}

/// <summary>
/// Describes where the embedded or associated video of a motion photo can be found.
/// The still image itself is decoded through the regular image pipeline; this record
/// only holds the coordinates needed to extract the video on demand.
/// </summary>
public sealed record MotionPhotoInfo
{
    public required MotionPhotoSource Source { get; init; }

    /// <summary>
    /// Byte offset of the video inside the source file. Only meaningful for
    /// <see cref="MotionPhotoSource.EmbeddedXmp"/> and <see cref="MotionPhotoSource.SamsungTrailer"/>.
    /// </summary>
    public long VideoOffset { get; init; }

    /// <summary>
    /// Length of the video in bytes as reported by the metadata, when known.
    /// The extractor reads from <see cref="VideoOffset"/> to the end of file regardless,
    /// as trailing junk is tolerated by the demuxer.
    /// </summary>
    public long VideoLength { get; init; }

    /// <summary>
    /// The sidecar video file. Only meaningful for <see cref="MotionPhotoSource.Sidecar"/>.
    /// </summary>
    public FileInfo? SidecarFile { get; init; }
}
