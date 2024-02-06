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

        // SaveButton
        SetButtonIconMouseOverAnimations(SaveButton, SaveButtonBrush, SaveButtonIconBrush);
        SaveButton.Click += async (_, _) =>
            await OpenSave.SaveFilesAsync(SettingsHelper.Settings.UIProperties.ShowFileSavingDialog).ConfigureAwait(false);

        // CopyButton
        SetButtonIconMouseOverAnimations(CopyButton, CopyButtonBrush, CopyButtonIconBrush);
        CopyButton.Click += (_, _) => CopyPaste.Copy();

        // PasteButton
        SetButtonIconMouseOverAnimations(PasteButton, PasteButtonBrush, PasteButtonIconBrush);
        PasteButton.Click += async (_, _) => await CopyPaste.PasteAsync().ConfigureAwait(false);
        PasteButton.PreviewMouseLeftButtonDown += delegate { UC.Close_UserControls(); };

        // FileLocationBorder
        SetButtonIconMouseOverAnimations(FileLocationBorder, FileLocationBrush,
            (SolidColorBrush)Resources["LocationBorderFill"]);
        FileLocationBorder.MouseLeftButtonDown +=
            (_, _) => OpenSave.OpenInExplorer(Navigation.Pics?[Navigation.FolderIndex]);
        FileLocationButton.Click += (_, _) => OpenSave.OpenInExplorer(Navigation.Pics?[Navigation.FolderIndex]);

        // PrintBorder
        SetButtonIconMouseOverAnimations(PrintBorder, PrintButtonBrush,
            (SolidColorBrush)Resources["PrintBorderFill"]);
        PrintBorder.MouseLeftButtonDown += (_, _) => OpenSave.Print(Navigation.Pics?[Navigation.FolderIndex]);
        PrintButton.Click += (_, _) => OpenSave.Print(Navigation.Pics?[Navigation.FolderIndex]);

        // ReloadButton
        SetButtonIconMouseOverAnimations(ReloadButton, ReloadButtonBrush,
            (SolidColorBrush)Resources["ReloadButtonIconBrush"]);
        ReloadButton.Click += async (_, _) => await ErrorHandling.ReloadAsync().ConfigureAwait(false);

        // RecycleButton
        SetButtonIconMouseOverAnimations(RecycleButton, RecycleButtonBrush,
            (SolidColorBrush)Resources["RecycleButtonIconBrush"]);
        RecycleButton.Click += async (_, _) =>
            await DeleteFiles.DeleteCurrentFileAsync(true).ConfigureAwait(false);

        // OpenWithBorder
        SetButtonIconMouseOverAnimations(OpenWithBorder, OpenWithBorderBrush,
            (SolidColorBrush)Resources["OpenWithBorderFill"]);
        OpenWithBorder.MouseLeftButtonDown += (_, _) => OpenSave.OpenWith();
        OpenWith.Click += (_, _) => OpenSave.OpenWith();

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

        UpdateLanguage();
    }

    internal void UpdateLanguage()
    {
        RenameBorder.ToolTip = TranslationHelper.GetTranslation("RenameFile");
        RenameButtonTextBlock.Text = TranslationHelper.GetTranslation("RenameFile");
        OpenWithBorder.ToolTip = TranslationHelper.GetTranslation("OpenWith");
        OpenWithTextBlock.Text = TranslationHelper.GetTranslation("OpenWith");
        RecycleBorder.ToolTip = TranslationHelper.GetTranslation("SendCurrentImageToRecycleBin");
        RecycleButtonTextBlock.Text = TranslationHelper.GetTranslation("DeleteFile");
        PrintBorder.ToolTip = TranslationHelper.GetTranslation("Print");
        PrintButtonTextBlock.Text = TranslationHelper.GetTranslation("Print");
        PasteButton.ToolTip = TranslationHelper.GetTranslation("FilePaste");
        OpenBorder.ToolTip = TranslationHelper.GetTranslation("OpenFileDialog");
        OpenButtonTextBlock.Text = TranslationHelper.GetTranslation("Open");
        ReloadButton.ToolTip = TranslationHelper.GetTranslation("Reload");
        ReloadText.Text = TranslationHelper.GetTranslation("Reload");
        FileLocationBorder.ToolTip = TranslationHelper.GetTranslation("ShowInFolder");
        FileLocationTextBlock.Text = TranslationHelper.GetTranslation("ShowInFolder");
        CopyButton.ToolTip = TranslationHelper.GetTranslation("Copy");
        SaveButton.ToolTip = TranslationHelper.GetTranslation("Save");
    }
}