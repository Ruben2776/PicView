using System.Windows.Controls;
using static PicView.MouseOverAnimations;

namespace PicView.UserControls
{
    /// <summary>
    /// Menu to open functions
    /// </summary>
    public partial class ToolsAndEffectsMenu : UserControl
    {
        public ToolsAndEffectsMenu()
        {
            InitializeComponent();

            ResizeButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ResizeButtonBrush);
            ResizeButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ResizeButtonBrush, true);
            ResizeButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ResizeButtonBrush, false);
            ResizeButton.Click += delegate 
            { 
                UC.Close_UserControls();
                LoadWindows.ResizeAndOptimizeWindow();
                Batch_Resize.UpdateValues();
            };

            EffectsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(EffectsButtonBrush);
            EffectsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(EffectsButtonBrush, true);
            EffectsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(EffectsButtonBrush, false);
            EffectsButton.Click += delegate
            {
                UC.Close_UserControls();
                LoadWindows.EffectsWindow();
            };

        }
    }
}
