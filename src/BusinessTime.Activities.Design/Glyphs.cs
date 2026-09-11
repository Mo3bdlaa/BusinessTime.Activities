using System.Windows;
using System.Windows.Media;

namespace BusinessTime.Activities.Design
{
    /// <summary>
    /// The icons shown on the activity cards, drawn as geometry so that the package carries no image files
    /// and stays crisp at every zoom level in the designer.
    /// </summary>
    internal static class Glyphs
    {
        private static readonly Color Accent = Color.FromRgb(0x1F, 0x6F, 0xB2);
        private static readonly Color Muted = Color.FromRgb(0x5A, 0x6B, 0x7A);

        /// <summary>A clock face, used by the activities that calculate with time.</summary>
        internal static DrawingBrush Clock => Build(includeCalendar: false);

        /// <summary>A clock over a calendar page, used by the calendar and scope activities.</summary>
        internal static DrawingBrush Calendar => Build(includeCalendar: true);

        private static DrawingBrush Build(bool includeCalendar)
        {
            var drawing = new DrawingGroup();

            if (includeCalendar)
            {
                var page = new Pen(new SolidColorBrush(Muted), 1.2);
                page.Freeze();

                drawing.Children.Add(new GeometryDrawing(
                    Brushes.Transparent, page, new RectangleGeometry(new Rect(1, 2.5, 11, 10), 1, 1)));

                // The two rings at the top of a wall calendar.
                drawing.Children.Add(new GeometryDrawing(
                    Brushes.Transparent, page, new LineGeometry(new Point(4, 1), new Point(4, 4))));
                drawing.Children.Add(new GeometryDrawing(
                    Brushes.Transparent, page, new LineGeometry(new Point(8.5, 1), new Point(8.5, 4))));
            }

            var face = new Pen(new SolidColorBrush(Accent), 1.4);
            face.Freeze();

            var centre = includeCalendar ? new Point(10, 10) : new Point(8, 8);
            double radius = includeCalendar ? 5 : 6.5;

            drawing.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Colors.White), face, new EllipseGeometry(centre, radius, radius)));

            // Hands reading roughly ten past two, so both are visible at small sizes.
            var hands = new GeometryGroup();
            hands.Children.Add(new LineGeometry(centre, new Point(centre.X, centre.Y - (radius * 0.55))));
            hands.Children.Add(new LineGeometry(centre, new Point(centre.X + (radius * 0.6), centre.Y)));
            drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, face, hands));

            drawing.Freeze();

            var brush = new DrawingBrush(drawing) { Stretch = Stretch.Uniform };
            brush.Freeze();
            return brush;
        }
    }
}
