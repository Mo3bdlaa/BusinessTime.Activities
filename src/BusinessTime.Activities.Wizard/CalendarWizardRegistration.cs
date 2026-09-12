using System;
using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Wizards;

namespace BusinessTime.Activities.Wizard
{
    /// <summary>
    /// Puts a <c>Business Calendar</c> button in Studio's ribbon, next to the other wizards, opening an
    /// editor for the calendar file a process reads.
    /// </summary>
    /// <remarks>
    /// Studio looks for implementations of <see cref="IRegisterWorkflowDesignApi"/> when it loads the
    /// package and calls <see cref="Initialize"/> once.
    /// </remarks>
    public sealed class CalendarWizardRegistration : IRegisterWorkflowDesignApi
    {
        /// <summary>
        /// The ribbon button's icon, a calendar page, carried inside this assembly so nothing has to be
        /// installed alongside it.
        /// </summary>
        private const string IconUri =
            "pack://application:,,,/BusinessTime.Activities.Wizard;component/Resources/calendar.png";

        /// <summary>Registers the wizard.</summary>
        public void Initialize(IWorkflowDesignApi api)
        {
            try
            {
                var wizards = new WizardCollection();

                wizards.WizardDefinitions.Add(new WizardDefinition
                {
                    DisplayName = "Business Calendar",
                    IconUri = IconUri,
                    Tooltip = "Create and maintain the business calendar file this process reads: " +
                              "the working week, the time zone, and the holidays, half days and shutdowns.",
                    MinimizeBeforeRun = false,
                    Wizard = new WizardBase { RunWizard = Open }
                });

                api.Wizards.Register(wizards);
            }
            catch (Exception exception)
            {
                // Never let a wizard cost the package its activities.
                Debug.WriteLine("BusinessTime calendar wizard could not be registered: " + exception);
            }
        }

        /// <summary>
        /// Opens the editor. The wizard contract expects an activity to drop on the canvas; this one only
        /// maintains a file, so it returns nothing.
        /// </summary>
        private static System.Activities.Activity Open()
        {
            try
            {
                new CalendarEditorWindow().ShowDialog();
            }
            catch (Exception exception)
            {
                Debug.WriteLine("BusinessTime calendar editor failed: " + exception);
            }

            return null;
        }
    }
}
