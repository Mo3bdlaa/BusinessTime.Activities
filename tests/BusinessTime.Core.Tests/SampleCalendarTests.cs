using System;
using System.IO;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    /// <summary>Keeps the calendar shipped in <c>samples/</c> honest, since the README points people at it.</summary>
    public class SampleCalendarTests
    {
        private static string SamplePath
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "samples")))
                    directory = directory.Parent;

                Assert.NotNull(directory);
                return Path.Combine(directory.FullName, "samples", "support-desk-calendar.json");
            }
        }

        [Fact]
        public void TheSampleCalendarLoadsAndBehavesAsDocumented()
        {
            BusinessCalendar calendar = BusinessCalendarSerializer.LoadFile(SamplePath);

            Assert.Equal("Germany - support desk", calendar.Name);
            Assert.Equal("Europe/Berlin", calendar.TimeZone.Id);
            Assert.Equal(TimeSpan.FromHours(7), calendar.HoursPerBusinessDay);
            Assert.Equal(6, calendar.Schedule.WorkingDaysPerWeek);

            // The annual entries repeat, the shutdown covers a range, and the half day is still worked.
            Assert.False(calendar.IsWorkingDay(new DateTime(2029, 12, 25)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 12, 30)));
            Assert.Equal(TimeSpan.FromHours(4), calendar.GetWorkingTimeOnDay(new DateTime(2026, 12, 24)));

            // A Saturday that is normally closed, opened for a catch-up day.
            Assert.Equal(TimeSpan.FromHours(4), calendar.GetWorkingTimeOnDay(new DateTime(2026, 1, 10)));
        }

        [Fact]
        public void TheReadmeExampleHoldsOnTheSampleCalendar()
        {
            BusinessCalendar calendar = BusinessCalendarSerializer.LoadFile(SamplePath);

            // A ticket raised on Friday 2 January 2026 at 16:30, with four business hours to answer it.
            var raised = new DateTime(2026, 1, 2, 16, 30, 0);
            DateTime dueAt = calendar.Add(raised, TimeSpan.FromHours(4));

            // Half an hour is left on Friday; Saturday opens 09:00-13:00 and absorbs the rest.
            Assert.Equal(new DateTime(2026, 1, 3, 12, 30, 0), dueAt);
            Assert.Equal(TimeSpan.FromHours(4), calendar.GetBusinessTimeBetween(raised, dueAt));
        }
    }
}
