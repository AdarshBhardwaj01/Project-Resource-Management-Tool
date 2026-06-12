using PRM.Business.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Tests.Helpers;

public class TimesheetWorkflowHelperTests
{
    [Fact]
    public void CanEmployeeSubmit_WhenUnlockedByManager_AllowsPastWeekSubmission()
    {
        var weekStart = new DateTime(2026, 6, 2);
        var today = new DateTime(2026, 6, 12);
        var timesheet = new Timesheet
        {
            WeekStartDate = weekStart,
            Status = TimesheetStatus.Pending,
            IsFrozen = false,
            IsUnlockedByManager = true
        };

        var canSubmit = TimesheetWorkflowHelper.CanEmployeeSubmit(timesheet, weekStart, today);

        Assert.True(canSubmit);
    }

    [Fact]
    public void IsFrozen_WhenUnlockedByManager_ReturnsFalseEvenIfStatusStillFrozen()
    {
        var timesheet = new Timesheet
        {
            Status = TimesheetStatus.Frozen,
            IsFrozen = false,
            IsUnlockedByManager = true
        };

        Assert.False(TimesheetWorkflowHelper.IsFrozen(timesheet));
    }

    [Fact]
    public void IsFrozen_WhenFrozenAndNotUnlocked_ReturnsTrue()
    {
        var timesheet = new Timesheet
        {
            Status = TimesheetStatus.Frozen,
            IsFrozen = true,
            IsUnlockedByManager = false
        };

        Assert.True(TimesheetWorkflowHelper.IsFrozen(timesheet));
    }
}
