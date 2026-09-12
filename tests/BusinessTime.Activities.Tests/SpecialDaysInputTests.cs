using System;
using System.Collections.Generic;
using System.IO;
using BusinessTime;
using Xunit;

namespace BusinessTime.Activities.Tests
{
    /// <summary>
    /// Covers the Special days input, which is what lets a calendar built in Studio say the same things a
    /// hand-written calendar file can: why a day is closed, and which days are only half worked.
    /// </summary>
    public class SpecialDaysInputTests
    {
        private static BusinessCalendar Build(params SpecialDay[] specialDays) =>
            WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                Name = WorkflowHarness.Arg("Support desk"),
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-17:00"),
                TimeZone = CommonTimeZone.UTC,
                SpecialDays = WorkflowHarness.Arg<IEnumerable<SpecialDay>>(specialDays)
            });

        [Fact]
        public void ANamedHolidayKeepsItsName()
        {
            BusinessCalendar calendar = Build(SpecialDay.Holiday(new DateTime(2026, 12, 25), "Christmas Day"));

            IDictionary<string, object> outputs = WorkflowHarness.Run(new IsBusinessTime
            {
                Calendar = WorkflowHarness.Arg(calendar),
                Date = WorkflowHarness.Arg(new DateTime(2026, 12, 25, 10, 0, 0))
            });

            Assert.False((bool)outputs["Result"]);
            Assert.False((bool)outputs["IsWorkingDay"]);
            Assert.Equal("Christmas Day", outputs["SpecialDayName"]);
        }

        [Fact]
        public void AHalfDayIsWorkedButOnlyForItsOwnHours()
        {
            BusinessCalendar calendar = Build(
                SpecialDay.CustomHours(new DateTime(2026, 12, 24), "09:00-13:00", "Christmas Eve"));

            IDictionary<string, object> outputs = WorkflowHarness.Run(new GetBusinessDayInfo
            {
                Calendar = WorkflowHarness.Arg(calendar),
                Date = WorkflowHarness.Arg(new DateTime(2026, 12, 24))
            });

            Assert.True((bool)outputs["Result"]);
            Assert.Equal(new DateTime(2026, 12, 24, 13, 0, 0), outputs["DayEnd"]);
            Assert.Equal(TimeSpan.FromHours(4), outputs["WorkingTime"]);
            Assert.Equal("09:00-13:00", outputs["Shifts"]);
            Assert.Equal("Christmas Eve", outputs["SpecialDayName"]);
        }

        [Fact]
        public void AShutdownClosesEveryDayInItsRange()
        {
            BusinessCalendar calendar = Build(
                SpecialDay.Shutdown(new DateTime(2026, 12, 27), new DateTime(2026, 12, 31), "Winter shutdown"));

            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 12, 28)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 12, 31)));
            Assert.True(calendar.IsWorkingDay(new DateTime(2027, 1, 1)));
            Assert.Equal("Winter shutdown", calendar.GetSpecialDay(new DateTime(2026, 12, 30)).Name);
        }

        [Fact]
        public void AnAnnualHolidayRepeatsAndKeepsItsName()
        {
            BusinessCalendar calendar = Build(SpecialDay.AnnualHoliday(1, 1, "New Year's Day"));

            Assert.False(calendar.IsWorkingDay(new DateTime(2031, 1, 1)));
            Assert.Equal("New Year's Day", calendar.GetSpecialDay(new DateTime(2031, 1, 1)).Name);
        }

        [Fact]
        public void ANamedEntryWinsOverAPlainHolidayForTheSameDay()
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-17:00"),
                TimeZone = CommonTimeZone.UTC,
                Holidays = WorkflowHarness.Arg<IEnumerable<DateTime>>(new[] { new DateTime(2026, 12, 24) }),
                SpecialDays = WorkflowHarness.Arg<IEnumerable<SpecialDay>>(new[]
                {
                    SpecialDay.CustomHours(new DateTime(2026, 12, 24), "09:00-13:00", "Christmas Eve")
                })
            });

            Assert.True(calendar.IsWorkingDay(new DateTime(2026, 12, 24)));
            Assert.Equal("Christmas Eve", calendar.GetSpecialDay(new DateTime(2026, 12, 24)).Name);
        }

        [Fact]
        public void TheNamesSurviveBeingSavedAndLoadedAgain()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

            try
            {
                BusinessCalendar calendar = Build(
                    SpecialDay.AnnualHoliday(1, 1, "New Year's Day"),
                    SpecialDay.Holiday(new DateTime(2026, 12, 25), "Christmas Day"),
                    SpecialDay.CustomHours(new DateTime(2026, 12, 24), "09:00-13:00", "Christmas Eve"),
                    SpecialDay.Shutdown(new DateTime(2026, 12, 27), new DateTime(2026, 12, 31), "Winter shutdown"));

                WorkflowHarness.Run(new SaveBusinessCalendar
                {
                    Calendar = WorkflowHarness.Arg(calendar),
                    FilePath = WorkflowHarness.Arg(path)
                });

                // The written document has to carry the names, which is the whole point of this input.
                string written = File.ReadAllText(path);
                Assert.Contains("Christmas Eve", written);
                Assert.Contains("Winter shutdown", written);

                BusinessCalendar reloaded = WorkflowHarness.RunFor(new LoadBusinessCalendar
                {
                    FilePath = WorkflowHarness.Arg(path)
                });

                Assert.Equal("Christmas Day", reloaded.GetSpecialDay(new DateTime(2026, 12, 25)).Name);
                Assert.Equal("Christmas Eve", reloaded.GetSpecialDay(new DateTime(2026, 12, 24)).Name);
                Assert.Equal("Winter shutdown", reloaded.GetSpecialDay(new DateTime(2026, 12, 29)).Name);
                Assert.Equal("New Year's Day", reloaded.GetSpecialDay(new DateTime(2032, 1, 1)).Name);
                Assert.Equal(TimeSpan.FromHours(4), reloaded.GetWorkingTimeOnDay(new DateTime(2026, 12, 24)));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
