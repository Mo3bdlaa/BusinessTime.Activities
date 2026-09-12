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
            Assert.Equal(TimeSpan.FromHours(1), OffsetOf(CommonTimeZone.London, midsummer));
            Assert.Equal(TimeSpan.FromHours(2), OffsetOf(CommonTimeZone.Berlin, midsummer));
            Assert.Equal(TimeSpan.FromHours(5.5), OffsetOf(CommonTimeZone.Mumbai, midsummer));
            Assert.Equal(TimeSpan.FromHours(9), OffsetOf(CommonTimeZone.Tokyo, midsummer));
            Assert.Equal(TimeSpan.FromHours(10), OffsetOf(CommonTimeZone.Sydney, midsummer));
            Assert.Equal(TimeSpan.FromHours(-4), OffsetOf(CommonTimeZone.NewYork, midsummer));
            Assert.Equal(TimeSpan.FromHours(-7), OffsetOf(CommonTimeZone.LosAngeles, midsummer));
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
