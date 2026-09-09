using System;
using System.Collections.Generic;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    public class CalendarDefinitionTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 1, 5);

        [Fact]
        public void HolidaysAreSkipped()
        {
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .AddHoliday(Monday.AddDays(1), "Company day")
                .Build();

            Assert.False(calendar.IsWorkingDay(Monday.AddDays(1)));

            // Monday 16:00 plus two hours: one hour left on Monday, Tuesday is closed, so Wednesday 10:00.
            Assert.Equal(Monday.AddDays(2).AddHours(10), calendar.Add(Monday.AddHours(16), TimeSpan.FromHours(2)));
            Assert.Equal(4, calendar.CountWorkingDays(Monday, Monday.AddDays(4)));
        }

        [Fact]
        public void AnnualHolidaysRepeatEveryYear()
        {
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Daily 09:00-17:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .AddAnnualHoliday(1, 1, "New Year's Day")
                .Build();

            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 1, 1)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2031, 1, 1)));
            Assert.True(calendar.IsWorkingDay(new DateTime(2031, 1, 2)));
            Assert.Equal("New Year's Day", calendar.GetSpecialDay(new DateTime(2029, 1, 1)).Name);
        }

        [Fact]
        public void ShutdownsCoverAWholeRange()
        {
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .AddShutdown(new DateTime(2026, 12, 24), new DateTime(2027, 1, 1), "Winter shutdown")
                .Build();

            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 12, 24)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 12, 31)));
            Assert.True(calendar.IsWorkingDay(new DateTime(2026, 12, 23)));

            // The first working moment after the shutdown is Monday 4 January.
            Assert.Equal(new DateTime(2027, 1, 4, 9, 0, 0), calendar.SnapForward(new DateTime(2026, 12, 24, 9, 0, 0)));
        }

        [Fact]
        public void SpecialHoursOverrideTheWeeklySchedule()
        {
            DateTime christmasEve = new DateTime(2026, 12, 24);

            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .AddSpecialHours(christmasEve, "09:00-13:00", "Christmas Eve")
                .Build();

            Assert.True(calendar.IsWorkingDay(christmasEve));
            Assert.Equal(TimeSpan.FromHours(4), calendar.GetWorkingTimeOnDay(christmasEve));
            Assert.False(calendar.IsWorkingTime(christmasEve.AddHours(14)));
        }

        [Fact]
        public void ExceptionalWorkingDaysCanBeAddedToTheWeekend()
        {
            DateTime saturday = new DateTime(2026, 1, 10);

            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .AddSpecialHours(saturday, "10:00-14:00", "Catch-up day")
                .Build();

            Assert.True(calendar.IsWorkingDay(saturday));

            // Friday closes at 17:00, so the next hour of work is the first hour of the catch-up day.
            Assert.Equal(saturday.AddHours(11), calendar.Add(new DateTime(2026, 1, 9, 17, 0, 0), TimeSpan.FromHours(1)));
            Assert.Equal(TimeSpan.FromHours(4), calendar.GetBusinessTimeBetween(saturday, saturday.AddDays(1)));
        }

        [Fact]
        public void TheLastMatchingSpecialDayWins()
        {
            DateTime inShutdown = new DateTime(2026, 12, 29);

            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .AddShutdown(new DateTime(2026, 12, 24), new DateTime(2027, 1, 1), "Winter shutdown")
                .AddSpecialHours(inShutdown, "10:00-12:00", "Skeleton crew")
                .Build();

            Assert.True(calendar.IsWorkingDay(inShutdown));
            Assert.Equal(TimeSpan.FromHours(2), calendar.GetWorkingTimeOnDay(inShutdown));
        }

        [Fact]
        public void NightShiftsRunPastMidnight()
        {
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 22:00-06:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .Build();

            Assert.True(calendar.IsWorkingTime(Monday.AddHours(23)));
            Assert.True(calendar.IsWorkingTime(Monday.AddDays(1).AddHours(2)));
            Assert.False(calendar.IsWorkingTime(Monday.AddDays(1).AddHours(7)));

            // Monday 23:00 plus four hours stays inside the same shift.
            Assert.Equal(Monday.AddDays(1).AddHours(3), calendar.Add(Monday.AddHours(23), TimeSpan.FromHours(4)));

            // Monday 04:00 belongs to Sunday's shift, which this calendar does not have, so work starts at 22:00.
            Assert.Equal(Monday.AddHours(23), calendar.Add(Monday.AddHours(4), TimeSpan.FromHours(1)));
            Assert.Equal(TimeSpan.FromHours(8), calendar.GetWorkingTimeOnDay(Monday));
        }

        [Fact]
        public void OverlappingNightShiftsAreNotCountedTwice()
        {
            // The Monday shift runs to 06:00 on Tuesday, overlapping Tuesday's own early start.
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon 22:00-06:00; Tue 00:00-08:00")
                .WithTimeZone(TimeZoneInfo.Utc)
                .Build();

            Assert.Equal(TimeSpan.FromHours(10), calendar.GetBusinessTimeBetween(Monday, Monday.AddDays(2)));
        }

        [Fact]
        public void TheBusinessDayLengthFollowsTheScheduleUnlessOverridden()
        {
            // Four days of 8.5 hours plus a six hour Friday is a 40 hour week over five days.
            BusinessCalendar derived = BusinessCalendar.FromSchedule("Mon-Thu 08:00-16:30; Fri 08:00-14:00");
            Assert.Equal(TimeSpan.FromHours(8), derived.HoursPerBusinessDay);

            BusinessCalendar overridden = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithHoursPerBusinessDay(7.5)
                .Build();
            Assert.Equal(TimeSpan.FromHours(7.5), overridden.HoursPerBusinessDay);
        }

        [Fact]
        public void ABuilderCanStartFromAnExistingCalendar()
        {
            BusinessCalendar original = BusinessCalendar.Create()
                .WithName("Original")
                .WithSchedule("Mon-Fri 09:00-17:00")
                .AddAnnualHoliday(1, 1, "New Year's Day")
                .Build();

            BusinessCalendar extended = original.ToBuilder()
                .WithName("Extended")
                .WithWorkingDay(DayOfWeek.Saturday, "09:00-13:00")
                .Build();

            Assert.Equal("Extended", extended.Name);
            Assert.Equal(6, extended.Schedule.WorkingDaysPerWeek);
            Assert.Single(extended.SpecialDays);
            Assert.Equal(5, original.Schedule.WorkingDaysPerWeek);
        }

        [Fact]
        public void DaysCanBeTurnedOffOneAtATime()
        {
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithDayOff(DayOfWeek.Wednesday)
                .Build();

            Assert.False(calendar.IsWorkingDay(Monday.AddDays(2)));
            Assert.Equal(4, calendar.Schedule.WorkingDaysPerWeek);
        }

        [Fact]
        public void UnknownTimeZonesFailWithAHelpfulMessage()
        {
            BusinessTimeException error = Assert.Throws<BusinessTimeException>(() => TimeZones.Resolve("Middle/Earth"));

            Assert.Contains("not a time zone", error.Message);
        }

        [Fact]
        public void CommonTimeZoneSpellingsResolve()
        {
            Assert.Equal(TimeZoneInfo.Utc, TimeZones.Resolve("UTC"));
            Assert.Equal(TimeZoneInfo.Local, TimeZones.Resolve(null));
            Assert.Equal(TimeZoneInfo.Local, TimeZones.Resolve("  "));
            Assert.NotNull(TimeZones.Resolve("Europe/Berlin"));
        }
    }
}
