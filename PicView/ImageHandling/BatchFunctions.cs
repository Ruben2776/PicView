using ImageMagick;
using PicView.FileHandling;
using System.IO;
using System.Text;

namespace PicView.ImageHandling
{
    internal static class BatchFunctions
    {
        internal static async Task<string> RunAsync(FileInfo sourceFile, int width, int height, int quality, string? ext, Percentage? percentage, bool? compress, string outputFolder, bool toResize)
        {
            var destination = outputFolder is null ? null : outputFolder + @"/" + sourceFile.Name;

            var sb = new StringBuilder();

            if (Directory.Exists(outputFolder) == false)
            {
                Directory.CreateDirectory(outputFolder);
            }

            sb.Append(sourceFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ').Append(FileFunctions.GetReadableFileSize(sourceFile.Length)).Append(" 🠚 ");

            if (toResize)
            {
                try
                {
                    using var filestream = new FileStream(sourceFile.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
                    using var magick = new MagickImage(filestream) 
                    {
                        ColorSpace = ColorSpace.Transparent,
                        Quality = quality,
                    };

                    if (percentage is not null)
                    {
                        magick.Resize(percentage.Value);
                    }
                    else
                    {
                        magick.Resize(width, height);
                    }

                    if (destination is null)
                    {
                        if (ext is not null)
                        {
                            destination = Path.ChangeExtension(sourceFile.FullName, ext);
                            using var saveStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                            await magick.WriteAsync(saveStream).ConfigureAwait(false);
                            DeleteFiles.TryDeleteFile(sourceFile.FullName, true);
                            if (compress.HasValue)
                            {
                                Optimize(compress.Value, destination);
                            }
                        }
                        else
                        {
                            await magick.WriteAsync(filestream).ConfigureAwait(false);
                            if (compress.HasValue)
                            {
                                Optimize(compress.Value, sourceFile.FullName);
                            }
                        }
                    }
                    else
                    {
                        if (ext is not null)
                        {
                            if (File.Exists(destination))
                            {
                                DeleteFiles.TryDeleteFile(destination, true);
                            }
                            destination = Path.ChangeExtension(destination, ext);
                        }

                        using var overwriteStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                        await magick.WriteAsync(overwriteStream).ConfigureAwait(false);
                        if (compress.HasValue)
                        {
                            Optimize(compress.Value, destination);
                        }
                    }
                }
                catch (Exception e)
                {
                    return e.Message;
                }

                var destinationFile = new FileInfo(destination);
                sb.Append(destinationFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ').Append(FileFunctions.GetReadableFileSize(destinationFile.Length)).
                    Append(' ').AppendLine(Environment.NewLine);

                return sb.ToString();
            }

            if (compress.HasValue == false) { return string.Empty; }

            var success = false;

            if (sourceFile.DirectoryName + @"\" == outputFolder)
            {
                await ImageFunctions.OptimizeImageAsync(sourceFile.FullName).ConfigureAwait(false);
            }
            else if (ext is null)
            {
                if (quality is 100)
                {
                    File.Copy(sourceFile.FullName, destination, true);
                }
                else
                {
                    success = await SaveImages.SaveImageAsync(null, sourceFile.FullName, destination, null, null, quality, ext).ConfigureAwait(false);
                }
            }
            else
            {
                success = await SaveImages.SaveImageAsync(null, sourceFile.FullName, destination, null, null, quality, ext).ConfigureAwait (false);
            }

            if (success is false)
            {
                return string.Empty;
            }

            Optimize(compress.Value, destination);
            var newSize = FileFunctions.GetReadableFileSize(new FileInfo(sourceFile.FullName).Length);
            sb.Append(sourceFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ').Append(FileFunctions.GetReadableFileSize(sourceFile.Length))
                .Append(" 🠚 ").Append(sourceFile.Name).Append(' ').Append(newSize).AppendLine(Environment.NewLine);

            return sb.ToString();
        }

        private static bool Optimize(bool isLossless, string file)
        {
            ImageOptimizer imageOptimizer = new()
            {
                OptimalCompression = isLossless
            };
            return imageOptimizer.Compress(file);
        }

        internal class ThumbNailHolder
        {
            internal readonly string Directory;
            internal readonly int Width;
            internal readonly int Height;
            internal readonly Percentage? Percentage;

            internal ThumbNailHolder(string directory, int width, int height, Percentage? percentage)
            {
                Directory = directory;
                Width = width;
                Height = height;
                Percentage = percentage;
            }
        }
    }
}