using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace BusinessTime.Activities.Tests
{
    /// <summary>
    /// Checks that every field the cards draw is really an argument on its activity.
    /// </summary>
    /// <remarks>
    /// The cards came out blank for a long time because the designer looked a property up by name, and
    /// <c>Activity&lt;TResult&gt;</c> shadows <c>ActivityWithResult.Result</c>, so every activity has two
    /// properties called Result and the lookup threw. The designer caught that and drew nothing, which looks
    /// exactly like a designer that never ran. These run the same lookups the card does.
    /// </remarks>
    public class InlineLayoutTests
    {
        /// <summary>The card layouts, read out of the designer source: activity name to field names.</summary>
        public static IEnumerable<object[]> Layouts()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
                directory = directory.Parent;

            string source = File.ReadAllText(Path.Combine(
                directory.FullName, "src", "BusinessTime.Activities.Design", "InlineActivityDesigner.cs"));

            // Not anchored on a trailing comma: the last entry in the table has none, and a parser that
            // quietly skips one layout is how a blank card would go unnoticed again.
            foreach (Match block in Regex.Matches(source, @"\[""(?<activity>\w+)""\] = new\[\]\s*\{(?<body>.*?)\}\s*[,\n]", RegexOptions.Singleline))
            {
                string[] fields = Regex.Matches(block.Groups["body"].Value, @"new InlineField\(""(?<name>\w+)""")
                    .Cast<Match>()
                    .Select(match => match.Groups["name"].Value)
                    .ToArray();

                yield return new object[] { block.Groups["activity"].Value, fields };
            }
        }

        [Theory]
        [MemberData(nameof(Layouts))]
        public void EveryFieldOnACardIsAnArgumentOnItsActivity(string activityName, string[] fields)
        {
            Type activity = typeof(AddBusinessTime).Assembly
                .GetExportedTypes()
                .SingleOrDefault(type => type.Name == activityName);

            Assert.NotNull(activity);
            Assert.NotEmpty(fields);

            foreach (string field in fields)
            {
                PropertyInfo[] matches = activity.GetProperties().Where(p => p.Name == field).ToArray();
                Assert.True(matches.Length > 0, $"{activityName} has no property called {field}.");

                PropertyInfo property = matches
                    .OrderByDescending(p => Depth(p.DeclaringType))
                    .First();

                Assert.True(
                    property.PropertyType.IsGenericType &&
                    (property.PropertyType.GetGenericTypeDefinition() == typeof(InArgument<>) ||
                     property.PropertyType.GetGenericTypeDefinition() == typeof(OutArgument<>)),
                    $"{activityName}.{field} is a {property.PropertyType.Name}, which a card cannot draw.");
            }
        }

        [Fact]
        public void EveryActivityWithALayoutIsActuallyChecked()
        {
            // Guards the reader above: a regex that matches fewer layouts than are declared would leave
            // some cards untested while the suite still looked green.
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
                directory = directory.Parent;

            string source = File.ReadAllText(Path.Combine(
                directory.FullName, "src", "BusinessTime.Activities.Design", "InlineActivityDesigner.cs"));

            int declared = Regex.Matches(source, @"\[""\w+""\] = new\[\]").Count;

            Assert.Equal(declared, Layouts().Count());
        }

        [Fact]
        public void LookingResultUpByNameAloneStillThrows()
        {
            // The reason the cards were blank. If this ever stops throwing the workaround can go, but until
            // then nothing in the designer may call GetProperty("Result").
            Assert.Throws<AmbiguousMatchException>(() => typeof(AddBusinessTime).GetProperty("Result"));
        }

        [Fact]
        public void TheDesignerDoesNotLookPropertiesUpTheWayThatThrows()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
                directory = directory.Parent;

            string source = File.ReadAllText(Path.Combine(
                directory.FullName, "src", "BusinessTime.Activities.Design", "InlineActivityDesigner.cs"));

            Assert.DoesNotContain(".GetProperty(", source);
        }

        private static int Depth(Type type)
        {
            int depth = 0;
            for (Type walk = type; walk != null; walk = walk.BaseType)
                depth++;
            return depth;
        }
    }
}
