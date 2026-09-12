using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessTime
{
    /// <summary>Fluent construction of a <see cref="BusinessCalendar"/>.</summary>
    /// <example>
    /// <code>
    /// BusinessCalendar calendar = BusinessCalendar.Create()
    ///     .WithName("Support desk")
    ///     .WithSchedule("Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00")
    ///     .WithTimeZone("Europe/Berlin")
    ///     .AddAnnualHoliday(1, 1, "New Year's Day")
    ///     .AddHoliday(new DateTime(2026, 12, 25), "Christmas")
    ///     .Build();
    /// </code>
    /// </example>
    public sealed class BusinessCalendarBuilder
    {
        private readonly List<SpecialDay> _specialDays = new List<SpecialDay>();
        private WeeklySchedule _schedule = WeeklySchedule.Standard;
        private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
        private TimeSpan? _hoursPerBusinessDay;
        private string _name;
        private bool _followsMachineTimeZone;

        /// <summary>Creates an empty builder seeded with a Monday-Friday 09:00-17:00 week.</summary>
        public BusinessCalendarBuilder()
        {
        }

        /// <summary>Creates a builder pre-loaded with an existing calendar's definition.</summary>
        public BusinessCalendarBuilder(BusinessCalendar calendar)
        {
            if (calendar == null)
                throw new ArgumentNullException(nameof(calendar));

            _schedule = calendar.Schedule;
            _timeZone = calendar.TimeZone;
            _hoursPerBusinessDay = calendar.HoursPerBusinessDay;
            _name = calendar.Name;
            _followsMachineTimeZone = calendar.FollowsMachineTimeZone;
            _specialDays.AddRange(calendar.SpecialDays);
        }

        /// <summary>Sets the label used in logs.</summary>
        public BusinessCalendarBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>Sets the working week from a schedule string such as <c>Mon-Fri 09:00-17:00</c>.</summary>
        public BusinessCalendarBuilder WithSchedule(string schedule) => WithSchedule(WeeklySchedule.Parse(schedule));

        /// <summary>Sets the working week.</summary>
        public BusinessCalendarBuilder WithSchedule(WeeklySchedule schedule)
        {
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            return this;
        }

        /// <summary>Sets the hours worked on one day of the week, for example <c>"09:00-12:00,13:00-17:00"</c>.</summary>
        public BusinessCalendarBuilder WithWorkingDay(DayOfWeek day, string shifts)
        {
            _schedule = _schedule.With(day, DaySchedule.Parse(shifts));
            return this;
        }

        /// <summary>Marks a day of the week as non-working.</summary>
        public BusinessCalendarBuilder WithDayOff(DayOfWeek day)
        {
            _schedule = _schedule.With(day, DaySchedule.NonWorking);
            return this;
        }

        /// <summary>Sets the time zone the working hours are expressed in.</summary>
        public BusinessCalendarBuilder WithTimeZone(string timeZoneId) => WithTimeZone(TimeZones.Resolve(timeZoneId));

        /// <summary>Sets the time zone the working hours are expressed in.</summary>
        public BusinessCalendarBuilder WithTimeZone(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone ?? TimeZoneInfo.Local;
            _followsMachineTimeZone = false;
            return this;
        }

        /// <summary>
        /// Says the hours mean local time wherever the calendar runs, so every robot uses its own zone
        /// rather than the one the calendar was written on.
        /// </summary>
        public BusinessCalendarBuilder WithMachineTimeZone()
        {
            _followsMachineTimeZone = true;
            _timeZone = TimeZoneInfo.Local;
            return this;
        }

        /// <summary>Overrides the length of a nominal business day.</summary>
        public BusinessCalendarBuilder WithHoursPerBusinessDay(double hours) =>
            WithHoursPerBusinessDay(TimeSpan.FromHours(hours));

        /// <summary>Overrides the length of a nominal business day.</summary>
        public BusinessCalendarBuilder WithHoursPerBusinessDay(TimeSpan length)
        {
            _hoursPerBusinessDay = length;
            return this;
        }

        /// <summary>Adds a non-working date.</summary>
        public BusinessCalendarBuilder AddHoliday(DateTime date, string name = null) =>
            AddSpecialDay(SpecialDay.Holiday(date, name));

        /// <summary>Adds several non-working dates.</summary>
        public BusinessCalendarBuilder AddHolidays(IEnumerable<DateTime> dates, string name = null)
        {
            foreach (DateTime date in dates ?? Enumerable.Empty<DateTime>())
                AddSpecialDay(SpecialDay.Holiday(date, name));
            return this;
        }

        /// <summary>Adds a non-working date that repeats every year.</summary>
        public BusinessCalendarBuilder AddAnnualHoliday(int month, int day, string name = null) =>
            AddSpecialDay(SpecialDay.AnnualHoliday(month, day, name));

        /// <summary>Adds a run of non-working dates, such as a company shutdown.</summary>
        public BusinessCalendarBuilder AddShutdown(DateTime from, DateTime through, string name = null) =>
            AddSpecialDay(SpecialDay.Shutdown(from, through, name));

        /// <summary>Adds a date that is worked with hours other than the weekly schedule's.</summary>
        public BusinessCalendarBuilder AddSpecialHours(DateTime date, string shifts, string name = null) =>
            AddSpecialDay(SpecialDay.CustomHours(date, shifts, name));

        /// <summary>Adds an already built special day.</summary>
        public BusinessCalendarBuilder AddSpecialDay(SpecialDay specialDay)
        {
            if (specialDay != null)
                _specialDays.Add(specialDay);
            return this;
        }

        /// <summary>Builds the calendar.</summary>
        public BusinessCalendar Build() =>
            new BusinessCalendar(_schedule, _specialDays, _timeZone, _hoursPerBusinessDay, _name, _followsMachineTimeZone);
    }
}
