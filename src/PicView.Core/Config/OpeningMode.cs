namespace PicView.Core.Config;

/// <summary>
/// Where a file that is opened from outside the current view goes. Stored as a number.
/// </summary>
public enum OpeningMode
{
    NewWindow = 0,
    SameWindow = 1,
    NewTab = 2
}
