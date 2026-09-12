using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>
    /// Builds the calendar every other activity calculates with: the working week, the time zone it is
    /// written in, and the days nobody works.
    /// </summary>
    /// <remarks>
    /// Drop this at the start of the process, keep the result in a variable, and point the other activities
    /// at that variable.
    /// </remarks>
    [DisplayName("Create Business Calendar")]
    [Description("Builds a business calendar from a working week, a time zone and a list of holidays. Usually the first Business Time activity in a process.")]
    public sealed class CreateBusinessCalendar : CodeActivity<BusinessCalendar>
    {
        /// <summary>The recurring working week.</summary>
        [Category(Categories.Input)]
        [DisplayName("Working week")]
        [Description("The working week. Examples: \"Mon-Fri 09:00-17:00\"; " +
                     "\"Mon-Fri 09:00-12:00,13:00-17:00\" for a lunch break; " +
                     "\"Mon-Thu 08:00-16:30; Fri 08:00-14:00\" for a short Friday; " +
                     "\"Mon-Fri 09:00-17:00; Sat 09:00-13:00\" to open on Saturday mornings; " +
                     "\"Daily 00:00-24:00\" for around the clock; \"Mon-Fri 22:00-06:00\" for a night shift. " +
                     "Days you never mention are days off. Defaults to Mon-Fri 09:00-17:00.")]
        public InArgument<string> Schedule { get; set; }

        /// <summary>The zone the working hours are written in.</summary>
        [Category(Categories.Input)]
        [DisplayName("Time zone")]
        [Description("The zone the working hours are written in, named by a city. Pick MachineLocal to follow the robot's own clock, " +
                     "or Custom to type an identifier into the Time zone id property below.")]
        public CommonTimeZone TimeZone { get; set; }

        /// <summary>An explicit time zone identifier, for zones not in the drop-down.</summary>
        [Category(Categories.Input)]
        [DisplayName("Time zone id")]
        [Description("Only needed when Time zone is set to Custom. Examples: \"Europe/Oslo\", \"Asia/Kolkata\", " +
                     "\"Central Asia Standard Time\". Both the IANA and the Windows spellings work.")]
        public InArgument<string> TimeZoneId { get; set; }

        /// <summary>Dates that are not worked.</summary>
        [Category(Categories.Input)]
        [DisplayName("Holidays")]
        [Description("Dates nobody works, as any collection of dates. Examples: " +
                     "new DateTime(){ new DateTime(2026,12,25), new DateTime(2026,12,26) }; " +
                     "or a column read from a spreadsheet, holidayTable.AsEnumerable().Select(Function(r) r.Field(Of DateTime)(\"Date\")).ToList().")]
        public InArgument<IEnumerable<DateTime>> Holidays { get; set; }

        /// <summary>Dates that are not worked and repeat every year.</summary>
        [Category(Categories.Input)]
        [DisplayName("Holidays every year")]
        [Description("Dates that repeat every year, so they need setting only once. Only the month and day are used, " +
                     "for example new DateTime(){ new DateTime(2000,1,1), new DateTime(2000,12,25) } for New Year and Christmas.")]
        public InArgument<IEnumerable<DateTime>> AnnualHolidays { get; set; }

        /// <summary>Length of one nominal business day.</summary>
        [Category(Categories.Options)]
        [DisplayName("Hours per business day")]
        [Description("How long one business day is, used whenever a duration is given in days. " +
                     "Leave at 0 to work it out from the working week, which is usually what you want: " +
                     "a Mon-Fri 09:00-17:00 week gives 8. Set 7.5 if a day is seven and a half hours.")]
        public InArgument<double> HoursPerBusinessDay { get; set; }

        /// <summary>Label used in logs.</summary>
        [Category(Categories.Options)]
        [DisplayName("Name")]
        [Description("An optional label shown in logs and error messages, for example \"Support desk\". Nothing depends on it.")]
        public InArgument<string> Name { get; set; }

        /// <inheritdoc />
        protected override BusinessCalendar Execute(CodeActivityContext context)
        {
            var builder = new BusinessCalendarBuilder();

            string schedule = Schedule.GetValue(context);
            if (!string.IsNullOrWhiteSpace(schedule))
                builder.WithSchedule(schedule);

            builder.WithTimeZone(CommonTimeZones.Resolve(TimeZone, TimeZoneId.GetValue(context)));
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

    /// <summary>Loads a calendar from a JSON file or from JSON text.</summary>
    /// <remarks>
    /// Keeping the calendar in a file lets several processes share one definition, and lets the business
    /// change the holidays without anyone republishing a package.
    /// </remarks>
    [DisplayName("Load Business Calendar")]
    [Description("Loads a business calendar from a JSON file, or from JSON text held in an Orchestrator asset.")]
    public sealed class LoadBusinessCalendar : CodeActivity<BusinessCalendar>
    {
        /// <summary>Path to a calendar JSON file.</summary>
        [Category(Categories.Input)]
        [DisplayName("File path")]
        [Description("Path to a calendar JSON file, for example \"Data\\calendar.json\" or " +
                     "Path.Combine(Environment.CurrentDirectory, \"Data\", \"calendar.json\"). Supply this or Json.")]
        public InArgument<string> FilePath { get; set; }

        /// <summary>Calendar JSON text.</summary>
        [Category(Categories.Input)]
        [DisplayName("Json")]
        [Description("Calendar JSON text, for when the definition comes from an Orchestrator asset rather than a file. " +
                     "The smallest useful document is {\"week\": \"Mon-Fri 09:00-17:00\", \"timeZone\": \"UTC\"}.")]
        public InArgument<string> Json { get; set; }

        /// <summary>Replaces the zone recorded in the document.</summary>
        [Category(Categories.Options)]
        [DisplayName("Time zone override")]
        [Description("Optional. Replaces the time zone recorded in the document, for running one shared calendar " +
                     "against another region. Leave at MachineLocal to keep what the document says.")]
        public CommonTimeZone TimeZoneOverride { get; set; }

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

            // MachineLocal is the property's default, so it is read as "leave the document alone".
            return TimeZoneOverride == CommonTimeZone.MachineLocal
                ? calendar
                : calendar.ToBuilder().WithTimeZone(CommonTimeZones.Resolve(TimeZoneOverride, null)).Build();
        }
    }

    /// <summary>Writes a calendar out to a JSON file.</summary>
    [DisplayName("Save Business Calendar")]
    [Description("Writes a business calendar to a JSON file, so it can be shared between processes or edited by the business.")]
    public sealed class SaveBusinessCalendar : CodeActivity
    {
        /// <summary>The calendar to write.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Calendar")]
        [Description("The calendar to write, for example the variable produced by Create Business Calendar.")]
        public InArgument<BusinessCalendar> Calendar { get; set; }

        /// <summary>Destination path.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("File path")]
        [Description("Where to write it, for example \"Data\\calendar.json\". Missing folders are created.")]
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
