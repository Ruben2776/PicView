namespace PicView.Core.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Shortens the given string `name` to the given `amount` and appends "..." to it.
    /// </summary>
    /// <param name="name">The string to shorten</param>
    /// <param name="amount">The length to shorten the string to</param>
    /// <returns>The shortened string</returns>
    public static string Shorten(this string name, int amount)
    {
        name = name[..amount];
        name += "...";
        return name;
    }
}