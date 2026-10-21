namespace PicView.Avalonia.Printing;

/// <summary>
/// What became of sending a Print Job to a Printer.
/// </summary>
public enum PrintOutcome
{
    /// <summary>
    /// The Printer took the Printed Page: the system took the job, or Save as PDF wrote the file.
    /// </summary>
    Sent,

    /// <summary>
    /// The user dismissed Save as PDF's file picker.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Nothing was printed or written.
    /// </summary>
    Failed
}
