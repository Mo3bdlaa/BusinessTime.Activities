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
        [Description("The moment to start from. A moment outside working hours is allowed: the clock simply starts running at the next working moment.")]
        public InArgument<DateTime> Date { get; set; }

        /// <summary>Number of business days to shift by.</summary>
        [Category(Categories.Input)]
        [DisplayName("Days")]
        [Description("Business days to shift by. Read according to the Day handling property.")]
        public InArgument<double> Days { get; set; }

        /// <summary>Number of business hours to shift by.</summary>
        [Category(Categories.Input)]
        [DisplayName("Hours")]
        [Description("Business hours to shift by. Fractions are allowed.")]
        public InArgument<double> Hours { get; set; }

        /// <summary>Number of business minutes to shift by.</summary>
        [Category(Categories.Input)]
        [DisplayName("Minutes")]
        [Description("Business minutes to shift by.")]
        public InArgument<double> Minutes { get; set; }

        /// <summary>An additional duration to shift by, for when the amount is already a TimeSpan.</summary>
        [Category(Categories.Input)]
        [DisplayName("Duration")]
        [Description("An additional amount of working time to shift by, for when the amount is already held in a TimeSpan.")]
        public InArgument<TimeSpan> Duration { get; set; }

        /// <summary>How the <see cref="Days"/> input should be read.</summary>
        [Category(Categories.Options)]
        [DisplayName("Day handling")]
        [Description("Whether a day means the calendar's hours per business day, or a whole day on the calendar with the clock time carried over.")]
        public DayHandling DayHandling { get; set; }

        /// <summary>The working time actually skipped over, including closed hours.</summary>
        [Category(Categories.Output)]
        [DisplayName("Elapsed time")]
        [Description("The wall clock time between the input and the result, including the closed hours that were skipped.")]
        public OutArgument<TimeSpan> ElapsedTime { get; set; }

        /// <summary>Direction this activity shifts in: 1 forwards, -1 backwards.</summary>
        protected abstract int Sign { get; }

        /// <summary>Name shown in error messages.</summary>
        protected abstract string ActivityName { get; }

        /// <inheritdoc />
        protected override DateTime Calculate(NativeActivityContext context)
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
