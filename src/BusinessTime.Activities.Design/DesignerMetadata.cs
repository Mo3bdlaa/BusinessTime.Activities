using System;
using System.Activities.Presentation.Metadata;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace BusinessTime.Activities.Design
{
    /// <summary>
    /// Attaches the designers to the activities. Studio looks for implementations of
    /// <see cref="IRegisterMetadata"/> when it loads the package and calls <see cref="Register"/> once.
    /// </summary>
    public sealed class DesignerMetadata : IRegisterMetadata
    {
        /// <summary>Registers the designers.</summary>
        /// <remarks>
        /// A failure here would otherwise stop Studio from loading the package at all, so anything
        /// unexpected is swallowed: the activities then fall back to the stock designers and keep working.
        /// </remarks>
        public void Register()
        {
            try
            {
                var builder = new AttributeTableBuilder();

                foreach (KeyValuePair<Type, string> entry in Results)
                {
                    Attach(builder, entry.Key);

                    // Result arrives from CodeActivity<T> with no category of its own, which lands it under
                    // Misc, away from the outputs it belongs with.
                    builder.AddCustomAttributes(
                        entry.Key,
                        "Result",
                        new CategoryAttribute("Output"),
                        new DisplayNameAttribute("Result"),
                        new DescriptionAttribute(entry.Value));
                }

                // Save Business Calendar writes a file rather than returning anything.
                Attach(builder, typeof(SaveBusinessCalendar));

                MetadataStore.AddAttributeTable(builder.CreateTable());
            }
            catch (Exception exception)
            {
                Debug.WriteLine("BusinessTime designers could not be registered: " + exception);
            }
        }

        /// <summary>What each activity's <c>Result</c> holds, said in its own terms.</summary>
        private static readonly Dictionary<Type, string> Results = new Dictionary<Type, string>
        {
            [typeof(CreateBusinessCalendar)] =
                "The calendar you have just described. Keep it in a variable and hand it to the Calendar property of the activities that follow.",
            [typeof(LoadBusinessCalendar)] =
                "The calendar read from the file or the JSON, ready to hand to the activities that follow.",

            [typeof(AddBusinessTime)] =
                "The moment you land on once the working time has been added, for example Monday 14:00 for Friday 16:30 plus four business hours.",
            [typeof(SubtractBusinessTime)] =
                "The moment the work would have had to start to finish on time.",
            [typeof(GetBusinessTimeBetween)] =
                "The working time separating the two moments, as a TimeSpan, for example 05:45:00. Closed hours, weekends and holidays cost nothing.",
            [typeof(CountBusinessDays)] =
                "How many working days the period covers, counting both end dates.",
            [typeof(IsBusinessTime)] =
                "True when that exact moment is inside working hours. Use Is working day to tell a closed day from merely being out of hours.",
            [typeof(GetBusinessDayInfo)] =
                "True when the date is worked at all. The other outputs describe it: when it opens, when it closes, and how much work it holds.",
            [typeof(SnapToBusinessTime)] =
                "The moment moved onto the calendar, or the moment itself when it was already working time.",
            [typeof(GetNextBusinessDay)] =
                "The moment work starts on that day, never midnight, so it can be used directly as a start time.",
            [typeof(GetWorkingIntervals)] =
                "The working windows inside the period, each with a Start, an End and a Duration."
        };

        private static void Attach(AttributeTableBuilder builder, Type activity)
        {
            builder.AddCustomAttributes(activity, new DesignerAttribute(typeof(InlineActivityDesigner)));
        }
    }
}
