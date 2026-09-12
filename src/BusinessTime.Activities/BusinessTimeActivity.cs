using System;
using System.Activities;
using System.ComponentModel;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>Category names used to group the activities and their properties in Studio.</summary>
    internal static class Categories
    {
        internal const string BusinessTime = "Business Time";

        internal const string Calendar = "Calendar";
        internal const string Input = "Input";
        internal const string Output = "Output";
        internal const string Options = "Options";
    }

    /// <summary>Convenience helpers for arguments a workflow may simply have left unbound.</summary>
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
    /// Shared behaviour for the calculating activities: each one needs a calendar, and each one finds it the
    /// same way.
    /// </summary>
    /// <typeparam name="TResult">Type of the activity's <c>Result</c> argument.</typeparam>
    public abstract class BusinessTimeActivity<TResult> : CodeActivity<TResult>
    {
        /// <summary>
        /// The calendar to calculate with, usually the output of a <c>Create Business Calendar</c> or
        /// <c>Load Business Calendar</c> activity.
        /// </summary>
        [Category(Categories.Calendar)]
        [DisplayName("Calendar")]
        [Description("The calendar to calculate with, for example the calendar variable produced by Create Business Calendar. " +
                     "Leave it empty to use the Working week property instead.")]
        public InArgument<BusinessCalendar> Calendar { get; set; }

        /// <summary>
        /// A working week written out in full, for a quick calculation that needs no holidays.
        /// </summary>
        [Category(Categories.Calendar)]
        [DisplayName("Working week")]
        [Description("A working week for when no Calendar is supplied, for example \"Mon-Fri 09:00-17:00\". " +
                     "Other examples: \"Mon-Fri 09:00-12:00,13:00-17:00\" for a lunch break, " +
                     "\"Mon-Thu 08:00-16:30; Fri 08:00-14:00\" for a short Friday, \"Daily 00:00-24:00\" for around the clock. " +
                     "Holidays need a Calendar. Defaults to Mon-Fri 09:00-17:00.")]
        public InArgument<string> Schedule { get; set; }

        /// <summary>
        /// Resolves the calendar: the supplied one first, then the working week, then a Monday to Friday
        /// nine to five in the robot's own time zone.
        /// </summary>
        protected BusinessCalendar ResolveCalendar(CodeActivityContext context)
        {
            BusinessCalendar supplied = Calendar.GetValue(context);
            if (supplied != null)
                return supplied;

            string schedule = Schedule.GetValue(context);
            if (!string.IsNullOrWhiteSpace(schedule))
                return BusinessCalendar.FromSchedule(schedule);

            return BusinessCalendar.Default;
        }
    }
}
