#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;
using PicView.Core.WindowsNT.FileHandling;
using Xunit;

namespace PicView.Tests.Clipboard;

[Collection("Sequential")]
public class Win32ClipboardTests
{
    private const uint CF_HDROP = 15;
    private const uint DROPEFFECT_MOVE = 2;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [Out] StringBuilder? lpszFile, uint cch);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CutFile_WithNullOrWhitespace_ReturnsFalse(string? path)
    {
        var result = Win32Clipboard.CutFile(path!);
        Assert.False(result);
    }

    [Fact]
    public void CutFile_WithNonExistentPath_ReturnsFalse()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        var result = Win32Clipboard.CutFile(nonExistentPath);
        Assert.False(result);
    }

    [Fact]
    public void CutFiles_WithNullOrEmpty_ReturnsFalse()
    {
        Assert.False(Win32Clipboard.CutFiles(null));
        Assert.False(Win32Clipboard.CutFiles([]));
    }

    [Fact]
    public void CutFile_WithValidFile_SetsHdropAndPreferredDropEffectMove()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var result = Win32Clipboard.CutFile(tempFile);
            Assert.True(result);

            Assert.True(OpenClipboard(IntPtr.Zero));
            try
            {
                var hDrop = GetClipboardData(CF_HDROP);
                Assert.NotEqual(IntPtr.Zero, hDrop);

                var fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                Assert.Equal(1u, fileCount);

                var sb = new StringBuilder(1024);
                var charsCopied = DragQueryFile(hDrop, 0, sb, (uint)sb.Capacity);
                var clipboardPath = sb.ToString();

                Assert.Equal(Path.GetFullPath(tempFile), clipboardPath, ignoreCase: true);

                var dropEffectFormat = RegisterClipboardFormat("Preferred DropEffect");
                Assert.True(dropEffectFormat > 0);

                var hDropEffect = GetClipboardData(dropEffectFormat);
                Assert.NotEqual(IntPtr.Zero, hDropEffect);

                var pDropEffect = GlobalLock(hDropEffect);
                Assert.NotEqual(IntPtr.Zero, pDropEffect);
                try
                {
                    var effect = (uint)Marshal.ReadInt32(pDropEffect);
                    Assert.Equal(DROPEFFECT_MOVE, effect);
                }
                finally
                {
                    GlobalUnlock(hDropEffect);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void CutFiles_WithMultipleFiles_SetsAllFilesInHdrop()
    {
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        try
        {
            var result = Win32Clipboard.CutFiles([tempFile1, tempFile2]);
            Assert.True(result);

            Assert.True(OpenClipboard(IntPtr.Zero));
            try
            {
                var hDrop = GetClipboardData(CF_HDROP);
                Assert.NotEqual(IntPtr.Zero, hDrop);

                var fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                Assert.Equal(2u, fileCount);

                var sb1 = new StringBuilder(1024);
                DragQueryFile(hDrop, 0, sb1, (uint)sb1.Capacity);
                var path1 = sb1.ToString();

                var sb2 = new StringBuilder(1024);
                DragQueryFile(hDrop, 1, sb2, (uint)sb2.Capacity);
                var path2 = sb2.ToString();

                Assert.Equal(Path.GetFullPath(tempFile1), path1, ignoreCase: true);
                Assert.Equal(Path.GetFullPath(tempFile2), path2, ignoreCase: true);
            }
            finally
            {
                CloseClipboard();
            }
        }
        finally
        {
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
        }
    }
}
#endif
