using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using BusinessTime;
using Microsoft.Win32;

namespace BusinessTime.Activities.Wizard
{
    /// <summary>One row of the special days grid.</summary>
    public sealed class SpecialDayRow : INotifyPropertyChanged
    {
        private DateTime? _date = DateTime.Today;
        private DateTime? _through;
        private string _name = string.Empty;
        private string _hours = "off";
        private bool _annual;

        /// <summary>The date, chosen from a picker so it can never be mistyped.</summary>
        public DateTime? Date { get => _date; set => Set(ref _date, value); }

        /// <summary>The last date, when the entry covers a run of days.</summary>
        public DateTime? Through { get => _through; set => Set(ref _through, value); }

        /// <summary>What to call it, for example <c>Christmas Eve</c>.</summary>
        public string Name { get => _name; set => Set(ref _name, value); }

        /// <summary>The hours worked, or <c>off</c> when nobody works.</summary>
        public string Hours { get => _hours; set => Set(ref _hours, value); }

        /// <summary>True when the entry repeats every year; the year of the date is then ignored.</summary>
        public bool Annual { get => _annual; set => Set(ref _annual, value); }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string property = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    /// <summary>
    /// The editor behind the ribbon button: reads and writes the calendar JSON a process loads, so the
    /// working week and the holidays can be maintained without hand-editing a file.
    /// </summary>
    public sealed class CalendarEditorWindow : Window
    {
        /// <summary>Where a project's calendar is looked for when nothing else is known.</summary>
        public const string DefaultRelativePath = @"Data\BusinessCalendar.json";

        private static readonly string[] DayNames =
            { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        private static readonly DayOfWeek[] Days =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        /// <summary>The file the editor was last pointed at, so reopening it comes back to the same place.</summary>
        private static string _lastPath;

        private readonly IReadOnlyList<TimeZoneChoice> _zones = TimeZoneChoice.All();
        private readonly TextBox _path = new TextBox { IsReadOnly = true };
        private readonly TextBox _name = new TextBox();
        private readonly TextBox _hoursPerDay = new TextBox { Text = "0" };
        private readonly ComboBox _timeZone = new ComboBox { DisplayMemberPath = nameof(TimeZoneChoice.Display) };
        private readonly TextBox[] _shifts = new TextBox[7];
        private readonly ObservableCollection<SpecialDayRow> _specialDays = new ObservableCollection<SpecialDayRow>();
        private readonly TextBlock _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };

        /// <summary>True once the file at <see cref="_path"/> is one the user opened or saved here.</summary>
        private bool _pathIsOurs;

        /// <summary>Builds the editor.</summary>
        public CalendarEditorWindow()
        {
            Title = "Business Calendar";
            Width = 820;
            Height = 760;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Canadian English writes short dates as yyyy-MM-dd, which is the format this package stores,
            // so what the pickers show and what lands in the file read the same way.
            Language = XmlLanguage.GetLanguage("en-CA");

            foreach (TimeZoneChoice zone in _zones)
                _timeZone.Items.Add(zone);

            Content = BuildLayout();
            Start();
        }

        // -------------------------------------------------------------------------------------------
        // Layout
        // -------------------------------------------------------------------------------------------

        private UIElement BuildLayout()
        {
            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

            Add(grid, Label("Calendar file"), 0, 0);
            Add(grid, _path, 0, 1);
            Add(grid, Button("Open…", (s, e) => OpenAnother()), 0, 2);

            _path.Margin = new Thickness(6, 0, 6, 0);
            _path.VerticalContentAlignment = VerticalAlignment.Center;
            _path.Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xF6, 0xF8));
            return grid;
        }

        private UIElement BuildDetails()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            for (int i = 0; i < 6; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = i % 2 == 0 ? GridLength.Auto : new GridLength(i == 3 ? 2 : 1, GridUnitType.Star)
                });

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

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var hint = Hint("\"09:00-17:00\", or \"09:00-12:00,13:00-17:00\" for a lunch break, " +
                            "\"22:00-06:00\" for a night shift, or \"off\".");
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
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var table = new DataGrid
            {
                ItemsSource = _specialDays,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };

            table.Columns.Add(DateColumn("Date", nameof(SpecialDayRow.Date), 140));
            table.Columns.Add(DateColumn("Through", nameof(SpecialDayRow.Through), 140));
            table.Columns.Add(TextColumn("Name", nameof(SpecialDayRow.Name), 230));
            table.Columns.Add(TextColumn("Hours", nameof(SpecialDayRow.Hours), 130));
            table.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Every year",
                Binding = new System.Windows.Data.Binding(nameof(SpecialDayRow.Annual))
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                }
            });

            Add(grid, table, 0);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            buttons.Children.Add(Button("Add day", (s, e) => _specialDays.Add(new SpecialDayRow())));
            buttons.Children.Add(Button("Remove selected", (s, e) =>
            {
                foreach (SpecialDayRow row in table.SelectedItems.Cast<SpecialDayRow>().ToList())
                    _specialDays.Remove(row);
            }));
            Add(grid, buttons, 1);

            Add(grid, Hint("Leave Through empty for a single day; fill it in for a shutdown. " +
                           "Hours \"off\" closes the day, \"09:00-13:00\" makes it a half day. " +
                           "Tick Every year and only the day and month are used."), 2);

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
            buttons.Children.Add(Button("Save", (s, e) => Save(askFirst: true)));
            buttons.Children.Add(Button("Save as…", (s, e) => SaveAs()));
            buttons.Children.Add(Button("Close", (s, e) => Close()));
            panel.Children.Add(buttons);

            return panel;
        }

        // -------------------------------------------------------------------------------------------
        // Behaviour
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Opens on the project's own calendar when there is one, so the common case needs no browsing, and
        /// never touches a file it has not been told to.
        /// </summary>
        private void Start()
        {
            Show(BusinessCalendar.Default);
            _name.Text = "Business calendar";

            string remembered = _lastPath;
            if (!string.IsNullOrWhiteSpace(remembered) && File.Exists(remembered))
            {
                Open(remembered);
                return;
            }

            string projectDefault = DefaultPath();
            _path.Text = projectDefault;

            if (File.Exists(projectDefault))
                Open(projectDefault);
            else
                Report($"No calendar at {projectDefault} yet. Fill this in and Save to create it, " +
                       "or Open… an existing file elsewhere.", false);
        }

        /// <summary>The project's conventional calendar path. Studio runs with the project as its folder.</summary>
        private static string DefaultPath()
        {
            string root;
            try
            {
                root = Environment.CurrentDirectory;
            }
            catch (Exception)
            {
                root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            return Path.Combine(root, DefaultRelativePath);
        }

        private void OpenAnother()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open a business calendar",
                Filter = "Calendar files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                InitialDirectory = SafeDirectory(_path.Text)
            };

            if (dialog.ShowDialog() == true)
                Open(dialog.FileName);
        }

        private void Open(string path)
        {
            try
            {
                BusinessCalendar calendar = BusinessCalendarSerializer.LoadFile(path);
                Show(calendar);

                _path.Text = path;
                _pathIsOurs = true;
                _lastPath = path;

                Report($"Opened {path}.", false);
            }
            catch (Exception exception)
            {
                Report(exception.Message, true);
            }
        }

        private void SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save the business calendar",
                Filter = "Calendar files (*.json)|*.json|All files (*.*)|*.*",
                FileName = Path.GetFileName(_path.Text),
                InitialDirectory = SafeDirectory(_path.Text),
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
                return;

            _path.Text = dialog.FileName;
            _pathIsOurs = true;
            Save(askFirst: false);
        }

        /// <summary>
        /// Writes the file. A file this editor has not opened is never replaced without being asked, so a
        /// calendar someone else maintains cannot be lost by pressing Save out of habit.
        /// </summary>
        private void Save(bool askFirst)
        {
            try
            {
                string path = _path.Text;

                if (string.IsNullOrWhiteSpace(path))
                {
                    Report("Choose where to save the file first, with Save as….", true);
                    return;
                }

                if (askFirst && !_pathIsOurs && File.Exists(path))
                {
                    MessageBoxResult answer = MessageBox.Show(
                        $"{path} already exists and was not opened here.\n\nReplace it?",
                        "Business Calendar",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (answer != MessageBoxResult.Yes)
                    {
                        Report("Nothing was written.", false);
                        return;
                    }
                }

                BusinessCalendar calendar = Read();
                BusinessCalendarSerializer.SaveFile(calendar, path);

                _pathIsOurs = true;
                _lastPath = path;

                Report($"Saved {path} — {calendar.Schedule.WorkingDaysPerWeek} working days a week, " +
                       $"{calendar.Schedule.WeeklyWorkingTime.TotalHours:0.##} hours, " +
                       $"{calendar.SpecialDays.Count} special day(s).", false);
            }
            catch (Exception exception)
            {
                Report(exception.Message, true);
            }
        }

        private static string SafeDirectory(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                return Directory.Exists(directory) ? directory : Environment.CurrentDirectory;
            }
            catch (Exception)
            {
                return Environment.CurrentDirectory;
            }
        }

        /// <summary>Fills the form in from a calendar.</summary>
        private void Show(BusinessCalendar calendar)
        {
            _name.Text = calendar.Name ?? string.Empty;
            _timeZone.SelectedItem = calendar.FollowsMachineTimeZone
                ? _zones[0]
                : TimeZoneChoice.For(_zones, calendar.TimeZone.Id);
            _hoursPerDay.Text = calendar.HoursPerBusinessDay.TotalHours.ToString("0.##", CultureInfo.InvariantCulture);

            for (int i = 0; i < 7; i++)
                _shifts[i].Text = calendar.Schedule[Days[i]].ToString();

            _specialDays.Clear();
            foreach (SpecialDay day in calendar.SpecialDays)
            {
                _specialDays.Add(new SpecialDayRow
                {
                    Date = day.Date,
                    Through = day.Through == day.Date ? (DateTime?)null : day.Through,
                    Name = day.Name ?? string.Empty,
                    Hours = day.IsNonWorking ? "off" : string.Join(",", day.Shifts.Select(shift => shift.ToString())),
                    Annual = day.IsAnnual
                });
            }
        }

        /// <summary>Builds a calendar from the form, naming the first field that cannot be read.</summary>
        private BusinessCalendar Read()
        {
            var selected = _timeZone.SelectedItem as TimeZoneChoice ?? _zones[0];

            var builder = new BusinessCalendarBuilder().WithName(_name.Text);

            // The system default entry records that the hours follow whichever robot runs the process,
            // rather than pinning them to the zone this machine happens to be in.
            if (selected.IsSystemDefault)
                builder.WithMachineTimeZone();
            else
                builder.WithTimeZone(selected.Id);

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
                if (!row.Date.HasValue)
                    continue;

                try
                {
                    // The engine reads every date and shift in this package, so the editor has no rule of
                    // its own that could disagree with what a workflow will later do.
                    builder.AddSpecialDay(SpecialDay.FromValues(
                        row.Date.Value, row.Through, row.Name, row.Hours, row.Annual));
                }
                catch (BusinessTimeException exception)
                {
                    string label = string.IsNullOrWhiteSpace(row.Name)
                        ? row.Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : row.Name;

                    throw new BusinessTimeException($"{label}: {exception.Message}");
                }
            }

            return builder.Build();
        }

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

        private static TextBlock Hint(string text) => new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x6B, 0x7A))
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
                Binding = new System.Windows.Data.Binding(property)
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                }
            };

        /// <summary>
        /// A column holding a date picker, so dates are chosen rather than typed and no spelling of a date
        /// can reach the file.
        /// </summary>
        private static DataGridColumn DateColumn(string header, string property, double width)
        {
            try
            {
                const string format = "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                                      "<DatePicker SelectedDate='{{Binding {0}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}' " +
                                      "BorderThickness='0' Background='Transparent' VerticalContentAlignment='Center' />" +
                                      "</DataTemplate>";

                var template = (DataTemplate)XamlReader.Parse(string.Format(CultureInfo.InvariantCulture, format, property));

                return new DataGridTemplateColumn
                {
                    Header = header,
                    Width = width,
                    CellTemplate = template,
                    CellEditingTemplate = template
                };
            }
            catch (Exception)
            {
                // A grid that types its dates is far better than a grid that will not draw.
                return TextColumn(header, property, width);
            }
        }

        private static void Add(Grid grid, UIElement element, int row, int column = 0)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }
    }
}
