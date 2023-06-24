using PicView.ConfigureSettings;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace PicView.Views.UserControls.Misc;

public partial class Icon
{
    public Icon()
    {
        InitializeComponent();
    }

    public void ChangeColor()
    {
        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, () =>
        {
            try
            {
                BaseBrush.Brush = new SolidColorBrush(ConfigColors.GetSecondaryAccentColor);
            }
            catch (Exception)
            {
                // Unhandled Exception: System.Reflection.TargetParameterCountException: Parameter count mismatch
            }
        });
    }
}