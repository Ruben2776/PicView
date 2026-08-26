using System.Text.RegularExpressions;

namespace PicView.Core.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="string"/> class.
/// </summary>
public static partial class StringExtensions
{
    public const string AppName = "PicView";

    /// <summary>
    /// Combines two strings with a " || " separator efficiently, with exactly one string allocation.
    /// </summary>
    /// <param name="a">The first string to combine.</param>
    /// <param name="b">The second string to combine.</param>
    /// <returns>The combined string.</returns>
    public static string CombineWithSeparator(string a, string b)
    {
        // Safety check to ensure we don't crash on null strings
        a ??= string.Empty;
        b ??= string.Empty;

        // Step 1: Calculate the total length needed 
        var requiredLength = a.Length + 4 + b.Length;

        // Step 2: Call string.Create. 
        // We pass the required length, and a "state" object (a tuple containing our two strings).
        // Passing the state prevents the lambda from creating an invisible class to capture the variables (closure allocation).
        return string.Create(requiredLength, (a, b), (destination, state) =>
        {
            // Convert our state strings into spans for fast copying
            var spanA = state.a.AsSpan();
            var spanB = state.b.AsSpan();

            // Step 3: Copy string 'a' to the beginning of the destination
            spanA.CopyTo(destination);

            // Step 4: Add the " || " separator
            destination[spanA.Length] = ' ';
            destination[spanA.Length + 1] = '|';
            destination[spanA.Length + 2] = '|';
            destination[spanA.Length + 3] = ' ';

            // Step 5: Copy string 'b' right after the separator
            spanB.CopyTo(destination.Slice(spanA.Length + 4));
        });
    }

    /// <summary>
    /// Combines the specified string with the application name ("PicView") using a " - " separator.
    /// </summary>
    /// <param name="value">The string to combine with the application name.</param>
    /// <returns>A new string containing the original string followed by " - PicView".</returns>
    public static string CombineWithAppName(string value)
    {
        const string separator = " - ";
        var requiredLength = value.Length + separator.Length + AppName.Length;

        return string.Create(requiredLength, (value, AppName), (destination, state) =>
        {
            var offset = 0;

            state.value.AsSpan().CopyTo(destination[offset..]);
            offset += state.value.Length;

            separator.AsSpan().CopyTo(destination[offset..]);
            offset += separator.Length;

            state.AppName.AsSpan().CopyTo(destination[offset..]);
        });
    }
    
    public static string CombineWithPercentage(double zoomValue)
    {
        Span<char> buffer = stackalloc char[32];
        if (!zoomValue.TryFormat(buffer, out var zoomLength))
        {
            return string.Empty;
        }

        return string.Create(zoomLength + 1, (zoomValue, zoomLength), static (destination, state) =>
        {
            state.zoomValue.TryFormat(destination[..state.zoomLength], out _);
            destination[state.zoomLength] = '%';
        });
    }
    
    
    public static string CombineSize(uint width, uint height)
    {
        // Allocate small buffers on the stack to determine character lengths
        Span<char> widthBuffer = stackalloc char[32];
        Span<char> heightBuffer = stackalloc char[32];

        // Format into the buffers to get the exact lengths
        if (!width.TryFormat(widthBuffer, out var widthLength) ||
            !height.TryFormat(heightBuffer, out var heightLength))
        {
            return string.Empty;
        }

        // Calculate total length: width length + 3 (for " x ") + height length
        var requiredLength = widthLength + 3 + heightLength;

        // Create the string with exactly one heap allocation
        return string.Create(requiredLength, (width, height, widthLength), static (destination, state) =>
        {
            // Format width into the beginning of the destination
            state.width.TryFormat(destination[..state.widthLength], out _);

            // Add the " x " separator manually
            var offset = state.widthLength;
            destination[offset++] = ' ';
            destination[offset++] = 'x';
            destination[offset++] = ' ';

            // Format height into the remaining space
            state.height.TryFormat(destination[offset..], out _);
        });
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

            if (double.TryParse(match.Groups[1].Value, System.Globalization.CultureInfo.CurrentCulture, out var percentage))
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