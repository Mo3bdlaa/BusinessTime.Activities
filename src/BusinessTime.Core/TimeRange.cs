using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BusinessTime
{
    /// <summary>
    /// A working window inside a single day, expressed as an offset from midnight of that day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="End"/> is allowed to run past 24 hours so that night shifts can be expressed on the day
    /// they start on: <c>22:00-06:00</c> is stored as <c>22:00</c> to <c>30:00</c> and spills into the next
    /// calendar day. A range is half-open, <c>[Start, End)</c>, so the exact end instant is not working time.
    /// </para>
    /// </remarks>
    public readonly struct TimeRange : IEquatable<TimeRange>, IComparable<TimeRange>
    {
        /// <summary>The largest value <see cref="End"/> may take (48 hours after midnight).</summary>
        public static readonly TimeSpan MaxEnd = TimeSpan.FromHours(48);

        private static readonly Regex RangePattern = new Regex(
            @"^\s*(?<from>[^-–]+?)\s*[-–]\s*(?<to>.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TimePattern = new Regex(
            @"^(?<h>\d{1,2})(?::(?<m>\d{1,2}))?(?::(?<s>\d{1,2}))?\s*(?<ap>am|pm)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Start of the window, measured from midnight of the owning day.</summary>
        public TimeSpan Start { get; }

        /// <summary>End of the window, measured from midnight of the owning day. May exceed 24 hours.</summary>
        public TimeSpan End { get; }

        /// <summary>Creates a working window.</summary>
        /// <exception cref="BusinessTimeException">The window is empty, negative, or ends more than 48 hours after midnight.</exception>
        public TimeRange(TimeSpan start, TimeSpan end)
        {
            if (start < TimeSpan.Zero)
                throw new BusinessTimeException($"A working window cannot start before midnight (got {Format(start)}).");
            if (end > MaxEnd)
                throw new BusinessTimeException($"A working window cannot end more than 48 hours after midnight (got {Format(end)}).");
            if (end <= start)
                throw new BusinessTimeException($"A working window must end after it starts (got {Format(start)}-{Format(end)}).");

            Start = start;
            End = end;
        }

        /// <summary>Creates a working window from whole hours and minutes.</summary>
        public TimeRange(int startHour, int startMinute, int endHour, int endMinute)
            : this(new TimeSpan(startHour, startMinute, 0), new TimeSpan(endHour, endMinute, 0))
        {
        }

        /// <summary>Length of the window.</summary>
        public TimeSpan Duration => End - Start;

        /// <summary>True when the window runs past midnight into the following calendar day.</summary>
        public bool SpillsIntoNextDay => End > TimeSpan.FromHours(24);

        /// <summary>True when <paramref name="timeOfDay"/> falls inside the half-open window.</summary>
        public bool Contains(TimeSpan timeOfDay) => timeOfDay >= Start && timeOfDay < End;

        /// <summary>True when this window overlaps or exactly touches <paramref name="other"/>.</summary>
        public bool Touches(TimeRange other) => Start <= other.End && other.Start <= End;

        /// <summary>Returns the smallest window covering both this window and <paramref name="other"/>.</summary>
        public TimeRange Union(TimeRange other) =>
            new TimeRange(Start < other.Start ? Start : other.Start, End > other.End ? End : other.End);

        /// <summary>
        /// Parses a window such as <c>09:00-17:00</c>, <c>9-17</c>, <c>9am-5:30pm</c> or <c>22:00-06:00</c>.
        /// </summary>
        /// <remarks>
        /// When the end time is not after the start time it is treated as a night shift and pushed into the
        /// next calendar day, so <c>22:00-06:00</c> becomes <c>22:00-30:00</c>.
        /// </remarks>
        /// <exception cref="BusinessTimeException">The text is not a valid window.</exception>
        public static TimeRange Parse(string text)
        {
            if (!TryParse(text, out TimeRange range, out string error))
                throw new BusinessTimeException(error);
            return range;
        }

        /// <summary>Attempts to parse a window. See <see cref="Parse"/> for the accepted formats.</summary>
        public static bool TryParse(string text, out TimeRange range)
        {
            return TryParse(text, out range, out _);
        }

        internal static bool TryParse(string text, out TimeRange range, out string error)
        {
            range = default;
            error = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "A working window cannot be empty.";
                return false;
            }

            Match match = RangePattern.Match(text);
            if (!match.Success)
            {
                error = $"'{text.Trim()}' is not a working window; expected something like '09:00-17:00'.";
                return false;
            }

            if (!TryParseTimeOfDay(match.Groups["from"].Value, out TimeSpan start) ||
                !TryParseTimeOfDay(match.Groups["to"].Value, out TimeSpan end))
            {
                error = $"'{text.Trim()}' contains a time that could not be read; expected something like '09:00-17:00'.";
                return false;
            }

            // A window that runs backwards is read as a night shift crossing midnight. An end equal to the
            // start is left alone, so it is reported as the mistake it almost always is.
            if (end < start)
                end += TimeSpan.FromHours(24);

            if (end <= start || end > MaxEnd)
            {
                error = $"'{text.Trim()}' does not describe a window that ends after it starts.";
                return false;
            }

            range = new TimeRange(start, end);
            return true;
        }

        /// <summary>Parses a time of day such as <c>17:30</c>, <c>5:30pm</c>, <c>9</c> or <c>24:00</c>.</summary>
        public static bool TryParseTimeOfDay(string text, out TimeSpan timeOfDay)
        {
            timeOfDay = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            Match match = TimePattern.Match(text.Trim());
            if (!match.Success)
                return false;

            int hours = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
            int minutes = match.Groups["m"].Success ? int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;
            int seconds = match.Groups["s"].Success ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;

            if (match.Groups["ap"].Success)
            {
                bool isPm = match.Groups["ap"].Value.Equals("pm", StringComparison.OrdinalIgnoreCase);
                if (hours < 1 || hours > 12)
                    return false;
                if (hours == 12)
                    hours = 0;
                if (isPm)
                    hours += 12;
            }

            if (hours > 24 || minutes > 59 || seconds > 59)
                return false;

            timeOfDay = new TimeSpan(hours, minutes, seconds);
            return timeOfDay <= TimeSpan.FromHours(24);
        }

        /// <summary>Renders the window as <c>HH:mm-HH:mm</c>, wrapping a night shift back to a next-day clock time.</summary>
        public override string ToString() => Format(Start) + "-" + Format(End);

        private static string Format(TimeSpan value)
        {
            TimeSpan wrapped = value >= TimeSpan.FromHours(24) ? value - TimeSpan.FromHours(24) : value;
            return wrapped.Seconds == 0
                ? string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", (int)wrapped.TotalHours, wrapped.Minutes)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", (int)wrapped.TotalHours, wrapped.Minutes, wrapped.Seconds);
        }

        /// <inheritdoc />
        public bool Equals(TimeRange other) => Start == other.Start && End == other.End;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is TimeRange other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        /// <inheritdoc />
        public int CompareTo(TimeRange other)
        {
            int byStart = Start.CompareTo(other.Start);
            return byStart != 0 ? byStart : End.CompareTo(other.End);
        }

        /// <summary>Equality operator.</summary>
        public static bool operator ==(TimeRange left, TimeRange right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(TimeRange left, TimeRange right) => !left.Equals(right);
    }
}
