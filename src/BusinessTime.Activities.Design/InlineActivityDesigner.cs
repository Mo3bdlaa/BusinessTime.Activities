using System;
using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Converters;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BusinessTime.Activities.Design
{
    /// <summary>One field shown on the face of an activity.</summary>
    internal sealed class InlineField
    {
        internal InlineField(string propertyName, string label, string hint = null)
        {
            PropertyName = propertyName;
            Label = label;
            Hint = hint;
        }

        internal string PropertyName { get; }

        internal string Label { get; }

        internal string Hint { get; }
    }

    /// <summary>
    /// Draws an activity's main inputs and outputs on the card itself, so the common cases can be filled in
    /// without opening the properties panel. Everything else stays in the panel as usual.
    /// </summary>
    /// <remarks>
    /// One class serves every activity: the rows to draw are looked up from the activity's type once the
    /// model item arrives, and the type and direction of each row are read from the argument property by
    /// reflection, so the table below only has to name the fields.
    /// </remarks>
    public class InlineActivityDesigner : ActivityDesigner
    {
        private static readonly Dictionary<string, InlineField[]> Layouts = new Dictionary<string, InlineField[]>
        {
            ["CreateBusinessCalendar"] = new[]
            {
                new InlineField("Schedule", "Working week", "\"Mon-Fri 09:00-17:00\""),
                new InlineField("Result", "Calendar", "a new BusinessCalendar variable")
            },
            ["LoadBusinessCalendar"] = new[]
            {
                new InlineField("FilePath", "File path", "\"Data\\calendar.json\""),
                new InlineField("Result", "Calendar", "a new BusinessCalendar variable")
            },
            ["SaveBusinessCalendar"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("FilePath", "File path", "\"Data\\calendar.json\"")
            },
            ["AddBusinessTime"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("Date", "From", "DateTime.Now"),
                new InlineField("Days", "Days", "0"),
                new InlineField("Hours", "Hours", "8"),
                new InlineField("Result", "Result", "a DateTime variable")
            },
            ["SubtractBusinessTime"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("Date", "From", "dueAt"),
                new InlineField("Days", "Days", "0"),
                new InlineField("Hours", "Hours", "4"),
                new InlineField("Result", "Result", "a DateTime variable")
            },
            ["GetBusinessTimeBetween"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("From", "From", "ticket.Created"),
                new InlineField("To", "To", "DateTime.Now"),
                new InlineField("Result", "Working time", "a TimeSpan variable")
            },
            ["CountBusinessDays"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("From", "From", "DateTime.Today"),
                new InlineField("To", "To", "DateTime.Today.AddMonths(1)"),
                new InlineField("Result", "Days", "an Int32 variable")
            },
            ["IsBusinessTime"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("Date", "Moment", "DateTime.Now"),
                new InlineField("Result", "Open now", "a Boolean variable")
            },
            ["SnapToBusinessTime"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("Date", "Moment", "request.Received"),
                new InlineField("Result", "Result", "a DateTime variable")
            },
            ["GetBusinessDayInfo"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("Date", "Date", "DateTime.Today"),
                new InlineField("Result", "Is a working day", "a Boolean variable")
            },
            ["GetNextBusinessDay"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("Date", "From", "DateTime.Today"),
                new InlineField("Result", "Next working day", "a DateTime variable")
            },
            ["GetWorkingIntervals"] = new[]
            {
                new InlineField("Calendar", "Calendar", "your calendar variable"),
                new InlineField("From", "From", "DateTime.Now"),
                new InlineField("To", "To", "DateTime.Now.AddDays(7)"),
                new InlineField("Result", "Windows", "an IList(Of BusinessTimeInterval) variable")
            }
        };

        /// <summary>
        /// Creates the designer for one activity.
        /// </summary>
        /// <remarks>
        /// Both the icon and the card are built here rather than when a model item arrives: the activities
        /// panel asks a designer type for its icon without ever giving it an activity, so anything set later
        /// leaves the panel blank.
        /// </remarks>
        protected InlineActivityDesigner(Type activityType)
        {
            try
            {
                Icon = Glyphs.For(activityType.Name);

                if (Layouts.TryGetValue(activityType.Name, out InlineField[] fields))
                {
                    UIElement card = BuildCard(activityType, fields);
                    if (card != null)
                        Content = card;
                }
            }
            catch (Exception exception)
            {
                // A card that cannot be drawn is not worth failing the designer over: leaving Content unset
                // falls back to the plain card, and every property is still reachable from the panel.
                Debug.WriteLine("BusinessTime inline designer could not be built: " + exception);
            }
        }

        private static UIElement BuildCard(Type activityType, InlineField[] fields)
        {
            var grid = new Grid { Margin = new Thickness(4, 2, 4, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;
            foreach (InlineField field in fields)
            {
                PropertyInfo property = MostDerived(activityType, field.PropertyName);
                if (property == null)
                    continue;

                if (!TryDescribeArgument(property.PropertyType, out Type valueType, out bool isOutput))
                    continue;

                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock
                {
                    Text = field.Label,
                    Margin = new Thickness(0, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x4D, 0x56))
                };
                Grid.SetRow(label, row);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                UIElement editor = BuildEditor(field, valueType, isOutput);
                Grid.SetRow(editor, row);
                Grid.SetColumn(editor, 1);
                grid.Children.Add(editor);

                row++;
            }

            return row == 0 ? null : grid;
        }

        private static UIElement BuildEditor(InlineField field, Type valueType, bool isOutput)
        {
            var editor = new ExpressionTextBox
            {
                ExpressionType = valueType,
                UseLocationExpression = isOutput,
                HintText = field.Hint,
                Margin = new Thickness(0, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Center
            };

            // An argument is edited through its expression, which is what this converter exposes; "Out"
            // tells it to treat the binding as a place to write to rather than a value to read.
            editor.SetBinding(ExpressionTextBox.ExpressionProperty, new Binding("ModelItem." + field.PropertyName)
            {
                Converter = new ArgumentToExpressionConverter(),
                ConverterParameter = isOutput ? "Out" : "In",
                Mode = BindingMode.TwoWay
            });

            editor.SetBinding(ExpressionTextBox.OwnerActivityProperty, new Binding("ModelItem"));

            return editor;
        }

        /// <summary>
        /// Finds a property by name, preferring the one declared furthest down the hierarchy.
        /// </summary>
        /// <remarks>
        /// <c>Activity&lt;TResult&gt;</c> shadows <c>ActivityWithResult.Result</c>, so every activity here
        /// has two properties called Result and asking for it by name alone throws.
        /// </remarks>
        private static PropertyInfo MostDerived(Type activityType, string propertyName)
        {
            PropertyInfo[] matches = activityType.GetProperties()
                .Where(property => property.Name == propertyName)
                .ToArray();

            if (matches.Length <= 1)
                return matches.FirstOrDefault();

            return matches.OrderByDescending(property => Depth(property.DeclaringType)).First();
        }

        private static int Depth(Type type)
        {
            int depth = 0;
            for (Type walk = type; walk != null; walk = walk.BaseType)
                depth++;
            return depth;
        }

        private static bool TryDescribeArgument(Type propertyType, out Type valueType, out bool isOutput)
        {
            valueType = null;
            isOutput = false;

            if (!propertyType.IsGenericType)
                return false;

            Type definition = propertyType.GetGenericTypeDefinition();

            if (definition == typeof(InArgument<>))
            {
                valueType = propertyType.GetGenericArguments()[0];
                return true;
            }

            if (definition == typeof(OutArgument<>))
            {
                valueType = propertyType.GetGenericArguments()[0];
                isOutput = true;
                return true;
            }

            return false;
        }
    }
}
