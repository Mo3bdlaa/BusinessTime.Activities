namespace BusinessTime.Activities.Design
{
    // One designer per activity. They differ only in which activity they belong to, but the activities panel
    // asks a designer type for its icon before any activity exists, so the type itself has to know.

    /// <summary>The card and icon for <see cref="CreateBusinessCalendar"/>.</summary>
    public sealed class CreateBusinessCalendarDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public CreateBusinessCalendarDesigner() : base(typeof(CreateBusinessCalendar))
        {
        }
    }

    /// <summary>The card and icon for <see cref="LoadBusinessCalendar"/>.</summary>
    public sealed class LoadBusinessCalendarDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public LoadBusinessCalendarDesigner() : base(typeof(LoadBusinessCalendar))
        {
        }
    }

    /// <summary>The card and icon for <see cref="SaveBusinessCalendar"/>.</summary>
    public sealed class SaveBusinessCalendarDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public SaveBusinessCalendarDesigner() : base(typeof(SaveBusinessCalendar))
        {
        }
    }

    /// <summary>The card and icon for <see cref="AddBusinessTime"/>.</summary>
    public sealed class AddBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public AddBusinessTimeDesigner() : base(typeof(AddBusinessTime))
        {
        }
    }

    /// <summary>The card and icon for <see cref="SubtractBusinessTime"/>.</summary>
    public sealed class SubtractBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public SubtractBusinessTimeDesigner() : base(typeof(SubtractBusinessTime))
        {
        }
    }

    /// <summary>The card and icon for <see cref="GetBusinessTimeBetween"/>.</summary>
    public sealed class GetBusinessTimeBetweenDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetBusinessTimeBetweenDesigner() : base(typeof(GetBusinessTimeBetween))
        {
        }
    }

    /// <summary>The card and icon for <see cref="CountBusinessDays"/>.</summary>
    public sealed class CountBusinessDaysDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public CountBusinessDaysDesigner() : base(typeof(CountBusinessDays))
        {
        }
    }

    /// <summary>The card and icon for <see cref="IsBusinessTime"/>.</summary>
    public sealed class IsBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public IsBusinessTimeDesigner() : base(typeof(IsBusinessTime))
        {
        }
    }

    /// <summary>The card and icon for <see cref="GetBusinessDayInfo"/>.</summary>
    public sealed class GetBusinessDayInfoDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetBusinessDayInfoDesigner() : base(typeof(GetBusinessDayInfo))
        {
        }
    }

    /// <summary>The card and icon for <see cref="SnapToBusinessTime"/>.</summary>
    public sealed class SnapToBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public SnapToBusinessTimeDesigner() : base(typeof(SnapToBusinessTime))
        {
        }
    }

    /// <summary>The card and icon for <see cref="GetNextBusinessDay"/>.</summary>
    public sealed class GetNextBusinessDayDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetNextBusinessDayDesigner() : base(typeof(GetNextBusinessDay))
        {
        }
    }

    /// <summary>The card and icon for <see cref="GetWorkingIntervals"/>.</summary>
    public sealed class GetWorkingIntervalsDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetWorkingIntervalsDesigner() : base(typeof(GetWorkingIntervals))
        {
        }
    }

}
