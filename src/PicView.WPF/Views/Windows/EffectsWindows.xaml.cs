﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PicView.Editing.HlslEffects;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.FileHandling;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Windows.Wallpaper;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;
using static PicView.WPF.Animations.MouseOverAnimations;
using static PicView.WPF.SystemIntegration.Wallpaper;

namespace PicView.WPF.Views.Windows;

public partial class EffectsWindow
{
    public EffectsWindow()
    {
        InitializeComponent();

        MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
        Width *= WindowSizing.MonitorInfo.DpiScaling;
        if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
        {
            WindowSizing.MonitorInfo = MonitorSize.GetMonitorSize(this);
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;
        }

        ContentRendered += Window_ContentRendered;
    }

    private void Window_ContentRendered(object? sender, EventArgs? e)
    {
        WindowBlur.EnableBlur(this);

        UpdateLanguage();

        KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(null, e, this);
        Deactivated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, null, false);
        Activated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, null, true);

        // CloseButton
        CloseButton.TheButton.Click += delegate
        {
            Hide();
            ConfigureWindows.GetMainWindow.Focus();
        };

        // MinButton
        MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

        IntensitySlider.ValueChanged += (_, _) => IntensitySlider_ValueChanged();

        MouseLeftButtonDown += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };

        #region button events

        // Button, Brush and Text for Negative effect
        NegativeButton.Click += (_, _) => Negative();
        SetButtonIconMouseOverAnimations(NegativeButton, NegativeButtonBrush, NegativeColorsText);

        // Button, Brush and Text for Grayscale effect
        GrayscaleButton.Click += (_, _) => GraySceale();
        SetButtonIconMouseOverAnimations(GrayscaleButton, GrayscaleButtonBrush, GrayscaleText);

        // Button, Brush and Text for Color Tone effect
        ColorToneButton.Click += (_, _) => ColorToneEffect();
        SetButtonIconMouseOverAnimations(ColorToneButton, ColorToneButtonBrush, ColorToneText);

        // Button, Brush and Text for Old Movie effect
        OldMovieButton.Click += (_, _) => OldMovieEffect();
        SetButtonIconMouseOverAnimations(OldMovieButton, OldMovieButtonBrush, OldMovieText);

        // Button, Brush and Text for Bloom effect
        BloomButton.Click += (_, _) => Bloom();
        SetButtonIconMouseOverAnimations(BloomButton, BloomButtonBrush, BloomText);

        // Button, Brush and Text for Gloom effect
        GloomButton.Click += (_, _) => Gloom();
        SetButtonIconMouseOverAnimations(GloomButton, GloomButtonBrush, GloomText);

        // Button, Brush and Text for Monochrome effect
        MonochromeButton.Click += (_, _) => Monochrome();
        SetButtonIconMouseOverAnimations(MonochromeButton, MonochromeButtonBrush, MonochromeText);

        // Button, Brush and Text for Wave Warper effect
        WavewarperButton.Click += (_, _) => WaveWarperEffect();
        SetButtonIconMouseOverAnimations(WavewarperButton, WaveWarperButtonBrush, WaveWarperText);

        // Button, Brush and Text for Underwater effect
        UnderwaterButton.Click += (_, _) => UnderWaterEffect();
        SetButtonIconMouseOverAnimations(UnderwaterButton, UnderwaterButtonBrush, UnderwaterText);

        // Button, Brush and Text for Banded Swirl effect
        BandedSwirlButton.Click += (_, _) => BandedSwirlEffect();
        SetButtonIconMouseOverAnimations(BandedSwirlButton, BandedSwirlButtonBrush, BandedSwirlText);

        // Button, Brush and Text for Swirl effect
        SwirlButton.Click += (_, _) => SwirlEffect();
        SetButtonIconMouseOverAnimations(SwirlButton, SwirlButtonBrush, SwirlText);

        // Button, Brush and Text for Ripple effect
        RippleButton.Click += (_, _) => RippleEffect1();
        SetButtonIconMouseOverAnimations(RippleButton, RippleButtonBrush, RippleText);

        // Button, Brush and Text for Ripple Alternate effect
        RippleAltButton.Click += (_, _) => RippleEffect2();
        SetButtonIconMouseOverAnimations(RippleAltButton, RippleAltButtonBrush, RippleAltText);

        // Button, Brush and Text for Blur effect
        BlurButton.Click += (_, _) => BlurEffect();
        SetButtonIconMouseOverAnimations(BlurButton, BlurButtonBrush, BlurText);

        // Button, Brush and Text for Directional Blur effect
        DirectionalBlurButton.Click += (_, _) => Dir_blur();
        SetButtonIconMouseOverAnimations(DirectionalBlurButton, DirectionalBlurButtonBrush, DirectionalBlurText);

        // Button, Brush and Text for Telescopic Blur effect
        TelescopicBlurButton.Click += (_, _) => Teleskopisk_blur();
        SetButtonIconMouseOverAnimations(TelescopicBlurButton, TelescopicBlurButtonBrush, TelescopicBlurText);

        // Button, Brush and Text for Pixelate effect
        PixelateButton.Click += (_, _) => PixelateEffect();
        SetButtonIconMouseOverAnimations(PixelateButton, PixelateButtonBrush, PixelateText);

        // Button, Brush and Text for Embossed effect
        EmbossedButton.Click += (_, _) => Embossed();
        SetButtonIconMouseOverAnimations(EmbossedButton, EmbossedButtonBrush, EmbossedText);

        // Button, Brush and Text for Smooth Magnify effect
        SmoothMagnifyButton.Click += (_, _) => MagnifySmoothEffect();
        SetButtonIconMouseOverAnimations(SmoothMagnifyButton, SmoothMagnifyButtonBrush, SmoothMagnifyText);

        // Button, Brush and Text for Pivot effect
        PivotButton.Click += (_, _) => PivotEffect();
        SetButtonIconMouseOverAnimations(PivotButton, PivotButtonBrush, PivotText);

        // Button, Brush and Text for Paper Fold effect
        PaperfoldButton.Click += (_, _) => PaperFoldEffect();
        SetButtonIconMouseOverAnimations(PaperfoldButton, PaperfoldButtonBrush, PaperFoldText);

        // Button, Brush and Text for Pencil Sketch Stroke effect
        PencilSketchButton.Click += (_, _) => SketchPencilStrokeEffect();
        SetButtonIconMouseOverAnimations(PencilSketchButton, PencilSketchButtonBrush, PencilSketchText);

        // Button, Brush and Text for Sketch effect
        SketchButton.Click += (_, _) => Sketch();
        SetButtonIconMouseOverAnimations(SketchButton, SketchButtonBrush, SketchText);

        // Button, Brush and Text for Tone Mapping effect
        ToneMappingButton.Click += (_, _) => ToneMapping();
        SetButtonIconMouseOverAnimations(ToneMappingButton, ToneMappingButtonBrush, ToneMappingText);

        // Button, Brush and Text for Bands effect
        BandsButton.Click += (_, _) => Bands();
        SetButtonIconMouseOverAnimations(BandsButton, BandsButtonBrush, BandsText);

        // Button, Brush and Text for Glass Tile effect
        GlasTileButton.Click += (_, _) => GlasTileEffect();
        SetButtonIconMouseOverAnimations(GlasTileButton, GlasTileButtonBrush, GlassTileText);

        // Button, Brush and Text for Frosty Outline effect
        FrostyOutlineButton.Click += (_, _) => FrostyOutlineEffect();
        SetButtonIconMouseOverAnimations(FrostyOutlineButton, FrostyOutlineButtonBrush, FrostyOutlineText);

        // Button, Brush and Text for SetAsWallpaper function
        SetButtonIconMouseOverAnimations(SetAsWallpaperButton, SetAsWallpaperButtonBrush, SetAsWallpaperText);
        SetAsWallpaperButton.Click += async delegate
        {
            var x = WallpaperHelper.WallpaperStyle.Fill;
            if (Fit.IsSelected)
            {
                x = WallpaperHelper.WallpaperStyle.Fit;
            }

            if (Center.IsSelected)
            {
                x = WallpaperHelper.WallpaperStyle.Center;
            }

            if (Tile.IsSelected)
            {
                x = WallpaperHelper.WallpaperStyle.Tile;
            }

            if (Fit.IsSelected)
            {
                x = WallpaperHelper.WallpaperStyle.Fit;
            }

            await SetWallpaperAsync(x).ConfigureAwait(false);
        };

        // Button, Brush and Text for SetAsLockscreen function
        SetButtonIconMouseOverAnimations(SetAsLockScreenButton, SetAsLockscreenButtonBrush, SetAsLockscreenText);
        SetAsLockScreenButton.Click += async (_, _) =>
            await LockScreenHelper.SetLockScreenImageAsync().ConfigureAwait(false);

        // Button, Brush and Text for Copy function
        SetButtonIconMouseOverAnimations(CopyButton, CopyBrush, CopyText);
        CopyButton.Click += (_, _) => CopyPaste.CopyBitmap();

        // Button, Brush and Text for Save function
        SetButtonIconMouseOverAnimations(SaveButton, SaveBrush, SaveText);
        SaveButton.Click += async (_, _) => await OpenSave.SaveFilesAsync(SettingsHelper.Settings.UIProperties.ShowFileSavingDialog);

        #endregion button events
    }

    public void UpdateLanguage()
    {
        Title = TranslationHelper.GetTranslation("Effects") + " - PicView";
        TitleText.Text = TranslationHelper.GetTranslation("Effects");
        CopyButton.Content = TranslationHelper.GetTranslation("Copy");
        CopyButton.ToolTip = TranslationHelper.GetTranslation("CopyImageTooltip");
        SaveButton.Content = TranslationHelper.GetTranslation("Save");
        Fill.Content = TranslationHelper.GetTranslation("Fill");
        Center.Content = TranslationHelper.GetTranslation("Center");
        Fit.Content = TranslationHelper.GetTranslation("Fit");
        Tile.Content = TranslationHelper.GetTranslation("Tile");
        Stretch.Content = TranslationHelper.GetTranslation("Stretch");
        SetAsWallpaperButton.Content = TranslationHelper.GetTranslation("SetAsWallpaper");
        SetAsLockScreenButton.Content = TranslationHelper.GetTranslation("SetAsLockScreenImage");
        NegativeColors.Text = TranslationHelper.GetTranslation("NegativeColors");
        BlackAndWhite.Text = TranslationHelper.GetTranslation("BlackAndWhite");
        ColorTone.Text = TranslationHelper.GetTranslation("ColorTone");
        OldMovie.Text = TranslationHelper.GetTranslation("OldMovie");
        BloomTxt.Text = TranslationHelper.GetTranslation("Bloom");
        GloomTxt.Text = TranslationHelper.GetTranslation("Gloom");
        MonochromeTxt.Text = TranslationHelper.GetTranslation("Monochrome");
        WaveWarper.Text = TranslationHelper.GetTranslation("WaveWarper");
        Underwater.Text = TranslationHelper.GetTranslation("Underwater");
        BandedSwirl.Text = TranslationHelper.GetTranslation("BandedSwirl");
        Swirl.Text = TranslationHelper.GetTranslation("Swirl");
        Ripple.Text = TranslationHelper.GetTranslation("Ripple");
        RippleAlt.Text = TranslationHelper.GetTranslation("RippleAlt");
        Blur.Text = TranslationHelper.GetTranslation("Blur");
        DirectionalBlur.Text = TranslationHelper.GetTranslation("DirectionalBlur");
        TelescopicBlur.Text = TranslationHelper.GetTranslation("TelescopicBlur");
        Pixelate.Text = TranslationHelper.GetTranslation("Pixelate");
        EmbossedTxt.Text = TranslationHelper.GetTranslation("Embossed");
        SmoothMagnify.Text = TranslationHelper.GetTranslation("SmoothMagnify");
        Pivot.Text = TranslationHelper.GetTranslation("Pivot");
        PaperFold.Text = TranslationHelper.GetTranslation("PaperFold");
        PencilSketch.Text = TranslationHelper.GetTranslation("PencilSketch");
        SketchTxt.Text = TranslationHelper.GetTranslation("Sketch");
        ToneMappingTxt.Text = TranslationHelper.GetTranslation("ToneMapping");
        FrostyOutline.Text = TranslationHelper.GetTranslation("FrostyOutline");
        BandsTxt.Text = TranslationHelper.GetTranslation("Bands");
        GlassTile.Text = TranslationHelper.GetTranslation("GlassTile");
    }

    private void IntensitySlider_ValueChanged()
    {
        if (PixelateButton.IsChecked.HasValue && PixelateButton.IsChecked.Value)
        {
            PixelateEffect();
        }
        else if (ColorToneButton.IsChecked.HasValue && ColorToneButton.IsChecked.Value)
        {
            ColorToneEffect();
        }
        else if (RippleAltButton.IsChecked.HasValue && RippleAltButton.IsChecked.Value)
        {
            RippleEffect2();
        }
        else if (BandedSwirlButton.IsChecked.HasValue && BandedSwirlButton.IsChecked.Value)
        {
            BandedSwirlEffect();
        }
        else if (DirectionalBlurButton.IsChecked.HasValue && DirectionalBlurButton.IsChecked.Value)
        {
            Dir_blur();
        }
        else if (BandsButton.IsChecked.HasValue && BandsButton.IsChecked.Value)
        {
            Bands();
        }
        else if (EmbossedButton.IsChecked.HasValue && EmbossedButton.IsChecked.Value)
        {
            Embossed();
        }
        else if (GlasTileButton.IsChecked.HasValue && GlasTileButton.IsChecked.Value)
        {
            GlasTileEffect();
        }
        else if (SmoothMagnifyButton.IsChecked.HasValue && SmoothMagnifyButton.IsChecked.Value)
        {
            MagnifySmoothEffect();
        }
        else if (PaperfoldButton.IsChecked.HasValue && PaperfoldButton.IsChecked.Value)
        {
            PaperFoldEffect();
        }
        else if (PivotButton.IsChecked.HasValue && PivotButton.IsChecked.Value)
        {
            PivotEffect();
        }
        else if (UnderwaterButton.IsChecked.HasValue && UnderwaterButton.IsChecked.Value)
        {
            UnderWaterEffect();
        }
        else if (WavewarperButton.IsChecked.HasValue && WavewarperButton.IsChecked.Value)
        {
            WaveWarperEffect();
        }
        else if (FrostyOutlineButton.IsChecked.HasValue && FrostyOutlineButton.IsChecked.Value)
        {
            FrostyOutlineEffect();
        }
        else if (OldMovieButton.IsChecked.HasValue && OldMovieButton.IsChecked.Value)
        {
            OldMovieEffect();
        }
        else if (SketchButton.IsChecked.HasValue && SketchButton.IsChecked.Value)
        {
            Sketch();
        }
        else if (SwirlButton.IsChecked.HasValue && SwirlButton.IsChecked.Value)
        {
            SwirlEffect();
        }
        else if (BloomButton.IsChecked.HasValue && BloomButton.IsChecked.Value)
        {
            Bloom();
        }
        else if (GloomButton.IsChecked.HasValue && GloomButton.IsChecked.Value)
        {
            Gloom();
        }
        else if (ToneMappingButton.IsChecked.HasValue && ToneMappingButton.IsChecked.Value)
        {
            ToneMapping();
        }
        else if (TelescopicBlurButton.IsChecked.HasValue && TelescopicBlurButton.IsChecked.Value)
        {
            Teleskopisk_blur();
        }
        else if (PencilSketchButton.IsChecked.HasValue && PencilSketchButton.IsChecked.Value)
        {
            SketchPencilStrokeEffect();
        }
    }

    #region HLSL Shader Effects

    private bool Remove_Effects()
    {
        var value = false;
        ConfigureWindows.GetMainWindow.MainImage.Effect = null;
        IntensitySlider.IsEnabled = false;

        var list = EffectsContainer.Children.OfType<Border>();
        foreach (var item in list)
        {
            var checkBox = (CheckBox)item.Child;
            if (checkBox is not null and { IsChecked: true })
            {
                checkBox.IsChecked = false;
                value = true;
            }
        }

        return value;
    }

    private void Negative()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new InvertColorEffect();
        NegativeButton.IsChecked = true;
        IntensitySlider.IsEnabled = false;
    }

    private void GraySceale()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new GrayscaleEffect();
        GrayscaleButton.IsChecked = true;
        IntensitySlider.IsEnabled = false;
    }

    private void ColorToneEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new ColorToneEffect(IntensitySlider.Value);
        ColorToneButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void RippleEffect1()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new Transition_RippleEffect();
        RippleButton.IsChecked = true;
        IntensitySlider.IsEnabled = false;
    }

    private void RippleEffect2()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new RippleEffect(IntensitySlider.Value);
        RippleAltButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void BandedSwirlEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new BandedSwirlEffect(IntensitySlider.Value);
        BandedSwirlButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void Monochrome()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new MonochromeEffect();
        MonochromeButton.IsChecked = true;
        IntensitySlider.IsEnabled = false;
    }

    private void SwirlEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new SwirlEffect(IntensitySlider.Value);
        SwirlButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void Bloom()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new BloomEffect(IntensitySlider.Value);
        BloomButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void Gloom()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new GloomEffect(IntensitySlider.Value);
        GloomButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void ToneMapping()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new ToneMappingEffect(IntensitySlider.Value);
        ToneMappingButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void Teleskopisk_blur()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new TelescopicBlurPS3Effect(IntensitySlider.Value);
        TelescopicBlurButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void BlurEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new GrowablePoissonDiskEffect();
        BlurButton.IsChecked = true;
        IntensitySlider.IsEnabled = false;
    }

    private void Dir_blur()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new DirectionalBlurEffect(IntensitySlider.Value);
        DirectionalBlurButton.IsChecked = true;
        IntensitySlider.IsEnabled = false;
    }

    private void Bands()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new BandsEffect(IntensitySlider.Value);
        BandsButton.IsChecked = true;
        IntensitySlider.IsEnabled = false;
    }

    private void Embossed()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new EmbossedEffect(IntensitySlider.Value);
        EmbossedButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void GlasTileEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new GlassTilesEffect(IntensitySlider.Value);
        GlasTileButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void MagnifySmoothEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new MagnifySmoothEffect(IntensitySlider.Value);
        SmoothMagnifyButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void PaperFoldEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new PaperFoldEffect(IntensitySlider.Value);
        PaperfoldButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void PivotEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new PivotEffect(IntensitySlider.Value);
        PivotButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void UnderWaterEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new UnderwaterEffect(IntensitySlider.Value);
        UnderwaterButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void WaveWarperEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new WaveWarperEffect(IntensitySlider.Value);
        WavewarperButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void FrostyOutlineEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new FrostyOutlineEffect(IntensitySlider.Value);
        FrostyOutlineButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void OldMovieEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new OldMovieEffect(IntensitySlider.Value);
        OldMovieButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void PixelateEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new PixelateEffect(IntensitySlider.Value);
        PixelateButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void Sketch()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new SketchGraniteEffect(IntensitySlider.Value);
        SketchButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    private void SketchPencilStrokeEffect()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
        {
            if (!Remove_Effects())
            {
                return;
            }
        }

        ConfigureWindows.GetMainWindow.MainImage.Effect = new SketchPencilStrokeEffect(IntensitySlider.Value);
        PencilSketchButton.IsChecked = true;
        IntensitySlider.IsEnabled = true;
    }

    #endregion HLSL Shader Effects
}