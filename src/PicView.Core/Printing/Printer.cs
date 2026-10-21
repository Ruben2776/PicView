using PicView.Core.Localization;

namespace PicView.Core.Printing;

/// <summary>
/// Where a Print Job is sent: one of the system's printers, or Save as PDF.
/// </summary>
/// <param name="Id">
/// What the remembered Print Settings store. It stays the same when the language changes: the system's name for a
/// printer, or <see cref="SaveAsPdfId"/>.
/// </param>
/// <param name="Name">What the Print window shows.</param>
public sealed record Printer(string Id, string Name)
{
    /// <summary>
    /// The <see cref="Id"/> of Save as PDF.
    /// </summary>
    public const string SaveAsPdfId = "PicView.SaveAsPdf";

    /// <summary>
    /// The Printer that writes the Printed Page to a PDF file instead of paper, named in the current language.
    /// </summary>
    public static Printer SaveAsPdf => new(SaveAsPdfId, TranslationManager.Translation.SaveAsPdf ?? "Save as PDF");

    /// <summary>
    /// One of the system's printers, by the name the system gives it.
    /// </summary>
    public static Printer FromSystem(string name) => new(name, name);

    public bool IsSaveAsPdf => string.Equals(Id, SaveAsPdfId, StringComparison.Ordinal);

    /// <summary>
    /// The name, which the Print window's list of Printers shows.
    /// </summary>
    public override string ToString() => Name;
}
