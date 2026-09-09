using System;
using System.Collections.Generic;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    public class BusinessTimeArithmeticTests
    {
        // 2026-01-05 is a Monday, so the whole week lines up predictably in these tests.
        private static readonly DateTime Monday = new DateTime(2026, 1, 5);
        private static readonly DateTime Friday = new DateTime(2026, 1, 9);

        private static BusinessCalendar NineToFive =>
            new BusinessCalendar(WeeklySchedule.Parse("Mon-Fri 09:00-17:00"), timeZone: TimeZoneInfo.Utc);

        private static BusinessCalendar WithLunchBreak =>
            new BusinessCalendar(WeeklySchedule.Parse("Mon-Fri 09:00-12:00,13:00-17:00"), timeZone: TimeZoneInfo.Utc);

        [Fact]
        public void EightHoursFromFridayAfternoonLandsOnMondayAfternoon()
        {
            DateTime result = NineToFive.Add(Friday.AddHours(14), TimeSpan.FromHours(8));

            Assert.Equal(Monday.AddDays(7).AddHours(14), result);
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        }

        [Fact]
        public void AddingWithinASingleDayJustMovesTheClock()
        {
            Assert.Equal(Monday.AddHours(13), NineToFive.Add(Monday.AddHours(10), TimeSpan.FromHours(3)));
        }

        [Fact]
        public void AddingSkipsTheLunchBreak()
        {
            // 11:00 + 2h: one hour to noon, then the second hour resumes at 13:00.
            Assert.Equal(Monday.AddHours(14), WithLunchBreak.Add(Monday.AddHours(11), TimeSpan.FromHours(2)));
        }

        [Fact]
        public void ADurationThatExactlyFillsTheDayStopsAtClosingTime()
        {
            // Landing on a boundary reports the moment work stopped, not the next morning.
            Assert.Equal(Monday.AddHours(17), NineToFive.Add(Monday.AddHours(9), TimeSpan.FromHours(8)));
        }

        [Fact]
        public void OneMinutePastClosingTimeRollsIntoTheNextMorning()
        {
            Assert.Equal(Monday.AddDays(1).AddHours(9).AddMinutes(1),
                NineToFive.Add(Monday.AddHours(9), TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(1))));
        }

        [Fact]
        public void TheClockOnlyStartsRunningAtTheNextWorkingMoment()
        {
            // Saturday and Sunday contribute nothing, so the two hours are spent on Monday morning.
            DateTime saturday = Friday.AddDays(1).AddHours(10);

            Assert.Equal(Monday.AddDays(7).AddHours(11), NineToFive.Add(saturday, TimeSpan.FromHours(2)));
        }

        [Fact]
        public void AddingBeforeOpeningTimeStartsAtOpeningTime()
        {
            Assert.Equal(Monday.AddHours(10), NineToFive.Add(Monday.AddHours(6), TimeSpan.FromHours(1)));
        }

        [Fact]
        public void AddingZeroLeavesTheMomentUntouched()
        {
            DateTime saturday = Friday.AddDays(1).AddHours(10);

            Assert.Equal(saturday, NineToFive.Add(saturday, TimeSpan.Zero));
        }

        [Fact]
        public void SubtractingWalksBackwardsOverTheWeekend()
        {
            // Monday 11:00 less 4 hours: two hours back to Monday 09:00, two more from Friday 17:00.
            Assert.Equal(Friday.AddDays(-7).AddHours(15), NineToFive.Subtract(Monday.AddHours(11), TimeSpan.FromHours(4)));
        }

        [Fact]
        public void SubtractingSkipsTheLunchBreak()
        {
            Assert.Equal(Monday.AddHours(11), WithLunchBreak.Subtract(Monday.AddHours(14), TimeSpan.FromHours(2)));
        }

        [Fact]
        public void SubtractingIsTheSameAsAddingANegativeDuration()
        {
            DateTime start = Friday.AddHours(10);

            Assert.Equal(NineToFive.Add(start, TimeSpan.FromHours(-13)), NineToFive.Subtract(start, TimeSpan.FromHours(13)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7.5)]
        [InlineData(40)]
        [InlineData(123.25)]
        public void AddingThenSubtractingReturnsToTheStartingMoment(double hours)
        {
            BusinessCalendar calendar = WithLunchBreak;
            DateTime start = Monday.AddHours(9.5);

            DateTime forward = calendar.Add(start, TimeSpan.FromHours(hours));

            Assert.Equal(start, calendar.Subtract(forward, TimeSpan.FromHours(hours)));
        }

        [Fact]
        public void AddedTimeIsExactlyWhatIsMeasuredBack()
        {
            BusinessCalendar calendar = WithLunchBreak;
            DateTime start = Monday.AddHours(10);
            TimeSpan duration = TimeSpan.FromHours(37.25);

            DateTime end = calendar.Add(start, duration);

            Assert.Equal(duration, calendar.GetBusinessTimeBetween(start, end));
        }

        [Fact]
        public void BusinessDaysUseTheCalendarsOwnDayLength()
        {
            BusinessCalendar calendar = NineToFive;

            Assert.Equal(TimeSpan.FromHours(8), calendar.HoursPerBusinessDay);
            Assert.Equal(Monday.AddDays(1).AddHours(14), calendar.AddBusinessDays(Monday.AddHours(14), 1));
            Assert.Equal(Monday.AddHours(13), calendar.AddBusinessDays(Monday.AddHours(9), 0.5));
        }

        [Fact]
        public void AFridayDeadlineOfThreeBusinessDaysLandsOnWednesday()
        {
            Assert.Equal(Monday.AddDays(9).AddHours(10), NineToFive.AddBusinessDays(Friday.AddHours(10), 3));
        }

        [Fact]
        public void WholeWorkingDaysKeepTheTimeOfDay()
        {
            DateTime result = NineToFive.AddWorkingDays(Friday.AddHours(14), 1);

            Assert.Equal(Monday.AddDays(7).AddHours(14), result);
        }

        [Fact]
        public void WholeWorkingDaysWalkBackwards()
        {
            Assert.Equal(Friday.AddDays(-7).AddHours(14), NineToFive.AddWorkingDays(Monday.AddHours(14), -1));
        }

        [Fact]
        public void MeasuringWorkingTimeIgnoresClosedHours()
        {
            // Friday 16:00 to Monday 10:00 is one hour on Friday plus one on Monday.
            TimeSpan measured = NineToFive.GetBusinessTimeBetween(Friday.AddHours(16), Monday.AddDays(7).AddHours(10));

            Assert.Equal(TimeSpan.FromHours(2), measured);
        }

        [Fact]
        public void MeasuringBackwardsGivesANegativeResult()
        {
            DateTime from = Friday.AddHours(16);
            DateTime to = Monday.AddDays(7).AddHours(10);

            Assert.Equal(-NineToFive.GetBusinessTimeBetween(from, to), NineToFive.GetBusinessTimeBetween(to, from));
        }

        [Fact]
        public void MeasuringAcrossAFullWeekGivesTheWeeklyTotal()
        {
            Assert.Equal(TimeSpan.FromHours(40), NineToFive.GetBusinessTimeBetween(Monday, Monday.AddDays(7)));
            Assert.Equal(5, NineToFive.GetBusinessDaysBetween(Monday, Monday.AddDays(7)), 6);
        }

        [Fact]
        public void MeasuringWithinClosedHoursGivesZero()
        {
            DateTime saturday = Friday.AddDays(1);

            Assert.Equal(TimeSpan.Zero, NineToFive.GetBusinessTimeBetween(saturday.AddHours(9), saturday.AddHours(17)));
        }

        [Fact]
        public void RecognisesWorkingTime()
        {
            BusinessCalendar calendar = WithLunchBreak;

            Assert.True(calendar.IsWorkingTime(Monday.AddHours(9)));
            Assert.True(calendar.IsWorkingTime(Monday.AddHours(16.99)));
            Assert.False(calendar.IsWorkingTime(Monday.AddHours(12.5)));
            Assert.False(calendar.IsWorkingTime(Monday.AddHours(8.99)));
            Assert.False(calendar.IsWorkingTime(Friday.AddDays(1).AddHours(10)));
        }

        [Fact]
        public void ClosingTimeItselfIsNotWorkingTime()
        {
            Assert.False(NineToFive.IsWorkingTime(Monday.AddHours(17)));
            Assert.True(NineToFive.IsWorkingTime(Monday.AddHours(17).AddTicks(-1)));
        }

        [Fact]
        public void SnappingForwardMovesOntoTheNextOpenWindow()
        {
            BusinessCalendar calendar = WithLunchBreak;

            Assert.Equal(Monday.AddHours(13), calendar.SnapForward(Monday.AddHours(12.5)));
            Assert.Equal(Monday.AddDays(7).AddHours(9), calendar.SnapForward(Friday.AddDays(1).AddHours(10)));
            Assert.Equal(Monday.AddHours(10), calendar.SnapForward(Monday.AddHours(10)));
        }

        [Fact]
        public void SnappingBackwardMovesOntoThePreviousClosingMoment()
        {
            BusinessCalendar calendar = WithLunchBreak;

            Assert.Equal(Monday.AddHours(12), calendar.SnapBackward(Monday.AddHours(12.5)));
            Assert.Equal(Friday.AddHours(17), calendar.SnapBackward(Friday.AddDays(1).AddHours(10)));
            Assert.Equal(Monday.AddHours(10), calendar.SnapBackward(Monday.AddHours(10)));
        }

        [Fact]
        public void ReportsTheBoundariesOfABusinessDay()
        {
            BusinessCalendar calendar = WithLunchBreak;

            Assert.Equal(Monday.AddHours(9), calendar.GetStartOfBusinessDay(Monday.AddHours(15)));
            Assert.Equal(Monday.AddHours(17), calendar.GetEndOfBusinessDay(Monday.AddHours(15)));
            Assert.Equal(TimeSpan.FromHours(7), calendar.GetWorkingTimeOnDay(Monday));
            Assert.Null(calendar.GetStartOfBusinessDay(Friday.AddDays(1)));
        }

        [Fact]
        public void CountsWorkingDaysInclusively()
        {
            Assert.Equal(5, NineToFive.CountWorkingDays(Monday, Friday));
            Assert.Equal(5, NineToFive.CountWorkingDays(Monday, Friday.AddDays(2)));
            Assert.Equal(1, NineToFive.CountWorkingDays(Monday, Monday));
            Assert.Equal(0, NineToFive.CountWorkingDays(Friday.AddDays(1), Friday.AddDays(2)));
            Assert.Equal(-5, NineToFive.CountWorkingDays(Friday, Monday));
        }

        [Fact]
        public void ListsTheWorkingWindowsInAPeriod()
        {
            IReadOnlyList<BusinessTimeInterval> windows =
                WithLunchBreak.GetWorkingIntervals(Friday.AddHours(11), Monday.AddDays(7).AddHours(10));

            Assert.Equal(3, windows.Count);
            Assert.Equal(new BusinessTimeInterval(Friday.AddHours(11), Friday.AddHours(12)), windows[0]);
            Assert.Equal(new BusinessTimeInterval(Friday.AddHours(13), Friday.AddHours(17)), windows[1]);
            Assert.Equal(new BusinessTimeInterval(Monday.AddDays(7).AddHours(9), Monday.AddDays(7).AddHours(10)), windows[2]);
        }

        [Fact]
        public void AroundTheClockCalendarsMeasureElapsedTime()
        {
            var calendar = new BusinessCalendar(WeeklySchedule.Continuous, timeZone: TimeZoneInfo.Utc);
            DateTime start = Friday.AddHours(14);

            Assert.Equal(start.AddHours(30), calendar.Add(start, TimeSpan.FromHours(30)));
            Assert.Equal(TimeSpan.FromDays(7), calendar.GetBusinessTimeBetween(start, start.AddDays(7)));
            Assert.True(calendar.IsWorkingTime(Friday.AddDays(1).AddHours(3)));
        }

        [Fact]
        public void ACalendarWithoutWorkingTimeFailsWithAClearMessage()
        {
            var calendar = new BusinessCalendar(WeeklySchedule.Empty, timeZone: TimeZoneInfo.Utc, name: "Closed");

            BusinessTimeException error = Assert.Throws<BusinessTimeException>(
                () => calendar.Add(Monday, TimeSpan.FromHours(1)));

            Assert.Contains("no working time", error.Message);
        }

        [Fact]
        public void LongDurationsAreHandledWithoutDrifting()
        {
            BusinessCalendar calendar = NineToFive;
            DateTime start = Monday.AddHours(9);

            // 260 working weeks of 40 hours is exactly five years of Mondays.
            DateTime end = calendar.Add(start, TimeSpan.FromHours(40 * 260));

            Assert.Equal(TimeSpan.FromHours(40 * 260), calendar.GetBusinessTimeBetween(start, end));
            Assert.Equal(DayOfWeek.Friday, end.DayOfWeek);
            Assert.Equal(17, end.Hour);
        }
    }
}
