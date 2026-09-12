using System;
using System.Collections.Generic;
using System.IO;
using BusinessTime;
using Xunit;

namespace BusinessTime.Activities.Tests
{
    /// <summary>
    /// Replays the demo workflow through the activities and pins every value it logs, so the numbers a user
    /// reads on screen are the numbers this package is held to.
    /// </summary>
    public class DemoWorkflowTests : IDisposable
    {
        private const string Week = "Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00";

        private static readonly DateTime TicketCreated = new DateTime(2026, 9, 11, 16, 30, 0);
        private static readonly DateTime TicketAnswered = new DateTime(2026, 9, 14, 10, 15, 0);
        private static readonly DateTime WeekendMoment = new DateTime(2026, 9, 13, 10, 0, 0);

        private readonly string _calendarPath =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        private static BusinessCalendar SupportDesk => WorkflowHarness.RunFor(new CreateBusinessCalendar
        {
            Name = WorkflowHarness.Arg("Support desk"),
            Schedule = WorkflowHarness.Arg(Week),
            TimeZone = CommonTimeZone.Berlin,
            HoursPerBusinessDay = WorkflowHarness.Arg(7.5),
            Holidays = WorkflowHarness.Arg<IEnumerable<DateTime>>(
                new[] { new DateTime(2026, 10, 3), new DateTime(2026, 12, 25) }),
            AnnualHolidays = WorkflowHarness.Arg<IEnumerable<DateTime>>(new[] { new DateTime(2026, 1, 1) })
        });

        public void Dispose()
        {
            if (File.Exists(_calendarPath))
                File.Delete(_calendarPath);
        }

        [Fact]
        public void TheDatesInTheDemoFallOnTheDaysItClaims()
        {
            Assert.Equal(DayOfWeek.Friday, TicketCreated.DayOfWeek);
            Assert.Equal(DayOfWeek.Sunday, WeekendMoment.DayOfWeek);
            Assert.Equal(DayOfWeek.Monday, TicketAnswered.DayOfWeek);
        }

        [Fact]
        public void Step1_TheCalendarSummaryIsRight()
        {
            BusinessCalendar calendar = SupportDesk;

            Assert.Equal("Support desk", calendar.Name);
            Assert.Equal(6, calendar.Schedule.WorkingDaysPerWeek);
            Assert.Equal(39, calendar.Schedule.WeeklyWorkingTime.TotalHours);
            Assert.Equal(7.5, calendar.HoursPerBusinessDay.TotalHours);
            Assert.Equal("W. Europe Standard Time", calendar.TimeZone.Id);
        }

        [Fact]
        public void Step3_SavingAndLoadingKeepsTheCalendar()
        {
            WorkflowHarness.Run(new SaveBusinessCalendar
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                FilePath = WorkflowHarness.Arg(_calendarPath)
            });

            BusinessCalendar loaded = WorkflowHarness.RunFor(new LoadBusinessCalendar
            {
                FilePath = WorkflowHarness.Arg(_calendarPath)
            });

            Assert.Equal("Support desk", loaded.Name);
            Assert.Equal("W. Europe Standard Time", loaded.TimeZone.Id);
            Assert.Equal(39, loaded.Schedule.WeeklyWorkingTime.TotalHours);

            // The holidays survive, even though the activity gave them no names.
            Assert.False(loaded.IsWorkingDay(new DateTime(2026, 10, 3)));
            Assert.False(loaded.IsWorkingDay(new DateTime(2027, 1, 1)));
        }

        [Fact]
        public void Step4_FourBusinessHoursFromFridayAfternoonLandsSaturdayLunchtime()
        {
            var activity = new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                Date = WorkflowHarness.Arg(TicketCreated),
                Hours = WorkflowHarness.Arg<double>(4)
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            Assert.Equal(new DateTime(2026, 9, 12, 12, 30, 0), outputs["Result"]);
            Assert.Equal(TimeSpan.FromHours(20), outputs["ElapsedTime"]);
        }

        [Fact]
        public void Step5_SubtractingAWholeDayAndChangeWalksBackToFridayMorning()
        {
            var activity = new SubtractBusinessTime
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                Date = WorkflowHarness.Arg(new DateTime(2026, 9, 12, 12, 30, 0)),
                Days = WorkflowHarness.Arg<double>(1),
                Hours = WorkflowHarness.Arg<double>(2),
                Minutes = WorkflowHarness.Arg<double>(30),
                Duration = WorkflowHarness.Arg(TimeSpan.FromMinutes(15)),
                DayHandling = DayHandling.AsWholeDays
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            // A whole working day back is Friday 12:30; 2h45m of working time then comes off, and because
            // 12:30 sits in the lunch break the clock resumes at 12:00, landing on 09:15.
            Assert.Equal(new DateTime(2026, 9, 11, 9, 15, 0), outputs["Result"]);
            Assert.Equal(-new TimeSpan(1, 3, 15, 0), outputs["ElapsedTime"]);
        }

        [Fact]
        public void Step6_TheHandlingTimeIgnoresTheWeekend()
        {
            var activity = new GetBusinessTimeBetween
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                From = WorkflowHarness.Arg(TicketCreated),
                To = WorkflowHarness.Arg(TicketAnswered)
            };

            IDictionary<string, object> outputs = WorkflowHarness.Run(activity);

            Assert.Equal(new TimeSpan(5, 45, 0), outputs["Result"]);
            Assert.Equal(5.75, (double)outputs["BusinessHours"], 6);
            Assert.Equal(5.75 / 7.5, (double)outputs["BusinessDays"], 6);
            Assert.Equal(3, outputs["WorkingDays"]);
        }

        [Fact]
        public void Step7_ThreeBusinessDaysSpanTheWeekend()
        {
            Assert.Equal(3, WorkflowHarness.RunFor(new CountBusinessDays
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                From = WorkflowHarness.Arg(TicketCreated),
                To = WorkflowHarness.Arg(TicketAnswered)
            }));
        }

        [Fact]
        public void Step8_SundayIsClosed()
        {
            IDictionary<string, object> outputs = WorkflowHarness.Run(new IsBusinessTime
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                Date = WorkflowHarness.Arg(WeekendMoment)
            });

            Assert.False((bool)outputs["Result"]);
            Assert.False((bool)outputs["IsWorkingDay"]);
            Assert.Equal(string.Empty, outputs["SpecialDayName"]);
        }

        [Fact]
        public void Step9_TheJsonCalendarKnowsAboutChristmasEve()
        {
            const string json = "{\"name\":\"Berlin support desk\",\"timeZone\":\"Europe/Berlin\"," +
                                "\"hoursPerBusinessDay\":7.5,\"week\":\"Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00\"," +
                                "\"specialDays\":[{\"date\":\"01-01\",\"name\":\"New Year Day\",\"annual\":true}," +
                                "{\"date\":\"2026-12-24\",\"name\":\"Christmas Eve\",\"hours\":\"09:00-13:00\"}," +
                                "{\"from\":\"2026-12-27\",\"to\":\"2026-12-31\",\"name\":\"Winter shutdown\"}]}";

            BusinessCalendar fromJson = WorkflowHarness.RunFor(new LoadBusinessCalendar
            {
                Json = WorkflowHarness.Arg(json),
                TimeZoneOverride = CommonTimeZone.Tokyo
            });

            Assert.Equal("Berlin support desk", fromJson.Name);
            Assert.Equal("Tokyo Standard Time", fromJson.TimeZone.Id);

            IDictionary<string, object> outputs = WorkflowHarness.Run(new GetBusinessDayInfo
            {
                Calendar = WorkflowHarness.Arg(fromJson),
                Date = WorkflowHarness.Arg(new DateTime(2026, 12, 24))
            });

            Assert.True((bool)outputs["Result"]);
            Assert.Equal(new DateTime(2026, 12, 24, 9, 0, 0), outputs["DayStart"]);
            Assert.Equal(new DateTime(2026, 12, 24, 13, 0, 0), outputs["DayEnd"]);
            Assert.Equal(TimeSpan.FromHours(4), outputs["WorkingTime"]);
            Assert.Equal("09:00-13:00", outputs["Shifts"]);
            Assert.Equal("Christmas Eve", outputs["SpecialDayName"]);
        }

        [Fact]
        public void Step10_SnappingGoesBothWays()
        {
            IDictionary<string, object> forward = WorkflowHarness.Run(new SnapToBusinessTime
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                Date = WorkflowHarness.Arg(WeekendMoment),
                Direction = SnapDirection.Forward
            });

            IDictionary<string, object> backward = WorkflowHarness.Run(new SnapToBusinessTime
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                Date = WorkflowHarness.Arg(WeekendMoment),
                Direction = SnapDirection.Backward
            });

            Assert.Equal(new DateTime(2026, 9, 14, 9, 0, 0), forward["Result"]);
            Assert.True((bool)forward["WasAdjusted"]);
            Assert.Equal(new DateTime(2026, 9, 12, 13, 0, 0), backward["Result"]);
            Assert.True((bool)backward["WasAdjusted"]);
        }

        [Fact]
        public void Step11_TheNextWorkingDayIsTheSaturdayMorning()
        {
            IDictionary<string, object> next = WorkflowHarness.Run(new GetNextBusinessDay
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                Date = WorkflowHarness.Arg(TicketCreated),
                Direction = DayDirection.Next
            });

            Assert.Equal(new DateTime(2026, 9, 12, 9, 0, 0), next["Result"]);
            Assert.Equal(new DateTime(2026, 9, 12, 13, 0, 0), next["DayEnd"]);
            Assert.Equal(TimeSpan.FromHours(4), next["WorkingTime"]);
            Assert.Equal(string.Empty, next["SpecialDayName"]);

            Assert.Equal(new DateTime(2026, 9, 10, 9, 0, 0), WorkflowHarness.RunFor(new GetNextBusinessDay
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                Date = WorkflowHarness.Arg(TicketCreated),
                Direction = DayDirection.Previous
            }));
        }

        [Fact]
        public void Step12_TheWindowsAddUpToTheMeasuredTime()
        {
            IDictionary<string, object> outputs = WorkflowHarness.Run(new GetWorkingIntervals
            {
                Calendar = WorkflowHarness.Arg(SupportDesk),
                From = WorkflowHarness.Arg(TicketCreated),
                To = WorkflowHarness.Arg(TicketAnswered)
            });

            var intervals = (IList<BusinessTimeInterval>)outputs["Result"];

            Assert.Equal(3, intervals.Count);
            Assert.Equal(new BusinessTimeInterval(new DateTime(2026, 9, 11, 16, 30, 0), new DateTime(2026, 9, 11, 17, 0, 0)), intervals[0]);
            Assert.Equal(new BusinessTimeInterval(new DateTime(2026, 9, 12, 9, 0, 0), new DateTime(2026, 9, 12, 13, 0, 0)), intervals[1]);
            Assert.Equal(new BusinessTimeInterval(new DateTime(2026, 9, 14, 9, 0, 0), new DateTime(2026, 9, 14, 10, 15, 0)), intervals[2]);
            Assert.Equal(new TimeSpan(5, 45, 0), outputs["TotalWorkingTime"]);
        }

        [Fact]
        public void Step13_TheWorkingWeekFallbackNeedsNoCalendar()
        {
            Assert.False(WorkflowHarness.RunFor(new IsBusinessTime
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-17:00"),
                Date = WorkflowHarness.Arg(WeekendMoment)
            }));
        }
    }
}
