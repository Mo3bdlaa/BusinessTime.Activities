using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BusinessTime
{
    /// <summary>
    /// A complete definition of working time: the recurring week, the exceptions to it (holidays, shutdowns,
    /// half days), the time zone the hours are expressed in, and the length of a nominal business day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All arithmetic on this class counts only time that falls inside a working window. Adding eight
    /// business hours to Friday 14:00 on a Monday-to-Friday 09:00-17:00 calendar consumes the three hours
    /// left on Friday and the first five hours of Monday, landing on Monday 14:00.
    /// </para>
    /// <para>
    /// Working windows are half-open: the instant a shift ends is no longer working time. Durations are
    /// measured on the wall clock of <see cref="TimeZone"/>, so a working day keeps its nominal length
    /// across a daylight-saving change.
    /// </para>
    /// <para>Instances are immutable and safe to share between threads.</para>
    /// </remarks>
    public sealed class BusinessCalendar
    {
        /// <summary>
        /// Upper bound on how many days a single calculation will scan before giving up, so that a calendar
        /// with (almost) no working time fails loudly instead of looping forever.
        /// </summary>
        public const int MaxDaysScanned = 366 * 50;

        private static readonly TimeSpan Day = TimeSpan.FromHours(24);

        private readonly SpecialDay[] _specialDays;
        private readonly TimeSpan _hoursPerBusinessDay;

        /// <summary>Creates a business calendar.</summary>
        /// <param name="schedule">The recurring week. Defaults to Monday-Friday 09:00-17:00.</param>
        /// <param name="specialDays">Holidays, shutdowns and days with exceptional hours.</param>
        /// <param name="timeZone">Time zone the schedule is expressed in. Defaults to the machine's zone.</param>
        /// <param name="hoursPerBusinessDay">
        /// Length of one nominal business day, used when a duration is expressed in days. Defaults to the
        /// average working day of <paramref name="schedule"/>.
        /// </param>
        /// <param name="name">Optional label for logging.</param>
        public BusinessCalendar(
            WeeklySchedule schedule = null,
            IEnumerable<SpecialDay> specialDays = null,
            TimeZoneInfo timeZone = null,
            TimeSpan? hoursPerBusinessDay = null,
            string name = null)
        {
            Schedule = schedule ?? WeeklySchedule.Standard;
            _specialDays = (specialDays ?? Enumerable.Empty<SpecialDay>()).Where(day => day != null).ToArray();
            TimeZone = timeZone ?? TimeZoneInfo.Local;
            Name = name;

            if (hoursPerBusinessDay.HasValue && hoursPerBusinessDay.Value <= TimeSpan.Zero)
                throw new BusinessTimeException("A business day must be longer than zero.");

            _hoursPerBusinessDay = hoursPerBusinessDay ?? DeriveHoursPerBusinessDay(Schedule);
        }

        /// <summary>Monday to Friday, 09:00-17:00, in the machine's time zone, with no holidays.</summary>
        public static BusinessCalendar Default => new BusinessCalendar(name: "Default");

        /// <summary>Optional label for logging.</summary>
        public string Name { get; }

        /// <summary>The recurring week.</summary>
        public WeeklySchedule Schedule { get; }

        /// <summary>Holidays, shutdowns and days with exceptional hours.</summary>
        public IReadOnlyList<SpecialDay> SpecialDays => _specialDays;

        /// <summary>Time zone the working hours are expressed in.</summary>
        public TimeZoneInfo TimeZone { get; }

        /// <summary>Length of one nominal business day, used to convert between days and hours.</summary>
        public TimeSpan HoursPerBusinessDay => _hoursPerBusinessDay;

        /// <summary>Builds a calendar from a schedule string such as <c>Mon-Fri 09:00-17:00</c>.</summary>
        public static BusinessCalendar FromSchedule(string schedule) =>
            new BusinessCalendar(WeeklySchedule.Parse(schedule));

        /// <summary>Starts a fluent definition of a calendar.</summary>
        public static BusinessCalendarBuilder Create() => new BusinessCalendarBuilder();

        /// <summary>Returns a builder pre-loaded with this calendar's definition.</summary>
        public BusinessCalendarBuilder ToBuilder() => new BusinessCalendarBuilder(this);

        // ---------------------------------------------------------------------------------------------
        // Day level questions
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// The working windows that apply on <paramref name="date"/>, taking special days into account.
        /// Windows are relative to midnight of that date and may run past 24:00 for a night shift.
        /// </summary>
        public IReadOnlyList<TimeRange> GetShifts(DateTime date)
        {
            DateTime day = ToCalendarTime(date).Date;

            // The last matching entry wins, so a specific date can override a shutdown it falls inside.
            for (int i = _specialDays.Length - 1; i >= 0; i--)
            {
                if (_specialDays[i].Covers(day))
                    return _specialDays[i].Schedule.Shifts;
            }

            return Schedule[day.DayOfWeek].Shifts;
        }

        /// <summary>The special day covering <paramref name="date"/>, or <c>null</c> when the week applies.</summary>
        public SpecialDay GetSpecialDay(DateTime date)
        {
            DateTime day = ToCalendarTime(date).Date;
            for (int i = _specialDays.Length - 1; i >= 0; i--)
            {
                if (_specialDays[i].Covers(day))
                    return _specialDays[i];
            }
            return null;
        }

        /// <summary>True when any work is scheduled on the date part of <paramref name="date"/>.</summary>
        public bool IsWorkingDay(DateTime date) => GetShifts(date).Count > 0;

        /// <summary>True when the exact moment falls inside a working window.</summary>
        /// <remarks>The instant a shift ends is not working time, so 17:00 on a 09:00-17:00 day is false.</remarks>
        public bool IsWorkingTime(DateTime moment)
        {
            DateTime local = ToCalendarTime(moment);

            // A night shift started the day before can still be running.
            for (int dayOffset = -1; dayOffset <= 0; dayOffset++)
            {
                DateTime day = local.Date.AddDays(dayOffset);
                if (day > local || day < DateTime.MinValue.AddDays(1))
                    continue;

                foreach (TimeRange shift in GetShifts(day))
                {
                    if (local >= day + shift.Start && local < day + shift.End)
                        return true;
                }
            }

            return false;
        }

        /// <summary>Total working time scheduled on the date part of <paramref name="date"/>.</summary>
        public TimeSpan GetWorkingTimeOnDay(DateTime date)
        {
            TimeSpan total = TimeSpan.Zero;
            foreach (TimeRange shift in GetShifts(date))
                total += shift.Duration;
            return total;
        }

        /// <summary>First working moment of the given date, or <c>null</c> when it is not a working day.</summary>
        public DateTime? GetStartOfBusinessDay(DateTime date)
        {
            DateTime local = ToCalendarTime(date).Date;
            IReadOnlyList<TimeRange> shifts = GetShifts(local);
            return shifts.Count == 0 ? (DateTime?)null : Restore(local + shifts[0].Start, date.Kind);
        }

        /// <summary>
        /// The moment work stops on the given date, or <c>null</c> when it is not a working day. For a night
        /// shift this falls on the following calendar day.
        /// </summary>
        public DateTime? GetEndOfBusinessDay(DateTime date)
        {
            DateTime local = ToCalendarTime(date).Date;
            IReadOnlyList<TimeRange> shifts = GetShifts(local);
            return shifts.Count == 0 ? (DateTime?)null : Restore(local + shifts[shifts.Count - 1].End, date.Kind);
        }

        /// <summary>
        /// Counts the working days between two dates, both included. The result is negative when
        /// <paramref name="to"/> falls before <paramref name="from"/>.
        /// </summary>
        public int CountWorkingDays(DateTime from, DateTime to)
        {
            DateTime start = ToCalendarTime(from).Date;
            DateTime end = ToCalendarTime(to).Date;

            int sign = 1;
            if (end < start)
            {
                sign = -1;
                DateTime swap = start;
                start = end;
                end = swap;
            }

            int count = 0;
            for (DateTime day = start; day <= end; day = day.AddDays(1))
            {
                if (IsWorkingDay(day))
                    count++;
            }

            return count * sign;
        }

        // ---------------------------------------------------------------------------------------------
        // Arithmetic
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds a duration of working time to <paramref name="start"/>, skipping everything that is not
        /// working time. A negative duration walks backwards.
        /// </summary>
        /// <remarks>
        /// The starting moment does not have to be working time: the clock simply begins to run at the next
        /// working moment. Adding zero returns the input unchanged; use <see cref="SnapForward"/> to move an
        /// out-of-hours moment onto the calendar.
        /// </remarks>
        /// <exception cref="BusinessTimeException">The calendar has too little working time to satisfy the request.</exception>
        public DateTime Add(DateTime start, TimeSpan businessDuration)
        {
            if (businessDuration == TimeSpan.Zero)
                return start;

            DateTime cursor = ToCalendarTime(start);
            TimeSpan remaining = businessDuration < TimeSpan.Zero ? -businessDuration : businessDuration;
            bool forward = businessDuration > TimeSpan.Zero;

            EnsureCalendarCanBeWalked(businessDuration);

            IEnumerable<BusinessTimeInterval> windows = forward
                ? EnumerateWindowsForward(cursor.Date.AddDays(-1))
                : EnumerateWindowsBackward(cursor.Date.AddDays(1));

            foreach (BusinessTimeInterval window in windows)
            {
                if (forward)
                {
                    if (window.End <= cursor)
                        continue;

                    DateTime from = window.Start > cursor ? window.Start : cursor;
                    TimeSpan available = window.End - from;
                    if (available >= remaining)
                        return Restore(from + remaining, start.Kind);

                    remaining -= available;
                }
                else
                {
                    if (window.Start >= cursor)
                        continue;

                    DateTime to = window.End < cursor ? window.End : cursor;
                    TimeSpan available = to - window.Start;
                    if (available >= remaining)
                        return Restore(to - remaining, start.Kind);

                    remaining -= available;
                }
            }

            throw new BusinessTimeException(
                $"Could not add {businessDuration} of business time to {start:yyyy-MM-dd HH:mm}: " +
                $"{Describe()} does not provide that much working time within {MaxDaysScanned / 366} years.");
        }

        /// <summary>Subtracts a duration of working time. Equivalent to <see cref="Add"/> with a negated duration.</summary>
        public DateTime Subtract(DateTime start, TimeSpan businessDuration) => Add(start, -businessDuration);

        /// <summary>
        /// Adds business days measured as working time, where one day is <see cref="HoursPerBusinessDay"/>.
        /// Fractions are allowed, so 0.5 is half a working day.
        /// </summary>
        public DateTime AddBusinessDays(DateTime start, double days) =>
            Add(start, TimeSpan.FromTicks((long)Math.Round(days * _hoursPerBusinessDay.Ticks, MidpointRounding.AwayFromZero)));

        /// <summary>Adds business hours. Fractions are allowed.</summary>
        public DateTime AddBusinessHours(DateTime start, double hours) =>
            Add(start, TimeSpan.FromTicks((long)Math.Round(hours * TimeSpan.TicksPerHour, MidpointRounding.AwayFromZero)));

        /// <summary>
        /// Moves forward or backward by whole working days while keeping the time of day, which is what
        /// "three business days from now" usually means for a deadline expressed in days.
        /// </summary>
        /// <remarks>
        /// Non-working days are skipped and never counted. The time of day is carried over untouched, even
        /// if it falls outside the target day's shifts.
        /// </remarks>
        public DateTime AddWorkingDays(DateTime start, int days)
        {
            if (days == 0)
                return start;

            DateTime local = ToCalendarTime(start);
            int step = days > 0 ? 1 : -1;
            int remaining = Math.Abs(days);
            int scanned = 0;

            while (remaining > 0)
            {
                local = local.AddDays(step);
                if (++scanned > MaxDaysScanned)
                    throw new BusinessTimeException($"Could not move {days} working days from {start:yyyy-MM-dd}: {Describe()} has no working days.");

                if (IsWorkingDay(local))
                    remaining--;
            }

            return Restore(local, start.Kind);
        }

        /// <summary>
        /// The working time between two moments. The result is negative when <paramref name="to"/> falls
        /// before <paramref name="from"/>.
        /// </summary>
        public TimeSpan GetBusinessTimeBetween(DateTime from, DateTime to)
        {
            DateTime start = ToCalendarTime(from);
            DateTime end = ToCalendarTime(to);

            if (end == start)
                return TimeSpan.Zero;

            int sign = 1;
            if (end < start)
            {
                sign = -1;
                DateTime swap = start;
                start = end;
                end = swap;
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (BusinessTimeInterval window in EnumerateWindowsForward(start.Date.AddDays(-1)))
            {
                if (window.Start >= end)
                    break;

                DateTime overlapStart = window.Start > start ? window.Start : start;
                DateTime overlapEnd = window.End < end ? window.End : end;
                if (overlapEnd > overlapStart)
                    total += overlapEnd - overlapStart;
            }

            return sign > 0 ? total : -total;
        }

        /// <summary>
        /// The working time between two moments expressed in business days of
        /// <see cref="HoursPerBusinessDay"/>. Negative when <paramref name="to"/> precedes <paramref name="from"/>.
        /// </summary>
        public double GetBusinessDaysBetween(DateTime from, DateTime to) =>
            GetBusinessTimeBetween(from, to).Ticks / (double)_hoursPerBusinessDay.Ticks;

        // ---------------------------------------------------------------------------------------------
        // Snapping and enumeration
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns <paramref name="moment"/> when it is already working time, otherwise the start of the
        /// next working window.
        /// </summary>
        public DateTime SnapForward(DateTime moment)
        {
            DateTime local = ToCalendarTime(moment);

            foreach (BusinessTimeInterval window in EnumerateWindowsForward(local.Date.AddDays(-1)))
            {
                if (window.End <= local)
                    continue;
                return Restore(window.Start > local ? window.Start : local, moment.Kind);
            }

            throw new BusinessTimeException($"No working time found after {moment:yyyy-MM-dd HH:mm} in {Describe()}.");
        }

        /// <summary>
        /// Returns <paramref name="moment"/> when it is already working time, otherwise the moment the
        /// previous working window ended.
        /// </summary>
        public DateTime SnapBackward(DateTime moment)
        {
            DateTime local = ToCalendarTime(moment);

            foreach (BusinessTimeInterval window in EnumerateWindowsBackward(local.Date.AddDays(1)))
            {
                if (window.Start >= local)
                    continue;
                return Restore(window.End < local ? window.End : local, moment.Kind);
            }

            throw new BusinessTimeException($"No working time found before {moment:yyyy-MM-dd HH:mm} in {Describe()}.");
        }

        /// <summary>
        /// The first working moment of the next working day, strictly after the date part of
        /// <paramref name="date"/>. On a 09:00-17:00 week, asking on a Friday returns Monday at 09:00.
        /// </summary>
        public DateTime NextWorkingDay(DateTime date) => FindWorkingDayStart(date, 1);

        /// <summary>
        /// The first working moment of the previous working day, strictly before the date part of
        /// <paramref name="date"/>. On a 09:00-17:00 week, asking on a Monday returns Friday at 09:00.
        /// </summary>
        public DateTime PreviousWorkingDay(DateTime date) => FindWorkingDayStart(date, -1);

        /// <summary>
        /// Walks whole days in <paramref name="step"/> direction until one carries working time, and returns
        /// the moment work starts on it.
        /// </summary>
        private DateTime FindWorkingDayStart(DateTime date, int step)
        {
            DateTime day = ToCalendarTime(date).Date;

            for (int scanned = 0; scanned < MaxDaysScanned; scanned++)
            {
                day = day.AddDays(step);

                IReadOnlyList<TimeRange> shifts = GetShifts(day);
                if (shifts.Count > 0)
                    return Restore(day + shifts[0].Start, date.Kind);
            }

            throw new BusinessTimeException(
                $"No working day found {(step > 0 ? "after" : "before")} {date:yyyy-MM-dd} in {Describe()}.");
        }

        /// <summary>
        /// The working windows that overlap the period between two moments, clipped to that period and
        /// merged where they run into each other.
        /// </summary>
        public IReadOnlyList<BusinessTimeInterval> GetWorkingIntervals(DateTime from, DateTime to)
        {
            DateTime start = ToCalendarTime(from);
            DateTime end = ToCalendarTime(to);

            if (end < start)
            {
                DateTime swap = start;
                start = end;
                end = swap;
            }

            var result = new List<BusinessTimeInterval>();

            foreach (BusinessTimeInterval window in EnumerateWindowsForward(start.Date.AddDays(-1)))
            {
                if (window.Start >= end)
                    break;

                DateTime clippedStart = window.Start > start ? window.Start : start;
                DateTime clippedEnd = window.End < end ? window.End : end;
                if (clippedEnd <= clippedStart)
                    continue;

                if (result.Count > 0 && result[result.Count - 1].End == clippedStart)
                    result[result.Count - 1] = new BusinessTimeInterval(result[result.Count - 1].Start, clippedEnd);
                else
                    result.Add(new BusinessTimeInterval(clippedStart, clippedEnd));
            }

            if (from.Kind != DateTimeKind.Unspecified)
            {
                for (int i = 0; i < result.Count; i++)
                    result[i] = new BusinessTimeInterval(Restore(result[i].Start, from.Kind), Restore(result[i].End, from.Kind));
            }

            return result;
        }

        /// <summary>A one line description of the calendar, useful in logs and error messages.</summary>
        public string Describe()
        {
            var builder = new StringBuilder();
            builder.Append(string.IsNullOrWhiteSpace(Name) ? "calendar" : "calendar '" + Name + "'");
            builder.Append(" [").Append(Schedule).Append(']');
            builder.Append(" tz=").Append(TimeZone.Id);
            builder.Append(" day=").Append(FormatHours(_hoursPerBusinessDay));
            if (_specialDays.Length > 0)
                builder.Append(" specialDays=").Append(_specialDays.Length);
            return builder.ToString();
        }

        /// <inheritdoc />
        public override string ToString() => Describe();

        private static string FormatHours(TimeSpan value) =>
            value.TotalHours.ToString("0.##", CultureInfo.InvariantCulture) + "h";

        // ---------------------------------------------------------------------------------------------
        // Window enumeration
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Walks working windows forward from midnight of <paramref name="fromDate"/>, in order and without
        /// overlaps. Windows that a night shift pushes into an already covered stretch are clipped.
        /// </summary>
        private IEnumerable<BusinessTimeInterval> EnumerateWindowsForward(DateTime fromDate)
        {
            DateTime day = fromDate.Date;
            DateTime lastEnd = DateTime.MinValue;
            int scanned = 0;

            while (scanned++ <= MaxDaysScanned)
            {
                if (day > DateTime.MaxValue.Date.AddDays(-3))
                    yield break;

                foreach (TimeRange shift in GetShifts(day))
                {
                    DateTime windowStart = day + shift.Start;
                    DateTime windowEnd = day + shift.End;

                    if (windowEnd <= lastEnd)
                        continue;
                    if (windowStart < lastEnd)
                        windowStart = lastEnd;

                    lastEnd = windowEnd;
                    yield return new BusinessTimeInterval(windowStart, windowEnd);
                }

                day = day.AddDays(1);
            }
        }

        /// <summary>Walks working windows backward from midnight of <paramref name="fromDate"/>.</summary>
        private IEnumerable<BusinessTimeInterval> EnumerateWindowsBackward(DateTime fromDate)
        {
            DateTime day = fromDate.Date;
            DateTime lastStart = DateTime.MaxValue;
            int scanned = 0;

            while (scanned++ <= MaxDaysScanned)
            {
                if (day < DateTime.MinValue.Date.AddDays(3))
                    yield break;

                IReadOnlyList<TimeRange> shifts = GetShifts(day);
                for (int i = shifts.Count - 1; i >= 0; i--)
                {
                    DateTime windowStart = day + shifts[i].Start;
                    DateTime windowEnd = day + shifts[i].End;

                    if (windowStart >= lastStart)
                        continue;
                    if (windowEnd > lastStart)
                        windowEnd = lastStart;

                    lastStart = windowStart;
                    yield return new BusinessTimeInterval(windowStart, windowEnd);
                }

                day = day.AddDays(-1);
            }
        }

        private void EnsureCalendarCanBeWalked(TimeSpan requested)
        {
            if (Schedule.HasWorkingTime || _specialDays.Any(day => !day.IsNonWorking))
                return;

            throw new BusinessTimeException(
                $"Cannot add {requested} of business time: {Describe()} defines no working time at all.");
        }

        private static TimeSpan DeriveHoursPerBusinessDay(WeeklySchedule schedule)
        {
            int workingDays = schedule.WorkingDaysPerWeek;
            return workingDays == 0
                ? TimeSpan.FromHours(8)
                : TimeSpan.FromTicks(schedule.WeeklyWorkingTime.Ticks / workingDays);
        }

        // ---------------------------------------------------------------------------------------------
        // Time zone plumbing
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts an incoming value into the calendar's wall clock. Values with an unspecified kind are
        /// assumed to already be in the calendar's zone, which is the common case in a workflow.
        /// </summary>
        internal DateTime ToCalendarTime(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Utc:
                    return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(value, TimeZone), DateTimeKind.Unspecified);
                case DateTimeKind.Local:
                    return TimeZoneInfo.Local.Id == TimeZone.Id
                        ? DateTime.SpecifyKind(value, DateTimeKind.Unspecified)
                        : DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Local, TimeZone), DateTimeKind.Unspecified);
                default:
                    return value;
            }
        }

        /// <summary>Converts a calendar wall clock value back into the kind the caller supplied.</summary>
        internal DateTime Restore(DateTime calendarTime, DateTimeKind originalKind)
        {
            DateTime unspecified = DateTime.SpecifyKind(calendarTime, DateTimeKind.Unspecified);

            switch (originalKind)
            {
                case DateTimeKind.Utc:
                    return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(ResolveInvalidTime(unspecified), TimeZone), DateTimeKind.Utc);
                case DateTimeKind.Local:
                    return TimeZoneInfo.Local.Id == TimeZone.Id
                        ? DateTime.SpecifyKind(unspecified, DateTimeKind.Local)
                        : TimeZoneInfo.ConvertTime(ResolveInvalidTime(unspecified), TimeZone, TimeZoneInfo.Local);
                default:
                    return unspecified;
            }
        }

        /// <summary>
        /// Moves a wall clock reading that daylight saving skipped over onto the first instant that exists,
        /// so that a schedule written across a spring-forward change still resolves.
        /// </summary>
        private DateTime ResolveInvalidTime(DateTime value)
        {
            if (!TimeZone.IsInvalidTime(value))
                return value;

            TimeSpan gap = TimeZone.GetUtcOffset(value.AddDays(1)) - TimeZone.GetUtcOffset(value.AddDays(-1));
            DateTime shifted = value + (gap > TimeSpan.Zero ? gap : TimeSpan.FromHours(1));
            return TimeZone.IsInvalidTime(shifted) ? value.Date.AddDays(1) : shifted;
        }
    }
}
