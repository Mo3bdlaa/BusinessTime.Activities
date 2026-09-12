using System;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    /// <summary>
    /// Covers the text form of a special day, which is what the calendar editor and any spreadsheet import
    /// hand over. The editor itself is WPF and cannot run here, so this is where its rules are held.
    /// </summary>
    public class SpecialDayTextTests
    {
        [Fact]
        public void APlainDateClosesTheDay()
        {
            SpecialDay day = SpecialDay.FromText("2026-12-25", name: "Christmas Day");

            Assert.Equal(new DateTime(2026, 12, 25), day.Date);
            Assert.True(day.IsNonWorking);
            Assert.Equal("Christmas Day", day.Name);
            Assert.False(day.IsAnnual);
        }

        [Fact]
        public void HoursMakeItAHalfDay()
        {
            SpecialDay day = SpecialDay.FromText("2026-12-24", name: "Christmas Eve", hours: "09:00-13:00");

            Assert.False(day.IsNonWorking);
            Assert.Single(day.Shifts);
            Assert.Equal(TimeSpan.FromHours(4), day.Shifts[0].Duration);
        }

        [Theory]
        [InlineData("off")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void TheWaysOfSayingClosedAllAgree(string hours)
        {
            Assert.True(SpecialDay.FromText("2026-12-25", hours: hours).IsNonWorking);
        }

        [Fact]
        public void AnEndDateMakesItAShutdown()
        {
            SpecialDay day = SpecialDay.FromText("2026-12-27", "2026-12-31", "Winter shutdown");

            Assert.True(day.Covers(new DateTime(2026, 12, 29)));
            Assert.False(day.Covers(new DateTime(2027, 1, 1)));
        }

        [Fact]
        public void AShortDateRepeatsEveryYear()
        {
            SpecialDay day = SpecialDay.FromText("12-25", name: "Christmas Day", isAnnual: true);

            Assert.True(day.Covers(new DateTime(2026, 12, 25)));
            Assert.True(day.Covers(new DateTime(2031, 12, 25)));
            Assert.False(day.Covers(new DateTime(2031, 12, 26)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("the 25th")]
        [InlineData("2026-13-45")]
        public void ADateThatCannotBeReadSaysSo(string date)
        {
            BusinessTimeException error = Assert.Throws<BusinessTimeException>(() => SpecialDay.FromText(date));

            Assert.Contains("date", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BadHoursSayWhichPartIsWrong()
        {
            Assert.Throws<BusinessTimeException>(() => SpecialDay.FromText("2026-12-24", hours: "nine to five"));
        }
    }
}
