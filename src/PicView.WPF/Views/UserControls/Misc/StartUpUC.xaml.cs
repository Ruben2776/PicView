using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.Animations;
using PicView.WPF.ChangeImage;
using PicView.WPF.FileHandling;
using System.Windows;
using System.Windows.Media;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Misc
{
    // ReSharper disable once InconsistentNaming
    public partial class StartUpUC
    {
        public StartUpUC()
        {
            InitializeComponent();

            Loaded += delegate
            {
                UpdateLanguage();
            };

            if (SettingsHelper.Settings.Theme.Dark)
            {
                SelectFile.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(FolderIconBrush1);
                    ButtonMouseOverAnim(FolderIconBrush2);
                    ButtonMouseOverAnim(SelectFileBrush);
                };

                SelectFile.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(FolderIconBrush1);
                    ButtonMouseLeaveAnim(FolderIconBrush2);
                    ButtonMouseLeaveAnim(SelectFileBrush);
                };

                OpenLastFileButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(OpenLastFileIconBrush1);
                    ButtonMouseOverAnim(OpenLastFileIconBrush2);
                    ButtonMouseOverAnim(OpenLastFileBrush);
                };

                OpenLastFileButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(OpenLastFileIconBrush1);
                    ButtonMouseLeaveAnim(OpenLastFileIconBrush2);
                    ButtonMouseLeaveAnim(OpenLastFileBrush);
                };

                PasteButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(PasteIconBrush);
                    ButtonMouseOverAnim(PasteBrush);
                };

                PasteButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(PasteIconBrush);
                    ButtonMouseLeaveAnim(PasteBrush);
                };
            }
            else
            {
                SelectFileBrush.Color = Colors.White;
                OpenLastFileBrush.Color = Colors.White;
                PasteBrush.Color = Colors.White;

                FolderIconBrush1.Color = Colors.White;
                FolderIconBrush2.Color = Colors.White;
                OpenLastFileIconBrush1.Color = Colors.White;
                OpenLastFileIconBrush2.Color = Colors.White;
                PasteIconBrush.Color = Colors.White;

                SelectFile.MouseEnter += delegate
                {
                    AnimationHelper.ColorAnimation.From = Colors.White;
                    AnimationHelper.ColorAnimation.To = AnimationHelper.GetPreferredColor();
                    FolderIconBrush1.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                    FolderIconBrush2.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                    SelectFileBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                };

                SelectFile.MouseLeave += delegate
                {
                    AnimationHelper.ColorAnimation.From = AnimationHelper.GetPreferredColor();
                    AnimationHelper.ColorAnimation.To = Colors.White;
                    FolderIconBrush1.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                    FolderIconBrush2.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                    SelectFileBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                };

                OpenLastFileButton.MouseEnter += delegate
                {
                    AnimationHelper.ColorAnimation.From = Colors.White;
                    AnimationHelper.ColorAnimation.To = AnimationHelper.GetPreferredColor();
                    OpenLastFileIconBrush1.BeginAnimation(SolidColorBrush.ColorProperty,
                        AnimationHelper.ColorAnimation);
                    OpenLastFileIconBrush2.BeginAnimation(SolidColorBrush.ColorProperty,
                        AnimationHelper.ColorAnimation);
                    OpenLastFileBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                };

                OpenLastFileButton.MouseLeave += delegate
                {
                    AnimationHelper.ColorAnimation.From = AnimationHelper.GetPreferredColor();
                    AnimationHelper.ColorAnimation.To = Colors.White;
                    OpenLastFileIconBrush1.BeginAnimation(SolidColorBrush.ColorProperty,
                        AnimationHelper.ColorAnimation);
                    OpenLastFileIconBrush2.BeginAnimation(SolidColorBrush.ColorProperty,
                        AnimationHelper.ColorAnimation);
                    OpenLastFileBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                };

                PasteButton.MouseEnter += delegate
                {
                    AnimationHelper.ColorAnimation.From = Colors.White;
                    AnimationHelper.ColorAnimation.To = AnimationHelper.GetPreferredColor();
                    PasteIconBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                    PasteBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                };

                PasteButton.MouseLeave += delegate
                {
                    AnimationHelper.ColorAnimation.From = AnimationHelper.GetPreferredColor();
                    AnimationHelper.ColorAnimation.To = Colors.White;
                    PasteIconBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                    PasteBrush.BeginAnimation(SolidColorBrush.ColorProperty, AnimationHelper.ColorAnimation);
                };
            }

            SelectFile.Click += async (_, _) => await OpenSave.OpenAsync().ConfigureAwait(false);

            OpenLastFileButton.Click += async (_, _) =>
                await FileHistoryNavigation.OpenLastFileAsync().ConfigureAwait(false);

            PasteButton.Click += async (_, _) => await CopyPaste.PasteAsync().ConfigureAwait(false);
        }

        public void ToggleMenu()
        {
            buttons.Visibility = buttons.IsVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ResponsiveSize(double width)
        {
            const int bottomMargin = 16;
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

        internal void UpdateLanguage()
        {
            OpenLastFileLabel.Content = TranslationHelper.GetTranslation("OpenLastFile");
            OpenFileDialogLabel.Content = TranslationHelper.GetTranslation("OpenFileDialog");
            FilePasteLabel.Content = TranslationHelper.GetTranslation("FilePaste");
            ErrorHandling.ResetTitle();
        }
    }
}