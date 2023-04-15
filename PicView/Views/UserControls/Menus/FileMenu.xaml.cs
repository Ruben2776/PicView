using System.Windows.Controls;
using System.Windows.Media;
using PicView.Animations;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Menus
{
    /// <summary>
    /// Interaction logic for fileMenu.xaml
    /// </summary>
    public partial class FileMenu : UserControl
    {
        public FileMenu()
        {
            InitializeComponent();

            PasteButton.PreviewMouseLeftButtonDown += delegate { UC.Close_UserControls(); };

            // OpenBorder
            SetIconButterMouseOverAnimations(OpenBorder, OpenBorderBrush, (SolidColorBrush)Resources["OpenBorderFill"]);
            OpenBorder.MouseLeftButtonDown += async (_, _) => await Open_Save.OpenAsync().ConfigureAwait(false);
            OpenButton.Click += async (_, _) => await Open_Save.OpenAsync().ConfigureAwait(false);

            // SaveButton
            SetIconButterMouseOverAnimations(SaveButton, SaveButtonBrush, SaveButtonIconBrush);
            SaveButton.Click += async (_, _) => await Open_Save.SaveFilesAsync().ConfigureAwait(false);

            // CopyButton
            SetIconButterMouseOverAnimations(CopyButton, CopyButtonBrush, CopyButtonIconBrush);
            CopyButton.Click += (_, _) => Copy_Paste.Copy();

            // PasteButton
            SetIconButterMouseOverAnimations(PasteButton, PasteButtonBrush, PasteButtonIconBrush);
            PasteButton.Click += async (_, _) => await Copy_Paste.PasteAsync().ConfigureAwait(false);

            // FileLocationBorder
            SetIconButterMouseOverAnimations(FileLocationBorder, FileLocationBrush, (SolidColorBrush)Resources["LocationBorderFill"]);
            FileLocationBorder.MouseLeftButtonDown += (_, _) => Open_Save.Open_In_Explorer();
            FileLocationButton.Click += (_, _) => Open_Save.Open_In_Explorer();

            // PrintBorder
            SetIconButterMouseOverAnimations(PrintBorder, PrintButtonBrush, (SolidColorBrush)Resources["PrintBorderFill"]);
            PrintBorder.MouseLeftButtonDown += (_, _) => Open_Save.Print(Navigation.Pics?[Navigation.FolderIndex]);
            PrintButton.Click += (_, _) => Open_Save.Print(Navigation.Pics?[Navigation.FolderIndex]);

            // ReloadButton
            SetIconButterMouseOverAnimations(ReloadButton, ReloadButtonBrush, (SolidColorBrush)Resources["ReloadButtonIconBrush"]);
            ReloadButton.Click += async (_, _) => await ErrorHandling.ReloadAsync().ConfigureAwait(false);

            // RecycleButton
            SetIconButterMouseOverAnimations(RecycleButton, RecycleButtonBrush, (SolidColorBrush)Resources["RecycleButtonIconBrush"]);
            RecycleButton.Click += async (_, _) => await DeleteFiles.DeleteFileAsync(true).ConfigureAwait(false);

            // OpenWithBorder
            SetIconButterMouseOverAnimations(OpenWithBorder, OpenWithBorderBrush, (SolidColorBrush)Resources["OpenWithBorderFill"]);
            OpenWithBorder.MouseLeftButtonDown += (_, _) => Open_Save.OpenWith();
            OpenWith.Click += (_, _) => Open_Save.OpenWith();

            // RenameBorder
            SetIconButterMouseOverAnimations(RenameBorder, RenameButtonBrush, (SolidColorBrush)Resources["RenameBorderFill"]);
            RenameBorder.MouseLeftButtonDown += (_, _) => { UC.Close_UserControls(); EditTitleBar.EditTitleBar_Text(); };
            RenameButton.Click += (_, _) => { UC.Close_UserControls(); EditTitleBar.EditTitleBar_Text(); };
        }
    }
}