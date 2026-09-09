using System;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    public class ScheduleParsingTests
    {
        [Theory]
        [InlineData("09:00-17:00", 9, 0, 17, 0)]
        [InlineData("9-17", 9, 0, 17, 0)]
        [InlineData("9am-5pm", 9, 0, 17, 0)]
        [InlineData("9:30am-5:45pm", 9, 30, 17, 45)]
        [InlineData("12am-12pm", 0, 0, 12, 0)]
        [InlineData("00:00-24:00", 0, 0, 24, 0)]
        public void ParsesWindows(string text, int startHour, int startMinute, int endHour, int endMinute)
        {
            TimeRange range = TimeRange.Parse(text);

            Assert.Equal(new TimeSpan(startHour, startMinute, 0), range.Start);
            Assert.Equal(new TimeSpan(endHour, endMinute, 0), range.End);
        }

        [Fact]
        public void ReadsABackwardsWindowAsANightShift()
        {
            TimeRange range = TimeRange.Parse("22:00-06:00");

            Assert.Equal(TimeSpan.FromHours(22), range.Start);
            Assert.Equal(TimeSpan.FromHours(30), range.End);
            Assert.True(range.SpillsIntoNextDay);
            Assert.Equal(TimeSpan.FromHours(8), range.Duration);
            Assert.Equal("22:00-06:00", range.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("09:00")]
        [InlineData("25:00-26:00")]
        [InlineData("09:00-09:00")]
        [InlineData("nine-five")]
        public void RejectsMalformedWindows(string text)
        {
            Assert.Throws<BusinessTimeException>(() => TimeRange.Parse(text));
        }

        [Fact]
        public void ParsesADayRange()
        {
            WeeklySchedule schedule = WeeklySchedule.Parse("Mon-Fri 09:00-17:00");

            Assert.Equal(5, schedule.WorkingDaysPerWeek);
            Assert.True(schedule[DayOfWeek.Monday].IsWorkingDay);
            Assert.True(schedule[DayOfWeek.Friday].IsWorkingDay);
            Assert.False(schedule[DayOfWeek.Saturday].IsWorkingDay);
            Assert.Equal(TimeSpan.FromHours(40), schedule.WeeklyWorkingTime);
        }

        [Fact]
        public void ParsesSeveralEntriesWithDifferentHours()
        {
            WeeklySchedule schedule = WeeklySchedule.Parse("Mon-Thu 08:00-16:30; Fri 08:00-14:00; Sat,Sun off");

            Assert.Equal(new TimeSpan(16, 30, 0), schedule[DayOfWeek.Thursday].EndOfDay);
            Assert.Equal(TimeSpan.FromHours(14), schedule[DayOfWeek.Friday].EndOfDay);
            Assert.False(schedule[DayOfWeek.Sunday].IsWorkingDay);
            Assert.Equal(5, schedule.WorkingDaysPerWeek);
        }

        [Fact]
        public void ParsesMultipleShiftsInADay()
        {
            WeeklySchedule schedule = WeeklySchedule.Parse("Mon-Fri 09:00-12:00,13:00-17:00");

            Assert.Equal(2, schedule[DayOfWeek.Monday].Shifts.Count);
            Assert.Equal(TimeSpan.FromHours(7), schedule[DayOfWeek.Monday].WorkingTime);
        }

        [Fact]
        public void MergesShiftsThatTouch()
        {
            WeeklySchedule schedule = WeeklySchedule.Parse("Mon 09:00-12:00,12:00-17:00");

            Assert.Single(schedule[DayOfWeek.Monday].Shifts);
            Assert.Equal(TimeSpan.FromHours(8), schedule[DayOfWeek.Monday].WorkingTime);
        }

        [Theory]
        [InlineData("Weekdays 09:00-17:00", 5)]
        [InlineData("Weekend 10:00-14:00", 2)]
        [InlineData("Daily 00:00-24:00", 7)]
        [InlineData("Mon,Wed,Fri 09:00-17:00", 3)]
        [InlineData("Monday - Friday: 09:00-17:00", 5)]
        [InlineData("Fri-Mon 09:00-17:00", 4)]
        public void ParsesDaySpecifications(string text, int expectedWorkingDays)
        {
            Assert.Equal(expectedWorkingDays, WeeklySchedule.Parse(text).WorkingDaysPerWeek);
        }

        [Fact]
        public void LaterEntriesReplaceEarlierOnesForTheSameDay()
        {
            WeeklySchedule schedule = WeeklySchedule.Parse("Mon-Fri 09:00-17:00; Wed 09:00-12:00");

            Assert.Equal(TimeSpan.FromHours(3), schedule[DayOfWeek.Wednesday].WorkingTime);
            Assert.Equal(TimeSpan.FromHours(8), schedule[DayOfWeek.Tuesday].WorkingTime);
        }

        [Fact]
        public void SeparatesEntriesOnLineBreaks()
        {
            WeeklySchedule schedule = WeeklySchedule.Parse("Mon-Fri 09:00-17:00\nSat 09:00-13:00");

            Assert.Equal(6, schedule.WorkingDaysPerWeek);
        }

        [Theory]
        [InlineData("Mon-Fri 09:00-17:00")]
        [InlineData("Mon-Thu 08:00-16:30; Fri 08:00-14:00")]
        [InlineData("Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00")]
        [InlineData("Mon-Sun 22:00-06:00")]
        public void RendersBackIntoAnEquivalentSchedule(string text)
        {
            WeeklySchedule original = WeeklySchedule.Parse(text);
            WeeklySchedule roundTripped = WeeklySchedule.Parse(original.ToString());

            Assert.Equal(original.ToString(), roundTripped.ToString());
            Assert.Equal(original.WeeklyWorkingTime, roundTripped.WeeklyWorkingTime);
        }

        [Theory]
        [InlineData("Funday 09:00-17:00")]
        [InlineData("Mon 09:00")]
        [InlineData("09:00-17:00")]
        public void RejectsMalformedSchedules(string text)
        {
            Assert.Throws<BusinessTimeException>(() => WeeklySchedule.Parse(text));
        }

        [Fact]
        public void ReportsAnEmptyScheduleAsNonWorking()
        {
            Assert.False(WeeklySchedule.Parse("Mon-Sun off").HasWorkingTime);
            Assert.False(WeeklySchedule.Empty.HasWorkingTime);
        }
    }
}
