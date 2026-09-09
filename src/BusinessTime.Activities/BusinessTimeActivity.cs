using System;
using System.Activities;
using System.ComponentModel;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>Category names used to group the activities in the Studio panel.</summary>
    internal static class Categories
    {
        internal const string BusinessTime = "Business Time";
        internal const string Calendar = "Business Time/Calendar";

        internal const string CalendarProperties = "Calendar";
        internal const string Input = "Input";
        internal const string Output = "Output";
        internal const string Options = "Options";
    }

    /// <summary>Convenience helpers for arguments that a workflow may simply have left unbound.</summary>
    internal static class ArgumentExtensions
    {
        internal static T GetValue<T>(this InArgument<T> argument, ActivityContext context) =>
            argument == null ? default(T) : argument.Get(context);

        internal static void SetValue<T>(this OutArgument<T> argument, ActivityContext context, T value)
        {
            argument?.Set(context, value);
        }
    }

    /// <summary>
    /// Shared behaviour for the activities: every one of them needs a calendar, and every one of them
    /// resolves it the same way.
    /// </summary>
    /// <remarks>
    /// These activities derive from <see cref="NativeActivity{TResult}"/> rather than <c>CodeActivity</c>
    /// because only a native context can read the execution properties that
    /// <see cref="BusinessCalendarScope"/> publishes.
    /// </remarks>
    /// <typeparam name="TResult">Type of the activity's <c>Result</c> argument.</typeparam>
    public abstract class BusinessTimeActivity<TResult> : NativeActivity<TResult>
    {
        /// <summary>
        /// The calendar to use. When it is left empty the activity falls back to the surrounding
        /// <see cref="BusinessCalendarScope"/>, then to <see cref="Schedule"/>, and finally to a
        /// Monday-Friday 09:00-17:00 week in the machine's time zone.
        /// </summary>
        [Category(Categories.CalendarProperties)]
        [DisplayName("Calendar")]
        [Description("Business calendar to use. Leave empty to inherit the surrounding Business Calendar Scope, or to fall back to the Schedule property.")]
        public InArgument<BusinessCalendar> Calendar { get; set; }

        /// <summary>
        /// A schedule string such as <c>Mon-Fri 09:00-17:00</c>, used only when no calendar is supplied and
        /// there is no surrounding scope. Convenient for a quick calculation that needs no holidays.
        /// </summary>
        [Category(Categories.CalendarProperties)]
        [DisplayName("Schedule")]
        [Description("Shorthand working week, for example 'Mon-Fri 09:00-17:00'. Used only when no Calendar is supplied and there is no surrounding scope.")]
        public InArgument<string> Schedule { get; set; }

        /// <summary>
        /// Resolves the calendar for this execution: the explicit argument first, then the surrounding
        /// scope, then the shorthand schedule, and finally the default working week.
        /// </summary>
        protected BusinessCalendar ResolveCalendar(NativeActivityContext context)
        {
            BusinessCalendar supplied = Calendar.GetValue(context);
            if (supplied != null)
                return supplied;

            BusinessCalendar scoped = BusinessCalendarScope.FindCalendar(context);
            if (scoped != null)
                return scoped;

            string schedule = Schedule.GetValue(context);
            if (!string.IsNullOrWhiteSpace(schedule))
                return BusinessCalendar.FromSchedule(schedule);

            return BusinessCalendar.Default;
        }

        /// <summary>Runs the activity and publishes its <c>Result</c>.</summary>
        protected sealed override void Execute(NativeActivityContext context)
        {
            Result.Set(context, Calculate(context));
        }

        /// <summary>Performs the calculation and returns the value for the <c>Result</c> argument.</summary>
        protected abstract TResult Calculate(NativeActivityContext context);
    }
}
