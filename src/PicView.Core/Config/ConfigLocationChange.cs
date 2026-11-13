namespace PicView.Core.Config;

/// <summary>
/// The result of <see cref="SettingsManager.SetConfigLocationAsync" />.
/// </summary>
public enum ConfigLocationChange
{
    /// <summary>The mode was already in use, or the platform does not allow a choice. Nothing changed.</summary>
    Unchanged,

    /// <summary>The mode was saved and every config file was moved.</summary>
    Changed,

    /// <summary>The folder next to the executable is not writable. Nothing changed.</summary>
    PortableNotWritable,

    /// <summary>The settings file could not be written. The switch was rolled back.</summary>
    SaveFailed,

    /// <summary>The mode was saved and the new folder is in use, but some files could not be moved.</summary>
    MoveIncomplete
}
