using System;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    /// <summary>
    /// Pins the figures the README opens with, since those are the ones people judge the package by.
    /// All of them are on a plain Monday to Friday, 09:00-17:00 week.
    /// </summary>
    public class ReadmeHeadlineTests
    {
        private static BusinessCalendar Office =>
            new BusinessCalendar(WeeklySchedule.Parse("Mon-Fri 09:00-17:00"), timeZone: TimeZoneInfo.Utc);

        private static readonly DateTime FridayAfternoon = new DateTime(2026, 9, 11, 16, 30, 0);
        private static readonly DateTime MondayMorning = new DateTime(2026, 9, 14, 10, 15, 0);

        [Fact]
        public void FridayHalfPastFourPlusFourHoursIsMondayHalfPastTwelve()
        {
            Assert.Equal(new DateTime(2026, 9, 14, 12, 30, 0), Office.Add(FridayAfternoon, TimeSpan.FromHours(4)));
        }

        [Fact]
        public void TheSameStretchIsUnderTwoWorkingHoursEvenThoughAlmostThreeDaysPass()
        {
            Assert.Equal(new TimeSpan(1, 45, 0), Office.GetBusinessTimeBetween(FridayAfternoon, MondayMorning));
            Assert.Equal(new TimeSpan(2, 17, 45, 0), MondayMorning - FridayAfternoon);
        }

        [Fact]
        public void SundayMorningIsClosed()
        {
            Assert.False(Office.IsWorkingTime(new DateTime(2026, 9, 13, 10, 0, 0)));
        }

        [Fact]
        public void ChristmasEveIsAHalfDayOnceYouSaySo()
        {
            BusinessCalendar calendar = Office.ToBuilder()
                .AddSpecialHours(new DateTime(2026, 12, 24), "09:00-13:00", "Christmas Eve")
                .Build();

            Assert.Equal(TimeSpan.FromHours(4), calendar.GetWorkingTimeOnDay(new DateTime(2026, 12, 24)));
            Assert.Equal("Christmas Eve", calendar.GetSpecialDay(new DateTime(2026, 12, 24)).Name);
        }
    }
}
