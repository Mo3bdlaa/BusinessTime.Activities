using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using BusinessTime;
using Xunit;

namespace BusinessTime.Activities.Tests
{
    public class CalendarResolutionTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 1, 5);

        private static BusinessCalendar SixHourDays => BusinessCalendar.Create()
            .WithName("Short days")
            .WithSchedule("Mon-Fri 09:00-15:00")
            .WithTimeZone(TimeZoneInfo.Utc)
            .Build();

        [Fact]
        public void ActivitiesInheritTheCalendarFromTheSurroundingScope()
        {
            var scope = new BusinessCalendarScope
            {
                Calendar = WorkflowHarness.Arg(SixHourDays)
            };

            var inner = new AddBusinessTime
            {
                Date = WorkflowHarness.Arg(Monday.AddHours(14)),
                Hours = WorkflowHarness.Arg<double>(2)
            };

            // Short days close at 15:00, so only one hour is left on Monday and the second runs on Tuesday.
            Assert.Equal(Monday.AddDays(1).AddHours(10), WorkflowHarness.RunInScope(scope, inner));
        }

        [Fact]
        public void AScopeCanBeDefinedWithAScheduleStringAlone()
        {
            var scope = new BusinessCalendarScope
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-15:00")
            };

            var inner = new CountBusinessDays
            {
                From = WorkflowHarness.Arg(Monday),
                To = WorkflowHarness.Arg(Monday.AddDays(6))
            };

            Assert.Equal(5, WorkflowHarness.RunInScope(scope, inner));
        }

        [Fact]
        public void AnActivityWithItsOwnCalendarIgnoresTheScope()
        {
            var scope = new BusinessCalendarScope
            {
                Calendar = WorkflowHarness.Arg(SixHourDays)
            };

            BusinessCalendar longDays = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .Build();

            var inner = new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(longDays),
                Date = WorkflowHarness.Arg(Monday.AddHours(14)),
                Hours = WorkflowHarness.Arg<double>(2)
            };

            Assert.Equal(Monday.AddHours(16), WorkflowHarness.RunInScope(scope, inner));
        }

        [Fact]
        public void TheScopeHandsItsCalendarToTheBodyAsAnArgument()
        {
            var scope = new BusinessCalendarScope
            {
                Calendar = WorkflowHarness.Arg(SixHourDays)
            };

            var inner = new AddBusinessTime
            {
                Calendar = new InArgument<BusinessCalendar>(scope.Body.Argument),
                Date = WorkflowHarness.Arg(Monday.AddHours(14)),
                Hours = WorkflowHarness.Arg<double>(2)
            };

            Assert.Equal(Monday.AddDays(1).AddHours(10), WorkflowHarness.RunInScope(scope, inner));
        }

        [Fact]
        public void WithoutACalendarOrScopeTheScheduleShorthandIsUsed()
        {
            var activity = new CountBusinessDays
            {
                Schedule = WorkflowHarness.Arg("Mon-Sat 09:00-17:00"),
                From = WorkflowHarness.Arg(Monday),
                To = WorkflowHarness.Arg(Monday.AddDays(6))
            };

            Assert.Equal(6, WorkflowHarness.RunFor(activity));
        }

        [Fact]
        public void WithNothingSuppliedAMondayToFridayWeekIsAssumed()
        {
            var activity = new CountBusinessDays
            {
                From = WorkflowHarness.Arg(Monday),
                To = WorkflowHarness.Arg(Monday.AddDays(6))
            };

            Assert.Equal(5, WorkflowHarness.RunFor(activity));
        }

        [Fact]
        public void CreateBusinessCalendarBuildsAUsableCalendar()
        {
            var activity = new CreateBusinessCalendar
            {
                Name = WorkflowHarness.Arg("Support desk"),
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00"),
                TimeZoneId = WorkflowHarness.Arg("UTC"),
                HoursPerBusinessDay = WorkflowHarness.Arg<double>(7),
                Holidays = WorkflowHarness.Arg((IEnumerable<DateTime>)new[] { Monday.AddDays(1) }),
                AnnualHolidays = WorkflowHarness.Arg((IEnumerable<DateTime>)new[] { new DateTime(2000, 1, 1) })
            };

            BusinessCalendar calendar = WorkflowHarness.RunFor(activity);

            Assert.Equal("Support desk", calendar.Name);
            Assert.Equal(TimeZoneInfo.Utc.Id, calendar.TimeZone.Id);
            Assert.Equal(TimeSpan.FromHours(7), calendar.HoursPerBusinessDay);
            Assert.Equal(6, calendar.Schedule.WorkingDaysPerWeek);
            Assert.False(calendar.IsWorkingDay(Monday.AddDays(1)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2032, 1, 1)));
        }

        [Fact]
        public void CalendarsSurviveASaveAndLoadThroughTheActivities()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

            try
            {
                WorkflowHarness.Run(new SaveBusinessCalendar
                {
                    Calendar = WorkflowHarness.Arg(SixHourDays),
                    FilePath = WorkflowHarness.Arg(path)
                });

                BusinessCalendar loaded = WorkflowHarness.RunFor(new LoadBusinessCalendar
                {
                    FilePath = WorkflowHarness.Arg(path)
                });

                Assert.Equal("Short days", loaded.Name);
                Assert.Equal(TimeSpan.FromHours(6), loaded.HoursPerBusinessDay);
                Assert.True(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void LoadBusinessCalendarAcceptsJsonText()
        {
            BusinessCalendar loaded = WorkflowHarness.RunFor(new LoadBusinessCalendar
            {
                Json = WorkflowHarness.Arg("{ \"week\": \"Mon-Fri 08:00-16:00\", \"timeZone\": \"UTC\" }"),
                TimeZoneId = WorkflowHarness.Arg("Europe/Berlin")
            });

            Assert.Equal("Europe/Berlin", loaded.TimeZone.Id);
            Assert.Equal(5, loaded.Schedule.WorkingDaysPerWeek);
        }

        [Fact]
        public void LoadBusinessCalendarNeedsSomethingToLoad()
        {
            BusinessTimeException error = Assert.Throws<BusinessTimeException>(
                () => WorkflowHarness.RunFor(new LoadBusinessCalendar()));

            Assert.Contains("needs either a file path or JSON text", error.Message);
        }
    }
}
