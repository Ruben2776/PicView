namespace PicView.Core.Models;

public struct KeyBindingsModel
{
    public KeyBindingsModel(string friendlyMethodName, string methodName, string key, string altKey)
    {
        FriendlyMethodName = friendlyMethodName;
        MethodName = methodName;
        Key = key;
        AltKey = altKey;
    }

    /// <summary>
    /// The translated name of the method.
    /// <remarks>Used for display in the UI.</remarks>
    /// </summary>
    public string? FriendlyMethodName { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the method to invoke when the key combination is pressed.
    /// <remarks>Used as identifier for the dictionary</remarks>
    /// </summary>
    public string? MethodName { get; set; } = string.Empty;
    /// <summary>
    /// The key combination to invoke the method.
    /// <remarks>Used for display in the UI.</remarks>
    /// </summary>
    public string? Key { get; set; } = string.Empty;
    /// <summary>
    /// The key combination to invoke for the alternative method.
    /// <remarks>Used for display in the UI.</remarks>
    /// </summary>
    public string? AltKey { get; set; } = string.Empty;
    
    public bool IsReadOnly { get; init; } = false;
}