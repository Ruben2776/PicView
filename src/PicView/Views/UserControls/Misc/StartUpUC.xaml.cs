using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.FileHandling;
using PicView.Properties;
using PicView.UILogic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Misc;

// ReSharper disable once InconsistentNaming
public partial class StartUpUC
{
    public StartUpUC()
    {
        InitializeComponent();

        SelectFile.MouseEnter += delegate
        {
            ButtonMouseOverAnim(folderBrush1);
            ButtonMouseOverAnim(folderBrush2);
            ButtonMouseOverAnim(selectBrush);
        };

        SelectFile.MouseLeave += delegate
        {
            ButtonMouseLeaveAnim(folderBrush1);
            ButtonMouseLeaveAnim(folderBrush2);
            ButtonMouseLeaveAnim(selectBrush);
        };

        SelectFile.Click += async (_, _) => await OpenSave.OpenAsync().ConfigureAwait(false);

        OpenLastFileButton.MouseEnter += delegate
        {
            ButtonMouseOverAnim(lastBrush1);
            ButtonMouseOverAnim(lastBrush2);
            ButtonMouseOverAnim(lastBrush);
        };

        OpenLastFileButton.MouseLeave += delegate
        {
            ButtonMouseLeaveAnim(lastBrush1);
            ButtonMouseLeaveAnim(lastBrush2);
            ButtonMouseLeaveAnim(lastBrush);
        };

        OpenLastFileButton.Click += async (_, _) => await Navigation.GetFileHistory.OpenLastFileAsync().ConfigureAwait(false);

        PasteButton.MouseEnter += delegate
        {
            ButtonMouseOverAnim(pasteBrush);
            ButtonMouseOverAnim(pasteTxt);
        };

        PasteButton.MouseLeave += delegate
        {
            ButtonMouseLeaveAnim(pasteBrush);
            ButtonMouseLeaveAnim(pasteTxt);
        };

        PasteButton.Click += async (_, _) => await CopyPaste.PasteAsync().ConfigureAwait(false);

        if (Settings.Default.DarkTheme == false)
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
        }
    }

    public void ToggleMenu()
    {
        buttons.Visibility = buttons.IsVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ResponsiveSize(double width)
    {
        var bottomMargin = Settings.Default.IsBottomGalleryShown && UC.GetPicGallery is not null ? UC.GetPicGallery.Height + 14 : 16;
        switch (width)
        {
            case < 1265:
                Logo.Width = 350;
                buttons.Margin = new Thickness(0, 0, 0, bottomMargin);
                buttons.VerticalAlignment = VerticalAlignment.Bottom;
                break;

            case > 1265:
                Logo.Width = double.NaN;
                buttons.Margin = new Thickness(0, 220, 25, bottomMargin - 100);
                buttons.VerticalAlignment = VerticalAlignment.Center;
                break;
        }
    }
}