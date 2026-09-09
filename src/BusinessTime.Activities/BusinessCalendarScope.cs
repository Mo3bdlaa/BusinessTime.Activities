using System;
using System.Activities;
using System.ComponentModel;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>
    /// Makes one calendar the default for every Business Time activity placed inside it, so that a process
    /// states its working hours once instead of on every activity.
    /// </summary>
    /// <remarks>
    /// The calendar is also handed to the body as an argument, so it can be passed on to invoked workflows.
    /// Activities inside the scope that set their own <c>Calendar</c> property still win.
    /// </remarks>
    [DisplayName("Business Calendar Scope")]
    [Description("Provides a business calendar to every Business Time activity inside it.")]
    public sealed class BusinessCalendarScope : NativeActivity
    {
        internal const string ScopePropertyName = "BusinessTime.Calendar";

        /// <summary>Creates the scope with an empty body.</summary>
        public BusinessCalendarScope()
        {
            Body = new ActivityAction<BusinessCalendar>
            {
                Argument = new DelegateInArgument<BusinessCalendar>("Calendar")
            };
        }

        /// <summary>
        /// The calendar to share with the activities inside. When left empty, <see cref="Schedule"/> is used,
        /// and failing that a Monday-Friday 09:00-17:00 week in the machine's time zone.
        /// </summary>
        [Category(Categories.CalendarProperties)]
        [DisplayName("Calendar")]
        [Description("Business calendar shared with every activity inside the scope.")]
        public InArgument<BusinessCalendar> Calendar { get; set; }

        /// <summary>A schedule string used to build the calendar when none is supplied.</summary>
        [Category(Categories.CalendarProperties)]
        [DisplayName("Schedule")]
        [Description("Shorthand working week, for example 'Mon-Fri 09:00-17:00'. Used only when no Calendar is supplied.")]
        public InArgument<string> Schedule { get; set; }

        /// <summary>The activities that run inside the scope.</summary>
        [Browsable(false)]
        public ActivityAction<BusinessCalendar> Body { get; set; }

        /// <inheritdoc />
        protected override void Execute(NativeActivityContext context)
        {
            BusinessCalendar calendar = Calendar.GetValue(context);

            if (calendar == null)
            {
                string schedule = Schedule.GetValue(context);
                calendar = string.IsNullOrWhiteSpace(schedule)
                    ? BusinessCalendar.Default
                    : BusinessCalendar.FromSchedule(schedule);
            }

            context.Properties.Add(ScopePropertyName, calendar);

            if (Body != null)
                context.ScheduleAction(Body, calendar);
        }

        /// <summary>Returns the calendar published by the nearest surrounding scope, or <c>null</c>.</summary>
        internal static BusinessCalendar FindCalendar(NativeActivityContext context) =>
            context?.Properties.Find(ScopePropertyName) as BusinessCalendar;
    }
}
