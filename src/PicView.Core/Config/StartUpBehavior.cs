namespace PicView.Core.Config;

/// <summary>
/// What PicView does at startup when it was not launched with a file. Stored as a number.
/// </summary>
public enum StartUpBehavior
{
    None = 0,
    OpenLastFile = 1,
    ContinueSession = 2
}
