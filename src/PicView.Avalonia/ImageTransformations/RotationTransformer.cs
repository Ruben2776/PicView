using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ImageTransformations;
using PicView.Core.Localization;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

namespace PicView.Avalonia.ImageTransformations;

public class RotationTransformer(LayoutTransformControl imageLayoutTransformControl, PicBox mainImage,
    MainWindowViewModel vm, MainWindow mainWindow)
{
    public void Rotate(bool clockWise)
    {
        if (mainImage.Source is null)
        {
            return;
        }

        var currentAngle = vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue;
        if (RotationHelper.IsValidRotation(currentAngle))
        {
            var nextAngle = RotationHelper.Rotate(currentAngle, clockWise);
            var validAngle = nextAngle switch
            {
                360 => 0,
                -90 => 270,
                _ => nextAngle
            };
            Rotate(validAngle);
        }
        else
        {
            Rotate(RotationHelper.NextRotationAngle(currentAngle, true));
        }
    }

    public void Rotate(int angle)
    {
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.RotationAngle.Value = angle;
        
        WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);

        if (OperatingSystem.IsMacOS() && Settings.WindowProperties.Fullscreen)
        {
            // Sometimes the window is off-center after rotating on macOS fullscreen view
            WindowFunctions.CenterWindowOnScreen();
        }

        switch (angle)
        {
            case 0:
                tab.IsRotated0.Value = true;
                tab.IsRotated90.Value = false;
                tab.IsRotated180.Value = false;
                tab.IsRotated270.Value = false;
                break;
            case 90:
                tab.IsRotated0.Value = false;
                tab.IsRotated90.Value = true;
                tab.IsRotated180.Value = false;
                tab.IsRotated270.Value = false;
                break;
            case 180:
                tab.IsRotated0.Value = false;
                tab.IsRotated90.Value = false;
                tab.IsRotated180.Value = true;
                tab.IsRotated270.Value = false;
                break;
            case 270:
                tab.IsRotated0.Value = false;
                tab.IsRotated90.Value = false;
                tab.IsRotated180.Value = false;
                tab.IsRotated270.Value = true;
                break;
        }
    }

    private ScaleTransform? _scaleTransform;
    public void Flip(bool animate)
    {
        if (mainImage.Source is null)
        {
            return;
        }
        
        _scaleTransform ??= new ScaleTransform();

        var prevScaleX = vm.WindowTabs.ActiveTab.CurrentValue.ScaleX.CurrentValue;
        var newScaleX = prevScaleX == -1 ? 1 : -1;

        if (animate)
        {
            _scaleTransform.Transitions ??=
            [
                new DoubleTransition
                {
                    Property = ScaleTransform.ScaleXProperty,
                    Duration = TimeSpan.FromSeconds(.2)
                }
            ];
        }
        else
        {
            _scaleTransform.Transitions = null;
        }
        imageLayoutTransformControl.RenderTransform = _scaleTransform;
        _scaleTransform.ScaleX = newScaleX;
        vm.WindowTabs.ActiveTab.CurrentValue.ScaleX.Value = newScaleX;
        vm.Translation.IsFlipped.Value =
            newScaleX is -1 ? TranslationManager.Translation.Flip : TranslationManager.Translation.Unflip;
    }
    
    public void ResetFlip()
    {
        _scaleTransform?.ScaleX = 1;
        vm.WindowTabs.ActiveTab.CurrentValue.ScaleX.Value = 1;
        vm.Translation.IsFlipped.Value = TranslationManager.Translation.Unflip;
    }
}