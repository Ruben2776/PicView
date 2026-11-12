namespace PicView.Core.Config;

/// <summary>
/// Where the configuration files are stored. Stored as a number in the settings file, so the values must never be reused.
/// </summary>
public enum ConfigLocation
{
    /// <summary>Use the folder next to the executable when it is writable, otherwise the roaming folder.</summary>
    Auto = 0,

    /// <summary>Always use the folder next to the executable.</summary>
    Portable = 1,

    /// <summary>Always use the roaming application data folder.</summary>
    Roaming = 2
}
