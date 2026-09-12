using System;
using System.Activities;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace BusinessTime.Activities.Tests
{
    /// <summary>
    /// Guards the designer registrations.
    /// </summary>
    /// <remarks>
    /// The design assembly is WPF, so it cannot be loaded on a build agent that is not Windows and its
    /// behaviour cannot be exercised here. What can be checked anywhere is that every activity is actually
    /// registered, which is the mistake that is easy to make: adding an activity and forgetting its designer
    /// leaves it looking unlike the rest of the pack, and nothing else would catch it.
    /// </remarks>
    public class DesignerCoverageTests
    {
        private static string DesignerMetadataSource
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
                    directory = directory.Parent;

                Assert.NotNull(directory);

                string path = Path.Combine(
                    directory.FullName, "src", "BusinessTime.Activities.Design", "DesignerMetadata.cs");

                Assert.True(File.Exists(path), $"Expected the designer registrations at {path}.");
                return File.ReadAllText(path);
            }
        }

        private static IEnumerable<Type> PublicActivities =>
            typeof(AddBusinessTime).Assembly
                .GetExportedTypes()
                .Where(type => !type.IsAbstract && typeof(Activity).IsAssignableFrom(type));

        [Fact]
        public void EveryActivityHasADesignerRegistered()
        {
            string source = DesignerMetadataSource;

            HashSet<string> registered = Regex.Matches(source, @"typeof\((?<name>[A-Za-z0-9_]+)\)")
                .Cast<Match>()
                .Select(match => match.Groups["name"].Value)
                .ToHashSet();

            string[] missing = PublicActivities
                .Select(type => type.Name)
                .Where(name => !registered.Contains(name))
                .OrderBy(name => name)
                .ToArray();

            Assert.True(
                missing.Length == 0,
                "These activities have no designer registered in DesignerMetadata.cs: " + string.Join(", ", missing));
        }

        [Fact]
        public void TheActivitySetIsWhatTheDocumentationClaims()
        {
            // Twelve activities, with no scope: the calendar is handed to each one directly.
            Assert.Equal(12, PublicActivities.Count());
        }

        [Fact]
        public void TheActivitiesAskTheHostForTheWorkflowRuntimeItActuallyShips()
        {
            // Studio and the Robot ship System.Activities 6.0.0.0. The runtime resolves an assembly forward
            // but never backward, so compiling against any higher version - the UiPath.Workflow builds on
            // nuget.org carry 6.0.3.0 - makes every activity fail to load in a real project with
            // "Could not load file or assembly 'System.Activities'". Nothing else here would notice, because
            // the tests supply their own copy of the runtime.
            AssemblyName reference = typeof(AddBusinessTime).Assembly
                .GetReferencedAssemblies()
                .SingleOrDefault(name => name.Name == "System.Activities");

            Assert.NotNull(reference);
            Assert.Equal(new Version(6, 0, 0, 0), reference.Version);
        }

        [Fact]
        public void EveryActivityDrawsItsMainFieldsOnTheCard()
        {
            // The inline designer only draws activities it has a layout for; one without falls back to a
            // bare card, which looks broken next to the rest of the pack.
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
                directory = directory.Parent;
            Assert.NotNull(directory);

            string source = File.ReadAllText(Path.Combine(
                directory.FullName, "src", "BusinessTime.Activities.Design", "InlineActivityDesigner.cs"));

            HashSet<string> laidOut = Regex.Matches(source, @"\[""(?<name>[A-Za-z0-9_]+)""\] = new\[\]")
                .Cast<Match>()
                .Select(match => match.Groups["name"].Value)
                .ToHashSet();

            string[] missing = PublicActivities
                .Select(type => type.Name)
                .Where(name => !laidOut.Contains(name))
                .OrderBy(name => name)
                .ToArray();

            Assert.True(
                missing.Length == 0,
                "These activities have no inline layout in InlineActivityDesigner.cs: " + string.Join(", ", missing));
        }

        [Fact]
        public void TheRuntimeAssemblyDoesNotDragInWpf()
        {
            string[] windowsOnly = typeof(AddBusinessTime).Assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name =>
                    name.StartsWith("Presentation", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("WindowsBase", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("System.Windows", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.True(
                windowsOnly.Length == 0,
                "The activities must stay cross-platform, but they reference: " + string.Join(", ", windowsOnly));
        }
    }
}
