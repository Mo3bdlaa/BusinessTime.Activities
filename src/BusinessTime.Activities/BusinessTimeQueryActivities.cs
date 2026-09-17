using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>Which way <see cref="SnapToBusinessTime"/> moves a moment that falls outside working hours.</summary>
    public enum SnapDirection
    {
        /// <summary>Move on to the start of the next working window.</summary>
        Forward = 0,

        /// <summary>Move back to the moment the previous working window ended.</summary>
        Backward = 1
    }

    /// <summary>Which way <see cref="GetNextBusinessDay"/> looks for a working day.</summary>
    public enum DayDirection
    {
        /// <summary>Look forward, to the next working day.</summary>
        Next = 0,

        /// <summary>Look backward, to the previous working day.</summary>
        Previous = 1
    }

    /// <summary>
    /// Measures how much working time separates two moments, ignoring everything the calendar does not
    /// count as work.
    /// </summary>
    /// <remarks>
    /// This is the activity to use for service levels: the answer is the time the team actually had, not the
    /// wall clock time, so a ticket raised on Friday afternoon is not penalised for the weekend.
    /// </remarks>
    [Category("Business Time")]
    [DisplayName("Get Business Time Between")]
    [Description("Measures the working time between two moments, ignoring closed hours, weekends and holidays.")]
    public sealed class GetBusinessTimeBetween : BusinessTimeActivity<TimeSpan>
    {
        /// <summary>The earlier moment.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("From")]
        [Description("The moment to measure from, for example ticket.Created.")]
        public InArgument<DateTime> From { get; set; }

        /// <summary>The later moment.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("To")]
        [Description("The moment to measure to, for example ticket.Answered or DateTime.Now. Earlier than From gives a negative result.")]
        public InArgument<DateTime> To { get; set; }

        /// <summary>The result expressed in business days.</summary>
        [Category(Categories.Output)]
        [DisplayName("Business days")]
        [Description("The same answer in business days, for example 1.5. Uses the calendar's hours per business day.")]
        public OutArgument<double> BusinessDays { get; set; }

        /// <summary>The result expressed in business hours.</summary>
        [Category(Categories.Output)]
        [DisplayName("Business hours")]
        [Description("The same answer in business hours, for example 12.5. Handy for writing a number straight into a report.")]
        public OutArgument<double> BusinessHours { get; set; }

        /// <summary>Number of working days touched by the period.</summary>
        [Category(Categories.Output)]
        [DisplayName("Working days")]
        [Description("How many working days the period touches, counting both end dates. Friday to Monday on a Mon-Fri week is 2.")]
        public OutArgument<int> WorkingDays { get; set; }

        /// <inheritdoc />
        protected override TimeSpan Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            DateTime from = From.GetValue(context);
            DateTime to = To.GetValue(context);

            TimeSpan result = calendar.GetBusinessTimeBetween(from, to);

            BusinessDays.SetValue(context, calendar.GetBusinessDaysBetween(from, to));
            BusinessHours.SetValue(context, result.TotalHours);
            WorkingDays.SetValue(context, calendar.CountWorkingDays(from, to));

            return result;
        }
    }

    /// <summary>Reports whether a moment falls inside working hours.</summary>
    /// <remarks>The instant a shift ends is not working time, so 17:00 on a 09:00-17:00 day is false.</remarks>
    [Category("Business Time")]
    [DisplayName("Is Business Time")]
    [Description("Reports whether a moment falls inside working hours, and whether its date is a working day at all.")]
    public sealed class IsBusinessTime : BusinessTimeActivity<bool>
    {
        /// <summary>The moment to test.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Date")]
        [Description("The moment to test, for example DateTime.Now.")]
        public InArgument<DateTime> Date { get; set; }

        /// <summary>Whether the date is a working day, regardless of the time of day.</summary>
        [Category(Categories.Output)]
        [DisplayName("Is working day")]
        [Description("True when any work at all is scheduled that date, whatever the time of day. Use this to tell a closed day from merely being out of hours.")]
        public OutArgument<bool> IsWorkingDay { get; set; }

        /// <summary>Name of the holiday or exception covering the date, when there is one.</summary>
        [Category(Categories.Output)]
        [DisplayName("Special day name")]
        [Description("The name of the holiday, shutdown or exception covering that date, for example \"Christmas Day\". Empty when the ordinary week applies, so it reads well in a log line.")]
        public OutArgument<string> SpecialDayName { get; set; }

        /// <inheritdoc />
        protected override bool Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            DateTime moment = Date.GetValue(context);

            SpecialDay specialDay = calendar.GetSpecialDay(moment);

            IsWorkingDay.SetValue(context, calendar.IsWorkingDay(moment));
            SpecialDayName.SetValue(context, specialDay?.Name ?? string.Empty);

            return calendar.IsWorkingTime(moment);
        }
    }

    /// <summary>
    /// Moves a moment that falls outside working hours onto the calendar, and leaves it alone when it is
    /// already working time.
    /// </summary>
    /// <remarks>
    /// Use it before starting a countdown, so that a request received at the weekend is treated as arriving
    /// when the office next opens.
    /// </remarks>
    [Category("Business Time.Windows")]
    [DisplayName("Snap To Business Time")]
    [Description("Moves a moment outside working hours to the next, or previous, working moment.")]
    public sealed class SnapToBusinessTime : BusinessTimeActivity<DateTime>
    {
        /// <summary>The moment to move onto the calendar.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Date")]
        [Description("The moment to move onto the calendar, for example request.Received. A request arriving at the weekend becomes Monday morning.")]
        public InArgument<DateTime> Date { get; set; }

        /// <summary>Which way to move.</summary>
        [Category(Categories.Options)]
        [DisplayName("Direction")]
        [Description("Forward moves to the start of the next working window, which is what you want before starting a countdown. Backward moves to the moment work last stopped.")]
        public SnapDirection Direction { get; set; }

        /// <summary>Whether the moment had to be moved at all.</summary>
        [Category(Categories.Output)]
        [DisplayName("Was adjusted")]
        [Description("False when the moment was already working time and came back unchanged, true when it had to be moved. Worth logging.")]
        public OutArgument<bool> WasAdjusted { get; set; }

        /// <inheritdoc />
        protected override DateTime Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            DateTime moment = Date.GetValue(context);

            DateTime result = Direction == SnapDirection.Backward
                ? calendar.SnapBackward(moment)
                : calendar.SnapForward(moment);

            WasAdjusted.SetValue(context, result != moment);
            return result;
        }
    }

    /// <summary>Describes one date: whether it is worked, when it opens and closes, and how long it lasts.</summary>
    [Category("Business Time.Days")]
    [DisplayName("Get Business Day Info")]
    [Description("Reports whether a date is a working day, when it opens and closes, and how much working time it holds.")]
    public sealed class GetBusinessDayInfo : BusinessTimeActivity<bool>
    {
        /// <summary>The date to describe. The time of day is ignored.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Date")]
        [Description("The date to describe, for example DateTime.Today. The time of day is ignored.")]
        public InArgument<DateTime> Date { get; set; }

        /// <summary>First working moment of the day.</summary>
        [Category(Categories.Output)]
        [DisplayName("Day start")]
        [Description("When work starts that day, for example 09:00. Left at its default when the date is not worked, so check Result first.")]
        public OutArgument<DateTime> DayStart { get; set; }

        /// <summary>The moment work stops.</summary>
        [Category(Categories.Output)]
        [DisplayName("Day end")]
        [Description("When work stops that day, for example 17:00. For a night shift this falls on the next calendar day.")]
        public OutArgument<DateTime> DayEnd { get; set; }

        /// <summary>Total working time scheduled on the date.</summary>
        [Category(Categories.Output)]
        [DisplayName("Working time")]
        [Description("How much working time that date holds, breaks excluded, for example 07:00:00 for a nine to five with an hour for lunch.")]
        public OutArgument<TimeSpan> WorkingTime { get; set; }

        /// <summary>The day's shifts, rendered as text.</summary>
        [Category(Categories.Output)]
        [DisplayName("Shifts")]
        [Description("The day's working windows as text, for example \"09:00-12:00,13:00-17:00\", or \"off\" when nobody works. Reads well in a log or an email.")]
        public OutArgument<string> Shifts { get; set; }

        /// <summary>Name of the holiday or exception covering the date, when there is one.</summary>
        [Category(Categories.Output)]
        [DisplayName("Special day name")]
        [Description("The name of the holiday, shutdown or exception covering that date, for example \"Christmas Day\". Empty when the ordinary week applies, so it reads well in a log line.")]
        public OutArgument<string> SpecialDayName { get; set; }

        /// <inheritdoc />
        protected override bool Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            DateTime date = Date.GetValue(context);

            DateTime? start = calendar.GetStartOfBusinessDay(date);
            DateTime? end = calendar.GetEndOfBusinessDay(date);
            SpecialDay specialDay = calendar.GetSpecialDay(date);

            DayStart.SetValue(context, start ?? default(DateTime));
            DayEnd.SetValue(context, end ?? default(DateTime));
            WorkingTime.SetValue(context, calendar.GetWorkingTimeOnDay(date));
            Shifts.SetValue(context, new DaySchedule(calendar.GetShifts(date)).ToString());
            SpecialDayName.SetValue(context, specialDay?.Name ?? string.Empty);

            return start.HasValue;
        }
    }

    /// <summary>Counts the working days between two dates, both included.</summary>
    [Category("Business Time.Days")]
    [DisplayName("Count Business Days")]
    [Description("Counts the working days between two dates, both included.")]
    public sealed class CountBusinessDays : BusinessTimeActivity<int>
    {
        /// <summary>First date of the period.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("From")]
        [Description("First date of the period, for example DateTime.Today.")]
        public InArgument<DateTime> From { get; set; }

        /// <summary>Last date of the period.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("To")]
        [Description("Last date of the period, counted in as well. Earlier than From gives a negative count.")]
        public InArgument<DateTime> To { get; set; }

        /// <inheritdoc />
        protected override int Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            return calendar.CountWorkingDays(From.GetValue(context), To.GetValue(context));
        }
    }

    /// <summary>
    /// Lists the working windows inside a period, which is what a scheduler needs in order to place work
    /// into the hours that are actually available.
    /// </summary>
    [Category("Business Time.Windows")]
    [DisplayName("Get Working Intervals")]
    [Description("Lists the working windows inside a period, clipped to that period.")]
    public sealed class GetWorkingIntervals : BusinessTimeActivity<IList<BusinessTimeInterval>>
    {
        /// <summary>Start of the period.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("From")]
        [Description("Start of the period, for example DateTime.Now.")]
        public InArgument<DateTime> From { get; set; }

        /// <summary>End of the period.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("To")]
        [Description("End of the period, for example DateTime.Now.AddDays(7).")]
        public InArgument<DateTime> To { get; set; }

        /// <summary>Total working time inside the period.</summary>
        [Category(Categories.Output)]
        [DisplayName("Total working time")]
        [Description("Total working time inside the period, for example 2.00:00:00 for two working days.")]
        public OutArgument<TimeSpan> TotalWorkingTime { get; set; }

        /// <inheritdoc />
        protected override IList<BusinessTimeInterval> Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            DateTime from = From.GetValue(context);
            DateTime to = To.GetValue(context);

            var intervals = new List<BusinessTimeInterval>(calendar.GetWorkingIntervals(from, to));

            TimeSpan total = TimeSpan.Zero;
            foreach (BusinessTimeInterval interval in intervals)
                total += interval.Duration;

            TotalWorkingTime.SetValue(context, total);
            return intervals;
        }
    }

    /// <summary>
    /// Finds the next, or previous, working day and reports the moment work starts on it.
    /// </summary>
    /// <remarks>
    /// The answer is when work actually begins on that day, not midnight, and it follows the day's own
    /// hours: a day with exceptional hours opens when those hours say it does.
    /// </remarks>
    [Category("Business Time.Days")]
    [DisplayName("Get Next Business Day")]
    [Description("Finds the next, or previous, working day and reports the moment work starts on it.")]
    public sealed class GetNextBusinessDay : BusinessTimeActivity<DateTime>
    {
        /// <summary>The date to search from. The date itself is never the answer.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Date")]
        [Description("The date to search from, for example DateTime.Today. The search is strict, so this date itself is never the answer.")]
        public InArgument<DateTime> Date { get; set; }

        /// <summary>Which way to look.</summary>
        [Category(Categories.Options)]
        [DisplayName("Direction")]
        [Description("Next looks forward, which is what you want for \"retry tomorrow\". Previous looks back, for example to find the last working day of a month.")]
        public DayDirection Direction { get; set; }

        /// <summary>The moment work stops on that day.</summary>
        [Category(Categories.Output)]
        [DisplayName("Day end")]
        [Description("When work stops on that day, for example 17:00, so it can be used as the end of a window.")]
        public OutArgument<DateTime> DayEnd { get; set; }

        /// <summary>Total working time scheduled on that day.</summary>
        [Category(Categories.Output)]
        [DisplayName("Working time")]
        [Description("Total working time scheduled on that day, breaks excluded.")]
        public OutArgument<TimeSpan> WorkingTime { get; set; }

        /// <summary>Name of the holiday or exception covering that day, when there is one.</summary>
        [Category(Categories.Output)]
        [DisplayName("Special day name")]
        [Description("The name of any exception covering that day, for example \"Christmas Eve\" for a half day. Empty when the ordinary week applies.")]
        public OutArgument<string> SpecialDayName { get; set; }

        /// <inheritdoc />
        protected override DateTime Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            DateTime from = Date.GetValue(context);

            DateTime result = Direction == DayDirection.Previous
                ? calendar.PreviousWorkingDay(from)
                : calendar.NextWorkingDay(from);

            DateTime? end = calendar.GetEndOfBusinessDay(result);

            DayEnd.SetValue(context, end ?? default(DateTime));
            WorkingTime.SetValue(context, calendar.GetWorkingTimeOnDay(result));
            SpecialDayName.SetValue(context, calendar.GetSpecialDay(result)?.Name ?? string.Empty);

            return result;
        }
    }
}
