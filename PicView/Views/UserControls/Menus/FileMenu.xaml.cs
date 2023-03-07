using PicView.Animations;
using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UILogic;
using System.Windows.Controls;
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
            OpenBorder.MouseEnter += delegate { ButtonMouseOverAnim(OpenBorderFill); };
            OpenBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(OpenBorderBrush); };
            OpenBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(OpenBorderFill); };
            OpenBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(OpenBorderBrush); };

            // PrintBorder
            PrintBorder.MouseEnter += delegate { ButtonMouseOverAnim(PrintFill); };
            PrintBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(PrintBrush); };
            PrintBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(PrintFill); };
            PrintBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(PrintBrush); };

            // SaveBorder
            SaveBorder.MouseEnter += delegate { ButtonMouseOverAnim(SaveFill); };
            SaveBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SaveBrush); };
            SaveBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(SaveFill); };
            SaveBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SaveBrush); };

            // FileLocationBorder
            FileLocationBorder.MouseEnter += delegate { ButtonMouseOverAnim(FileLocationFill); };
            FileLocationBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(FileLocationBrush); };
            FileLocationBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FileLocationFill); };
            FileLocationBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(FileLocationBrush); };

            // CopyButton
            CopyButton.TheButton.Click += delegate
            {
                UC.Close_UserControls();
                Copy_Paste.CopyFile();
            };

            Open.Click += async (_, _) => await Open_Save.OpenAsync().ConfigureAwait(false);
            FileLocation.Click += (_, _) => Open_Save.Open_In_Explorer();
            Print.Click += (_, _) => Open_Save.Print(Navigation.Pics?[Navigation.FolderIndex]);
            SaveButton.Click += async (sender, e) => await Open_Save.SaveFilesAsync();

            OpenBorder.MouseLeftButtonUp += async (_, _) => await Open_Save.OpenAsync().ConfigureAwait(false);
            FileLocationBorder.MouseLeftButtonUp += (_, _) => Open_Save.Open_In_Explorer();
            PrintBorder.MouseLeftButtonUp += (_, _) => Open_Save.Print(Navigation.Pics?[Navigation.FolderIndex]);
            SaveBorder.MouseLeftButtonUp += async (sender, e) => await Open_Save.SaveFilesAsync();
        }
    }
}