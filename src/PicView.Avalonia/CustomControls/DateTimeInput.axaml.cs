using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using R3;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A custom control for date and time input, composed of separate fields
/// for year, month, day, hour, and minute. The order of the fields is
/// determined by the current culture's date and time formats.
/// </summary>
public class DateTimeInput : TemplatedControl
{
    /// <summary>
    /// Defines the SelectedDateTime dependency property.
    /// </summary>
    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty =
        AvaloniaProperty.Register<DateTimeInput, DateTime?>(
            nameof(SelectedDateTime),
            defaultBindingMode: BindingMode.TwoWay);

    // Holds the button for AM/PM selection.
    private Button? _ampmToggle;

    // Tracks if the current culture uses a 12-hour clock.
    private bool _is12HourClock;

    // Tracks the current AM/PM state when in 12-hour mode.
    private bool _isAm = true;

    // Flag to prevent recursive updates between the main property and the text boxes.
    private bool _isUpdatingFromProperty;

    // Holds the TextBoxes for each part of the DateTime.
    private TextBox? _yearBox, _monthBox, _dayBox, _hourBox, _minuteBox;

    private CompositeDisposable _disposables = new();

    private const string PARTContainer = "PART_Container";
    private DockPanel? _controlsContainer;

    /// <summary>
    /// Static constructor to register the default style for this control.
    /// </summary>
    static DateTimeInput()
    {
        // When the SelectedDateTime property changes, update the internal TextBox values.
        SelectedDateTimeProperty.Changed.AddClassHandler<DateTimeInput>((x, e) => x.OnSelectedDateTimeChanged(e));
    }

    /// <summary>
    /// Gets or sets the selected date and time.
    /// This is the main property to bind to from your ViewModel.
    /// </summary>
    public DateTime? SelectedDateTime
    {
        get => GetValue(SelectedDateTimeProperty);
        set => SetValue(SelectedDateTimeProperty, value);
    }


    /// <summary>
    /// Called when the control's template is applied. This is where we find
    /// the container panel and dynamically add our input fields.
    /// </summary>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Find the container that will hold our dynamic controls.
        var container = e.NameScope.Find<DockPanel>(PARTContainer);

        _controlsContainer = container ??
                             throw new InvalidOperationException("Could not find PART_Container in the control template.");

        // Generate and add the date/time input controls based on current culture.
        BuildInputControls(container);

        // Set initial values if SelectedDateTime is already set.
        UpdateTextBoxesFromDateTime(SelectedDateTime);
    }

    /// <summary>
    /// Handles changes to the SelectedDateTime property, updating the UI.
    /// </summary>
    private void OnSelectedDateTimeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (vm.PicViewer?.FileInfo.Value?.Exists == true)
            {
                _controlsContainer?.IsVisible = true;
            }
            else
            {
                _controlsContainer?.IsVisible = false;
            }
        }
        // Set a flag to indicate that the update is coming from the property,
        // not from user input in the TextBoxes.
        _isUpdatingFromProperty = true;
        UpdateTextBoxesFromDateTime(e.NewValue as DateTime?);
        _isUpdatingFromProperty = false;
    }

    /// <summary>
    /// Populates the container panel with TextBox and separator controls
    /// in an order determined by the current culture.
    /// </summary>
    /// <param name="container">The panel to add controls to.</param>
    private void BuildInputControls(DockPanel container)
    {
        // Detach handlers from previously created controls and clear subscriptions.
        DetachHandlersAndClearState();
        
        container.Children.Clear();
        var culture = CultureInfo.CurrentCulture;
        var dateTimeFormat = culture.DateTimeFormat;
        
        _is12HourClock = dateTimeFormat.ShortTimePattern.Contains('h');
        
        var today = DateTime.Now;

        // --- Create TextBoxes ---
        _yearBox = CreateNumericTextBox(maxLength:4, watermark: today.Year.ToString("D4"));
        _monthBox = CreateNumericTextBox(maxLength:2, watermark: today.Month.ToString("D2"));
        _dayBox = CreateNumericTextBox(maxLength:2, watermark: today.Day.ToString("D2"));
        _hourBox = CreateNumericTextBox(maxLength:2, watermark: today.TimeOfDay.Hours.ToString("D2"));
        _minuteBox = CreateNumericTextBox(maxLength:2, watermark: today.TimeOfDay.Minutes.ToString("D2"));

        // --- Determine Date Field Order ---
        var dateParts = dateTimeFormat.ShortDatePattern.Split(dateTimeFormat.DateSeparator)
            .Select(p => p.ToLowerInvariant().Trim(' '));

        var dateControls = new List<Control>();
        foreach (var part in dateParts)
        {
            if (part.Contains('y') && _yearBox != null)
            {
                dateControls.Add(_yearBox);
            }
            else if (part.Contains('m') && _monthBox != null)
            {
                dateControls.Add(_monthBox);
            }
            else if (part.Contains('d') && _dayBox != null)
            {
                // Make sure not to select day of week (dddd)
                if (part is "d" or "dd")
                {
                    dateControls.Add(_dayBox);
                }
            }
        }

        // --- Add Date Controls to Container ---
        for (var i = 0; i < dateControls.Count; i++)
        {
            container.Children.Add(dateControls[i]);
            if (i < dateControls.Count - 1)
            {
                container.Children.Add(CreateLineSeparator(CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator[0]));
            }
        }

        // --- Add Time Controls to Container ---
        container.Children.Add(CreateLineSeparator('—'));
        container.Children.Add(_hourBox);
        container.Children.Add(CreateTimeSeparator());
        container.Children.Add(_minuteBox);

        if (!_is12HourClock)
        {
            return;
        }

        _ampmToggle = new Button
        {
            Cursor = new Cursor(StandardCursorType.Hand),
            Content = new TextBlock
            {
                Classes = { "txt" },
                Text = dateTimeFormat.AMDesignator
            },
            Padding = new Thickness(5, 0),
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 0, 0)
        };
        Observable.EveryValueChanged(this, x => x.IsEffectivelyEnabled, UIHelper.GetFrameProvider)
            .Subscribe(b => _ampmToggle.IsVisible = b)
            .AddTo(_disposables);
        _ampmToggle.Click += OnAmPmToggleClick;
        container.Children.Add(_ampmToggle);
        _isAm = true; // Default to AM
    }

    private void OnAmPmToggleClick(object? sender, RoutedEventArgs e)
    {
        _isAm = !_isAm;
        _ampmToggle?.Content =
            _isAm
                ? CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator
                : CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
        // Trigger a re-parse and update of the main property
        OnPartTextChanged(null, null);
    }

    /// <summary>
    /// Creates a TextBox configured for numeric input.
    /// </summary>
    private TextBox CreateNumericTextBox(int maxLength, string watermark)
    {
        var textBox = new TextBox
        {
            MaxLength = maxLength,
            Watermark = watermark,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
            FontFamily = UIHelper.MediumFontFamily,
            FontSize = 12,
            MinWidth = maxLength * 7 // fix for macOS not having proper width
        };

        // Attach event handlers
        textBox.TextChanged += OnPartTextChanged;
        textBox.KeyDown += OnTextBoxKeyDown; 
        return textBox;
    }

    /// <summary>
    /// Creates a time separator control based on the current culture's time separator format.
    /// </summary>
    /// <returns>
    /// A <see cref="TextBlock"/> representing the time separator with appropriate styling and visibility bindings.
    /// </returns>
    private TextBlock CreateTimeSeparator()
    {
        var textBlock = new TextBlock
        {
            Classes = { "txt" },
            Text = CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator,
            VerticalAlignment = VerticalAlignment.Center,
            LineHeight = 22
        };
        DockPanel.SetDock(textBlock, Dock.Left);
        HideTextBlockWhenNotEnabledSubscription(textBlock);
        return textBlock;
    }

    /// <summary>
    /// Creates a line separator as a textual visual element.
    /// </summary>
    /// <param name="separatorChar">The character to be used as a separator in the line.</param>
    /// <returns>A <see cref="TextBlock"/> configured with the specified separator character and styling.</returns>
    private TextBlock CreateLineSeparator(char separatorChar)
    {
        var textBlock = new TextBlock
        {
            Classes = { "txt" },
            Text = $" {separatorChar} ",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 18,
            Opacity = .6,
            LineHeight = 22
        };
        HideTextBlockWhenNotEnabledSubscription(textBlock);
        return textBlock;
    }

    private void HideTextBlockWhenNotEnabledSubscription(TextBlock textBlock)
    {
        Observable.EveryValueChanged(this, x => x.IsEffectivelyEnabled, UIHelper.GetFrameProvider)
            .Subscribe(b => textBlock.IsVisible = b)
            .AddTo(_disposables);
    }
    
    /// <summary>
    /// Handles the KeyDown event for the numeric text boxes to increment/decrement values.
    /// </summary>
    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        // We only care about the Up and Down arrow keys.
        if (e.Key != Key.Up && e.Key != Key.Down)
        {
            return;
        }

        if (sender is not TextBox textBox)
        {
            return;
        }

        // Mark the event as handled to prevent default behaviors like caret movement.
        e.Handled = true;

        var change = e.Key == Key.Up ? 1 : -1;

        // Use the current date/time if available, otherwise default to now.
        // This ensures the control is responsive even when empty.
        var currentDt = SelectedDateTime ?? DateTime.Now;
        DateTime newDt;

        // Determine which text box fired the event and apply the correct logic.
        if (ReferenceEquals(textBox, _yearBox))
        {
            newDt = currentDt.AddYears(change);
        }
        else if (ReferenceEquals(textBox, _monthBox))
        {
            newDt = currentDt.AddMonths(change);
        }
        else if (ReferenceEquals(textBox, _dayBox))
        {
            newDt = currentDt.AddDays(change);
        }
        else if (ReferenceEquals(textBox, _hourBox))
        {
            newDt = currentDt.AddHours(change);
        }
        else if (ReferenceEquals(textBox, _minuteBox))
        {
            newDt = currentDt.AddMinutes(change);
        }
        else
        {
            return; // Should not happen
        }

        // Update the main property. This will trigger the update of all text boxes.
        SelectedDateTime = newDt;

        // Select the text so the user can immediately press the key again.
        textBox.SelectAll();
    }

    /// <summary>
    /// Event handler for when text changes in any of the input TextBoxes.
    /// It attempts to parse the current input into a DateTime object.
    /// </summary>
    private void OnPartTextChanged(object? sender, TextChangedEventArgs? e)
    {
        // If the text is being changed by our own code, do nothing.
        if (_isUpdatingFromProperty)
        {
            return;
        }
        
        // Always clear any previous error state when the text changes.
        // We will re-add it if the new value is invalid.
        PseudoClasses.Remove(":error");

        // Try to parse the values from the text boxes into integers.
        var yearParsed = int.TryParse(_yearBox?.Text, out var year);
        var monthParsed = int.TryParse(_monthBox?.Text, out var month);
        var dayParsed = int.TryParse(_dayBox?.Text, out var day);
        var hourParsed = int.TryParse(_hourBox?.Text, out var hour);
        var minuteParsed = int.TryParse(_minuteBox?.Text, out var minute);

        // Only update the source property if all fields have a valid number.
        if (yearParsed && monthParsed && dayParsed && hourParsed && minuteParsed)
        {
            // Adjust hour for 12-hour clock format
            if (_is12HourClock)
            {
                if (!_isAm && hour < 12) // PM and not 12 PM
                {
                    hour += 12;
                }
                else if (_isAm && hour == 12) // 12 AM
                {
                    hour = 0;
                }
            }

            try
            {
                // Attempt to create a valid DateTime object.
                var newDateTime = new DateTime(year, month, day, hour, minute, 0);
                SetCurrentValue(SelectedDateTimeProperty, newDateTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                // One of the values is out of range (e.g., month 13).
                OnError();
            }
        }
        else
        {
            // If any part is not a valid number, the overall DateTime is invalid.
            SetCurrentValue(SelectedDateTimeProperty, null);
        }
        return;
        
        void OnError()
        {
            PseudoClasses.Add(":error");
            SetCurrentValue(SelectedDateTimeProperty, null);
        }
    }

    public void ClearTextBoxes()
    {
        _yearBox?.Text = string.Empty;
        _monthBox?.Text = string.Empty;
        _dayBox?.Text = string.Empty;
        _hourBox?.Text = string.Empty;
        _minuteBox?.Text = string.Empty;
    }

    /// <summary>
    /// Updates the text in the individual TextBoxes based on the provided DateTime.
    /// </summary>
    private void UpdateTextBoxesFromDateTime(DateTime? dt)
    {
        if (_yearBox == null)
        {
            return; // Controls not ready yet.
        }

        if (dt.HasValue)
        {
            _yearBox.Text = dt.Value.Year.ToString("D4");
            _monthBox!.Text = dt.Value.Month.ToString("D2");
            _dayBox!.Text = dt.Value.Day.ToString("D2");

            var hour = dt.Value.Hour;
            if (_is12HourClock)
            {
                _isAm = hour < 12;
                _ampmToggle?.Content = _isAm
                    ? CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator
                    : CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;

                if (hour == 0)
                {
                    hour = 12; // 12 AM
                }
                else if (hour > 12)
                {
                    hour -= 12; // PM hours
                }
            }

            _hourBox!.Text = hour.ToString("D2");
            _minuteBox!.Text = dt.Value.Minute.ToString("D2");
        }
        else
        {
            // If the DateTime is null, clear all text boxes.
            _yearBox.Text = string.Empty;
            _monthBox!.Text = string.Empty;
            _dayBox!.Text = string.Empty;
            _hourBox!.Text = string.Empty;
            _minuteBox!.Text = string.Empty;
            if (!_is12HourClock || _ampmToggle == null)
            {
                return;
            }

            _isAm = true;
            _ampmToggle.Content = CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _disposables.Dispose();
        DetachHandlersAndClearState();
    }

    private void DetachHandlersAndClearState()
    {
        SafeDetach(_yearBox);
        SafeDetach(_monthBox);
        SafeDetach(_dayBox);
        SafeDetach(_hourBox);
        SafeDetach(_minuteBox);

        if (_ampmToggle != null)
        {
            _ampmToggle.Click -= OnAmPmToggleClick;
            _ampmToggle = null;
        }

        // Dispose prior subscriptions and create a fresh CompositeDisposable
        _disposables.Dispose();
        _disposables = new CompositeDisposable();

        // Null out controls so we don't hold stale references
        _yearBox = _monthBox = _dayBox = _hourBox = _minuteBox = null;
        return;

        // Detach event handlers from previous text boxes
        void SafeDetach(TextBox? tb)
        {
            if (tb == null)
            {
                return;
            }

            tb.TextChanged -= OnPartTextChanged;
            tb.KeyDown -= OnTextBoxKeyDown;
        }
    }
}