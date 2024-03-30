namespace PicView.Core.Extensions;

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => string.Empty,
            "" => string.Empty,
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };
}