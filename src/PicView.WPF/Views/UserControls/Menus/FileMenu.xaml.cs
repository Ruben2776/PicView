using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.FileHandling;
using PicView.Core.Config;
using PicView.WPF.UILogic;
using System.Windows.Media;
using PicView.Core.Localization;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Menus;

public partial class FileMenu
{
    public FileMenu()
    {
        InitializeComponent();

        // OpenBorder
        SetButtonIconMouseOverAnimations(OpenBorder, OpenBorderBrush, (SolidColorBrush)Resources["OpenBorderFill"]);
        OpenBorder.MouseLeftButtonDown += async (_, _) => await OpenSave.OpenAsync().ConfigureAwait(false);
        OpenButton.Click += async (_, _) => await OpenSave.OpenAsync().ConfigureAwait(false);
        OpenBorder.ToolTip = TranslationHelper.GetTranslation("OpenFileDialog");
        OpenButtonTextBlock.Text = TranslationHelper.GetTranslation("Open");

        // SaveButton
        SetButtonIconMouseOverAnimations(SaveButton, SaveButtonBrush, SaveButtonIconBrush);
        SaveButton.Click += async (_, _) =>
            await OpenSave.SaveFilesAsync(SettingsHelper.Settings.UIProperties.ShowFileSavingDialog).ConfigureAwait(false);
        SaveButton.ToolTip = TranslationHelper.GetTranslation("Save");

        // CopyButton
        SetButtonIconMouseOverAnimations(CopyButton, CopyButtonBrush, CopyButtonIconBrush);
        CopyButton.Click += (_, _) => CopyPaste.Copy();
        CopyButton.ToolTip = TranslationHelper.GetTranslation("Copy");

        // PasteButton
        SetButtonIconMouseOverAnimations(PasteButton, PasteButtonBrush, PasteButtonIconBrush);
        PasteButton.Click += async (_, _) => await CopyPaste.PasteAsync().ConfigureAwait(false);
        PasteButton.PreviewMouseLeftButtonDown += delegate { UC.Close_UserControls(); };
        PasteButton.ToolTip = TranslationHelper.GetTranslation("FilePaste");

        // FileLocationBorder
        SetButtonIconMouseOverAnimations(FileLocationBorder, FileLocationBrush,
            (SolidColorBrush)Resources["LocationBorderFill"]);
        FileLocationBorder.MouseLeftButtonDown +=
            (_, _) => OpenSave.OpenInExplorer(Navigation.Pics?[Navigation.FolderIndex]);
        FileLocationButton.Click += (_, _) => OpenSave.OpenInExplorer(Navigation.Pics?[Navigation.FolderIndex]);
        FileLocationBorder.ToolTip = TranslationHelper.GetTranslation("ShowInFolder");
        FileLocationTextBlock.Text = TranslationHelper.GetTranslation("ShowInFolder");

        // PrintBorder
        SetButtonIconMouseOverAnimations(PrintBorder, PrintButtonBrush,
            (SolidColorBrush)Resources["PrintBorderFill"]);
        PrintBorder.MouseLeftButtonDown += (_, _) => OpenSave.Print(Navigation.Pics?[Navigation.FolderIndex]);
        PrintButton.Click += (_, _) => OpenSave.Print(Navigation.Pics?[Navigation.FolderIndex]);
        PrintBorder.ToolTip = TranslationHelper.GetTranslation("Print");
        PrintButtonTextBlock.Text = TranslationHelper.GetTranslation("Print");

        // ReloadButton
        SetButtonIconMouseOverAnimations(ReloadButton, ReloadButtonBrush,
            (SolidColorBrush)Resources["ReloadButtonIconBrush"]);
        ReloadButton.Click += async (_, _) => await ErrorHandling.ReloadAsync().ConfigureAwait(false);
        ReloadButton.ToolTip = TranslationHelper.GetTranslation("Reload");
        ReloadText.Text = TranslationHelper.GetTranslation("Reload");

        // RecycleButton
        SetButtonIconMouseOverAnimations(RecycleButton, RecycleButtonBrush,
            (SolidColorBrush)Resources["RecycleButtonIconBrush"]);
        RecycleButton.Click += async (_, _) =>
            await DeleteFiles.DeleteCurrentFileAsync(true).ConfigureAwait(false);
        RecycleBorder.ToolTip = TranslationHelper.GetTranslation("SendCurrentImageToRecycleBin");
        RecycleButtonTextBlock.Text = TranslationHelper.GetTranslation("DeleteFile");

        // OpenWithBorder
        SetButtonIconMouseOverAnimations(OpenWithBorder, OpenWithBorderBrush,
            (SolidColorBrush)Resources["OpenWithBorderFill"]);
        OpenWithBorder.MouseLeftButtonDown += (_, _) => OpenSave.OpenWith();
        OpenWith.Click += (_, _) => OpenSave.OpenWith();
        OpenWithBorder.ToolTip = TranslationHelper.GetTranslation("OpenWith");
        OpenWithTextBlock.Text = TranslationHelper.GetTranslation("OpenWith");

        // RenameBorder
        SetButtonIconMouseOverAnimations(RenameBorder, RenameButtonBrush,
            (SolidColorBrush)Resources["RenameBorderFill"]);
        RenameBorder.MouseLeftButtonDown += (_, _) =>
        {
            UC.Close_UserControls();
            EditTitleBar.EditTitleBar_Text();
        };
        RenameButton.Click += (_, _) =>
        {
            UC.Close_UserControls();
            EditTitleBar.EditTitleBar_Text();
        };
        RenameBorder.ToolTip = TranslationHelper.GetTranslation("RenameFile");
        RenameButtonTextBlock.Text = TranslationHelper.GetTranslation("RenameFile");
    }
}