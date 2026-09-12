using System;
using System.Activities;
using System.ComponentModel;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>How the <c>Days</c> input of a shift activity should be read.</summary>
    public enum DayHandling
    {
        /// <summary>
        /// A day is an amount of working time, the calendar's hours per business day. Friday 14:00 plus one
        /// day on a 09:00-17:00 week is Monday 14:00, and half days are allowed.
        /// </summary>
        AsWorkingHours = 0,

        /// <summary>
        /// A day is a whole day on the calendar. The clock time is carried over untouched and only
        /// non-working days are skipped, which is what a deadline of "three business days" usually means.
        /// </summary>
        AsWholeDays = 1
    }

    /// <summary>Shared implementation of adding and subtracting business time.</summary>
    public abstract class BusinessTimeShiftActivity : BusinessTimeActivity<DateTime>
    {
        /// <summary>The moment to start from.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Date")]
        [Description("The moment to start from, for example DateTime.Now or ticket.Created. A moment outside working hours is fine: the clock simply starts running when the office next opens.")]
        public InArgument<DateTime> Date { get; set; }

        /// <summary>Number of business days to shift by.</summary>
        [Category(Categories.Input)]
        [DisplayName("Days")]
        [Description("Business days to shift by, for example 3 for a three business day deadline. Read according to the Day handling property below. Combine freely with Hours and Minutes: 1 day and 4 hours is Days 1, Hours 4.")]
        public InArgument<double> Days { get; set; }

        /// <summary>Number of business hours to shift by.</summary>
        [Category(Categories.Input)]
        [DisplayName("Hours")]
        [Description("Business hours to shift by, for example 8 for a working day, 4 for half a day, 1.5 for ninety minutes. Negative values move the other way.")]
        public InArgument<double> Hours { get; set; }

        /// <summary>Number of business minutes to shift by.</summary>
        [Category(Categories.Input)]
        [DisplayName("Minutes")]
        [Description("Business minutes to shift by, for example 30 or 90. Adds on top of Days and Hours.")]
        public InArgument<double> Minutes { get; set; }

        /// <summary>An additional duration to shift by, for when the amount is already a TimeSpan.</summary>
        [Category(Categories.Input)]
        [DisplayName("Duration")]
        [Description("An extra amount of working time, for when the amount is already a TimeSpan, for example TimeSpan.FromHours(4) or sla.Duration. Adds on top of Days, Hours and Minutes.")]
        public InArgument<TimeSpan> Duration { get; set; }

        /// <summary>How the <see cref="Days"/> input should be read.</summary>
        [Category(Categories.Options)]
        [DisplayName("Day handling")]
        [Description("What a day in the Days property means. AsWorkingHours: a day is the calendar's working hours, so Friday 14:00 plus 1 day is Monday 14:00 and half days are allowed. AsWholeDays: a day is a whole day on the calendar, keeping the clock time, which is what \"three business days\" usually means for a deadline.")]
        public DayHandling DayHandling { get; set; }

        /// <summary>The working time actually skipped over, including closed hours.</summary>
        [Category(Categories.Output)]
        [DisplayName("Elapsed time")]
        [Description("How much real time passed between the input and the result, closed hours included. Friday 14:00 plus 8 business hours lands on Monday 14:00, so this reports 3 days.")]
        public OutArgument<TimeSpan> ElapsedTime { get; set; }

        /// <summary>Direction this activity shifts in: 1 forwards, -1 backwards.</summary>
        protected abstract int Sign { get; }

        /// <summary>Name shown in error messages.</summary>
        protected abstract string ActivityName { get; }

        /// <inheritdoc />
        protected override DateTime Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = ResolveCalendar(context);
            DateTime start = Date.GetValue(context);

            double days = Days.GetValue(context);
            TimeSpan duration =
                TimeSpan.FromHours(Hours.GetValue(context)) +
                TimeSpan.FromMinutes(Minutes.GetValue(context)) +
                Duration.GetValue(context);

            DateTime result = start;

            if (DayHandling == DayHandling.AsWholeDays && days != 0)
            {
                if (days != Math.Floor(days))
                    throw new BusinessTimeException(
                        $"{ActivityName} was asked to move {days} days with Day handling set to AsWholeDays. " +
                        "Whole days cannot be split; use AsWorkingHours for a fraction of a day.");

                result = calendar.AddWorkingDays(result, Sign * (int)days);
            }
            else if (days != 0)
            {
                duration += TimeSpan.FromTicks((long)Math.Round(days * calendar.HoursPerBusinessDay.Ticks, MidpointRounding.AwayFromZero));
            }

            if (duration != TimeSpan.Zero)
                result = calendar.Add(result, Sign > 0 ? duration : -duration);

            ElapsedTime.SetValue(context, result - start);
            return result;
        }
    }

    /// <summary>
    /// Moves a moment forward by an amount of working time, skipping everything the calendar does not count
    /// as work.
    /// </summary>
    /// <remarks>
    /// On a Monday-Friday 09:00-17:00 calendar, Friday 14:00 plus eight business hours is Monday 14:00: three
    /// hours are spent on Friday afternoon and the remaining five on Monday morning.
    /// </remarks>
    [DisplayName("Add Business Time")]
    [Description("Moves a date forward by a number of business days, hours and minutes, skipping closed hours, weekends and holidays.")]
    public sealed class AddBusinessTime : BusinessTimeShiftActivity
    {
        /// <inheritdoc />
        protected override int Sign => 1;

        /// <inheritdoc />
        protected override string ActivityName => "Add Business Time";
    }

    /// <summary>Moves a moment backward by an amount of working time.</summary>
    /// <remarks>
    /// Useful for working out when something had to start: a task that needs four business hours and is due
    /// Monday 11:00 has to begin on the previous Friday at 15:00.
    /// </remarks>
    [DisplayName("Subtract Business Time")]
    [Description("Moves a date backward by a number of business days, hours and minutes, skipping closed hours, weekends and holidays.")]
    public sealed class SubtractBusinessTime : BusinessTimeShiftActivity
    {
        /// <inheritdoc />
        protected override int Sign => -1;

        /// <inheritdoc />
        protected override string ActivityName => "Subtract Business Time";
    }
}
