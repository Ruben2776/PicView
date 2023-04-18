using System.Windows.Controls;
using System.Windows.Media;
using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Editing.Crop;
using PicView.Properties;
using PicView.UILogic;
using static PicView.Animations.MouseOverAnimations;
using static PicView.UILogic.UC;

namespace PicView.Views.UserControls.Menus
{
    /// <summary>
    /// Interaction logic for QuickSettingsMenu.xaml
    /// </summary>
    public partial class QuickSettingsMenu : UserControl
    {
        public QuickSettingsMenu()
        {
            InitializeComponent();

            SettingsButton.TheButton.Click += delegate
            {
                ConfigureWindows.AllSettingsWindow();
                Close_UserControls();
            };

            // Toggle Scroll
            ToggleScroll.IsChecked = Settings.Default.ScrollEnabled;
            ToggleScroll.Click += (s, x) => UpdateUIValues.SetScrolling(Settings.Default.ScrollEnabled);
            ToggleScrollBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetScrolling(Settings.Default.ScrollEnabled);
            SetButtonIconMouseOverAnimations(ToggleScrollBorder, ToggleScrollBrush, ToggleScrollFill);

            // Toggle Fill
            ToggleFill.IsChecked = Settings.Default.FillImage;
            ToggleFill.Click += UpdateUIValues.SetAutoFill;
            ToggleFillBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFill;
            SetButtonIconMouseOverAnimations(ToggleFillBorder, ToggleFillBrush, ToggleFillFill);

            // Toggle Looping
            ToggleLooping.IsChecked = Settings.Default.Looping;
            ToggleLooping.Click += (_, _) => UpdateUIValues.SetLooping();
            ToggleLoopingBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetLooping();
            SetButtonIconMouseOverAnimations(ToggleLoopingBorder, ToggleLoopBrush, ToggleLoopFill);

            // Set Fit
            SetFit.IsChecked = Settings.Default.AutoFitWindow;
            SetFit.Click += UpdateUIValues.SetAutoFit;
            SetFitBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFit;
            SetButtonIconMouseOverAnimations(SetFitBorder, SetFitBrush, SetFitFill);
            
            // Stay on top
            StayOnTop.IsChecked = Settings.Default.TopMost;
            StayOnTop.Click += (_,_) => UpdateUIValues.SetTopMost();
            StayOnTopBorder.MouseLeftButtonDown += (_,_) => UpdateUIValues.SetTopMost();
            SetButtonIconMouseOverAnimations(StayOnTopBorder, StayOnTopBrush, StayOnTopFill);
        }
    }
}