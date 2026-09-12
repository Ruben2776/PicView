using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class DateTimeInputTests
{
    static DateTimeInputTests()
    {
        if (Application.Current == null)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
    }

    [Fact]
    public void ValidationTextBox_SetEmpty_ShouldToggleEmptyPseudoClass()
    {
        var box = new ValidationTextBox();
        // Newly created TextBox with empty text has :empty
        Assert.Contains(ValidationTextBox.Empty, box.Classes);

        box.SetEmpty(false);
        Assert.DoesNotContain(ValidationTextBox.Empty, box.Classes);

        box.SetEmpty(true);
        Assert.Contains(ValidationTextBox.Empty, box.Classes);
    }

    [Fact]
    public void ValidationTextBox_SetError_ShouldToggleErrorPseudoClass()
    {
        var box = new ValidationTextBox();
        Assert.DoesNotContain(ValidationTextBox.Error, box.Classes);

        box.SetError(true);
        Assert.Contains(ValidationTextBox.Error, box.Classes);

        box.SetError(false);
        Assert.DoesNotContain(ValidationTextBox.Error, box.Classes);
    }

    [Fact]
    public void DateTimeInput_InitialState_ShouldHaveEmptyPseudoClass()
    {
        var input = new DateTimeInput();
        Assert.Contains(DateTimeInput.Empty, input.Classes);
        Assert.DoesNotContain(DateTimeInput.Error, input.Classes);
    }

    [Fact]
    public void DateTimeInput_WhenBoxesEmpty_ShouldHaveEmptyAndNotError()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(input.YearBox);
        Assert.NotNull(input.MonthBox);
        Assert.NotNull(input.DayBox);
        Assert.NotNull(input.HourBox);
        Assert.NotNull(input.MinuteBox);

        Assert.Contains(DateTimeInput.Empty, input.Classes);
        Assert.DoesNotContain(DateTimeInput.Error, input.Classes);

        Assert.Contains(ValidationTextBox.Empty, input.YearBox!.Classes);
        Assert.DoesNotContain(ValidationTextBox.Error, input.YearBox.Classes);

        Assert.Contains(ValidationTextBox.Empty, input.MonthBox!.Classes);
        Assert.DoesNotContain(ValidationTextBox.Error, input.MonthBox.Classes);
    }

    [Fact]
    public void DateTimeInput_WhenValidDateTimeSet_ShouldClearEmptyAndError()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        input.SelectedDateTime = new DateTime(2026, 9, 7, 14, 30, 0);
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain(DateTimeInput.Empty, input.Classes);
        Assert.DoesNotContain(DateTimeInput.Error, input.Classes);

        Assert.DoesNotContain(ValidationTextBox.Empty, input.YearBox!.Classes);
        Assert.DoesNotContain(ValidationTextBox.Error, input.YearBox.Classes);
        Assert.Equal("2026", input.YearBox.Text);

        Assert.DoesNotContain(ValidationTextBox.Empty, input.MonthBox!.Classes);
        Assert.DoesNotContain(ValidationTextBox.Error, input.MonthBox.Classes);
        Assert.Equal("09", input.MonthBox.Text);
    }

    [Fact]
    public void DateTimeInput_WhenCleared_ShouldSetEmptyAndNotError()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        input.SelectedDateTime = new DateTime(2026, 9, 7, 14, 30, 0);
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(DateTimeInput.Empty, input.Classes);

        input.SelectedDateTime = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(DateTimeInput.Empty, input.Classes);
        Assert.DoesNotContain(DateTimeInput.Error, input.Classes);
        Assert.Contains(ValidationTextBox.Empty, input.YearBox!.Classes);
        Assert.DoesNotContain(ValidationTextBox.Error, input.YearBox.Classes);
        Assert.Empty(input.YearBox.Text ?? string.Empty);
    }

    [Fact]
    public void DateTimeInput_WhenInvalidMonthEntered_ShouldSetErrorAndNotOverallEmpty()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Start with a valid date
        input.SelectedDateTime = new DateTime(2026, 9, 7, 14, 30, 0);
        Dispatcher.UIThread.RunJobs();

        // User enters an invalid month
        input.MonthBox!.Text = "99";
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(ValidationTextBox.Error, input.MonthBox.Classes);
        Assert.DoesNotContain(ValidationTextBox.Empty, input.MonthBox.Classes);

        Assert.Contains(DateTimeInput.Error, input.Classes);
        Assert.DoesNotContain(DateTimeInput.Empty, input.Classes);
        Assert.Null(input.SelectedDateTime);
    }

    [Fact]
    public void DateTimeInput_WhenNonNumericEntered_ShouldSetError()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        input.SelectedDateTime = new DateTime(2026, 9, 7, 14, 30, 0);
        Dispatcher.UIThread.RunJobs();

        input.YearBox!.Text = "abcd";
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(ValidationTextBox.Error, input.YearBox.Classes);
        Assert.DoesNotContain(ValidationTextBox.Empty, input.YearBox.Classes);

        Assert.Contains(DateTimeInput.Error, input.Classes);
        Assert.DoesNotContain(DateTimeInput.Empty, input.Classes);
        Assert.Null(input.SelectedDateTime);
    }

    [Fact]
    public void DateTimeInput_WhenInvalidDateCombinationEntered_ShouldSetError()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // 2026-02-28 is valid
        input.SelectedDateTime = new DateTime(2026, 2, 28, 12, 0, 0);
        Dispatcher.UIThread.RunJobs();

        // Enter Feb 30 (invalid combination)
        input.DayBox!.Text = "30";
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(ValidationTextBox.Error, input.DayBox.Classes);
        Assert.Contains(DateTimeInput.Error, input.Classes);
        Assert.Null(input.SelectedDateTime);
    }

    [Fact]
    public void DateTimeInput_WhenValueErased_ShouldBeEmptyInsteadOfError()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Start with a valid date
        input.SelectedDateTime = new DateTime(2026, 9, 7, 14, 30, 0);
        Dispatcher.UIThread.RunJobs();

        // User erases the month
        input.MonthBox!.Text = string.Empty;
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(ValidationTextBox.Empty, input.MonthBox.Classes);
        Assert.DoesNotContain(ValidationTextBox.Error, input.MonthBox.Classes);

        Assert.DoesNotContain(DateTimeInput.Error, input.Classes);
        Assert.Contains(DateTimeInput.Empty, input.Classes);
        Assert.Null(input.SelectedDateTime);
    }

    [Fact]
    public void DateTimeInput_GetEnteredDateTime_ShouldReturnValidDateTime()
    {
        var input = new DateTimeInput();
        var window = new Window { Content = input };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        input.SelectedDateTime = new DateTime(2026, 9, 7, 14, 30, 0);
        Dispatcher.UIThread.RunJobs();

        var entered = input.GetEnteredDateTime();
        Assert.NotNull(entered);
        Assert.Equal(2026, entered.Value.Year);
        Assert.Equal(9, entered.Value.Month);
        Assert.Equal(7, entered.Value.Day);

        // Invalid input returns null without throwing
        input.MonthBox!.Text = "99";
        Assert.Null(input.GetEnteredDateTime());

        input.MonthBox.Text = "invalid";
        Assert.Null(input.GetEnteredDateTime());
    }
}
