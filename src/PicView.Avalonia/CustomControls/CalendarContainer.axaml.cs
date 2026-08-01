using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.Animations;

namespace PicView.Avalonia.CustomControls;

public partial class CalendarContainer : UserControl
{
    public event EventHandler? Accepted;
    public event EventHandler? Cancelled;
    
    /// <summary>
    /// Gets or sets the date currently selected in the calendar.
    /// </summary>
    public DateTime? SelectedDate
    {
        get => PartCalendar.SelectedDate;
        set => PartCalendar.SelectedDate = value;
    }
    
    public DateTime? InitialDate;
    
    public CalendarContainer()
    {
        InitializeComponent();
        
        Loaded += (_, _) => { InitialDate = SelectedDate; };

        AcceptButton.Click += async (_, _) => await Accept();
        CancelButton.Click += async (_, _) => await Cancel();
    }
    
    private async Task Accept()
    {
        Accepted?.Invoke(this, EventArgs.Empty);
        var closeAnimation = AnimationsHelper.OpacityAnimation(1, 0, .3);
        await closeAnimation.RunAsync(this);
        IsVisible = false;      // Hide the control
    }

    private async Task Cancel()
    {
        SelectedDate = InitialDate; // Reset to the original time
        var closeAnimation = AnimationsHelper.OpacityAnimation(1, 0, .3);
        await closeAnimation.RunAsync(this);
        IsVisible = false;      // Hide the control
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}