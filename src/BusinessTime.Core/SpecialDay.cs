using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BusinessTime
{
    /// <summary>
    /// A date, or a run of dates, whose hours differ from the weekly schedule: a public holiday, a company
    /// shutdown, a half day, or an exceptional working Saturday.
    /// </summary>
    public sealed class SpecialDay
    {
        private readonly DaySchedule _schedule;

        /// <summary>Creates a special day.</summary>
        /// <param name="date">First date the entry applies to. The time component is ignored.</param>
        /// <param name="name">Label shown in logs, for example <c>New Year's Day</c>.</param>
        /// <param name="shifts">Working windows for these dates; <c>null</c> or empty makes them non-working.</param>
        /// <param name="through">Last date the entry applies to, inclusive. Defaults to <paramref name="date"/>.</param>
        /// <param name="isAnnual">When true the entry repeats every year on the same month and day.</param>
        public SpecialDay(DateTime date, string name = null, IEnumerable<TimeRange> shifts = null, DateTime? through = null, bool isAnnual = false)
        {
            Date = date.Date;
            Through = (through ?? date).Date;
            Name = name;
            IsAnnual = isAnnual;
            _schedule = shifts == null ? DaySchedule.NonWorking : new DaySchedule(shifts);

            if (!IsAnnual && Through < Date)
                throw new BusinessTimeException($"Special day '{name ?? Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}' ends before it starts.");
        }

        /// <summary>First date the entry applies to. For annual entries only the month and day matter.</summary>
        public DateTime Date { get; }

        /// <summary>Last date the entry applies to, inclusive.</summary>
        public DateTime Through { get; }

        /// <summary>Optional label, shown when the calendar is described or logged.</summary>
        public string Name { get; }

        /// <summary>True when the entry repeats every year on the same month and day.</summary>
        public bool IsAnnual { get; }

        /// <summary>Hours worked on these dates. Empty for a non-working day.</summary>
        public IReadOnlyList<TimeRange> Shifts => _schedule.Shifts;

        /// <summary>True when these dates are fully non-working.</summary>
        public bool IsNonWorking => !_schedule.IsWorkingDay;

        internal DaySchedule Schedule => _schedule;

        /// <summary>A single non-working date.</summary>
        public static SpecialDay Holiday(DateTime date, string name = null) => new SpecialDay(date, name);

        /// <summary>A non-working date that repeats every year, such as the first of January.</summary>
        public static SpecialDay AnnualHoliday(int month, int day, string name = null) =>
            new SpecialDay(new DateTime(2000, month, day), name, isAnnual: true);

        /// <summary>A run of non-working dates, such as a company shutdown.</summary>
        public static SpecialDay Shutdown(DateTime from, DateTime through, string name = null) =>
            new SpecialDay(from, name, through: through);

        /// <summary>A date that is worked, but with hours that differ from the weekly schedule.</summary>
        public static SpecialDay CustomHours(DateTime date, string shifts, string name = null) =>
            new SpecialDay(date, name, DaySchedule.Parse(shifts).Shifts);

        /// <summary>A date that is worked, but with hours that differ from the weekly schedule.</summary>
        public static SpecialDay CustomHours(DateTime date, IEnumerable<TimeRange> shifts, string name = null) =>
            new SpecialDay(date, name, shifts);

        /// <summary>True when this entry covers <paramref name="date"/>.</summary>
        public bool Covers(DateTime date)
        {
            DateTime day = date.Date;

            if (!IsAnnual)
                return day >= Date && day <= Through;

            int target = Ordinal(day);
            int from = Ordinal(Date);
            int to = Ordinal(Through);

            // A range whose end falls before its start wraps across the turn of the year.
            return from <= to
                ? target >= from && target <= to
                : target >= from || target <= to;
        }

        private static int Ordinal(DateTime date) => (date.Month * 100) + date.Day;

        /// <inheritdoc />
        public override string ToString()
        {
            string format = IsAnnual ? "MM-dd" : "yyyy-MM-dd";
            string dates = Date == Through
                ? Date.ToString(format, CultureInfo.InvariantCulture)
                : Date.ToString(format, CultureInfo.InvariantCulture) + ".." + Through.ToString(format, CultureInfo.InvariantCulture);

            string hours = IsNonWorking ? "non-working" : string.Join(",", Shifts.Select(shift => shift.ToString()));
            return string.IsNullOrWhiteSpace(Name) ? $"{dates} ({hours})" : $"{dates} {Name} ({hours})";
        }
    }
}
