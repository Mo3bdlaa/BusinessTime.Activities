using System;
using System.Collections.Generic;
using System.Linq;
using BusinessTime;
using Xunit;

namespace BusinessTime.Activities.Tests
{
    /// <summary>
    /// Checks the time zone drop-down. Every entry is a hand-written identifier, so a typo in any one of
    /// them would otherwise only show up when someone picked that city in a real process.
    /// </summary>
    public class TimeZoneDropDownTests
    {
        public static IEnumerable<object[]> EveryChoice =>
            Enum.GetValues(typeof(CommonTimeZone))
                .Cast<CommonTimeZone>()
                .Where(choice => choice != CommonTimeZone.Custom)
                .Select(choice => new object[] { choice });

        [Theory]
        [MemberData(nameof(EveryChoice))]
        public void EveryChoiceInTheDropDownResolvesToARealZone(CommonTimeZone choice)
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                TimeZone = choice
            });

            Assert.NotNull(calendar.TimeZone);
        }

        [Fact]
        public void TheCitiesLandInTheOffsetsYouWouldExpect()
        {
            // A midsummer moment, so the northern zones are on their summer offsets.
            var midsummer = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.Equal(TimeSpan.Zero, OffsetOf(CommonTimeZone.UTC, midsummer));
            Assert.Equal(TimeSpan.FromHours(1), OffsetOf(CommonTimeZone.UTC_00_London, midsummer));
            Assert.Equal(TimeSpan.FromHours(2), OffsetOf(CommonTimeZone.UTC_plus_01_Berlin, midsummer));
            Assert.Equal(TimeSpan.FromHours(5.5), OffsetOf(CommonTimeZone.UTC_plus_05_30_Mumbai, midsummer));
            Assert.Equal(TimeSpan.FromHours(9), OffsetOf(CommonTimeZone.UTC_plus_09_Tokyo, midsummer));
            Assert.Equal(TimeSpan.FromHours(10), OffsetOf(CommonTimeZone.UTC_plus_10_Sydney, midsummer));
            Assert.Equal(TimeSpan.FromHours(-4), OffsetOf(CommonTimeZone.UTC_minus_05_NewYork, midsummer));
            Assert.Equal(TimeSpan.FromHours(-7), OffsetOf(CommonTimeZone.UTC_minus_08_LosAngeles, midsummer));
        }

        [Fact]
        public void CustomTakesAnyIdentifierTheMachineKnows()
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                TimeZone = CommonTimeZone.Custom,
                TimeZoneId = WorkflowHarness.Arg("Europe/Oslo")
            });

            Assert.Equal(TimeSpan.FromHours(2), calendar.TimeZone.GetUtcOffset(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void CustomWithoutAnIdentifierSaysSo()
        {
            BusinessTimeException error = Assert.Throws<BusinessTimeException>(
                () => WorkflowHarness.RunFor(new CreateBusinessCalendar { TimeZone = CommonTimeZone.Custom }));

            Assert.Contains("Time zone id", error.Message);
        }

        [Fact]
        public void AnIdentifierOnItsOwnStillWins()
        {
            // Workflows written before the drop-down existed only set the identifier, and must keep working.
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                TimeZoneId = WorkflowHarness.Arg("Asia/Tokyo")
            });

            Assert.Equal(TimeSpan.FromHours(9), calendar.TimeZone.GetUtcOffset(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc)));
        }

        private static TimeSpan OffsetOf(CommonTimeZone choice, DateTime moment)
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar { TimeZone = choice });
            return calendar.TimeZone.GetUtcOffset(moment);
        }
    }
}

namespace BusinessTime.Activities.Tests
{
    /// <summary>Covers what the MachineLocal choice means once a calendar is saved and moved.</summary>
    public class MachineLocalChoiceTests
    {
        [Fact]
        public void MachineLocalMakesACalendarThatFollowsEachRobot()
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-17:00"),
                TimeZone = CommonTimeZone.MachineLocal
            });

            Assert.True(calendar.FollowsMachineTimeZone);
            Assert.Contains("\"timeZone\": \"Local\"", BusinessCalendarSerializer.ToJson(calendar));
        }

        [Fact]
        public void ACityPinsTheHoursToThatPlace()
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-17:00"),
                TimeZone = CommonTimeZone.UTC_plus_09_Tokyo
            });

            Assert.False(calendar.FollowsMachineTimeZone);
            Assert.Contains("Tokyo", BusinessCalendarSerializer.ToJson(calendar));
        }

        [Fact]
        public void AnIdentifierAlongsideMachineLocalStillPinsIt()
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new CreateBusinessCalendar
            {
                Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-17:00"),
                TimeZoneId = WorkflowHarness.Arg("Europe/Oslo")
            });

            Assert.False(calendar.FollowsMachineTimeZone);
            Assert.Equal("Europe/Oslo", calendar.TimeZone.Id);
        }
    }
}

namespace BusinessTime.Activities.Tests
{
    /// <summary>
    /// The point of a portable calendar is what happens on the robot, not in the editor: the file names no
    /// zone, and whichever machine loads it supplies its own.
    /// </summary>
    public class PortableCalendarTests
    {
        private const string PortableJson = "{ \"name\": \"Follows the robot\", \"week\": \"Mon-Fri 09:00-17:00\", \"timeZone\": \"Local\" }";

        [Fact]
        public void LoadBusinessCalendarStillGivesAnOrdinaryCalendarVariable()
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new LoadBusinessCalendar
            {
                Json = WorkflowHarness.Arg(PortableJson)
            });

            Assert.Equal("Follows the robot", calendar.Name);
            Assert.True(calendar.FollowsMachineTimeZone);

            // Resolved on the machine doing the loading, which on a robot is that robot.
            Assert.Equal(TimeZoneInfo.Local.Id, calendar.TimeZone.Id);
        }

        [Fact]
        public void ThatVariableWorksInEveryOtherActivityAsBefore()
        {
            BusinessCalendar calendar = WorkflowHarness.RunFor(new LoadBusinessCalendar
            {
                Json = WorkflowHarness.Arg(PortableJson)
            });

            // A Friday at 14:00 in the robot's own time, plus eight business hours.
            var friday = new DateTime(2026, 9, 11, 14, 0, 0);

            DateTime result = WorkflowHarness.RunFor(new AddBusinessTime
            {
                Calendar = WorkflowHarness.Arg(calendar),
                Date = WorkflowHarness.Arg(friday),
                Hours = WorkflowHarness.Arg<double>(8)
            });

            Assert.Equal(new DateTime(2026, 9, 14, 14, 0, 0), result);
        }

        [Fact]
        public void SavingAndLoadingItAgainKeepsItPortable()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

            try
            {
                BusinessCalendar built = WorkflowHarness.RunFor(new CreateBusinessCalendar
                {
                    Schedule = WorkflowHarness.Arg("Mon-Fri 09:00-17:00"),
                    TimeZone = CommonTimeZone.MachineLocal
                });

                WorkflowHarness.Run(new SaveBusinessCalendar
                {
                    Calendar = WorkflowHarness.Arg(built),
                    FilePath = WorkflowHarness.Arg(path)
                });

                Assert.Contains("\"timeZone\": \"Local\"", System.IO.File.ReadAllText(path));

                BusinessCalendar reloaded = WorkflowHarness.RunFor(new LoadBusinessCalendar
                {
                    FilePath = WorkflowHarness.Arg(path)
                });

                Assert.True(reloaded.FollowsMachineTimeZone);
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }
    }
}
