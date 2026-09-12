using System;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    /// <summary>
    /// Covers a calendar whose hours mean local time wherever it runs, as against one pinned to a place.
    /// The difference only shows when the file travels, so it is the file that has to carry the intent.
    /// </summary>
    public class MachineTimeZoneTests
    {
        [Fact]
        public void AMachineCalendarUsesWhateverZoneItIsLoadedOn()
        {
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithMachineTimeZone()
                .Build();

            Assert.True(calendar.FollowsMachineTimeZone);
            Assert.Equal(TimeZoneInfo.Local.Id, calendar.TimeZone.Id);
        }

        [Fact]
        public void ThatIntentIsWhatTheFileRecords()
        {
            BusinessCalendar calendar = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithMachineTimeZone()
                .Build();

            string json = BusinessCalendarSerializer.ToJson(calendar);

            // Not the zone this machine happens to be in: the instruction to use the reader's own.
            Assert.Contains("\"timeZone\": \"Local\"", json);
            Assert.DoesNotContain(TimeZoneInfo.Local.Id, json);
        }

        [Fact]
        public void ReadingItBackKeepsItFollowingTheMachine()
        {
            BusinessCalendar restored = BusinessCalendarSerializer.FromJson(
                "{ \"week\": \"Mon-Fri 09:00-17:00\", \"timeZone\": \"Local\" }");

            Assert.True(restored.FollowsMachineTimeZone);
            Assert.Equal(TimeZoneInfo.Local.Id, restored.TimeZone.Id);
        }

        [Theory]
        [InlineData("Local")]
        [InlineData("local")]
        [InlineData("MachineLocal")]
        public void TheWaysOfSayingItAllAgree(string spelling)
        {
            BusinessCalendar restored = BusinessCalendarSerializer.FromJson(
                "{ \"week\": \"Mon-Fri 09:00-17:00\", \"timeZone\": \"" + spelling + "\" }");

            Assert.True(restored.FollowsMachineTimeZone);
        }

        [Fact]
        public void APlaceIsStillAPlace()
        {
            BusinessCalendar berlin = BusinessCalendar.Create()
                .WithSchedule("Mon-Fri 09:00-17:00")
                .WithTimeZone("Europe/Berlin")
                .Build();

            string json = BusinessCalendarSerializer.ToJson(berlin);
            BusinessCalendar restored = BusinessCalendarSerializer.FromJson(json);

            Assert.False(berlin.FollowsMachineTimeZone);
            Assert.False(restored.FollowsMachineTimeZone);
            Assert.Contains("Europe/Berlin", json);
            Assert.Equal("Europe/Berlin", restored.TimeZone.Id);
        }

        [Fact]
        public void NamingAZoneAfterwardsPinsTheCalendarDown()
        {
            BusinessCalendar pinned = BusinessCalendar.Create()
                .WithMachineTimeZone()
                .WithTimeZone("Asia/Tokyo")
                .Build();

            Assert.False(pinned.FollowsMachineTimeZone);
            Assert.Equal("Asia/Tokyo", pinned.TimeZone.Id);
        }

        [Fact]
        public void TheIntentSurvivesEditingTheCalendar()
        {
            BusinessCalendar edited = BusinessCalendar.Create()
                .WithMachineTimeZone()
                .Build()
                .ToBuilder()
                .AddHoliday(new DateTime(2026, 12, 25), "Christmas Day")
                .Build();

            Assert.True(edited.FollowsMachineTimeZone);
        }

        [Fact]
        public void ItIsSaidPlainlyInTheLog()
        {
            string description = BusinessCalendar.Create().WithMachineTimeZone().Build().Describe();

            Assert.Contains("tz=machine", description);
        }
    }
}
