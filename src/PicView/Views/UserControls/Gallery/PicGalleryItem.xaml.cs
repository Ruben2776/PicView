using PicView.Animations;
using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static PicView.PicGallery.GalleryNavigation;

namespace PicView.Views.UserControls.Gallery
{
    public partial class PicGalleryItem
    {
        internal string FileName { get; set; }

        public PicGalleryItem(ImageSource? pic, string fileName, bool selected)
        {
            InitializeComponent();

            if (pic != null)
            {
                ThumbImage.Source = pic;
            }

            FileName = fileName;

            OuterBorder.Width = OuterBorder.Height = PicGalleryItemSize;
            InnerBorder.Width = InnerBorder.Height =
                Settings.Default.IsBottomGalleryShown ? PicGalleryItemSize : PicGalleryItemSizeS;

            ThumbImage.MouseEnter += delegate
            {
                InnerBorder.BorderBrush = (SolidColorBrush)Application.Current.Resources["ChosenColorBrush"];
            };

            ThumbImage.MouseLeave += delegate
            {
                if (Navigation.Pics.IndexOf(FileName) == Navigation.FolderIndex)
                    return;
                if (GalleryFunctions.IsGalleryOpen)
                {
                    if (Math.Abs(InnerBorder.Width - PicGalleryItemSize) < 0.5 && Math.Abs(InnerBorder.Height - PicGalleryItemSize) < 0.5)
                    {
                        return;
                    }
                }

                InnerBorder.BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"];
            };

            if (!selected) return;
            InnerBorder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPreferredColor());
            InnerBorder.Width = InnerBorder.Height = PicGalleryItemSize;
        }

        public void AddContextMenu()
        {
            var cm = new ContextMenu();

            //Print
            var printMenu = new MenuItem
            {
                Header = Application.Current.Resources["Print"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M448 1536h896v-256h-896v256zm0-640h896v-384h-160q-40 0-68-28t-28-68v-160h-640v640zm1152 64q0-26-19-45t-45-19-45 19-19 45 19 45 45 19 45-19 19-45zm128 0v416q0 13-9.5 22.5t-22.5 9.5h-224v160q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-160h-224q-13 0-22.5-9.5t-9.5-22.5v-416q0-79 56.5-135.5t135.5-56.5h64v-544q0-40 28-68t68-28h672q40 0 88 20t76 48l152 152q28 28 48 76t20 88v256h64q79 0 135.5 56.5t56.5 135.5z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            printMenu.Click += (_, _) => OpenSave.Print(FileName);
            cm.Items.Add(printMenu);

            // Open With
            var openWithMenu = new MenuItem
            {
                Header = Application.Current.Resources["OpenWith"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M0 0l20 10L0 20V0zm0 8v4l10-2L0 8z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            openWithMenu.Click += (_, _) => OpenSave.OpenWith(FileName);
            cm.Items.Add(openWithMenu);

            // Show in folder
            var showInFolderMenu = new MenuItem
            {
                Header = Application.Current.Resources["ShowInFolder"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M442,386.7l-84.8-85.9c13.8-24.1,21-50.9,21-77.9c0-87.6-71.2-158.9-158.6-158.9C135.2,64,64,135.3,64,222.9  c0,87.6,71.2,158.9,158.6,158.9c27.9,0,55.5-7.7,80.1-22.4l84.4,85.6c1.9,1.9,4.6,3.1,7.3,3.1c2.7,0,5.4-1.1,7.3-3.1l43.3-43.8  C449,397.1,449,390.7,442,386.7z M222.6,125.9c53.4,0,96.8,43.5,96.8,97c0,53.5-43.4,97-96.8,97c-53.4,0-96.8-43.5-96.8-97  C125.8,169.4,169.2,125.9,222.6,125.9z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            showInFolderMenu.Click += (_, _) => OpenSave.OpenInExplorer(FileName);
            cm.Items.Add(showInFolderMenu);

            cm.Items.Add(new Separator());

            // Set As Wallpaper
            var setAsWallpaperMenu = new MenuItem
            {
                Header = Application.Current.Resources["SetAsWallpaper"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M448 405.333V106.667C448 83.198 428.802 64 405.333 64H106.667C83.198 64 64 83.198 64 106.667v298.666C64 428.802 83.198 448 106.667 448h298.666C428.802 448 448 428.802 448 405.333zM181.333 288l53.334 64 74.666-96 96 128H106.667l74.666-96z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            setAsWallpaperMenu.Click += async (_, _) =>
                await Wallpaper.SetWallpaperAsync(Wallpaper.WallpaperStyle.Fill, FileName)
                    .ConfigureAwait(false);
            cm.Items.Add(setAsWallpaperMenu);

            // Set As Lockscreen
            var setAsLockScreenImageMenu = new MenuItem
            {
                Header = Application.Current.Resources["SetAsLockScreenImage"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M448 405.333V106.667C448 83.198 428.802 64 405.333 64H106.667C83.198 64 64 83.198 64 106.667v298.666C64 428.802 83.198 448 106.667 448h298.666C428.802 448 448 428.802 448 405.333zM181.333 288l53.334 64 74.666-96 96 128H106.667l74.666-96z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            setAsLockScreenImageMenu.Click += async (_, _) =>
                await LockScreenHelper.SetLockScreenImageAsync().ConfigureAwait(false);
            cm.Items.Add(setAsLockScreenImageMenu);

            cm.Items.Add(new Separator());

            // Copy File
            var copyFileMenu = new MenuItem
            {
                Header = Application.Current.Resources["CopyFile"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M1696 384q40 0 68 28t28 68v1216q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-288h-544q-40 0-68-28t-28-68v-672q0-40 20-88t48-76l408-408q28-28 76-48t88-20h416q40 0 68 28t28 68v328q68-40 128-40h416zm-544 213l-299 299h299v-299zm-640-384l-299 299h299v-299zm196 647l316-316v-416h-384v416q0 40-28 68t-68 28h-416v640h512v-256q0-40 20-88t48-76zm956 804v-1152h-384v416q0 40-28 68t-68 28h-416v640h896z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            copyFileMenu.Click += (_, _) => CopyPaste.CopyFile(FileName);
            cm.Items.Add(copyFileMenu);

            // Copy Image
            var copyImageMenu = new MenuItem
            {
                Header = Application.Current.Resources["CopyImage"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M1696 384q40 0 68 28t28 68v1216q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-288h-544q-40 0-68-28t-28-68v-672q0-40 20-88t48-76l408-408q28-28 76-48t88-20h416q40 0 68 28t28 68v328q68-40 128-40h416zm-544 213l-299 299h299v-299zm-640-384l-299 299h299v-299zm196 647l316-316v-416h-384v416q0 40-28 68t-68 28h-416v640h512v-256q0-40 20-88t48-76zm956 804v-1152h-384v416q0 40-28 68t-68 28h-416v640h896z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            copyImageMenu.Click += (_, _) => CopyPaste.CopyBitmap(Navigation.Pics.IndexOf(FileName));
            cm.Items.Add(copyImageMenu);

            // Copy Image
            var copyBase64Menu = new MenuItem
            {
                Header = Application.Current.Resources["Copy"] + "base64",
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M1696 384q40 0 68 28t28 68v1216q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-288h-544q-40 0-68-28t-28-68v-672q0-40 20-88t48-76l408-408q28-28 76-48t88-20h416q40 0 68 28t28 68v328q68-40 128-40h416zm-544 213l-299 299h299v-299zm-640-384l-299 299h299v-299zm196 647l316-316v-416h-384v416q0 40-28 68t-68 28h-416v640h512v-256q0-40 20-88t48-76zm956 804v-1152h-384v416q0 40-28 68t-68 28h-416v640h896z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            copyBase64Menu.Click += async (_, _) =>
                await Base64.SendToClipboard(FileName).ConfigureAwait(false);
            cm.Items.Add(copyBase64Menu);

            cm.Items.Add(new Separator());

            // Cut
            var fileCutMenu = new MenuItem
            {
                Header = Application.Current.Resources["FileCut"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M9.77 11.5l5.34 3.91c.44.33 1.24.59 1.79.59H20L6.89 6.38A3.5 3.5 0 1 0 5.5 8.37L7.73 10 5.5 11.63a3.5 3.5 0 1 0 1.38 1.99l2.9-2.12zM3.5 7a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm0 9a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zM15.1 4.59A3.53 3.53 0 0 1 16.9 4H20l-7.5 5.5L10.45 8l4.65-3.41z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            fileCutMenu.Click += (_, _) => CopyPaste.Cut(FileName);
            cm.Items.Add(fileCutMenu);

            // Delete file
            var deleteFileMenu = new MenuItem
            {
                Header = Application.Current.Resources["DeleteFile"],
                Icon = new Path
                {
                    Width = 14,
                    Height = 14,
                    Data = Geometry.Parse(
                        "M836 1169l-15 368-2 22-420-29q-36-3-67-31.5t-47-65.5q-11-27-14.5-55t4-65 12-55 21.5-64 19-53q78 12 509 28zm-387-586l180 379-147-92q-63 72-111.5 144.5t-72.5 125-39.5 94.5-18.5 63l-4 21-190-357q-17-26-18-56t6-47l8-18q35-63 114-188l-140-86zm1231 517l-188 359q-12 29-36.5 46.5t-43.5 20.5l-18 4q-71 7-219 12l8 164-230-367 211-362 7 173q170 16 283 5t170-33zm-785-924q-47 63-265 435l-317-187-19-12 225-356q20-31 60-45t80-10q24 2 48.5 12t42 21 41.5 33 36 34.5 36 39.5 32 35zm655 307l212 363q18 37 12.5 76t-27.5 74q-13 20-33 37t-38 28-48.5 22-47 16-51.5 14-46 12q-34-72-265-436l313-195zm-143-226l142-83-220 373-419-20 151-86q-34-89-75-166t-75.5-123.5-64.5-80-47-46.5l-17-13 405 1q31-3 58 10.5t39 28.5l11 15q39 61 112 190z"),
                    Fill = (SolidColorBrush)Application.Current.Resources["IconColorBrush"],
                    Stretch = Stretch.Fill
                }
            };
            deleteFileMenu.Click += async (_, _) =>
                await DeleteFiles.DeleteFileAsync(true, Navigation.Pics.IndexOf(FileName)).ConfigureAwait(false);
            cm.Items.Add(deleteFileMenu);

            ContextMenu = cm;
        }
    }
}