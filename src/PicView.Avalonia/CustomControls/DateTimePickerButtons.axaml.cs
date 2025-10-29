using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace PicView.Avalonia.CustomControls;

public partial class DateTimePickerButtons : UserControl
{
    /// <summary>
    /// Defines the SelectedDateTime dependency property.
    /// This property is two-way bindable and represents the final selected date and time.
    /// </summary>
    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty =
        AvaloniaProperty.Register<DateTimePickerButtons, DateTime?>(
            nameof(SelectedDateTime),
            DateTime.Now,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<IconButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<IconButton, object?>(nameof(CommandParameter));

    private CalendarContainer? _calendarContainer;
    private Flyout? _calendarFlyout;
    private AnalogClock? _clock;
    private Flyout? _timePickerFlyout;

    public DateTimePickerButtons()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected date and time.
    /// </summary>
    public DateTime? SelectedDateTime
    {
        get => GetValue(SelectedDateTimeProperty);
        set => SetValue(SelectedDateTimeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != SelectedDateTimeProperty)
        {
            return;
        }

        if (SelectedDateTime.HasValue)
        {
            DateBox.SelectedDateTime = SelectedDateTime.Value;
        }
        else
        {
            DateBox.SelectedDateTime = null;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (SelectedDateTime.HasValue)
        {
            DateBox.SelectedDateTime = SelectedDateTime.Value;
        }
        else
        {
            DateBox.SelectedDateTime = null;
        }
        
        _calendarContainer = new CalendarContainer();
        _calendarFlyout = new Flyout
        {
            Placement = PlacementMode.Top,
            ShowMode = FlyoutShowMode.Standard,
            Content = _calendarContainer
        };
        FlyoutBase.SetAttachedFlyout(CalendarButton, _calendarFlyout);

        _clock = new AnalogClock();
        _timePickerFlyout = new Flyout
        {
            Placement = PlacementMode.Top,
            ShowMode = FlyoutShowMode.Standard,
            Content = _clock,
            HorizontalOffset = -TimePickerButton.Width / 2 + 5
        };
        FlyoutBase.SetAttachedFlyout(TimePickerButton, _timePickerFlyout);

        CalendarButton.Click += (_, _) => ShowPopUpControl(true);
        TimePickerButton.Click += (_, _) => ShowPopUpControl(false);

        _calendarContainer.Accepted += OnCalendarAccepted;
        _calendarContainer.Cancelled += (_, _) => _calendarFlyout.Hide();

        _clock.Accepted += OnClockAccepted;
        _clock.Cancelled += (_, _) => _timePickerFlyout.Hide();
        
        DateBox.KeyUp += DateBoxOnKeyUp;
    }

    private void DateBoxOnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        SelectedDateTime = DateBox.SelectedDateTime;
        ExecuteCommand();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        
        DateBox.KeyUp -= DateBoxOnKeyUp;
        _clock.Accepted -= OnClockAccepted;
        _calendarContainer.Accepted -= OnCalendarAccepted;
    }

    private void ShowPopUpControl(bool calendar)
    {
        if (calendar)
        {
            _calendarContainer.IsVisible = true;
            _calendarContainer.Opacity = 1;
            if (SelectedDateTime.HasValue)
            {
                _calendarContainer.PartCalendar.SelectedDate = SelectedDateTime.Value;
                _calendarContainer.PartCalendar.DisplayDate = SelectedDateTime.Value;
            }
        }
        else
        {
            _clock.IsVisible = true;
            _clock.Opacity = 1;
            if (SelectedDateTime.HasValue)
            {
                _clock.SelectedTime = SelectedDateTime.Value;
            }
        }

        FlyoutBase.ShowAttachedFlyout(calendar ? CalendarButton : TimePickerButton);
    }

    /// <summary>
    /// Handles the Accepted event from the calendar popup.
    /// It combines the newly selected date with the existing time.
    /// </summary>
    private void OnCalendarAccepted(object? sender, EventArgs e)
    {
        if (!_calendarContainer.SelectedDate.HasValue)
        {
            return;
        }
        
        var newDate = _calendarContainer.PartCalendar.DisplayDate.Date;
        var oldTime = SelectedDateTime?.TimeOfDay ?? DateTime.Now.TimeOfDay;
        SelectedDateTime = newDate + oldTime;

        ExecuteCommand();
    }

    /// <summary>
    /// Handles the Accepted event from the clock popup.
    /// It combines the existing date with the newly selected time.
    /// </summary>
    private void OnClockAccepted(object? sender, EventArgs e)
    {
        if (!SelectedDateTime.HasValue)
        {
            return;
        }
        var oldDate = SelectedDateTime.Value.Date;
        var newTime = _clock.SelectedTime.TimeOfDay;
        SelectedDateTime = oldDate + newTime;

        ExecuteCommand();
    }

    private void ExecuteCommand()
    {
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }
}