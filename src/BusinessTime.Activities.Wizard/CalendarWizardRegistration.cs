using System;
using System.Diagnostics;
using System.IO;
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
        /// <summary>
        /// Finds the ribbon button's icon.
        /// </summary>
        /// <remarks>
        /// A pack URI is the obvious way to name an image inside an assembly, and it does not work here:
        /// resolving one asks WPF to load this assembly by name, which fails when Studio has loaded it into
        /// a context of its own. So the icon is shipped as a file beside this assembly and named by its path,
        /// which needs nothing resolved. The copy embedded in the assembly is kept as a fallback for a host
        /// that can resolve a pack URI after all.
        /// </remarks>
        private static string FindIcon()
        {
            const string embedded = "/BusinessTime.Activities.Wizard;component/Resources/calendar.png";

            try
            {
                string assembly = typeof(CalendarWizardRegistration).Assembly.Location;
                if (string.IsNullOrEmpty(assembly))
                    return embedded;

                string beside = Path.Combine(Path.GetDirectoryName(assembly) ?? string.Empty, "calendar.png");
                return File.Exists(beside) ? new Uri(beside).AbsoluteUri : embedded;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("BusinessTime calendar icon could not be located: " + exception);
                return embedded;
            }
        }

        /// <summary>Registers the wizard.</summary>
        public void Initialize(IWorkflowDesignApi api)
        {
            try
            {
                var wizards = new WizardCollection();

                wizards.WizardDefinitions.Add(new WizardDefinition
                {
                    DisplayName = "Business Calendar",
                    IconUri = FindIcon(),
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
