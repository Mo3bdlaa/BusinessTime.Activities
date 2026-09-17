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

        private static string DesignSource(string fileName)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
                directory = directory.Parent;
            Assert.NotNull(directory);

            string path = Path.Combine(directory.FullName, "src", "BusinessTime.Activities.Design", fileName);
            Assert.True(File.Exists(path), $"Expected {path}.");
            return File.ReadAllText(path);
        }

        [Fact]
        public void EveryActivityHasAnIconOfItsOwn()
        {
            // One shared icon would leave the pack unreadable on a canvas, so each activity names its own.
            string source = DesignSource("Glyphs.cs");

            string[] missing = PublicActivities
                .Select(type => type.Name)
                .Where(name => !source.Contains("\"" + name + "\""))
                .OrderBy(name => name)
                .ToArray();

            Assert.True(missing.Length == 0, "These activities have no icon in Glyphs.cs: " + string.Join(", ", missing));
        }

        [Fact]
        public void EveryResultIsDescribedInItsOwnTerms()
        {
            // Result arrives from the base class with no category and no description; the designer supplies
            // both, and a shared description would be no better than none.
            string source = DesignSource("DesignerMetadata.cs");

            string[] missing = PublicActivities
                // Activity<T> shadows ActivityWithResult.Result, so asking by name is ambiguous.
                .Where(type => type.GetProperties().Any(property => property.Name == "Result"))
                .Select(type => type.Name)
                .Where(name => !source.Contains("typeof(" + name + ")"))
                .OrderBy(name => name)
                .ToArray();

            Assert.True(missing.Length == 0, "These activities have no Result description: " + string.Join(", ", missing));
            Assert.Contains("CategoryAttribute(\"Output\")", source);
        }

        [Fact]
        public void EveryActivityHasADesignerTypeOfItsOwn()
        {
            // The activities panel asks a designer type for its icon before any activity exists, so an icon
            // set once a model item arrives leaves the panel blank. One type per activity is what carries it.
            string source = DesignSource("ActivityDesigners.cs");

            string[] missing = PublicActivities
                .Select(type => type.Name)
                .Where(name => !source.Contains("class " + name + "Designer"))
                .OrderBy(name => name)
                .ToArray();

            Assert.True(missing.Length == 0, "These activities have no designer type: " + string.Join(", ", missing));
        }

        [Fact]
        public void EveryActivitySaysWhereItBelongsInThePanel()
        {
            // Without this the panel falls back to the assembly name and buries everything a level down.
            string[] missing = PublicActivities
                .Where(type => !type.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), false)
                                    .Cast<System.ComponentModel.CategoryAttribute>()
                                    .Any(attribute => attribute.Category.StartsWith("Business Time")))
                .Select(type => type.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.True(missing.Length == 0, "These activities have no Business Time category: " + string.Join(", ", missing));
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
