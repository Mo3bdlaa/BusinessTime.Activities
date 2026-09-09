using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessTime
{
    /// <summary>
    /// The working windows that apply to one day, for example <c>09:00-12:00</c> and <c>13:00-17:00</c>.
    /// </summary>
    /// <remarks>
    /// Windows are normalised on construction: they are sorted and any overlapping or touching windows are
    /// merged, so <c>09:00-12:00</c> plus <c>12:00-17:00</c> is stored as the single window <c>09:00-17:00</c>.
    /// </remarks>
    public sealed class DaySchedule
    {
        /// <summary>A day with no working time at all.</summary>
        public static readonly DaySchedule NonWorking = new DaySchedule(new TimeRange[0]);

        private readonly TimeRange[] _shifts;

        /// <summary>Creates a day schedule from the supplied windows.</summary>
        public DaySchedule(IEnumerable<TimeRange> shifts)
        {
            _shifts = Normalize(shifts);
        }

        /// <summary>Creates a day schedule from the supplied windows.</summary>
        public DaySchedule(params TimeRange[] shifts) : this((IEnumerable<TimeRange>)shifts)
        {
        }

        /// <summary>The normalised working windows for this day, ordered by start time.</summary>
        public IReadOnlyList<TimeRange> Shifts => _shifts;

        /// <summary>True when the day contains any working time.</summary>
        public bool IsWorkingDay => _shifts.Length > 0;

        /// <summary>Total working time defined for this day.</summary>
        public TimeSpan WorkingTime
        {
            get
            {
                TimeSpan total = TimeSpan.Zero;
                for (int i = 0; i < _shifts.Length; i++)
                    total += _shifts[i].Duration;
                return total;
            }
        }

        /// <summary>First moment of work on this day, or <c>null</c> when the day is non-working.</summary>
        public TimeSpan? StartOfDay => _shifts.Length > 0 ? _shifts[0].Start : (TimeSpan?)null;

        /// <summary>Last moment of work on this day, or <c>null</c> when the day is non-working.</summary>
        public TimeSpan? EndOfDay => _shifts.Length > 0 ? _shifts[_shifts.Length - 1].End : (TimeSpan?)null;

        /// <summary>True when <paramref name="timeOfDay"/> falls inside one of the windows.</summary>
        public bool Contains(TimeSpan timeOfDay) => _shifts.Any(shift => shift.Contains(timeOfDay));

        /// <summary>
        /// Parses a list of windows such as <c>09:00-12:00,13:00-17:00</c>. The words <c>off</c>,
        /// <c>closed</c> and <c>none</c> produce a non-working day.
        /// </summary>
        public static DaySchedule Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return NonWorking;

            string trimmed = text.Trim();
            if (IsNonWorkingKeyword(trimmed))
                return NonWorking;

            var shifts = new List<TimeRange>();
            foreach (string part in trimmed.Split(new[] { ',', '+', '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                if (IsNonWorkingKeyword(part.Trim()))
                    continue;
                shifts.Add(TimeRange.Parse(part));
            }

            return new DaySchedule(shifts);
        }

        internal static bool IsNonWorkingKeyword(string text) =>
            text.Equals("off", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("closed", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("-", StringComparison.Ordinal);

        /// <summary>Renders the day as <c>09:00-12:00,13:00-17:00</c>, or <c>off</c> when non-working.</summary>
        public override string ToString() =>
            _shifts.Length == 0 ? "off" : string.Join(",", _shifts.Select(shift => shift.ToString()));

        private static TimeRange[] Normalize(IEnumerable<TimeRange> shifts)
        {
            if (shifts == null)
                return new TimeRange[0];

            List<TimeRange> ordered = shifts.ToList();
            if (ordered.Count == 0)
                return new TimeRange[0];

            ordered.Sort();

            var merged = new List<TimeRange>(ordered.Count) { ordered[0] };
            for (int i = 1; i < ordered.Count; i++)
            {
                TimeRange last = merged[merged.Count - 1];
                TimeRange current = ordered[i];
                if (last.Touches(current))
                    merged[merged.Count - 1] = last.Union(current);
                else
                    merged.Add(current);
            }

            return merged.ToArray();
        }
    }
}
