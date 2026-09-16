using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace BusinessTime.Activities.Design
{
    /// <summary>
    /// The icons on the activity cards, drawn as geometry so the package carries no image files and stays
    /// crisp at any zoom.
    /// </summary>
    /// <remarks>
    /// Each icon is a clock or a calendar page with a small mark in the corner saying what the activity does
    /// to it, so the pack reads as one family while each activity is still told apart at a glance.
    /// </remarks>
    internal static class Glyphs
    {
        private static readonly Color Accent = Color.FromRgb(0x1F, 0x6F, 0xB2);
        private static readonly Color Muted = Color.FromRgb(0x5A, 0x6B, 0x7A);

        private static readonly Dictionary<string, DrawingBrush> Cache = new Dictionary<string, DrawingBrush>();

        /// <summary>The icon for an activity, by type name. Unknown names get the plain clock.</summary>
        internal static DrawingBrush For(string activityName)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(activityName, out DrawingBrush cached))
                    return cached;

                DrawingBrush brush = Build(activityName);
                Cache[activityName] = brush;
                return brush;
            }
        }

        private static DrawingBrush Build(string activityName)
        {
            var drawing = new DrawingGroup();

            switch (activityName)
            {
                case "CreateBusinessCalendar":
                    Page(drawing); Badge(drawing, Plus()); break;
                case "LoadBusinessCalendar":
                    Page(drawing); Badge(drawing, Arrow(down: true)); break;
                case "SaveBusinessCalendar":
                    Page(drawing); Badge(drawing, Arrow(down: false)); break;

                case "AddBusinessTime":
                    Clock(drawing); Badge(drawing, Plus()); break;
                case "SubtractBusinessTime":
                    Clock(drawing); Badge(drawing, Minus()); break;
                case "GetBusinessTimeBetween":
                    Clock(drawing); Badge(drawing, Span()); break;
                case "CountBusinessDays":
                    Page(drawing); Badge(drawing, Hash()); break;
                case "IsBusinessTime":
                    Clock(drawing); Badge(drawing, Tick()); break;
                case "GetBusinessDayInfo":
                    Page(drawing); Badge(drawing, Info()); break;
                case "SnapToBusinessTime":
                    Clock(drawing); Badge(drawing, IntoBar()); break;
                case "GetNextBusinessDay":
                    Page(drawing); Badge(drawing, Next()); break;
                case "GetWorkingIntervals":
                    Clock(drawing); Badge(drawing, Bars()); break;

                default:
                    Clock(drawing); break;
            }

            drawing.Freeze();
            var brush = new DrawingBrush(drawing) { Stretch = Stretch.Uniform };
            brush.Freeze();
            return brush;
        }

        // -------------------------------------------------------------------------------------------
        // The two bases
        // -------------------------------------------------------------------------------------------

        private static void Clock(DrawingGroup drawing)
        {
            Pen pen = Stroke(Accent, 1.4);
            var centre = new Point(9, 9);
            const double radius = 6.5;

            drawing.Children.Add(new GeometryDrawing(Brushes.White, pen, new EllipseGeometry(centre, radius, radius)));

            var hands = new GeometryGroup();
            hands.Children.Add(new LineGeometry(centre, new Point(centre.X, centre.Y - 3.6)));
            hands.Children.Add(new LineGeometry(centre, new Point(centre.X + 3.9, centre.Y)));
            drawing.Children.Add(new GeometryDrawing(null, pen, hands));
        }

        private static void Page(DrawingGroup drawing)
        {
            Pen pen = Stroke(Accent, 1.3);
            Pen rings = Stroke(Muted, 1.3);

            drawing.Children.Add(new GeometryDrawing(Brushes.White, pen, new RectangleGeometry(new Rect(2, 3.5, 14, 13), 1, 1)));
            drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Accent), pen, new RectangleGeometry(new Rect(2, 3.5, 14, 3.5))));

            var hangers = new GeometryGroup();
            hangers.Children.Add(new LineGeometry(new Point(5.5, 1.5), new Point(5.5, 4.5)));
            hangers.Children.Add(new LineGeometry(new Point(12.5, 1.5), new Point(12.5, 4.5)));
            drawing.Children.Add(new GeometryDrawing(null, rings, hangers));
        }

        // -------------------------------------------------------------------------------------------
        // The corner mark
        // -------------------------------------------------------------------------------------------

        private static void Badge(DrawingGroup drawing, Geometry mark)
        {
            // A white disc first, so the mark stays readable wherever it lands on the base.
            drawing.Children.Add(new GeometryDrawing(
                Brushes.White, Stroke(Accent, 1.1), new EllipseGeometry(new Point(14.5, 14.5), 5, 5)));

            drawing.Children.Add(new GeometryDrawing(null, Stroke(Accent, 1.4), mark));
        }

        private static Geometry Plus()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(14.5, 12.2), new Point(14.5, 16.8)));
            group.Children.Add(new LineGeometry(new Point(12.2, 14.5), new Point(16.8, 14.5)));
            return group;
        }

        private static Geometry Minus() =>
            new LineGeometry(new Point(12.2, 14.5), new Point(16.8, 14.5));

        private static Geometry Span()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(12.1, 14.5), new Point(16.9, 14.5)));
            group.Children.Add(new LineGeometry(new Point(12.1, 12.6), new Point(12.1, 16.4)));
            group.Children.Add(new LineGeometry(new Point(16.9, 12.6), new Point(16.9, 16.4)));
            return group;
        }

        private static Geometry Hash()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(13.3, 12.4), new Point(13.3, 16.6)));
            group.Children.Add(new LineGeometry(new Point(15.7, 12.4), new Point(15.7, 16.6)));
            group.Children.Add(new LineGeometry(new Point(12.2, 13.5), new Point(16.8, 13.5)));
            group.Children.Add(new LineGeometry(new Point(12.2, 15.5), new Point(16.8, 15.5)));
            return group;
        }

        private static Geometry Tick()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(12.4, 14.6), new Point(13.9, 16.4)));
            group.Children.Add(new LineGeometry(new Point(13.9, 16.4), new Point(16.7, 12.7)));
            return group;
        }

        private static Geometry Info()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(14.5, 13.9), new Point(14.5, 16.6)));
            group.Children.Add(new LineGeometry(new Point(14.5, 12.3), new Point(14.5, 12.5)));
            return group;
        }

        private static Geometry IntoBar()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(11.9, 14.5), new Point(15.6, 14.5)));
            group.Children.Add(new LineGeometry(new Point(14.1, 13.1), new Point(15.6, 14.5)));
            group.Children.Add(new LineGeometry(new Point(14.1, 15.9), new Point(15.6, 14.5)));
            group.Children.Add(new LineGeometry(new Point(16.9, 12.4), new Point(16.9, 16.6)));
            return group;
        }

        private static Geometry Next()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(12.1, 14.5), new Point(16.8, 14.5)));
            group.Children.Add(new LineGeometry(new Point(15.2, 12.9), new Point(16.8, 14.5)));
            group.Children.Add(new LineGeometry(new Point(15.2, 16.1), new Point(16.8, 14.5)));
            return group;
        }

        private static Geometry Bars()
        {
            var group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(12.2, 12.8), new Point(16.8, 12.8)));
            group.Children.Add(new LineGeometry(new Point(12.2, 14.5), new Point(15.4, 14.5)));
            group.Children.Add(new LineGeometry(new Point(12.2, 16.2), new Point(16.8, 16.2)));
            return group;
        }

        private static Geometry Arrow(bool down)
        {
            var group = new GeometryGroup();
            double from = down ? 12.2 : 16.8;
            double to = down ? 16.8 : 12.2;
            double back = down ? 15.2 : 13.8;

            group.Children.Add(new LineGeometry(new Point(14.5, from), new Point(14.5, to)));
            group.Children.Add(new LineGeometry(new Point(12.9, back), new Point(14.5, to)));
            group.Children.Add(new LineGeometry(new Point(16.1, back), new Point(14.5, to)));
            return group;
        }

        private static Pen Stroke(Color colour, double thickness)
        {
            var pen = new Pen(new SolidColorBrush(colour), thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();
            return pen;
        }
    }
}
