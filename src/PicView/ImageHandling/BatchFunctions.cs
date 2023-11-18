using ImageMagick;
using PicView.FileHandling;
using System.IO;
using System.Text;

namespace PicView.ImageHandling
{
    internal static class BatchFunctions
    {
        internal static async Task<string> RunAsync(FileInfo sourceFile, int width, int height, int quality,
            string? ext, Percentage? percentage, bool? compress, string? outputFolder, bool toResize)
        {
            var destination = outputFolder is null ? null : outputFolder + @"/" + sourceFile.Name;

            var sb = new StringBuilder();

            if (Directory.Exists(outputFolder) == false)
            {
                Directory.CreateDirectory(outputFolder);
            }

            sb.Append(sourceFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ')
                .Append(sourceFile.Length.GetReadableFileSize())
                .Append("\n 🠚 \n");

            if (toResize)
            {
                try
                {
                    await using var fileStream = new FileStream(sourceFile.FullName, FileMode.OpenOrCreate,
                        FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
                    // ReSharper disable once IdentifierTypo
                    using var magickImage = new MagickImage(fileStream)
                    {
                        ColorSpace = ColorSpace.Transparent,
                        Quality = quality,
                    };

                    if (percentage is not null)
                    {
                        magickImage.Resize(percentage.Value);
                    }
                    else
                    {
                        magickImage.Resize(width, height);
                    }

                    if (destination is null)
                    {
                        if (ext is not null)
                        {
                            switch (ext)
                            {
                                case ".jpeg":
                                case ".jpg":
                                    magickImage.Format = MagickFormat.Jpeg;
                                    break;
                                case ".png":
                                    magickImage.Format = MagickFormat.Png;
                                    break;
                                case ".jxl":
                                    magickImage.Format = MagickFormat.Jxl;
                                    break;
                                case ".gif":
                                    magickImage.Format = MagickFormat.Gif;
                                    break;
                                case ".webp":
                                    magickImage.Format = MagickFormat.WebP;
                                    break;
                                case ".heic":
                                    magickImage.Format = MagickFormat.Heic;
                                    break;
                                case ".heif":
                                    magickImage.Format = MagickFormat.Heif;
                                    break;
                            }

                            destination = Path.ChangeExtension(sourceFile.FullName, ext);
                            await using var saveStream = new FileStream(destination, FileMode.Create, FileAccess.Write,
                                FileShare.ReadWrite, 4096, true);
                            await magickImage.WriteAsync(saveStream).ConfigureAwait(false);
                            DeleteFiles.TryDeleteFile(sourceFile.FullName, true);
                            if (compress.HasValue)
                            {
                                ImageFunctions.OptimizeImage(destination, compress.Value);
                            }
                        }
                        else
                        {
                            await magickImage.WriteAsync(fileStream).ConfigureAwait(false);
                            if (compress.HasValue)
                            {
                                ImageFunctions.OptimizeImage(sourceFile.FullName, compress.Value);
                            }
                        }
                    }
                    else
                    {
                        if (ext is not null)
                        {
                            switch (ext)
                            {
                                case ".jpeg":
                                case ".jpg":
                                    magickImage.Format = MagickFormat.Jpeg;
                                    break;
                                case ".png":
                                    magickImage.Format = MagickFormat.Png;
                                    break;
                                case ".jxl":
                                    magickImage.Format = MagickFormat.Jxl;
                                    break;
                                case ".gif":
                                    magickImage.Format = MagickFormat.Gif;
                                    break;
                                case ".webp":
                                    magickImage.Format = MagickFormat.WebP;
                                    break;
                                case ".heic":
                                    magickImage.Format = MagickFormat.Heic;
                                    break;
                                case ".heif":
                                    magickImage.Format = MagickFormat.Heif;
                                    break;
                            }

                            if (File.Exists(destination))
                            {
                                DeleteFiles.TryDeleteFile(destination, true);
                            }

                            destination = Path.ChangeExtension(destination, ext);
                        }

                        await using var overwriteStream = new FileStream(destination, FileMode.Create, FileAccess.Write,
                            FileShare.ReadWrite, 4096, true);
                        await magickImage.WriteAsync(overwriteStream).ConfigureAwait(false);
                        if (compress.HasValue)
                        {
                            ImageFunctions.OptimizeImage(destination, compress.Value);
                        }
                    }
                }
                catch (Exception e)
                {
                    return e.Message;
                }

                var destinationFile = new FileInfo(destination);
                sb.Append(destinationFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ')
                    .Append(destinationFile.Length.GetReadableFileSize()).Append(' ').AppendLine(Environment.NewLine);

                return sb.ToString();
            }

            if (compress.HasValue == false)
            {
                return string.Empty;
            }

            var success = false;

            if (sourceFile.DirectoryName + @"\" == outputFolder)
            {
                ImageFunctions.OptimizeImage(sourceFile.FullName);
            }
            else if (ext is null)
            {
                if (quality is 100)
                {
                    File.Copy(sourceFile.FullName, destination, true);
                }
                else
                {
                    success = await SaveImages
                        .SaveImageAsync(null, sourceFile.FullName, destination, null, null, quality, ext)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                success = await SaveImages
                    .SaveImageAsync(null, sourceFile.FullName, destination, null, null, quality, ext)
                    .ConfigureAwait(false);
            }

            if (success is false)
            {
                return string.Empty;
            }

            ImageFunctions.OptimizeImage(destination, compress.Value);
            try
            {
                var newSize = new FileInfo(sourceFile.FullName).Length.GetReadableFileSize();
                sb.Append(sourceFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ')
                    .Append(sourceFile.Length.GetReadableFileSize())
                    .Append("\n 🠚 \n")
                    .Append(sourceFile.Name).Append(' ').Append(newSize)
                    .AppendLine(Environment.NewLine);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return sb.ToString();
        }

        internal class ThumbNailHolder
        {
            internal readonly string? Directory;
            internal readonly int Width;
            internal readonly int Height;
            internal readonly Percentage? Percentage;

            internal ThumbNailHolder(string? directory, int width, int height, Percentage? percentage)
            {
                Directory = directory;
                Width = width;
                Height = height;
                Percentage = percentage;
            }
        }
    }
}