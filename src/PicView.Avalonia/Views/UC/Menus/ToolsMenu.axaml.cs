using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Conversion;
using R3;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class ToolsMenu : AnimatedMenu
{
    public ToolsMenu()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
            {
                if (!Settings.Theme.GlassTheme)
                {
                    TopBorder.Background = Brushes.White;
                }
                else
                {
                    if (Application.Current.TryGetResource("NoisyTexture",
                            ThemeVariant.Dark, out var texture))
                    {
                        var brush = texture as ImageBrush;
                        MainBorder.Background = brush;
                        DownArrow.Fill = brush;
                        DownArrow.StrokeThickness = 0;
                    }
                }

                // Batch
                BatchResizeButton.Classes.Remove("noBorderHover");
                BatchResizeButton.Classes.Add("noBorderHoverAlt");
                if (TryGetResource("BatchBrush", Application.Current.RequestedThemeVariant,
                        out var batchBrush))
                {
                    if (batchBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(BatchResizeButton, brush);
                    }
                }

                // Effects
                EffectsButton.Classes.Remove("noBorderHover");
                EffectsButton.Classes.Add("noBorderHoverAlt");
                if (TryGetResource("EffectsBrush", Application.Current.RequestedThemeVariant,
                        out var effectsBrush))
                {
                    if (effectsBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(EffectsButton, brush);
                    }
                }

                // Image info
                ImageInfoButton.Classes.Remove("altHover");
                ImageInfoButton.Classes.Add("hover");
                if (TryGetResource("ImageInfoBrush", Application.Current.RequestedThemeVariant,
                        out var imageInfoBrush))
                {
                    if (imageInfoBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(ImageInfoButton, brush);
                    }
                }

                // Optimize
                OptimizeImageButton.Classes.Remove("altHover");
                OptimizeImageButton.Classes.Add("hover");
                if (TryGetResource("OptimizeBrush", Application.Current.RequestedThemeVariant,
                        out var optimizeBrush))
                {
                    if (optimizeBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(OptimizeImageButton, brush);
                    }
                }

                // Change bg
                ChangeBgButton.Classes.Remove("altHover");
                ChangeBgButton.Classes.Add("hover");
                if (TryGetResource("ChangeBgBrush", Application.Current.RequestedThemeVariant,
                        out var changeBgBrush))
                {
                    if (changeBgBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(ChangeBgButton, brush);
                    }
                }

                // Keybindings
                KeybindingsButton.Classes.Remove("altHover");
                KeybindingsButton.Classes.Add("hover");
                if (TryGetResource("KeybindingBrush", Application.Current.RequestedThemeVariant,
                        out var keybindingBrush) && Application.Current.TryGetResource("ShortcutsImageAlt",
                        Application.Current.RequestedThemeVariant,
                        out var shortcutsImageAlt) && Application.Current.TryGetResource("ShortcutsImage",
                        Application.Current.RequestedThemeVariant,
                        out var shortcutsImage))
                {
                    if (keybindingBrush is SolidColorBrush brush && shortcutsImageAlt is DrawingImage imgAlt &&
                        shortcutsImage is DrawingImage img)
                    {
                        UIHelper.SetButtonHover(KeybindingsButton, brush);
                        KeybindingsButton.PointerEntered += (_, _) => { KeybindingsButton.Icon = imgAlt; };
                        KeybindingsButton.PointerExited += (_, _) => { KeybindingsButton.Icon = img; };
                    }
                }

                InterfaceButton.Classes.Remove("altHover");
                InterfaceButton.Classes.Add("hover");
                if (TryGetResource("InterfaceBrush", Application.Current.RequestedThemeVariant,
                        out var interfaceBrush))
                {
                    if (interfaceBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(InterfaceButton, brush);
                    }
                }
            }
            
            this.GetObservable(IsOpenProperty).ToObservable()
                .Where(x => x)
                .Subscribe(_ => { DetermineIfOptimizeImageShouldBeEnabled(); });
        };
    }

    private void DetermineIfOptimizeImageShouldBeEnabled()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.PicViewer.ShouldOptimizeImageBeEnabled.Value = ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(vm.PicViewer.FileInfo?.CurrentValue);
    }
}