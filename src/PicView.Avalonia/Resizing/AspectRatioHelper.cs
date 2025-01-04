using System.Globalization;
using Avalonia.Controls;
using PicView.Avalonia.ViewModels;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.Resizing;

public static class AspectRatioHelper
{
    public static void SetAspectRatioForTextBox(TextBox widthTextBox, TextBox heightTextBox, bool isWidth,
        double aspectRatio, MainViewModel vm)
    {
        var percentage = isWidth ? widthTextBox.Text.GetPercentage() : heightTextBox.Text.GetPercentage();
        if (percentage > 0)
        {
            var newWidth = vm.PixelWidth * (percentage / 100);
            var newHeight = vm.PixelHeight * (percentage / 100);

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
            if (!uint.TryParse(widthTextBox.Text, out var width) || !uint.TryParse(heightTextBox.Text, out var height))
            {
                // Invalid input, delete last character
                try
                {
                    if (isWidth && widthTextBox.Text.Length > 1)
                    {
                        widthTextBox.Text = widthTextBox.Text[..^1];
                    }
                    else if (heightTextBox.Text.Length > 1)
                    {
                        heightTextBox.Text = heightTextBox.Text[..^1];
                    }

                }
                catch (Exception e)
                {
#if DEBUG
                    Console.WriteLine(e);
#endif
                }

                return;
            }

            if (isWidth)
            {
                var newHeight = Math.Round(width / aspectRatio);
                heightTextBox.Text = newHeight.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                var newWidth = Math.Round(height * aspectRatio);
                widthTextBox.Text = newWidth.ToString(CultureInfo.CurrentCulture);
            }
        }
    }

    public static PrintSizes GetPrintSizes(int pixelWidth, int pixelHeight, double dpiX, double dpiY)
    {
        var cm = TranslationHelper.Translation.Centimeters;
        var mp = TranslationHelper.Translation.MegaPixels;
        var inches = TranslationHelper.Translation.Inches;
        var inchesWidth = pixelWidth / dpiX;
        var inchesHeight = pixelHeight / dpiY;
        var printSizeInch =
            $"{inchesWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)} {inches}";

        var cmWidth = pixelWidth / dpiX * 2.54;
        var cmHeight = pixelHeight / dpiY * 2.54;
        var printSizeCm =
            $"{cmWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {cmHeight.ToString("0.##", CultureInfo.CurrentCulture)} {cm}";
        var sizeMp =
            $"{((float)pixelHeight *pixelWidth / 1000000).ToString("0.##", CultureInfo.CurrentCulture)} {mp}";

        return new PrintSizes(printSizeCm, printSizeInch, sizeMp);
        
    }

    public static string GetFormattedAspectRatio(int gcd, int width, int height)
    {
        var square = TranslationHelper.Translation.Square;
        var landscape = TranslationHelper.Translation.Landscape;
        var portrait = TranslationHelper.Translation.Portrait;
        
        var firstRatio = width / gcd;
        var secondRatio = height / gcd;

        if (firstRatio == secondRatio)
        {
            return $"{firstRatio}:{secondRatio} ({square})";
        }
        return firstRatio > secondRatio ?
            $"{firstRatio}:{secondRatio} ({landscape})" :
            $"{firstRatio}:{secondRatio} ({portrait})";
    }
    
    public readonly struct PrintSizes(string printSizeCm, string printSizeInch, string sizeMp)
    {
        public string PrintSizeCm { get; } = printSizeCm;
        public string PrintSizeInch { get; } = printSizeInch;
        public string SizeMp { get; } = sizeMp;
    }
}
