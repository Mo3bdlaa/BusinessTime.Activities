using System;
using System.Activities.Presentation.Metadata;
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

                // The activities that build calendars carry the calendar icon.
                Attach(builder, typeof(CreateBusinessCalendar), typeof(InlineCalendarActivityDesigner));
                Attach(builder, typeof(LoadBusinessCalendar), typeof(InlineCalendarActivityDesigner));
                Attach(builder, typeof(SaveBusinessCalendar), typeof(InlineCalendarActivityDesigner));

                // The ones that calculate carry the clock.
                Attach(builder, typeof(AddBusinessTime), typeof(InlineActivityDesigner));
                Attach(builder, typeof(SubtractBusinessTime), typeof(InlineActivityDesigner));
                Attach(builder, typeof(GetBusinessTimeBetween), typeof(InlineActivityDesigner));
                Attach(builder, typeof(CountBusinessDays), typeof(InlineActivityDesigner));
                Attach(builder, typeof(IsBusinessTime), typeof(InlineActivityDesigner));
                Attach(builder, typeof(GetBusinessDayInfo), typeof(InlineActivityDesigner));
                Attach(builder, typeof(SnapToBusinessTime), typeof(InlineActivityDesigner));
                Attach(builder, typeof(GetNextBusinessDay), typeof(InlineActivityDesigner));
                Attach(builder, typeof(GetWorkingIntervals), typeof(InlineActivityDesigner));

                MetadataStore.AddAttributeTable(builder.CreateTable());
            }
            catch (Exception exception)
            {
                Debug.WriteLine("BusinessTime designers could not be registered: " + exception);
            }
        }

        private static void Attach(AttributeTableBuilder builder, Type activity, Type designer)
        {
            builder.AddCustomAttributes(activity, new DesignerAttribute(designer));
        }
    }
}
