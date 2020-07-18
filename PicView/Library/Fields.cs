using PicView.SystemIntegration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.Library
{
    internal static class Fields
    {
        internal const string AppName = "PicView";

        internal const string SupportedFilesFilter =
        " *.jpg *.jpeg *.jpe *.png *.bmp *.tif *.tiff *.gif *.ico *.wdp *.svg *.psd *.psb *.orf *.cr2 *.crw *.dng *.raf *.raw *.mrw *.nef *.x3f *.arw *.webp *"
        + ".aai *.ai *.art *.bgra *.bgro *.canvas *.cin *.cmyk *.cmyka *.cur *.cut *.dcm *.dcr *.dcx *.dds *.dfont *.dlib *.dpx *.dxt1 *.dxt5 *.emf *.epi *.eps *.ept"
        + " *.ept2 *.ept3 *.exr *.fax *.fits *.flif *.g3 *.g4 *.gif87 *.gradient *.gray *.group4 *.hald *.hdr *.hrz *.icb *.icon *.ipl *.jc2 *.j2k *.jng *.jnx"
        + " *.jpm *.jps *.jpt *.kdc *.label *.map *.nrw *.otb *.otf *.pbm *.pcd *.pcds *.pcl *.pct *.pcx *.pfa *.pfb *.pfm *.picon *.pict *.pix *.pjpeg *.png00"
        + " *.png24 *.png32 *.png48 *.png64 *.png8 *.pnm *.ppm *.ps *.radialgradient *.ras *.rgb *.rgba *.rgbo *.rla *.rle *.scr *.screenshot *.sgi *.srf *.sun"
        + " *.svgz *.tiff64 *.ttf *.vda *.vicar *.vid *.viff *.vst *.vmf *.wpg *.xbm *.xcf *.yuv";

        /// <summary>
        ///  Files filterering string used for file/save dialog
        ///  TODO update for and check file support
        /// </summary>
        internal const string FilterFiles =
            "All Supported files|*.bmp;*.jpg;*.png;*.tif;*.gif;*.ico;*.jpeg;*.wdp;*.psd;*.psb;*.cbr;*.cb7;*.cbt;"
            + "*.cbz;*.xz;*.orf;*.cr2;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw;*.webp;"
            + "*.zip;*.7zip;*.7z;*.rar;*.bzip2;*.tar;*.wim;*.iso;*.cab"
            ////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
            + "|Pictures|*.bmp;*.jpg;*.png;.tif;*.gif;*.ico;*.jpeg*.wdp*"                                   // Common pics
            + "|jpg| *.jpg;*.jpeg*"                                                                         // JPG
            + "|bmp|*.bmp;"                                                                                 // BMP
            + "|PNG|*.png;"                                                                                 // PNG
            + "|gif|*.gif;"                                                                                 // GIF
            + "|ico|*.ico;"                                                                                 // ICO
            + "|wdp|*.wdp;"                                                                                 // WDP
            + "|svg|*.svg;"                                                                                 // SVG
            + "|tif|*.tif;"                                                                                 // Tif
            + "|Photoshop|*.psd;*.psb"                                                                      // PSD
            + "|Archives|*.zip;*.7zip;*.7z;*.rar;*.bzip2;*.tar;*.wim;*.iso;*.cab"                           // Archives
            + "|Comics|*.cbr;*.cb7;*.cbt;*.cbz;*.xz"                                                        // Comics
            + "|Camera files|*.orf;*.cr2;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw";      // Camera files

        internal const int zoomSpeed = 45;

        /// <summary>
        /// The Main Window?
        /// </summary>
        internal static readonly Views.Windows.MainWindow TheMainWindow = (Views.Windows.MainWindow)Application.Current.MainWindow;

        /// <summary>
        /// Backup of Previous file, if changed folder etc.
        /// </summary>
        internal static string BackupPath { get; set; }

        /// <summary>
        /// File path for the extracted folder
        /// </summary>
        internal static string TempZipPath { get; set; }

        /// <summary>
        /// File path for the extracted zip file
        /// </summary>
        internal static string TempZipFile { get; set; }

        internal static bool LeftbuttonClicked { get; set; }
        internal static bool RightbuttonClicked { get; set; }
        internal static bool FastPicRunning { get; set; }
        
        internal static bool CanNavigate { get; set; }
        internal static bool FreshStartup { get; set; }
        
        internal static bool ClickArrowRightClicked { get; set; }
        internal static bool ClickArrowLeftClicked { get; set; }
        internal static bool Reverse { get; set; }
        internal static bool IsDialogOpen { get; set; }

        /// <summary>
        /// Backup of Width data
        /// </summary>
        internal static double xWidth;

        /// <summary>
        /// Backup of Height data
        /// </summary>
        internal static double xHeight;

        /// <summary>
        /// Used to get and set Aspect Ratio
        /// </summary>
        internal static double AspectRatio { get; set; }


        /// <summary>
        /// Used to get and set monitor size
        /// </summary>
        internal static MonitorSize MonitorInfo { get; set; }

        /// <summary>
        /// Backup of image
        /// </summary>
        internal static ImageSource prevPicResource;

        /// <summary>
        /// Primary ContextMenu
        /// </summary>
        internal static ContextMenu cm;
    }
}