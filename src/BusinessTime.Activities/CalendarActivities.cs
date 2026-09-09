using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>
    /// Builds a business calendar from a schedule string, a time zone and a list of holidays.
    /// </summary>
    /// <remarks>
    /// This is the quickest way to standardise working hours across a process: one activity at the start of
    /// the workflow, its result stored in a variable, and every later calculation pointed at that variable.
    /// </remarks>
    [DisplayName("Create Business Calendar")]
    [Description("Builds a business calendar from a working week, a time zone and a list of holidays.")]
    public sealed class CreateBusinessCalendar : CodeActivity<BusinessCalendar>
    {
        /// <summary>Working week, for example <c>Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00</c>.</summary>
        [Category(Categories.Input)]
        [DisplayName("Schedule")]
        [Description("Working week, for example 'Mon-Fri 09:00-17:00' or 'Mon-Thu 08:00-16:30; Fri 08:00-14:00'. Defaults to Mon-Fri 09:00-17:00.")]
        public InArgument<string> Schedule { get; set; }

        /// <summary>Time zone the working hours are expressed in.</summary>
        [Category(Categories.Input)]
        [DisplayName("Time zone")]
        [Description("Time zone the hours are expressed in, for example 'Europe/Berlin' or 'W. Europe Standard Time'. Leave empty to use the robot's own time zone.")]
        public InArgument<string> TimeZoneId { get; set; }

        /// <summary>Dates that are not worked.</summary>
        [Category(Categories.Input)]
        [DisplayName("Holidays")]
        [Description("Dates that are not worked. Accepts any collection of dates, such as a list built from a queue or a spreadsheet column.")]
        public InArgument<IEnumerable<DateTime>> Holidays { get; set; }

        /// <summary>Dates that are not worked and repeat every year.</summary>
        [Category(Categories.Input)]
        [DisplayName("Annual holidays")]
        [Description("Dates that are not worked and repeat every year. Only the month and day are used.")]
        public InArgument<IEnumerable<DateTime>> AnnualHolidays { get; set; }

        /// <summary>Length of a nominal business day.</summary>
        [Category(Categories.Options)]
        [DisplayName("Hours per business day")]
        [Description("Length of one business day, used whenever a duration is given in days. Leave at zero to derive it from the schedule.")]
        public InArgument<double> HoursPerBusinessDay { get; set; }

        /// <summary>Label used in logs.</summary>
        [Category(Categories.Options)]
        [DisplayName("Name")]
        [Description("Optional label for the calendar, shown in logs and error messages.")]
        public InArgument<string> Name { get; set; }

        /// <inheritdoc />
        protected override BusinessCalendar Execute(CodeActivityContext context)
        {
            var builder = new BusinessCalendarBuilder();

            string schedule = Schedule.GetValue(context);
            if (!string.IsNullOrWhiteSpace(schedule))
                builder.WithSchedule(schedule);

            builder.WithTimeZone(TimeZones.Resolve(TimeZoneId.GetValue(context)));
            builder.WithName(Name.GetValue(context));

            double dayLength = HoursPerBusinessDay.GetValue(context);
            if (dayLength > 0)
                builder.WithHoursPerBusinessDay(dayLength);

            IEnumerable<DateTime> holidays = Holidays.GetValue(context);
            if (holidays != null)
                builder.AddHolidays(holidays);

            IEnumerable<DateTime> annualHolidays = AnnualHolidays.GetValue(context);
            if (annualHolidays != null)
            {
                foreach (DateTime holiday in annualHolidays)
                    builder.AddAnnualHoliday(holiday.Month, holiday.Day);
            }

            return builder.Build();
        }
    }

    /// <summary>Loads a business calendar from a JSON file or from JSON text.</summary>
    /// <remarks>
    /// Keeping the calendar in a file lets several processes share one definition, and lets the business
    /// update holidays without anyone republishing a package.
    /// </remarks>
    [DisplayName("Load Business Calendar")]
    [Description("Loads a business calendar from a JSON file or from JSON text.")]
    public sealed class LoadBusinessCalendar : CodeActivity<BusinessCalendar>
    {
        /// <summary>Path to a calendar JSON file.</summary>
        [Category(Categories.Input)]
        [DisplayName("File path")]
        [Description("Path to a calendar JSON file. Either this or Json must be supplied.")]
        public InArgument<string> FilePath { get; set; }

        /// <summary>Calendar JSON text, for when the definition comes from an asset or a queue item.</summary>
        [Category(Categories.Input)]
        [DisplayName("Json")]
        [Description("Calendar JSON text, for when the definition comes from an Orchestrator asset instead of a file.")]
        public InArgument<string> Json { get; set; }

        /// <summary>Overrides the time zone recorded in the document.</summary>
        [Category(Categories.Options)]
        [DisplayName("Time zone override")]
        [Description("Optional time zone that replaces the one recorded in the document.")]
        public InArgument<string> TimeZoneId { get; set; }

        /// <inheritdoc />
        protected override BusinessCalendar Execute(CodeActivityContext context)
        {
            string path = FilePath.GetValue(context);
            string json = Json.GetValue(context);

            if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(json))
                throw new BusinessTimeException("Load Business Calendar needs either a file path or JSON text.");

            BusinessCalendar calendar = string.IsNullOrWhiteSpace(path)
                ? BusinessCalendarSerializer.FromJson(json)
                : BusinessCalendarSerializer.LoadFile(path);

            string timeZoneId = TimeZoneId.GetValue(context);
            return string.IsNullOrWhiteSpace(timeZoneId)
                ? calendar
                : calendar.ToBuilder().WithTimeZone(timeZoneId).Build();
        }
    }

    /// <summary>Writes a business calendar to a JSON file.</summary>
    [DisplayName("Save Business Calendar")]
    [Description("Writes a business calendar to a JSON file.")]
    public sealed class SaveBusinessCalendar : CodeActivity
    {
        /// <summary>The calendar to write.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Calendar")]
        [Description("The calendar to write.")]
        public InArgument<BusinessCalendar> Calendar { get; set; }

        /// <summary>Destination file path. Missing folders are created.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("File path")]
        [Description("Destination file path. Missing folders are created.")]
        public InArgument<string> FilePath { get; set; }

        /// <inheritdoc />
        protected override void Execute(CodeActivityContext context)
        {
            BusinessCalendar calendar = Calendar.GetValue(context)
                ?? throw new BusinessTimeException("Save Business Calendar was given no calendar.");

            BusinessCalendarSerializer.SaveFile(calendar, FilePath.GetValue(context));
        }
    }
}
