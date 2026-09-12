using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessTime;
using Microsoft.Win32;

namespace BusinessTime.Activities.Wizard
{
    /// <summary>One row of the special days grid, kept as text so a half-typed entry never throws.</summary>
    public sealed class SpecialDayRow
    {
        /// <summary>The date, as <c>2026-12-25</c>, or <c>12-25</c> for one that repeats.</summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>The last date, when the entry covers a run of days. Empty for a single date.</summary>
        public string Through { get; set; } = string.Empty;

        /// <summary>What to call it, for example <c>Christmas Eve</c>.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The hours worked, or <c>off</c> when nobody works.</summary>
        public string Hours { get; set; } = "off";

        /// <summary>True when the entry repeats every year.</summary>
        public bool Annual { get; set; }
    }

    /// <summary>
    /// The editor behind the ribbon button: reads and writes the calendar JSON a process loads, so the
    /// working week and the holidays can be maintained without hand-editing a file.
    /// </summary>
    public sealed class CalendarEditorWindow : Window
    {
        private static readonly string[] DayNames =
            { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        private static readonly DayOfWeek[] Days =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        private readonly TextBox _path = new TextBox();
        private readonly TextBox _name = new TextBox { Text = "Business calendar" };
        private readonly TextBox _hoursPerDay = new TextBox { Text = "0" };
        private readonly ComboBox _timeZone = new ComboBox();
        private readonly TextBox[] _shifts = new TextBox[7];
        private readonly ObservableCollection<SpecialDayRow> _specialDays = new ObservableCollection<SpecialDayRow>();
        private readonly TextBlock _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };

        /// <summary>Builds the editor.</summary>
        public CalendarEditorWindow()
        {
            Title = "Business Calendar";
            Width = 760;
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            foreach (TimeZoneInfo zone in TimeZoneInfo.GetSystemTimeZones())
                _timeZone.Items.Add(zone.Id);
            _timeZone.SelectedItem = TimeZoneInfo.Local.Id;

            Content = BuildLayout();
            LoadDefaults();
        }

        private UIElement BuildLayout()
        {
            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // file
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // details
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // week
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // special days
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // status + buttons

            Add(root, BuildFileRow(), 0);
            Add(root, BuildDetails(), 1);
            Add(root, BuildWeek(), 2);
            Add(root, BuildSpecialDays(), 3);
            Add(root, BuildFooter(), 4);

            return root;
        }

        private UIElement BuildFileRow()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Add(grid, Label("Calendar file"), 0, 0);
            Add(grid, _path, 0, 1);
            Add(grid, Button("Browse…", Browse), 0, 2);
            Add(grid, Button("Open", (s, e) => Load()), 0, 3);

            _path.Margin = new Thickness(6, 0, 6, 0);
            _path.VerticalContentAlignment = VerticalAlignment.Center;
            return grid;
        }

        private UIElement BuildDetails()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            for (int i = 0; i < 6; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = i % 2 == 0 ? GridLength.Auto : new GridLength(1, GridUnitType.Star) });

            Add(grid, Label("Name"), 0, 0);
            Add(grid, _name, 0, 1);
            Add(grid, Label("Time zone"), 0, 2);
            Add(grid, _timeZone, 0, 3);
            Add(grid, Label("Hours per business day"), 0, 4);
            Add(grid, _hoursPerDay, 0, 5);

            _name.Margin = _timeZone.Margin = _hoursPerDay.Margin = new Thickness(6, 0, 12, 0);
            return grid;
        }

        private UIElement BuildWeek()
        {
            var box = new GroupBox { Header = "Working week", Margin = new Thickness(0, 0, 0, 10) };
            var grid = new Grid { Margin = new Thickness(8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < 7; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _shifts[i] = new TextBox { Margin = new Thickness(0, 2, 0, 2) };
                Add(grid, Label(DayNames[i]), i, 0);
                Add(grid, _shifts[i], i, 1);
            }

            var hint = new TextBlock
            {
                Text = "One day per line: \"09:00-17:00\", \"09:00-12:00,13:00-17:00\" for a lunch break, " +
                       "\"22:00-06:00\" for a night shift, or \"off\".",
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x6B, 0x7A))
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(hint, 7);
            Grid.SetColumnSpan(hint, 2);
            grid.Children.Add(hint);

            box.Content = grid;
            return box;
        }

        private UIElement BuildSpecialDays()
        {
            var box = new GroupBox { Header = "Holidays, half days and shutdowns" };
            var grid = new Grid { Margin = new Thickness(8) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var table = new DataGrid
            {
                ItemsSource = _specialDays,
                AutoGenerateColumns = false,
                CanUserAddRows = true,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };

            table.Columns.Add(TextColumn("Date", nameof(SpecialDayRow.Date), 110));
            table.Columns.Add(TextColumn("Through", nameof(SpecialDayRow.Through), 110));
            table.Columns.Add(TextColumn("Name", nameof(SpecialDayRow.Name), 220));
            table.Columns.Add(TextColumn("Hours", nameof(SpecialDayRow.Hours), 130));
            table.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Every year",
                Binding = new System.Windows.Data.Binding(nameof(SpecialDayRow.Annual))
            });

            Add(grid, table, 0);

            var hint = new TextBlock
            {
                Text = "Date 2026-12-25, or 12-25 with \"Every year\" ticked. Through fills in a shutdown. " +
                       "Hours \"off\" closes the day; \"09:00-13:00\" makes it a half day.",
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x6B, 0x7A))
            };
            Add(grid, hint, 1);

            box.Content = grid;
            return box;
        }

        private UIElement BuildFooter()
        {
            var panel = new StackPanel();
            panel.Children.Add(_status);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };
            buttons.Children.Add(Button("Save", (s, e) => Save()));
            buttons.Children.Add(Button("Close", (s, e) => Close()));
            panel.Children.Add(buttons);

            return panel;
        }

        // -------------------------------------------------------------------------------------------
        // Behaviour
        // -------------------------------------------------------------------------------------------

        private void LoadDefaults()
        {
            Show(BusinessCalendar.Default);
            _name.Text = "Business calendar";
            Report("Pick a file to open, or fill this in and save a new one.", false);
        }

        private void Browse(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Business calendar file",
                Filter = "Calendar files (*.json)|*.json|All files (*.*)|*.*",
                FileName = string.IsNullOrWhiteSpace(_path.Text) ? "calendar.json" : _path.Text,
                OverwritePrompt = false,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
                _path.Text = dialog.FileName;
        }

        private void Load()
        {
            try
            {
                BusinessCalendar calendar = BusinessCalendarSerializer.LoadFile(_path.Text);
                Show(calendar);
                Report($"Opened {_path.Text}.", false);
            }
            catch (Exception exception)
            {
                Report(exception.Message, true);
            }
        }

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_path.Text))
                {
                    Report("Choose where to save the file first.", true);
                    return;
                }

                BusinessCalendar calendar = Read();
                BusinessCalendarSerializer.SaveFile(calendar, _path.Text);

                Report($"Saved {_path.Text} — {calendar.Schedule.WorkingDaysPerWeek} working days a week, " +
                       $"{calendar.Schedule.WeeklyWorkingTime.TotalHours:0.##} hours, " +
                       $"{calendar.SpecialDays.Count} special day(s).", false);
            }
            catch (Exception exception)
            {
                Report(exception.Message, true);
            }
        }

        /// <summary>Fills the form in from a calendar.</summary>
        private void Show(BusinessCalendar calendar)
        {
            _name.Text = calendar.Name ?? string.Empty;
            _timeZone.SelectedItem = calendar.TimeZone.Id;
            _hoursPerDay.Text = calendar.HoursPerBusinessDay.TotalHours.ToString("0.##", CultureInfo.InvariantCulture);

            for (int i = 0; i < 7; i++)
                _shifts[i].Text = calendar.Schedule[Days[i]].ToString();

            _specialDays.Clear();
            foreach (SpecialDay day in calendar.SpecialDays)
            {
                _specialDays.Add(new SpecialDayRow
                {
                    Date = day.Date.ToString(day.IsAnnual ? "MM-dd" : "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Through = day.Through == day.Date
                        ? string.Empty
                        : day.Through.ToString(day.IsAnnual ? "MM-dd" : "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Name = day.Name ?? string.Empty,
                    Hours = day.IsNonWorking ? "off" : string.Join(",", day.Shifts.Select(shift => shift.ToString())),
                    Annual = day.IsAnnual
                });
            }
        }

        /// <summary>Builds a calendar from the form, throwing a readable message at the first bad field.</summary>
        private BusinessCalendar Read()
        {
            var builder = new BusinessCalendarBuilder()
                .WithName(_name.Text)
                .WithTimeZone((string)_timeZone.SelectedItem);

            var week = new List<KeyValuePair<DayOfWeek, DaySchedule>>();
            for (int i = 0; i < 7; i++)
            {
                try
                {
                    week.Add(new KeyValuePair<DayOfWeek, DaySchedule>(Days[i], DaySchedule.Parse(_shifts[i].Text)));
                }
                catch (BusinessTimeException exception)
                {
                    throw new BusinessTimeException($"{DayNames[i]}: {exception.Message}");
                }
            }

            builder.WithSchedule(new WeeklySchedule(week));

            if (double.TryParse(_hoursPerDay.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours) && hours > 0)
                builder.WithHoursPerBusinessDay(hours);

            foreach (SpecialDayRow row in _specialDays)
            {
                if (string.IsNullOrWhiteSpace(row.Date))
                    continue;

                builder.AddSpecialDay(ToSpecialDay(row));
            }

            return builder.Build();
        }

        /// <summary>
        /// Hands the row's text to the engine, which is where every date and shift in this package is read,
        /// so the editor never has a parsing rule of its own to get wrong.
        /// </summary>
        private static SpecialDay ToSpecialDay(SpecialDayRow row) =>
            SpecialDay.FromText(row.Date, row.Through, row.Name, row.Hours, row.Annual);

        private void Report(string message, bool isError)
        {
            _status.Text = message;
            _status.Foreground = new SolidColorBrush(isError
                ? Color.FromRgb(0xB3, 0x26, 0x1A)
                : Color.FromRgb(0x2E, 0x5A, 0x2E));
        }

        // -------------------------------------------------------------------------------------------
        // Small helpers, so the layout above stays readable
        // -------------------------------------------------------------------------------------------

        private static TextBlock Label(string text) => new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 4, 4)
        };

        private static Button Button(string text, RoutedEventHandler onClick)
        {
            var button = new Button { Content = text, Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(4, 0, 0, 0) };
            button.Click += onClick;
            return button;
        }

        private static DataGridTextColumn TextColumn(string header, string property, double width) =>
            new DataGridTextColumn
            {
                Header = header,
                Width = width,
                Binding = new System.Windows.Data.Binding(property) { Mode = System.Windows.Data.BindingMode.TwoWay }
            };

        private static void Add(Grid grid, UIElement element, int row, int column = 0)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }
    }
}
