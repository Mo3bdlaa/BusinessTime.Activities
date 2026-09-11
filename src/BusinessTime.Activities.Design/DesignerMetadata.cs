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

                Attach(builder, typeof(BusinessCalendarScope), typeof(BusinessCalendarScopeDesigner));

                Attach(builder, typeof(CreateBusinessCalendar), typeof(BusinessCalendarActivityDesigner));
                Attach(builder, typeof(LoadBusinessCalendar), typeof(BusinessCalendarActivityDesigner));
                Attach(builder, typeof(SaveBusinessCalendar), typeof(BusinessCalendarActivityDesigner));

                Attach(builder, typeof(AddBusinessTime), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(SubtractBusinessTime), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(GetBusinessTimeBetween), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(CountBusinessDays), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(IsBusinessTime), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(GetBusinessDayInfo), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(SnapToBusinessTime), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(GetNextBusinessDay), typeof(BusinessTimeActivityDesigner));
                Attach(builder, typeof(GetWorkingIntervals), typeof(BusinessTimeActivityDesigner));

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
