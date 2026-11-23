using ImageMagick;
using PicView.Core.Exif;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// One value an enumerated Exif tag can hold, with the word Image Information shows for it. The blank choice, with no
/// code, is the tag left out of the file.
/// </summary>
public sealed record ExifChoice(ushort? Code, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// An enumerated Exif tag, such as Contrast, as Image Information's dropdown edits it: the user picks one of its words
/// instead of typing a code, or the blank first choice, which removes the tag. Codes that share a word, such as the
/// flash codes that all mean "Flash did not fire", are one choice, which writes the first of them. A read-only field,
/// such as Compression, only shows the file's choice: it describes the image and writing it would change nothing.
/// </summary>
public sealed class ExifChoiceField : IDisposable
{
    private readonly ExifTag<ushort> _tag;
    private readonly Func<ushort, string> _label;

    // The choice the file holds, so a dropdown closed on it writes nothing
    private int _fileIndex = -1;

    /// <param name="tag">The tag the field reads and writes.</param>
    /// <param name="codes">The codes to offer, in order. A code whose word an earlier code has is not offered.</param>
    /// <param name="label">The word of a code, as <see cref="ExifReader"/> shows it.</param>
    /// <param name="isReadOnly">Whether the field only shows the file's choice and never writes.</param>
    public ExifChoiceField(ExifTag<ushort> tag, ReadOnlySpan<ushort> codes, Func<ushort, string> label,
        bool isReadOnly = false)
    {
        _tag = tag;
        _label = label;
        IsReadOnly = isReadOnly;

        var choices = new List<ExifChoice>(codes.Length + 1) { new(null, string.Empty) };
        foreach (var code in codes)
        {
            var word = label(code);
            if (word.Length > 0 && !choices.Exists(c => string.Equals(c.Label, word, StringComparison.Ordinal)))
            {
                choices.Add(new ExifChoice(code, word));
            }
        }

        Choices = [.. choices];
    }

    public ExifChoice[] Choices { get; }

    /// <summary>Whether the field only shows the file's choice, so the dropdown goes back to it when another is picked.</summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// The choice the dropdown shows: the file's, until the user picks another. 0, the blank choice, when the file
    /// has no such tag, and -1 when it holds a code that has no word.
    /// </summary>
    public BindableReactiveProperty<int> SelectedIndex { get; } = new(-1);

    /// <summary>The word of the file's code, to copy. Empty when the file has none or a code with no word.</summary>
    public BindableReactiveProperty<string> Text { get; } = new(string.Empty, StringComparer.Ordinal);

    /// <summary>Shows what <paramref name="profile"/> holds for the tag.</summary>
    internal void Read(IExifProfile? profile) => Read(profile?.GetValue(_tag)?.Value);

    /// <summary>Shows <paramref name="fileCode"/>, the code the file holds for the tag, or null when it has none.</summary>
    internal void Read(ushort? fileCode)
    {
        if (fileCode is not { } code)
        {
            _fileIndex = 0;
            Text.Value = string.Empty;
        }
        else
        {
            var word = _label(code);
            // A code with no word is none of the choices, so picking the blank one still removes it
            _fileIndex = word.Length is 0
                ? -1
                : Array.FindIndex(Choices, c => string.Equals(c.Label, word, StringComparison.Ordinal));
            Text.Value = word;
        }

        SelectedIndex.Value = _fileIndex;
    }

    /// <summary>Shows the file's choice again, as a read-only dropdown does when another is picked.</summary>
    public void ShowFileChoice() => SelectedIndex.Value = _fileIndex;

    /// <summary>Shows the blank choice, as for a file whose metadata was removed.</summary>
    internal void Clear()
    {
        _fileIndex = 0;
        Text.Value = string.Empty;
        SelectedIndex.Value = 0;
    }

    /// <summary>
    /// Writes the chosen code to <paramref name="file"/>, or removes the tag for the blank choice, when the user picked
    /// another choice than the file's. When
    /// writing fails, the dropdown shows the file's choice again, so it never shows what was not written.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    internal async Task<bool> CommitAsync(FileInfo? file)
    {
        if (IsReadOnly)
        {
            ShowFileChoice();
            return false;
        }

        var index = SelectedIndex.Value;
        if (index == _fileIndex || index < 0 || index >= Choices.Length)
        {
            return false;
        }

        var choice = Choices[index];
        if (!await ExifWriter.SetChoiceAsync(file, _tag, choice.Code).ConfigureAwait(false))
        {
            SelectedIndex.Value = _fileIndex;
            return false;
        }

        _fileIndex = index;
        Text.Value = choice.Label;
        return true;
    }

    public void Dispose()
    {
        SelectedIndex.Dispose();
        Text.Dispose();
    }
}
