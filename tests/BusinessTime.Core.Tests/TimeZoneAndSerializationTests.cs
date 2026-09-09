using System;
using BusinessTime;
using Xunit;

namespace BusinessTime.Tests
{
    public class TimeZoneAndSerializationTests
    {
        private static readonly TimeZoneInfo Berlin = TimeZones.Resolve("Europe/Berlin");

        private static BusinessCalendar BerlinOffice => BusinessCalendar.Create()
            .WithName("Berlin office")
            .WithSchedule("Mon-Fri 09:00-17:00")
            .WithTimeZone(Berlin)
            .Build();

        [Fact]
        public void UtcInputIsReadInTheCalendarsZoneAndAnsweredInUtc()
        {
            // Berlin is two hours ahead in July, so 07:00 UTC is 09:00 in the office.
            var utcStart = new DateTime(2026, 7, 3, 7, 0, 0, DateTimeKind.Utc);

            DateTime result = BerlinOffice.Add(utcStart, TimeSpan.FromHours(9));

            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.Equal(new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void UtcMomentsAreClassifiedAgainstLocalOfficeHours()
        {
            BusinessCalendar calendar = BerlinOffice;

            Assert.True(calendar.IsWorkingTime(new DateTime(2026, 7, 3, 7, 0, 0, DateTimeKind.Utc)));
            Assert.False(calendar.IsWorkingTime(new DateTime(2026, 7, 3, 6, 59, 0, DateTimeKind.Utc)));
            Assert.False(calendar.IsWorkingTime(new DateTime(2026, 7, 3, 15, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void UnspecifiedInputIsTakenAsOfficeLocalTime()
        {
            DateTime result = BerlinOffice.Add(new DateTime(2026, 7, 3, 9, 0, 0), TimeSpan.FromHours(9));

            Assert.Equal(DateTimeKind.Unspecified, result.Kind);
            Assert.Equal(new DateTime(2026, 7, 6, 10, 0, 0), result);
        }

        [Fact]
        public void WorkingDaysKeepTheirLengthAcrossADaylightSavingChange()
        {
            // Central European clocks jump forward in the early hours of 29 March 2026.
            BusinessCalendar calendar = BerlinOffice;
            var beforeChange = new DateTime(2026, 3, 27, 9, 0, 0);

            DateTime end = calendar.Add(beforeChange, TimeSpan.FromHours(16));

            Assert.Equal(new DateTime(2026, 3, 30, 17, 0, 0), end);
            Assert.Equal(TimeSpan.FromHours(16), calendar.GetBusinessTimeBetween(beforeChange, end));
        }

        [Fact]
        public void RoundTripsThroughJson()
        {
            BusinessCalendar original = BusinessCalendar.Create()
                .WithName("Germany - support desk")
                .WithSchedule("Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00")
                .WithTimeZone(Berlin)
                .WithHoursPerBusinessDay(7.5)
                .AddAnnualHoliday(1, 1, "New Year's Day")
                .AddHoliday(new DateTime(2026, 12, 25), "Christmas Day")
                .AddShutdown(new DateTime(2026, 12, 27), new DateTime(2026, 12, 31), "Winter shutdown")
                .AddSpecialHours(new DateTime(2026, 12, 24), "09:00-13:00", "Christmas Eve")
                .Build();

            BusinessCalendar restored = BusinessCalendarSerializer.FromJson(BusinessCalendarSerializer.ToJson(original));

            Assert.Equal(original.Name, restored.Name);
            Assert.Equal(original.Schedule.ToString(), restored.Schedule.ToString());
            Assert.Equal(original.TimeZone.Id, restored.TimeZone.Id);
            Assert.Equal(original.HoursPerBusinessDay, restored.HoursPerBusinessDay);
            Assert.Equal(original.SpecialDays.Count, restored.SpecialDays.Count);
            Assert.False(restored.IsWorkingDay(new DateTime(2027, 1, 1)));
            Assert.False(restored.IsWorkingDay(new DateTime(2026, 12, 29)));
            Assert.Equal(TimeSpan.FromHours(4), restored.GetWorkingTimeOnDay(new DateTime(2026, 12, 24)));
        }

        [Fact]
        public void ReadsACalendarWrittenByHand()
        {
            const string json = @"{
              'name': 'Support desk',
              'timeZone': 'UTC',
              'hoursPerBusinessDay': 7.5,
              'week': {
                'monday': '09:00-17:00',
                'tuesday': ['09:00-12:00', '13:00-17:00'],
                'wednesday': '09:00-17:00',
                'thursday': '09:00-17:00',
                'friday': '09:00-14:00',
                'saturday': 'off',
                'sunday': null
              },
              'holidays': [
                { 'date': '12-25', 'name': 'Christmas Day', 'annual': true },
                { 'from': '2026-08-03', 'to': '2026-08-07', 'name': 'Summer shutdown' }
              ]
            }";

            BusinessCalendar calendar = BusinessCalendarSerializer.FromJson(json.Replace('\'', '"'));

            Assert.Equal("Support desk", calendar.Name);
            Assert.Equal(TimeZoneInfo.Utc.Id, calendar.TimeZone.Id);
            Assert.Equal(TimeSpan.FromHours(7.5), calendar.HoursPerBusinessDay);
            Assert.Equal(5, calendar.Schedule.WorkingDaysPerWeek);
            Assert.Equal(TimeSpan.FromHours(7), calendar.GetWorkingTimeOnDay(new DateTime(2026, 1, 6)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2030, 12, 25)));
            Assert.False(calendar.IsWorkingDay(new DateTime(2026, 8, 5)));
            Assert.True(calendar.IsWorkingDay(new DateTime(2026, 8, 10)));
        }

        [Fact]
        public void AcceptsAScheduleStringInPlaceOfAWeekObject()
        {
            BusinessCalendar calendar = BusinessCalendarSerializer.FromJson(
                "{ \"week\": \"Mon-Fri 08:00-16:00\", \"timeZone\": \"UTC\" }");

            Assert.Equal(5, calendar.Schedule.WorkingDaysPerWeek);
            Assert.Equal(TimeSpan.FromHours(8), calendar.HoursPerBusinessDay);
        }

        [Fact]
        public void SavesAndLoadsACalendarFile()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

            try
            {
                BusinessCalendarSerializer.SaveFile(BerlinOffice, path);
                BusinessCalendar loaded = BusinessCalendarSerializer.LoadFile(path);

                Assert.Equal("Berlin office", loaded.Name);
                Assert.Equal(Berlin.Id, loaded.TimeZone.Id);
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("[]")]
        [InlineData("{ \"week\": 42 }")]
        [InlineData("{ \"specialDays\": [ { \"name\": \"no date\" } ] }")]
        [InlineData("not json at all")]
        public void RejectsBrokenCalendarDocuments(string json)
        {
            Assert.Throws<BusinessTimeException>(() => BusinessCalendarSerializer.FromJson(json));
        }

        [Fact]
        public void ReportsAMissingCalendarFileClearly()
        {
            BusinessTimeException error = Assert.Throws<BusinessTimeException>(
                () => BusinessCalendarSerializer.LoadFile("/no/such/calendar.json"));

            Assert.Contains("was not found", error.Message);
        }
    }
}
