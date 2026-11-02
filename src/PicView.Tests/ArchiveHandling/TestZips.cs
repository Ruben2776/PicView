using System.Text;

namespace PicView.Tests.ArchiveHandling;

internal static class TestZips
{
    /// <summary>
    /// Gives an entry a compression method no reader knows, as a corrupt entry is: the archive still lists it, but
    /// extracting it fails.
    /// </summary>
    public static void CorruptEntry(string zip, string entry)
    {
        var bytes = File.ReadAllBytes(zip);
        var name = Encoding.UTF8.GetBytes(entry);
        for (var i = 0; i + name.Length <= bytes.Length; i++)
        {
            if (!bytes.AsSpan(i, name.Length).SequenceEqual(name))
            {
                continue;
            }

            // A local file header has the name 30 bytes in and the method 8 bytes in; a central directory header, 46 and 10
            if (i >= 30 && BitConverter.ToUInt32(bytes, i - 30) == 0x04034b50)
            {
                BitConverter.TryWriteBytes(bytes.AsSpan(i - 30 + 8), (ushort)77);
            }
            else if (i >= 46 && BitConverter.ToUInt32(bytes, i - 46) == 0x02014b50)
            {
                BitConverter.TryWriteBytes(bytes.AsSpan(i - 46 + 10), (ushort)77);
            }
        }
        File.WriteAllBytes(zip, bytes);
    }
}
