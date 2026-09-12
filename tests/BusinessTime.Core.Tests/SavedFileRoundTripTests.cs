using System;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    /// <summary>
    /// Loads the exact document the demo wrote, to confirm that what this package saves is what it can read
    /// back, holidays included.
    /// </summary>
    public class SavedFileRoundTripTests
    {
        private const string Saved = @"{
  ""name"": ""Support desk"",
  ""timeZone"": ""W. Europe Standard Time"",
  ""hoursPerBusinessDay"": 7.5,
  ""week"": ""Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00"",
  ""specialDays"": [
    { ""date"": ""2026-10-03"" },
    { ""date"": ""2026-12-25"" },
    { ""date"": ""01-01"", ""annual"": true }
  ]
}";

        [Fact]
        public void TheFileTheDemoWroteLoadsBackIdentically()
        {
            BusinessCalendar calendar = BusinessCalendarSerializer.FromJson(Saved);

            Assert.Equal("Support desk", calendar.Name);
            Assert.Equal("W. Europe Standard Time", calendar.TimeZone.Id);
            Assert.Equal(7.5, calendar.HoursPerBusinessDay.TotalHours);
            Assert.Equal(6, calendar.Schedule.WorkingDaysPerWeek);
            Assert.Equal(39, calendar.Schedule.WeeklyWorkingTime.TotalHours);
        }

        [Fact]
        public void TheHolidaysInTheFileStillApply()
        {
            BusinessCalendar calendar = BusinessCalendarSerializer.FromJson(Saved);

            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 10, 3)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 12, 25)));

            // The annual entry has to hold in every year, not only the one it was written in.
            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 1, 1)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2031, 1, 1)));
            Assert.True(calendar.IsWorkingDay(new DateTime(2031, 1, 2)));
        }

        [Fact]
        public void ANamelessHolidayReportsAnEmptyNameRatherThanFailing()
        {
            BusinessCalendar calendar = BusinessCalendarSerializer.FromJson(Saved);

            SpecialDay holiday = calendar.GetSpecialDay(new DateTime(2026, 12, 25));

            Assert.NotNull(holiday);
            Assert.Null(holiday.Name);
            Assert.True(holiday.IsNonWorking);
        }

        [Fact]
        public void SavingWhatWasLoadedProducesTheSameCalendarAgain()
        {
            BusinessCalendar once = BusinessCalendarSerializer.FromJson(Saved);
            BusinessCalendar twice = BusinessCalendarSerializer.FromJson(BusinessCalendarSerializer.ToJson(once));

            Assert.Equal(once.Schedule.ToString(), twice.Schedule.ToString());
            Assert.Equal(once.TimeZone.Id, twice.TimeZone.Id);
            Assert.Equal(once.HoursPerBusinessDay, twice.HoursPerBusinessDay);
            Assert.Equal(once.SpecialDays.Count, twice.SpecialDays.Count);
            Assert.False(twice.IsWorkingDay(new DateTime(2031, 1, 1)));
        }
    }
}
