using System;
using System.Collections.Generic;
using System.Linq;
using BusinessTime;
using BusinessTime.Activities.Wizard;
using Xunit;

namespace BusinessTime.Tests
{
    /// <summary>
    /// Covers the time zone list the calendar editor shows. The window itself is WPF and cannot run here,
    /// but which zones are offered, in what order, and how they read is ordinary logic.
    /// </summary>
    public class TimeZoneChoiceTests
    {
        [Fact]
        public void TheMachinesOwnZoneIsOfferedFirst()
        {
            IReadOnlyList<TimeZoneChoice> choices = TimeZoneChoice.All();

            Assert.True(choices[0].IsSystemDefault);
            Assert.StartsWith("System default", choices[0].Display);
            Assert.Equal(TimeZoneInfo.Local.Id, choices[0].Id);
        }

        [Fact]
        public void EveryZoneTheMachineKnowsIsOffered()
        {
            IReadOnlyList<TimeZoneChoice> choices = TimeZoneChoice.All();

            Assert.Equal(TimeZoneInfo.GetSystemTimeZones().Count + 1, choices.Count);
        }

        [Fact]
        public void EachOneShowsItsOffsetAsWellAsItsName()
        {
            // Identifiers differ by platform, so rather than name a zone, hold every listed zone to the
            // rule: the offset in brackets, then the identifier this machine knows it by.
            foreach (TimeZoneChoice choice in TimeZoneChoice.All().Skip(1))
            {
                TimeSpan offset = TimeZoneInfo.FindSystemTimeZoneById(choice.Id).BaseUtcOffset;
                string sign = offset < TimeSpan.Zero ? "-" : "+";
                TimeSpan size = offset < TimeSpan.Zero ? offset.Negate() : offset;

                Assert.Equal($"(UTC{sign}{(int)size.TotalHours:00}:{size.Minutes:00}) {choice.Id}", choice.Display);
            }
        }

        [Fact]
        public void ZonesBehindUtcReadAsMinus()
        {
            TimeZoneChoice behind = TimeZoneChoice.All()
                .Skip(1)
                .First(choice => TimeZoneInfo.FindSystemTimeZoneById(choice.Id).BaseUtcOffset < TimeSpan.Zero);

            Assert.StartsWith("(UTC-", behind.Display);
        }

        [Fact]
        public void HalfHourOffsetsAreNotRoundedAway()
        {
            TimeZoneChoice halfHour = TimeZoneChoice.All()
                .Skip(1)
                .FirstOrDefault(choice => TimeZoneInfo.FindSystemTimeZoneById(choice.Id).BaseUtcOffset.Minutes == 30);

            Assert.NotNull(halfHour);
            Assert.Contains(":30) ", halfHour.Display);
        }

        [Fact]
        public void TheListRunsWestToEast()
        {
            TimeZoneChoice[] zones = TimeZoneChoice.All().Skip(1).ToArray();

            TimeSpan[] offsets = zones
                .Select(choice => TimeZoneInfo.FindSystemTimeZoneById(choice.Id).BaseUtcOffset)
                .ToArray();

            Assert.Equal(offsets.OrderBy(offset => offset).ToArray(), offsets);
        }

        [Fact]
        public void AStoredZoneComesBackSelected()
        {
            IReadOnlyList<TimeZoneChoice> choices = TimeZoneChoice.All();
            string anyZone = choices[5].Id;

            Assert.Equal(anyZone, TimeZoneChoice.For(choices, anyZone).Id);
            Assert.Equal(anyZone, TimeZoneChoice.For(choices, anyZone.ToUpperInvariant()).Id);
        }

        [Fact]
        public void AZoneThisMachineDoesNotKnowFallsBackToTheDefault()
        {
            Assert.True(TimeZoneChoice.For(TimeZoneChoice.All(), "Middle/Earth").IsSystemDefault);
        }
    }
}
