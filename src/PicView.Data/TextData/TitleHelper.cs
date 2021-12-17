using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PicView.Data.IO;

namespace PicView.Data.TextData;

public static class TitleHelper
{
        private const string AppName = "PicView";

        private static int Gcd(int x, int y)
        {
            while (true)
            {
                if (y == 0) return x;
                var x1 = x;
                x = y;
                y = x1 % y;
            }
        }

        private static string StringAspect(int width, int height)
        {
            var gcd = Gcd(width, height);
            var x = width / gcd;
            var y = height / gcd;

            if (x > 48 || y > 18)
            {
                return ") ";
            }

            return $", {x} : {y}) ";
        }
        
        public static string[] TitleString(int width, int height, int index, FileInfo? fileInfo, List<string> pics)
        {
            if (fileInfo == null)
            {
                throw new Exception();
            }

            if (fileInfo.Exists == false)
            {
                throw new Exception();
            }

            var files = pics.Count == 1 ?"File": "Files"; // TODO add to translation

            var s1 = new StringBuilder(90);
            s1.Append(fileInfo.Name).Append(' ').Append(index + 1).Append('/').Append(pics.Count).Append(' ')
                .Append(files).Append(" (").Append(width).Append(" x ").Append(height)
                .Append(StringAspect(width, height))
                .Append(FileHelper.GetSizeReadable(fileInfo.Length));

            var zoomPercentage = string.Empty; // TODO Get zoom percentage

            if (!string.IsNullOrEmpty(zoomPercentage))
            {
                s1.Append(", ").Append(zoomPercentage);
            }

            s1.Append(" - ").Append(AppName);

            var array = new string[3];
            array[0] = s1.ToString();
            s1.Remove(s1.Length - (AppName.Length + 3), AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            s1.Replace(Path.GetFileName(pics[index]), pics[index]);
            array[2] = s1.ToString();
            return array;
        }
}