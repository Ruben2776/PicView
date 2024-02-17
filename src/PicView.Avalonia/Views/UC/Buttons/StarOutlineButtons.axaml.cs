using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.UC.Buttons;

public partial class StarOutlineButtons : UserControl
{
    public StarOutlineButtons()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext == null)
            {
                return;
            }
            var vm = (MainViewModel)DataContext;
            vm.PropertyChanged += (_, x) =>
            {
                if (x.PropertyName != nameof(MainViewModel.EXIFRating))
                {
                    return;
                }
                SetStars(vm.EXIFRating);
            };
            SetStars(vm.EXIFRating);
        };
    }

    public void SetStars(uint stars)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            switch (stars)
            {
                case 1:
                    FillStar1();
                    break;

                case 2:
                    FillStar2();
                    break;

                case 3:
                    FillStar3();
                    break;

                case 4:
                    FillStar4();
                    break;

                case 5:
                    FillStar5();
                    break;

                default:
                    OutlineStars();
                    break;
            }
        });
    }

    public void FillStar1()
    {
        if (!this.TryFindResource("StarFilledDrawingImage", ThemeVariant.Default, out var resourceValue1))
        {
            return;
        }
        if (!this.TryFindResource("StarOutlineDrawingImage", ThemeVariant.Default, out var resourceValue2))
        {
            return;
        }
        var filledStar = resourceValue1 as DrawingImage;
        Star1.Source = filledStar;
        var outlinedStar = resourceValue2 as DrawingImage;
        Star2.Source = outlinedStar;
        Star3.Source = outlinedStar;
        Star4.Source = outlinedStar;
        Star5.Source = outlinedStar;
    }

    public void FillStar2()
    {
        if (!this.TryFindResource("StarFilledDrawingImage", ThemeVariant.Default, out var resourceValue1))
        {
            return;
        }

        if (!this.TryFindResource("StarOutlineDrawingImage", ThemeVariant.Default, out var resourceValue2))
        {
            return;
        }
        var filledStar = resourceValue1 as DrawingImage;
        Star1.Source = filledStar;
        Star2.Source = filledStar;
        Star3.Source = resourceValue2 as DrawingImage;
        Star4.Source = resourceValue2 as DrawingImage;
        Star5.Source = resourceValue2 as DrawingImage;
    }

    public void FillStar3()
    {
        if (!this.TryFindResource("StarFilledDrawingImage", ThemeVariant.Default, out var resourceValue1))
        {
            return;
        }

        if (!this.TryFindResource("StarOutlineDrawingImage", ThemeVariant.Default, out var resourceValue2))
        {
            return;
        }
        var filledStar = resourceValue1 as DrawingImage;
        Star1.Source = filledStar;
        Star2.Source = filledStar;
        Star3.Source = filledStar;
        Star4.Source = resourceValue2 as DrawingImage;
        Star5.Source = resourceValue2 as DrawingImage;
    }

    public void FillStar4()
    {
        if (!this.TryFindResource("StarFilledDrawingImage", ThemeVariant.Default, out var resourceValue1))
        {
            return;
        }

        if (!this.TryFindResource("StarOutlineDrawingImage", ThemeVariant.Default, out var resourceValue2))
        {
            return;
        }
        var filledStar = resourceValue1 as DrawingImage;
        Star1.Source = filledStar;
        Star2.Source = filledStar;
        Star3.Source = filledStar;
        Star4.Source = filledStar;
        Star5.Source = resourceValue2 as DrawingImage;
    }

    public void FillStar5()
    {
        if (!this.TryFindResource("StarFilledDrawingImage", ThemeVariant.Default, out var resourceValue))
        {
            return;
        }
        var filledStar = resourceValue as DrawingImage;
        Star1.Source = filledStar;
        Star2.Source = filledStar;
        Star3.Source = filledStar;
        Star4.Source = filledStar;
        Star5.Source = filledStar;
    }

    public void OutlineStars()
    {
        if (!this.TryFindResource("StarOutlineDrawingImage", ThemeVariant.Default, out var resourceValue))
        {
            return;
        }
        var drawingImage = resourceValue as DrawingImage;
        Star1.Source = drawingImage;
        Star2.Source = drawingImage;
        Star3.Source = drawingImage;
        Star4.Source = drawingImage;
        Star5.Source = drawingImage;
    }

    private void Star1_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        FillStar1();
    }

    private void Stars_OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is null)
        {
            OutlineStars();
            return;
        }
        var vm = (MainViewModel)DataContext;
        SetStars(vm.EXIFRating);
    }

    private void Star2_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        FillStar2();
    }

    private void Star3_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        FillStar3();
    }

    private void Star4_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        FillStar4();
    }

    private void Star5_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        FillStar5();
    }

    private void FiveStarCLick(object? sender, RoutedEventArgs e)
    {
        FillStar5();
    }

    private void FourStarCLick(object? sender, RoutedEventArgs e)
    {
        FillStar4();
    }

    private void ThreeStarCLick(object? sender, RoutedEventArgs e)
    {
        FillStar3();
    }

    private void TwoStarCLick(object? sender, RoutedEventArgs e)
    {
        FillStar2();
    }

    private void OneStarCLick(object? sender, RoutedEventArgs e)
    {
        FillStar1();
    }
}