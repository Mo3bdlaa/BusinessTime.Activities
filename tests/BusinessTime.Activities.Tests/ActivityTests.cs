using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using BusinessTime;
using Xunit;

namespace BusinessTime.Activities.Tests
{
    public class ActivityTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 1, 5);
        private static readonly DateTime Friday = new DateTime(2026, 1, 9);

        private static BusinessCalendar NineToFive => BusinessCalendar.Create()
            .WithSchedule("Mon-Fri 09:00-17:00")
            .WithTimeZone(TimeZoneInfo.Utc)
            .Build();

        [Fact]
        public void AddBusinessTimeMovesFridayAfternoonToMonday()
        {
            var activity = new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Friday.AddHours(14)),
                Hours = WorkflowHarness.Arg<double>(8)
            };

            Assert.Equal(Monday.AddDays(7).AddHours(14), WorkflowHarness.RunFor(activity));
        }

        [Fact]
        public void AddBusinessTimeCombinesDaysHoursAndMinutes()
        {
            var activity = new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Monday.AddHours(9)),
                Days = WorkflowHarness.Arg<double>(1),
                Hours = WorkflowHarness.Arg<double>(1),
                Minutes = WorkflowHarness.Arg<double>(30)
            };

            // One business day is eight hours here, so the total is Tuesday 09:00 plus 90 minutes.
            Assert.Equal(Monday.AddDays(1).AddHours(10.5), WorkflowHarness.RunFor(activity));
        }

        [Fact]
        public void AddBusinessTimeReportsTheElapsedWallClockTime()
        {
            var activity = new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Friday.AddHours(14)),
                Hours = WorkflowHarness.Arg<double>(8)
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            Assert.Equal(Monday.AddDays(7).AddHours(14), outputs["Result"]);
            Assert.Equal(TimeSpan.FromHours(72), outputs["ElapsedTime"]);
        }

        [Fact]
        public void WholeDayHandlingKeepsTheTimeOfDay()
        {
            var activity = new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Friday.AddHours(16)),
                Days = WorkflowHarness.Arg<double>(1),
                DayHandling = DayHandling.AsWholeDays
            };

            Assert.Equal(Monday.AddDays(7).AddHours(16), WorkflowHarness.RunFor(activity));
        }

        [Fact]
        public void WholeDayHandlingRefusesAFractionOfADay()
        {
            var activity = new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Monday.AddHours(9)),
                Days = WorkflowHarness.Arg<double>(1.5),
                DayHandling = DayHandling.AsWholeDays
            };

            BusinessTimeException error = Assert.Throws<BusinessTimeException>(() => WorkflowHarness.RunFor(activity));

            Assert.Contains("Whole days cannot be split", error.Message);
        }

        [Fact]
        public void SubtractBusinessTimeWalksBackwards()
        {
            var activity = new SubtractBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Monday.AddDays(7).AddHours(11)),
                Hours = WorkflowHarness.Arg<double>(4)
            };

            Assert.Equal(Friday.AddHours(15), WorkflowHarness.RunFor(activity));
        }

        [Fact]
        public void GetBusinessTimeBetweenReportsEveryUnit()
        {
            var activity = new GetBusinessTimeBetween
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                From = WorkflowHarness.Arg(Friday.AddHours(16)),
                To = WorkflowHarness.Arg(Monday.AddDays(7).AddHours(10))
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            Assert.Equal(TimeSpan.FromHours(2), outputs["Result"]);
            Assert.Equal(2d, (double)outputs["BusinessHours"], 6);
            Assert.Equal(0.25, (double)outputs["BusinessDays"], 6);
            Assert.Equal(2, outputs["WorkingDays"]);
        }

        [Fact]
        public void IsBusinessTimeAnswersForTheMomentAndTheDay()
        {
            BusinessCalendar calendar = NineToFive.ToBuilder()
                .AddHoliday(Monday.AddDays(1), "Company day")
                .Build();

            var duringLunchOnAHoliday = new IsBusinessTime
            {
                Calendar = WorkflowHarness.Arg(calendar),
                Date = WorkflowHarness.Arg(Monday.AddDays(1).AddHours(12))
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(duringLunchOnAHoliday);

            Assert.False((bool)outputs["Result"]);
            Assert.False((bool)outputs["IsWorkingDay"]);
            Assert.Equal("Company day", outputs["SpecialDayName"]);
        }

        [Fact]
        public void SnapToBusinessTimeMovesAWeekendRequestToMondayMorning()
        {
            var activity = new SnapToBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Friday.AddDays(1).AddHours(10))
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            Assert.Equal(Monday.AddDays(7).AddHours(9), outputs["Result"]);
            Assert.True((bool)outputs["WasAdjusted"]);
        }

        [Fact]
        public void SnapToBusinessTimeLeavesWorkingHoursAlone()
        {
            var activity = new SnapToBusinessTime
            {
                Calendar = WorkflowHarness.Arg(NineToFive),
                Date = WorkflowHarness.Arg(Monday.AddHours(10)),
                Direction = SnapDirection.Backward
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            Assert.Equal(Monday.AddHours(10), outputs["Result"]);
            Assert.False((bool)outputs["WasAdjusted"]);
        }

        [Fact]
        public void GetBusinessDayInfoDescribesADayWithABreak()
        {
            var activity = new GetBusinessDayInfo
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-12:00,13:00-17:00"),
                Date = WorkflowHarness.Arg(Monday.AddHours(15))
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            Assert.True((bool)outputs["Result"]);
            Assert.Equal(Monday.AddHours(9), outputs["DayStart"]);
            Assert.Equal(Monday.AddHours(17), outputs["DayEnd"]);
            Assert.Equal(TimeSpan.FromHours(7), outputs["WorkingTime"]);
            Assert.Equal("09:00-12:00,13:00-17:00", outputs["Shifts"]);
        }

        [Fact]
        public void CountBusinessDaysSkipsAHoliday()
        {
            BusinessCalendar calendar = NineToFive.ToBuilder()
                .AddHoliday(Monday.AddDays(2), "Company day")
                .Build();

            var activity = new CountBusinessDays
            {
                Calendar = WorkflowHarness.Arg(calendar),
                From = WorkflowHarness.Arg(Monday),
                To = WorkflowHarness.Arg(Friday)
            };

            Assert.Equal(4, WorkflowHarness.RunFor(activity));
        }

        [Fact]
        public void GetWorkingIntervalsListsTheOpenWindows()
        {
            var activity = new GetWorkingIntervals
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-12:00,13:00-17:00"),
                From = WorkflowHarness.Arg(Monday.AddHours(11)),
                To = WorkflowHarness.Arg(Monday.AddHours(14))
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);
            var intervals = (IList<BusinessTimeInterval>)outputs["Result"];

            Assert.Equal(2, intervals.Count);
            Assert.Equal(new BusinessTimeInterval(Monday.AddHours(11), Monday.AddHours(12)), intervals[0]);
            Assert.Equal(new BusinessTimeInterval(Monday.AddHours(13), Monday.AddHours(14)), intervals[1]);
            Assert.Equal(TimeSpan.FromHours(2), outputs["TotalWorkingTime"]);
        }
    }
}
