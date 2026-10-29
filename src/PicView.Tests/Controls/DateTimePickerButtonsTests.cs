using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class DateTimePickerButtonsTests
{
    public DateTimePickerButtonsTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void CalendarAccept_AfterClickingDay_ShouldUseClickedDate()
    {
        var (picker, container) = ShowPickerWithCalendar(new DateTime(2024, 3, 10, 14, 30, 0));

        var dayButton = container.PartCalendar.GetVisualDescendants()
            .OfType<CalendarDayButton>()
            .First(b => b.DataContext is DateTime d && d == new DateTime(2024, 3, 20));
        Click(dayButton);
        Click(container.AcceptButton);

        Assert.Equal(new DateTime(2024, 3, 20, 14, 30, 0), picker.SelectedDateTime);
    }

    [AvaloniaFact]
    public void CalendarAccept_AfterClickingDayOfNextMonth_ShouldUseClickedDate()
    {
        var (picker, container) = ShowPickerWithCalendar(new DateTime(2024, 3, 10, 14, 30, 0));

        // March 2024's grid ends with the first days of April
        var dayButton = container.PartCalendar.GetVisualDescendants()
            .OfType<CalendarDayButton>()
            .First(b => b.DataContext is DateTime d && d == new DateTime(2024, 4, 2));
        Click(dayButton);
        Click(container.AcceptButton);

        Assert.Equal(new DateTime(2024, 4, 2, 14, 30, 0), picker.SelectedDateTime);
    }

    [AvaloniaTheory]
    [InlineData(10, 1999, 3, 10)]
    [InlineData(10, 2024, 7, 10)]
    // February 1999 has no 31st, so the day falls back to the month's last day
    [InlineData(31, 1999, 2, 28)]
    public void CalendarAccept_AfterChangingYearOrMonthOnly_ShouldUseDisplayedYearAndMonth(int selectedDay, int year,
        int month, int expectedDay)
    {
        var (picker, container) = ShowPickerWithCalendar(new DateTime(2024, 3, selectedDay, 14, 30, 0));

        // Picking a year or month in the calendar's header moves DisplayDate without selecting a day
        container.PartCalendar.DisplayDate = new DateTime(year, month, 1);
        Dispatcher.UIThread.RunJobs();
        Click(container.AcceptButton);

        Assert.Equal(new DateTime(year, month, expectedDay, 14, 30, 0), picker.SelectedDateTime);
    }

    private static (DateTimePickerButtons Picker, CalendarContainer Container) ShowPickerWithCalendar(
        DateTime selected)
    {
        var picker = new DateTimePickerButtons { Width = 300 };
        new Window { Content = picker, Width = 800, Height = 800 }.Show();
        Dispatcher.UIThread.RunJobs();
        picker.SelectedDateTime = selected;

        // The headless platform cannot open the flyout popup, so host its content in a window of its own
        // and prime it the way ShowPopUpControl does before it opens the flyout
        var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(picker.CalendarButton)!;
        var container = (CalendarContainer)flyout.Content!;
        flyout.Content = null;
        new Window { Content = container, Width = 800, Height = 800 }.Show();
        container.PartCalendar.SelectedDate = selected;
        container.PartCalendar.DisplayDate = selected;
        Dispatcher.UIThread.RunJobs();

        return (picker, container);
    }

    private static void Click(Visual target)
    {
        var topLevel = TopLevel.GetTopLevel(target)!;
        var center = new Point(target.Bounds.Width / 2, target.Bounds.Height / 2);
        var point = target.TranslatePoint(center, topLevel)!.Value;
        topLevel.MouseDown(point, MouseButton.Left);
        topLevel.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
