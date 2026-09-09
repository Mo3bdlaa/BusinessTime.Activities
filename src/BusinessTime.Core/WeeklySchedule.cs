using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BusinessTime
{
    /// <summary>
    /// The recurring week: which days are working days and which hours are worked on each of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A schedule can be written as a single, human readable string, which is the format used by the
    /// activities and by the JSON calendar files:
    /// </para>
    /// <code>
    /// Mon-Fri 09:00-17:00
    /// Mon-Thu 08:00-16:30; Fri 08:00-14:00
    /// Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00; Sun off
    /// Weekdays 9am-5pm
    /// Daily 00:00-24:00
    /// </code>
    /// <para>
    /// Entries are separated by <c>;</c>, <c>|</c> or a line break. Days may be given as a single day
    /// (<c>Mon</c>), a range (<c>Mon-Fri</c>), a list (<c>Mon,Wed,Fri</c>) or one of the groups
    /// <c>Weekdays</c>, <c>Weekend</c>, <c>Daily</c>/<c>All</c>. Days that are never mentioned are
    /// non-working, and a later entry replaces an earlier one for the same day.
    /// </para>
    /// </remarks>
    public sealed class WeeklySchedule
    {
        private static readonly Regex EntryPattern = new Regex(
            @"^\s*(?<days>[A-Za-z]+(?:\s*[-,]\s*[A-Za-z]+)*)\s*[:=]?\s*(?<shifts>.*)$",
            RegexOptions.Compiled);

        private static readonly DayOfWeek[] AllDays =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        private readonly DaySchedule[] _days;

        /// <summary>Creates a weekly schedule; days missing from <paramref name="days"/> are non-working.</summary>
        public WeeklySchedule(IEnumerable<KeyValuePair<DayOfWeek, DaySchedule>> days)
        {
            _days = new DaySchedule[7];
            for (int i = 0; i < 7; i++)
                _days[i] = DaySchedule.NonWorking;

            if (days == null)
                return;

            foreach (KeyValuePair<DayOfWeek, DaySchedule> entry in days)
                _days[(int)entry.Key] = entry.Value ?? DaySchedule.NonWorking;
        }

        private WeeklySchedule(DaySchedule[] days)
        {
            _days = days;
        }

        /// <summary>Monday to Friday, 09:00-17:00.</summary>
        public static WeeklySchedule Standard => Parse("Mon-Fri 09:00-17:00");

        /// <summary>Every day, around the clock. Business time and elapsed time are then the same thing.</summary>
        public static WeeklySchedule Continuous => Parse("Daily 00:00-24:00");

        /// <summary>A week with no working time at all.</summary>
        public static WeeklySchedule Empty => new WeeklySchedule(Enumerable.Repeat(DaySchedule.NonWorking, 7).ToArray());

        /// <summary>The schedule for one day of the week.</summary>
        public DaySchedule this[DayOfWeek day] => _days[(int)day];

        /// <summary>Number of days in the week that contain working time.</summary>
        public int WorkingDaysPerWeek => _days.Count(day => day.IsWorkingDay);

        /// <summary>Total working time defined across the week.</summary>
        public TimeSpan WeeklyWorkingTime
        {
            get
            {
                TimeSpan total = TimeSpan.Zero;
                foreach (DaySchedule day in _days)
                    total += day.WorkingTime;
                return total;
            }
        }

        /// <summary>True when at least one day of the week contains working time.</summary>
        public bool HasWorkingTime => _days.Any(day => day.IsWorkingDay);

        /// <summary>Returns a copy of this schedule with <paramref name="day"/> replaced.</summary>
        public WeeklySchedule With(DayOfWeek day, DaySchedule schedule)
        {
            var copy = (DaySchedule[])_days.Clone();
            copy[(int)day] = schedule ?? DaySchedule.NonWorking;
            return new WeeklySchedule(copy);
        }

        /// <summary>Returns a copy of this schedule with the given windows applied to <paramref name="day"/>.</summary>
        public WeeklySchedule With(DayOfWeek day, params TimeRange[] shifts) => With(day, new DaySchedule(shifts));

        /// <summary>Parses a schedule string such as <c>Mon-Fri 09:00-17:00; Sat 09:00-13:00</c>.</summary>
        /// <exception cref="BusinessTimeException">The schedule string cannot be read.</exception>
        public static WeeklySchedule Parse(string text)
        {
            if (text == null)
                throw new BusinessTimeException("A schedule string cannot be null.");

            var days = new DaySchedule[7];
            for (int i = 0; i < 7; i++)
                days[i] = DaySchedule.NonWorking;

            string[] entries = text.Split(new[] { ';', '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool sawEntry = false;

            foreach (string rawEntry in entries)
            {
                string entry = rawEntry.Trim();
                if (entry.Length == 0)
                    continue;

                Match match = EntryPattern.Match(entry);
                if (!match.Success)
                    throw new BusinessTimeException($"'{entry}' is not a valid schedule entry; expected something like 'Mon-Fri 09:00-17:00'.");

                IReadOnlyList<DayOfWeek> targetDays = ParseDays(match.Groups["days"].Value);
                DaySchedule schedule = DaySchedule.Parse(match.Groups["shifts"].Value);

                foreach (DayOfWeek day in targetDays)
                    days[(int)day] = schedule;

                sawEntry = true;
            }

            if (!sawEntry && text.Trim().Length > 0)
                throw new BusinessTimeException($"'{text.Trim()}' is not a valid schedule; expected something like 'Mon-Fri 09:00-17:00'.");

            return new WeeklySchedule(days);
        }

        /// <summary>Attempts to parse a schedule string. See <see cref="Parse"/> for the accepted format.</summary>
        public static bool TryParse(string text, out WeeklySchedule schedule)
        {
            try
            {
                schedule = Parse(text);
                return true;
            }
            catch (BusinessTimeException)
            {
                schedule = null;
                return false;
            }
        }

        /// <summary>
        /// Renders the schedule back into the parseable string format, collapsing consecutive days that
        /// share the same hours into ranges.
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();
            int index = 0;

            while (index < AllDays.Length)
            {
                DayOfWeek first = AllDays[index];
                DaySchedule schedule = this[first];

                int last = index;
                while (last + 1 < AllDays.Length && SameShifts(this[AllDays[last + 1]], schedule))
                    last++;

                if (schedule.IsWorkingDay)
                {
                    string dayPart = index == last
                        ? Abbreviate(first)
                        : Abbreviate(first) + "-" + Abbreviate(AllDays[last]);
                    parts.Add(dayPart + " " + schedule);
                }

                index = last + 1;
            }

            return parts.Count == 0 ? "none" : string.Join("; ", parts);
        }

        private static bool SameShifts(DaySchedule left, DaySchedule right)
        {
            if (left.Shifts.Count != right.Shifts.Count)
                return false;
            for (int i = 0; i < left.Shifts.Count; i++)
            {
                if (left.Shifts[i] != right.Shifts[i])
                    return false;
            }
            return true;
        }

        private static string Abbreviate(DayOfWeek day) => day.ToString().Substring(0, 3);

        private static IReadOnlyList<DayOfWeek> ParseDays(string text)
        {
            var result = new List<DayOfWeek>();

            foreach (string token in text.Split(','))
            {
                string part = token.Trim();
                if (part.Length == 0)
                    continue;

                int dash = part.IndexOf('-');
                if (dash > 0)
                {
                    DayOfWeek from = ParseDay(part.Substring(0, dash));
                    DayOfWeek to = ParseDay(part.Substring(dash + 1));
                    foreach (DayOfWeek day in Range(from, to))
                        AddDistinct(result, day);
                    continue;
                }

                DayOfWeek[] group = ParseDayGroup(part);
                if (group != null)
                {
                    foreach (DayOfWeek day in group)
                        AddDistinct(result, day);
                    continue;
                }

                AddDistinct(result, ParseDay(part));
            }

            if (result.Count == 0)
                throw new BusinessTimeException($"'{text.Trim()}' does not name any day of the week.");

            return result;
        }

        private static void AddDistinct(List<DayOfWeek> days, DayOfWeek day)
        {
            if (!days.Contains(day))
                days.Add(day);
        }

        private static IEnumerable<DayOfWeek> Range(DayOfWeek from, DayOfWeek to)
        {
            // Ranges are walked in Monday-first order so that 'Fri-Mon' wraps across the weekend.
            int start = Array.IndexOf(AllDays, from);
            int end = Array.IndexOf(AllDays, to);
            int count = ((end - start + 7) % 7) + 1;

            for (int offset = 0; offset < count; offset++)
                yield return AllDays[(start + offset) % 7];
        }

        private static DayOfWeek[] ParseDayGroup(string text)
        {
            switch (text.ToLowerInvariant())
            {
                case "weekday":
                case "weekdays":
                    return new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
                case "weekend":
                case "weekends":
                    return new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
                case "daily":
                case "everyday":
                case "all":
                    return AllDays;
                default:
                    return null;
            }
        }

        private static DayOfWeek ParseDay(string text)
        {
            string value = text.Trim().ToLowerInvariant();
            switch (value)
            {
                case "mon": case "monday": case "mo": case "m": return DayOfWeek.Monday;
                case "tue": case "tues": case "tuesday": case "tu": return DayOfWeek.Tuesday;
                case "wed": case "weds": case "wednesday": case "we": return DayOfWeek.Wednesday;
                case "thu": case "thur": case "thurs": case "thursday": case "th": return DayOfWeek.Thursday;
                case "fri": case "friday": case "fr": case "f": return DayOfWeek.Friday;
                case "sat": case "saturday": case "sa": return DayOfWeek.Saturday;
                case "sun": case "sunday": case "su": return DayOfWeek.Sunday;
                default:
                    throw new BusinessTimeException($"'{text.Trim()}' is not a day of the week.");
            }
        }

        /// <summary>Enumerates the seven days of the week, Monday first, with their schedules.</summary>
        public IEnumerable<KeyValuePair<DayOfWeek, DaySchedule>> Days()
        {
            foreach (DayOfWeek day in AllDays)
                yield return new KeyValuePair<DayOfWeek, DaySchedule>(day, this[day]);
        }

        /// <summary>Renders the schedule as one line per working day, for logging and troubleshooting.</summary>
        public string ToDetailedString()
        {
            var builder = new StringBuilder();
            foreach (KeyValuePair<DayOfWeek, DaySchedule> entry in Days())
                builder.AppendLine($"{entry.Key,-9} {entry.Value}");
            return builder.ToString();
        }
    }
}
