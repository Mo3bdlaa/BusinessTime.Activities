using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BusinessTime
{
    /// <summary>
    /// Reads and writes business calendars as JSON, so that a team can keep one shared calendar file in
    /// source control and every process can load the same definition.
    /// </summary>
    /// <remarks>
    /// <para>The canonical shape is:</para>
    /// <code>
    /// {
    ///   "name": "Germany - support desk",
    ///   "timeZone": "Europe/Berlin",
    ///   "hoursPerBusinessDay": 8,
    ///   "week": "Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00",
    ///   "specialDays": [
    ///     { "date": "01-01", "name": "New Year's Day", "annual": true },
    ///     { "date": "2026-12-24", "name": "Christmas Eve", "hours": "09:00-13:00" },
    ///     { "from": "2026-12-27", "to": "2026-12-31", "name": "Winter shutdown" }
    ///   ]
    /// }
    /// </code>
    /// <para>
    /// The <c>week</c> member also accepts an object keyed by day name, whose values are either a shift
    /// string or an array of shift strings. <c>holidays</c> is accepted as an alias for <c>specialDays</c>.
    /// </para>
    /// </remarks>
    public static class BusinessCalendarSerializer
    {
        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd", "yyyy/MM/dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "MM-dd", "MM/dd"
        };

        /// <summary>Parses a calendar from JSON text.</summary>
        /// <exception cref="BusinessTimeException">The text is not a valid calendar document.</exception>
        public static BusinessCalendar FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new BusinessTimeException("The calendar JSON is empty.");

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            }
            catch (JsonException ex)
            {
                throw new BusinessTimeException("The calendar JSON could not be parsed: " + ex.Message, ex);
            }

            using (document)
            {
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new BusinessTimeException("The calendar JSON must be an object.");

                var builder = new BusinessCalendarBuilder();

                if (TryGet(root, out JsonElement name, "name", "calendarName") && name.ValueKind == JsonValueKind.String)
                    builder.WithName(name.GetString());

                if (TryGet(root, out JsonElement timeZone, "timeZone", "timezone", "timeZoneId") && timeZone.ValueKind == JsonValueKind.String)
                    builder.WithTimeZone(timeZone.GetString());

                if (TryGet(root, out JsonElement week, "week", "schedule", "workingHours"))
                    builder.WithSchedule(ReadSchedule(week));

                if (TryGet(root, out JsonElement dayLength, "hoursPerBusinessDay", "businessDayHours", "hoursPerDay"))
                    builder.WithHoursPerBusinessDay(ReadDuration(dayLength));

                if (TryGet(root, out JsonElement specialDays, "specialDays", "holidays", "exceptions"))
                {
                    if (specialDays.ValueKind != JsonValueKind.Array)
                        throw new BusinessTimeException("'specialDays' must be an array.");

                    foreach (JsonElement entry in specialDays.EnumerateArray())
                        builder.AddSpecialDay(ReadSpecialDay(entry));
                }

                return builder.Build();
            }
        }

        /// <summary>Loads a calendar from a JSON file.</summary>
        /// <exception cref="BusinessTimeException">The file is missing or is not a valid calendar document.</exception>
        public static BusinessCalendar LoadFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new BusinessTimeException("No calendar file path was supplied.");
            if (!File.Exists(path))
                throw new BusinessTimeException($"Calendar file '{path}' was not found.");

            return FromJson(File.ReadAllText(path));
        }

        /// <summary>Renders a calendar as JSON in the canonical shape.</summary>
        public static string ToJson(BusinessCalendar calendar, bool indented = true)
        {
            if (calendar == null)
                throw new ArgumentNullException(nameof(calendar));

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
                {
                    writer.WriteStartObject();

                    if (!string.IsNullOrWhiteSpace(calendar.Name))
                        writer.WriteString("name", calendar.Name);

                    writer.WriteString("timeZone", calendar.TimeZone.Id);
                    writer.WriteNumber("hoursPerBusinessDay", Math.Round(calendar.HoursPerBusinessDay.TotalHours, 4));
                    writer.WriteString("week", calendar.Schedule.ToString());

                    if (calendar.SpecialDays.Count > 0)
                    {
                        writer.WriteStartArray("specialDays");
                        foreach (SpecialDay day in calendar.SpecialDays)
                            WriteSpecialDay(writer, day);
                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>Writes a calendar to a JSON file, creating the directory when needed.</summary>
        public static void SaveFile(BusinessCalendar calendar, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new BusinessTimeException("No calendar file path was supplied.");

            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, ToJson(calendar));
        }

        private static void WriteSpecialDay(Utf8JsonWriter writer, SpecialDay day)
        {
            writer.WriteStartObject();

            string format = day.IsAnnual ? "MM-dd" : "yyyy-MM-dd";
            if (day.Date == day.Through)
            {
                writer.WriteString("date", day.Date.ToString(format, CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteString("from", day.Date.ToString(format, CultureInfo.InvariantCulture));
                writer.WriteString("to", day.Through.ToString(format, CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(day.Name))
                writer.WriteString("name", day.Name);

            if (day.IsAnnual)
                writer.WriteBoolean("annual", true);

            if (!day.IsNonWorking)
                writer.WriteString("hours", string.Join(",", day.Shifts.Select(shift => shift.ToString())));

            writer.WriteEndObject();
        }

        private static WeeklySchedule ReadSchedule(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return WeeklySchedule.Parse(element.GetString());

            if (element.ValueKind != JsonValueKind.Object)
                throw new BusinessTimeException("'week' must be a schedule string or an object keyed by day name.");

            var days = new List<KeyValuePair<DayOfWeek, DaySchedule>>();
            foreach (JsonProperty property in element.EnumerateObject())
            {
                DayOfWeek day = ParseDayName(property.Name);
                days.Add(new KeyValuePair<DayOfWeek, DaySchedule>(day, ReadDaySchedule(property.Value, property.Name)));
            }

            return new WeeklySchedule(days);
        }

        private static DaySchedule ReadDaySchedule(JsonElement element, string dayName)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return DaySchedule.NonWorking;
                case JsonValueKind.String:
                    return DaySchedule.Parse(element.GetString());
                case JsonValueKind.Array:
                    var shifts = new List<TimeRange>();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                            throw new BusinessTimeException($"The hours listed for '{dayName}' must be strings such as '09:00-17:00'.");
                        shifts.Add(TimeRange.Parse(item.GetString()));
                    }
                    return new DaySchedule(shifts);
                default:
                    throw new BusinessTimeException($"The hours for '{dayName}' must be a string or an array of strings.");
            }
        }

        private static SpecialDay ReadSpecialDay(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return SpecialDay.Holiday(ReadDate(element, "date"));

            if (element.ValueKind != JsonValueKind.Object)
                throw new BusinessTimeException("Each special day must be a date string or an object.");

            bool annual = TryGet(element, out JsonElement annualElement, "annual", "recurring", "yearly") &&
                          annualElement.ValueKind == JsonValueKind.True;

            if (!TryGet(element, out JsonElement fromElement, "date", "from", "start"))
                throw new BusinessTimeException("A special day needs a 'date' (or a 'from' and 'to' pair).");

            DateTime from = ReadDate(fromElement, "date");
            DateTime through = TryGet(element, out JsonElement toElement, "to", "through", "end")
                ? ReadDate(toElement, "to")
                : from;

            string name = TryGet(element, out JsonElement nameElement, "name", "label", "description") && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;

            IEnumerable<TimeRange> shifts = null;
            if (TryGet(element, out JsonElement hoursElement, "hours", "shifts", "workingHours"))
                shifts = ReadDaySchedule(hoursElement, name ?? from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Shifts;

            return new SpecialDay(from, name, shifts, through, annual);
        }

        private static DateTime ReadDate(JsonElement element, string member)
        {
            if (element.ValueKind != JsonValueKind.String)
                throw new BusinessTimeException($"'{member}' must be a date such as '2026-12-25' or '12-25'.");

            string text = element.GetString()?.Trim() ?? string.Empty;

            if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact))
                return exact.Year == 1 ? new DateTime(2000, exact.Month, exact.Day) : exact.Date;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed.Date;

            throw new BusinessTimeException($"'{text}' is not a date; expected something like '2026-12-25' or '12-25'.");
        }

        private static TimeSpan ReadDuration(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    return TimeSpan.FromHours(element.GetDouble());
                case JsonValueKind.String:
                    string text = element.GetString()?.Trim() ?? string.Empty;
                    if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out TimeSpan span))
                        return span;
                    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours))
                        return TimeSpan.FromHours(hours);
                    throw new BusinessTimeException($"'{text}' is not a business day length; use a number of hours or a value such as '07:30:00'.");
                default:
                    throw new BusinessTimeException("The business day length must be a number of hours or a duration string.");
            }
        }

        private static DayOfWeek ParseDayName(string name)
        {
            WeeklySchedule probe = WeeklySchedule.Parse(name + " 00:00-24:00");
            foreach (KeyValuePair<DayOfWeek, DaySchedule> entry in probe.Days())
            {
                if (entry.Value.IsWorkingDay)
                    return entry.Key;
            }

            throw new BusinessTimeException($"'{name}' is not a day of the week.");
        }

        private static bool TryGet(JsonElement element, out JsonElement value, params string[] names)
        {
            foreach (string name in names)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }
}
