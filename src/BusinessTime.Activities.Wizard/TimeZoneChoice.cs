using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BusinessTime.Activities.Wizard
{
    /// <summary>One entry in the time zone list, shown with its offset so it can be picked by either.</summary>
    public sealed class TimeZoneChoice
    {
        private TimeZoneChoice(string id, string display, bool isSystemDefault)
        {
            Id = id;
            Display = display;
            IsSystemDefault = isSystemDefault;
        }

        /// <summary>The identifier stored in the calendar file.</summary>
        public string Id { get; }

        /// <summary>What the list shows, for example <c>(UTC+01:00) W. Europe Standard Time</c>.</summary>
        public string Display { get; }

        /// <summary>True for the entry that follows the machine's own clock.</summary>
        public bool IsSystemDefault { get; }

        /// <inheritdoc />
        public override string ToString() => Display;

        /// <summary>
        /// Every zone the machine knows, ordered by offset, with the machine's own zone offered first.
        /// </summary>
        public static IReadOnlyList<TimeZoneChoice> All()
        {
            var choices = new List<TimeZoneChoice>
            {
                new TimeZoneChoice(
                    TimeZoneInfo.Local.Id,
                    "System default — " + Describe(TimeZoneInfo.Local),
                    true)
            };

            choices.AddRange(TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(zone => zone.BaseUtcOffset)
                .ThenBy(zone => zone.Id, StringComparer.OrdinalIgnoreCase)
                .Select(zone => new TimeZoneChoice(zone.Id, Describe(zone), false)));

            return choices;
        }

        /// <summary>Finds the entry for an identifier, falling back to the system default.</summary>
        public static TimeZoneChoice For(IReadOnlyList<TimeZoneChoice> choices, string id) =>
            choices.FirstOrDefault(choice => !choice.IsSystemDefault && string.Equals(choice.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? choices[0];

        private static string Describe(TimeZoneInfo zone)
        {
            TimeSpan offset = zone.BaseUtcOffset;
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            TimeSpan size = offset < TimeSpan.Zero ? offset.Negate() : offset;

            return string.Format(
                CultureInfo.InvariantCulture,
                "(UTC{0}{1:00}:{2:00}) {3}",
                sign, (int)size.TotalHours, size.Minutes, zone.Id);
        }
    }
}
