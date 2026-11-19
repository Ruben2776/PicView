using System.IO.Compression;

namespace PicView.Tests.ArchiveHandling;

/// <summary>
///     Zips whose entries have the sizes and modified times a test gives, in the order given, to order pages by them.
/// </summary>
internal static class OrderedZip
{
    /// <summary>
    ///     Writes a zip at <paramref name="path" /> holding each entry with that many bytes, modified at its time. A zip
    ///     keeps local time to two seconds, so the times given should be whole even seconds.
    /// </summary>
    public static string Create(string path, params (string Name, int Size, DateTime Modified)[] entries)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, size, modified) in entries)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
            entry.LastWriteTime = new DateTimeOffset(DateTime.SpecifyKind(modified, DateTimeKind.Local));
            using var stream = entry.Open();
            var bytes = new byte[size];
            Array.Fill(bytes, (byte)0xAB);
            stream.Write(bytes);
        }

        return path;
    }

    /// <summary>
    ///     Clears the modified time of the entries named, or of every entry when none is named, of the zip at
    ///     <paramref name="path" />: a zero date, which a zip has when it holds no time, and which reads as the first day
    ///     of 1980.
    /// </summary>
    public static void ClearModifiedTimes(string path, params string[] names)
    {
        var bytes = File.ReadAllBytes(path);
        for (var i = 0; i + 4 <= bytes.Length; i++)
        {
            if (bytes[i] != 0x50 || bytes[i + 1] != 0x4B)
            {
                continue;
            }

            // A local header holds its time and date 10 bytes in and its name's length 26 bytes in, the name from 30; a
            // central directory header holds them 12 and 28 bytes in, the name from 46
            var (at, nameLengthAt, nameAt) = (bytes[i + 2], bytes[i + 3]) switch
            {
                (3, 4) => (i + 10, i + 26, i + 30),
                (1, 2) => (i + 12, i + 28, i + 46),
                _ => (-1, -1, -1)
            };
            if (at < 0)
            {
                continue;
            }

            var name = System.Text.Encoding.UTF8.GetString(bytes, nameAt,
                BitConverter.ToUInt16(bytes, nameLengthAt));
            if (names.Length is 0 || names.Contains(name, StringComparer.Ordinal))
            {
                Array.Clear(bytes, at, 4);
            }
        }

        File.WriteAllBytes(path, bytes);
    }
}
