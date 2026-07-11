using System.Globalization;
using Avalonia.Controls;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;

namespace PicView.Avalonia.Resizing;

public static class AspectRatioHelper
{
    /// <summary>
    /// Adjusts the dimensions of the TextBoxes while maintaining the specified aspect ratio.
    /// </summary>
    /// <param name="widthTextBox">The TextBox that contains the width value.</param>
    /// <param name="heightTextBox">The TextBox that contains the height value.</param>
    /// <param name="isWidth">Indicates whether the width is being adjusted. If false, height is adjusted.</param>
    /// <param name="aspectRatio">The aspect ratio to maintain between width and height.</param>
    /// <param name="pixelWidth">The current pixel width of the image.</param>
    /// <param name="pixelHeight">The current pixel height of the image.</param>
    public static void SetAspectRatioForTextBox(TextBox widthTextBox, TextBox heightTextBox, bool isWidth,
        double aspectRatio, uint pixelWidth, uint pixelHeight)
    {
        try
        {
            var percentage = isWidth ? widthTextBox.Text.GetPercentage() : heightTextBox.Text.GetPercentage();
            if (percentage > 0)
            {
                // Clamp the calculated value to prevent overflow
                var newWidth = (uint)Math.Clamp(pixelWidth * (percentage / 100),
                    uint.MinValue,
                    uint.MaxValue);
                var newHeight = (uint)Math.Clamp(pixelHeight * (percentage / 100),
                    uint.MinValue,
                    uint.MaxValue);

                widthTextBox.Text = newWidth.ToString("# ", CultureInfo.CurrentCulture);
                heightTextBox.Text = newHeight.ToString("# ", CultureInfo.CurrentCulture);

                if (isWidth)
                {
                    heightTextBox.Text = newHeight.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    widthTextBox.Text = newWidth.ToString(CultureInfo.CurrentCulture);
                }
            }
            else
            {
                if (!uint.TryParse(widthTextBox.Text, out var width) ||
                    !uint.TryParse(heightTextBox.Text, out var height))
                {
                    // Invalid input, delete last character
                    // TODO: Find a more user friendly solution
                    if (isWidth && widthTextBox.Text.Length > 1)
                    {
                        widthTextBox.Text = widthTextBox.Text[..^1];
                    }
                    else if (heightTextBox.Text.Length > 1)
                    {
                        heightTextBox.Text = heightTextBox.Text[..^1];
                    }
                }
                else
                {
                    if (isWidth)
                    {
                        // Clamp the calculated value to prevent overflow
                        var newHeight = (uint)Math.Clamp(Math.Round(width / aspectRatio), uint.MinValue, uint.MaxValue);
                        heightTextBox.Text = newHeight.ToString(CultureInfo.CurrentCulture);
                    }
                    else
                    {
                        // Clamp the calculated value to prevent overflow
                        var newWidth = (uint)Math.Clamp(Math.Round(height * aspectRatio), uint.MinValue, uint.MaxValue);
                        widthTextBox.Text = newWidth.ToString(CultureInfo.CurrentCulture);
                    }
                }
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(AspectRatioHelper), nameof(SetAspectRatioForTextBox), e);
        }
    }
}