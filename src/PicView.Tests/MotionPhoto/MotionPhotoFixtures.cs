using System.IO.Compression;
using System.Text;

namespace PicView.Tests.MotionPhoto;

internal static class MotionPhotoFixtures
{
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "picview-motionphoto-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    public static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    /// <summary>Builds a minimal valid MP4 "ftyp" box followed by filler bytes.</summary>
    public static byte[] BuildMp4Head(uint boxSize = 32)
    {
        var box = new byte[boxSize];
        box[0] = (byte)(boxSize >> 24);
        box[1] = (byte)(boxSize >> 16);
        box[2] = (byte)(boxSize >> 8);
        box[3] = (byte)boxSize;
        "ftyp"u8.CopyTo(box.AsSpan(4, 4));
        "isom"u8.CopyTo(box.AsSpan(8, 4));
        return box;
    }

    public static string NewStandardXmp(long videoLength) =>
        $"""
         <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
         <x:xmpmeta xmlns:x="adobe:ns:meta/">
           <rdf:RDF xmlns:rdf="http://www.w3.org/1999-02-22-rdf-syntax-ns#">
             <rdf:Description xmlns:Container="http://ns.google.com/photos/1.0/container/"
                              xmlns:Item="http://ns.google.com/photos/1.0/container/item/"
                              xmlns:GCamera="http://ns.google.com/photos/1.0/camera/"
                              GCamera:MotionPhoto="1"
                              GCamera:MotionPhotoVersion="1"
                              GCamera:MotionPhotoPresentationTimestampUs="0">
               <Container:Directory>
                 <rdf:Seq>
                   <rdf:li rdf:parseType="Resource">
                     <Container:Item rdf:parseType="Resource">
                       <Item:Semantic>Primary</Item:Semantic>
                       <Item:Mime>image/jpeg</Item:Mime>
                     </Container:Item>
                   </rdf:li>
                   <rdf:li rdf:parseType="Resource">
                     <Container:Item rdf:parseType="Resource">
                       <Item:Semantic>MotionPhoto</Item:Semantic>
                       <Item:Mime>video/mp4</Item:Mime>
                       <Item:Length>{videoLength}</Item:Length>
                     </Container:Item>
                   </rdf:li>
                 </rdf:Seq>
               </Container:Directory>
             </rdf:Description>
           </rdf:RDF>
         </x:xmpmeta>
         <?xpacket end="w"?>
         """;

    public static string MicroVideoXmp(long microVideoOffset) =>
        $"""
         <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
         <x:xmpmeta xmlns:x="adobe:ns:meta/">
           <rdf:RDF xmlns:rdf="http://www.w3.org/1999-02-22-rdf-syntax-ns#">
             <rdf:Description xmlns:GCamera="http://ns.google.com/photos/1.0/camera/"
                              GCamera:MicroVideo="1"
                              GCamera:MicroVideoVersion="1"
                              GCamera:MicroVideoOffset="{microVideoOffset}"
                              GCamera:MicroVideoPresentationTimestampUs="1432795"/>
           </rdf:RDF>
         </x:xmpmeta>
         <?xpacket end="w"?>
         """;

    public const string PlainXmp =
        """
        <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
          <rdf:RDF xmlns:rdf="http://www.w3.org/1999-02-22-rdf-syntax-ns#">
            <rdf:Description xmlns:dc="http://purl.org/dc/elements/1.1/">
              <dc:creator>Test</dc:creator>
            </rdf:Description>
          </rdf:RDF>
        </x:xmpmeta>
        <?xpacket end="w"?>
        """;

    /// <summary>Wraps an XMP packet in a JPEG-like byte structure (SOI + APP1 XMP segment).</summary>
    public static byte[] BuildJpegWithXmp(string xmpPacket)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD8);
        stream.WriteByte(0xFF);
        stream.WriteByte(0xE1);
        var header = "http://ns.adobe.com/xap/1.0/\0"u8.ToArray();
        var packet = Encoding.UTF8.GetBytes(xmpPacket);
        var segmentLength = 2 + header.Length + packet.Length;
        stream.WriteByte((byte)(segmentLength >> 8));
        stream.WriteByte((byte)segmentLength);
        stream.Write(header);
        stream.Write(packet);
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD9);
        return stream.ToArray();
    }

    /// <summary>Creates a synthetic embedded motion photo: jpeg head + XMP + filler + mp4 tail.</summary>
    public static FileInfo CreateEmbeddedMotionPhoto(string directory, string fileName, byte[] videoBytes, string xmpPacket)
    {
        var jpeg = BuildJpegWithXmp(xmpPacket);
        var path = Path.Combine(directory, fileName);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        stream.Write(jpeg);
        stream.Write(videoBytes);
        return new FileInfo(path);
    }

    /// <summary>Creates a .livp (zip) container with the given image and video entries.</summary>
    public static FileInfo CreateLivp(string directory, string fileName, byte[] imageBytes, byte[] videoBytes)
    {
        var path = Path.Combine(directory, fileName);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

        var imageEntry = zip.CreateEntry("IMG_0001.jpg");
        using (var imageStream = imageEntry.Open())
        {
            imageStream.Write(imageBytes);
        }

        var videoEntry = zip.CreateEntry("IMG_0001.MOV");
        using (var videoStream = videoEntry.Open())
        {
            videoStream.Write(videoBytes);
        }

        return new FileInfo(path);
    }
}
