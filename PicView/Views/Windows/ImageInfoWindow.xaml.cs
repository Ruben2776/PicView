using PicView.Animations;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.ImageInfo;

namespace PicView.Views.Windows
{
    public partial class ImageInfoWindow : Window
    {
        private double startheight, extendedheight;

        public ImageInfoWindow()
        {
            InitializeComponent();
            startheight = Height;
            extendedheight = 750;
            ContentRendered += async (_, _) =>
            {
                WindowBlur.EnableBlur(this);
                Window_ContentRendered();
                if (Pics.Count > FolderIndex)
                {
                    await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
                }
                else
                {
                    await UpdateValuesAsync(null).ConfigureAwait(false);
                }
            };
        }

        private void Window_ContentRendered()
        {
            Activated += async (_, _) =>
            {
                if (Pics.Count > 0 && Pics.Count > FolderIndex
                && FullPathBox.Text != Pics[FolderIndex])
                {
                    await UpdateValuesAsync(null).ConfigureAwait(false);
                }
            };

            KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(Scroller, e, this);

            // Hack to deselect border on mouse click
            MouseLeftButtonDown += delegate
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);
                Keyboard.ClearFocus();
                Focus();
            };

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); ConfigureWindows.GetMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += (_, _) => SystemCommands.MinimizeWindow(this);

            TitleBar.MouseLeftButtonDown += (_, _) => DragMove();
            MainBackground.MouseLeftButtonDown += (_, _) => DragMove();

            // FileProperties
            FileProperties.MouseEnter += (_, _) => ButtonMouseOverAnim(FilePropertiesFill);
            FileProperties.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(FilePropertiesBrush);
            FileProperties.MouseLeave += (_, _) => ButtonMouseLeaveAnim(FilePropertiesFill);
            FileProperties.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(FilePropertiesBrush);
            FileProperties.Click += (_, _) => FileHandling.FileProperties.ShowFileProperties();

            // Delete
            Delete.MouseEnter += (_, _) => ButtonMouseOverAnim(DeleteFill);
            Delete.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(DeleteBrush);
            Delete.MouseLeave += (_, _) => ButtonMouseLeaveAnim(DeleteFill);
            Delete.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(DeleteBrush);
            Delete.Click += async (_, _) => await DeleteFiles.DeleteFileAsync(Keyboard.IsKeyDown(Key.LeftShift)).ConfigureAwait(false);

            // OpenWith
            OpenWith.MouseEnter += (_, _) => ButtonMouseOverAnim(OpenWithFill);
            OpenWith.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(OpenWithBrush);
            OpenWith.MouseLeave += (_, _) => ButtonMouseLeaveAnim(OpenWithFill);
            OpenWith.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(OpenWithBrush);
            OpenWith.Click += (_, _) => Open_Save.OpenWith(Pics[FolderIndex]);

            // ShowInFolder
            ShowInFolder.MouseEnter += (_, _) => ButtonMouseOverAnim(ShowInFolderFill);
            ShowInFolder.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ShowInFolderBrush);
            ShowInFolder.MouseLeave += (_, _) => ButtonMouseLeaveAnim(ShowInFolderFill);
            ShowInFolder.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ShowInFolderBrush);
            ShowInFolder.Click += (_, _) => Open_Save.Open_In_Explorer();

            // Optimize Image
            OptimizeImageButton.MouseEnter += (_, _) => ButtonMouseOverAnim(OptimizeImageFill);
            OptimizeImageButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(OptimizeImageBrush);
            OptimizeImageButton.MouseLeave += (_, _) => ButtonMouseLeaveAnim(OptimizeImageFill);
            OptimizeImageButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(OptimizeImageBrush);
            OptimizeImageButton.Click += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

            // ExpandButton
            ExpandButton.MouseEnter += (_, _) => ButtonMouseOverAnim(chevronDownBrush);
            ExpandButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ExpandButtonBg);
            ExpandButton.MouseLeave += (_, _) => ButtonMouseLeaveAnim(chevronDownBrush);
            ExpandButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ExpandButtonBg);

            ExpandButton.Click += (_, _) => UIHelper.ExtendOrCollopase(Height, startheight, extendedheight, this, Scroller, xGeo);

            PreviewMouseWheel += (_, e) => // Collapse when scrolling down
            {
                if (e.Delta < 0 && Height == startheight)
                {
                    UIHelper.ExtendOrCollopase(Height, startheight, extendedheight, this, Scroller, xGeo);
                }
            };

            TitleBar.MouseLeave += (_, _) => UpdateStars();

            // Stars
            Star1.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(1).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star1.MouseEnter += (_, _) => UpdateStars(1);
            Star1.MouseLeave += (_, _) => UpdateStars();

            Star2.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(2).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star2.MouseEnter += (_, _) => UpdateStars(2);
            Star2.MouseLeave += (_, _) => UpdateStars();

            Star3.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(3).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star3.MouseEnter += (_, _) => UpdateStars(3);
            Star3.MouseLeave += (_, _) => UpdateStars();

            Star4.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(4).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star4.MouseEnter += (_, _) => UpdateStars(4);
            Star4.MouseLeave += (_, _) => UpdateStars();

            Star5.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(5).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star5.MouseEnter += (_, _) => UpdateStars(5);
            Star5.MouseLeave += (_, _) => UpdateStars();

            // FilenameBox
            FilenameBox.AcceptsReturn = false;
            FilenameBox.KeyUp += async (_, e) =>
            await RenameTask(
                e,
                FilenameBox,
                (Path.GetDirectoryName(Pics[FolderIndex])) + "/" + FilenameBox.Text + Path.GetExtension(Pics[FolderIndex])).ConfigureAwait(false);

            // FolderBox
            FolderBox.AcceptsReturn = false;
            FolderBox.KeyUp += async (_, e) => await RenameTask(e, FolderBox, FolderBox.Text + "/" + Path.GetFileName(FullPathBox.Text)).ConfigureAwait(false);

            // FullPathBox
            FullPathBox.AcceptsReturn = false;
            FullPathBox.KeyUp += async (_, e) => await RenameTask(e, FullPathBox, FullPathBox.Text).ConfigureAwait(false);

            // WidhtBox
            WidthBox.AcceptsReturn = false;
            WidthBox.PreviewKeyDown += async (_, e) => await QuickResizeShortcuts.QuickResizePreviewKeys(e, WidthBox.Text, HeightBox.Text).ConfigureAwait(false);

            // HeightBox
            HeightBox.AcceptsReturn = false;
            HeightBox.PreviewKeyDown += async (_, e) => await QuickResizeShortcuts.QuickResizePreviewKeys(e, WidthBox.Text, HeightBox.Text).ConfigureAwait(false);

            WidthBox.KeyUp += (_, e) => QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, true, e);
            HeightBox.KeyUp += (_, e) => QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, false, e);

            FilenameCopy.TheButton.Click += (_, _) => Clipboard.SetText(FilenameBox.Text);

            FolderCopy.TheButton.Click += (_, _) => Clipboard.SetText(FolderBox.Text);

            FullpathCopy.TheButton.Click += (_, _) => Clipboard.SetText(FullPathBox.Text);

            CreatedCopy.TheButton.Click += (_, _) => Clipboard.SetText(CreatedBox.Text);

            ModifiedCopy.TheButton.Click += (_, _) => Clipboard.SetText(ModifiedBox.Text);

            WidthCopy.TheButton.Click += (_, _) => Clipboard.SetText(WidthBox.Text);

            HeightCopy.TheButton.Click += (_, _) => Clipboard.SetText(HeightBox.Text);

            SizeMpCopy.TheButton.Click += (_, _) => Clipboard.SetText(SizeMpBox.Text);

            DpiCopy.TheButton.Click += (_, _) => Clipboard.SetText(ResolutionBox.Text);

            PrintSizeCmCopy.TheButton.Click += (_, _) => Clipboard.SetText(PrintSizeCmBox.Text);

            PrintSizeInCopy.TheButton.Click += (_, _) => Clipboard.SetText(PrintSizeInBox.Text);

            AspectRatioCopy.TheButton.Click += (_, _) => Clipboard.SetText(AspectRatioBox.Text);
        }
    }
}