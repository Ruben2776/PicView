using System.Text.RegularExpressions;

namespace PicView.Core.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="string"/> class.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Converts the first character of the string to uppercase.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>A string with the first character capitalized. If the string is null or empty, an empty string is returned.</returns>
    public static string FirstCharToUpper(this string input)
    {
        return input switch
        {
            null => string.Empty,
            "" => string.Empty,
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };
    }

    /// <summary>
    /// Shortens the given string <paramref name="name"/> to the specified <paramref name="amount"/> and appends "..." to it.
    /// </summary>
    /// <param name="name">The string to shorten.</param>
    /// <param name="amount">The length to shorten the string to.</param>
    /// <returns>The shortened string with "..." appended at the end.</returns>
    public static string Shorten(this string name, int amount)
    {
        name = name[..amount];
        name += "...";
        return name;
    }

    /// <summary>
    /// Extracts the percentage value from the string, if present.
    /// </summary>
    /// <param name="text">The string containing a percentage value.</param>
    /// <returns>The percentage value found in the string, or 0 if no valid percentage is found.</returns>
    public static double GetPercentage(this string text)
    {
        foreach (Match match in PercentageRegex().Matches(text)) // Find % sign
        {
            if (!match.Success)
            {
                continue;
            }

            if (double.TryParse(match.Groups[1].Value, out var percentage))
            {
                return percentage;
            }
        }

        return 0;
    }

    /// <summary>
    /// A regex pattern used to match percentage values (e.g., "50%").
    /// </summary>
    /// <returns>A regex pattern that matches numbers followed by a percentage sign.</returns>
    [GeneratedRegex("(\\d+)%")]
    private static partial Regex PercentageRegex();
}