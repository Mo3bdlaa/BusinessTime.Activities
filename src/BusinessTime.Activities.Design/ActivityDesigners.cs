namespace BusinessTime.Activities.Design
{
    // One designer per activity. They differ only in which icon they carry, but the activities panel asks a
    // designer type for its icon before any activity exists, so the type itself has to know which one it is.

    /// <summary>The card and icon for <c>CreateBusinessCalendar</c>.</summary>
    public sealed class CreateBusinessCalendarDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public CreateBusinessCalendarDesigner() : base("CreateBusinessCalendar")
        {
        }
    }

    /// <summary>The card and icon for <c>LoadBusinessCalendar</c>.</summary>
    public sealed class LoadBusinessCalendarDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public LoadBusinessCalendarDesigner() : base("LoadBusinessCalendar")
        {
        }
    }

    /// <summary>The card and icon for <c>SaveBusinessCalendar</c>.</summary>
    public sealed class SaveBusinessCalendarDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public SaveBusinessCalendarDesigner() : base("SaveBusinessCalendar")
        {
        }
    }

    /// <summary>The card and icon for <c>AddBusinessTime</c>.</summary>
    public sealed class AddBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public AddBusinessTimeDesigner() : base("AddBusinessTime")
        {
        }
    }

    /// <summary>The card and icon for <c>SubtractBusinessTime</c>.</summary>
    public sealed class SubtractBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public SubtractBusinessTimeDesigner() : base("SubtractBusinessTime")
        {
        }
    }

    /// <summary>The card and icon for <c>GetBusinessTimeBetween</c>.</summary>
    public sealed class GetBusinessTimeBetweenDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetBusinessTimeBetweenDesigner() : base("GetBusinessTimeBetween")
        {
        }
    }

    /// <summary>The card and icon for <c>CountBusinessDays</c>.</summary>
    public sealed class CountBusinessDaysDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public CountBusinessDaysDesigner() : base("CountBusinessDays")
        {
        }
    }

    /// <summary>The card and icon for <c>IsBusinessTime</c>.</summary>
    public sealed class IsBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public IsBusinessTimeDesigner() : base("IsBusinessTime")
        {
        }
    }

    /// <summary>The card and icon for <c>GetBusinessDayInfo</c>.</summary>
    public sealed class GetBusinessDayInfoDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetBusinessDayInfoDesigner() : base("GetBusinessDayInfo")
        {
        }
    }

    /// <summary>The card and icon for <c>SnapToBusinessTime</c>.</summary>
    public sealed class SnapToBusinessTimeDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public SnapToBusinessTimeDesigner() : base("SnapToBusinessTime")
        {
        }
    }

    /// <summary>The card and icon for <c>GetNextBusinessDay</c>.</summary>
    public sealed class GetNextBusinessDayDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetNextBusinessDayDesigner() : base("GetNextBusinessDay")
        {
        }
    }

    /// <summary>The card and icon for <c>GetWorkingIntervals</c>.</summary>
    public sealed class GetWorkingIntervalsDesigner : InlineActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetWorkingIntervalsDesigner() : base("GetWorkingIntervals")
        {
        }
    }

}
