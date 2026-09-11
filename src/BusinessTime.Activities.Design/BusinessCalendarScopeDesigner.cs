using System.Activities.Presentation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BusinessTime.Activities.Design
{
    /// <summary>
    /// The designer for <c>Business Calendar Scope</c>.
    /// </summary>
    /// <remarks>
    /// Without this, the scope has nowhere to drop activities: the default designer draws a plain card and
    /// leaves the <c>ActivityAction</c> body unreachable, which makes the scope unusable on the canvas.
    /// </remarks>
    public sealed class BusinessCalendarScopeDesigner : ActivityDesigner
    {
        /// <summary>Builds the designer's visual tree.</summary>
        public BusinessCalendarScopeDesigner()
        {
            Icon = Glyphs.Calendar;

            var body = new WorkflowItemPresenter
            {
                HintText = "Drop activities here",
                MinHeight = 64,
                Margin = new Thickness(6)
            };

            // The scope's body is an ActivityAction, so the child sits on its Handler.
            body.SetBinding(WorkflowItemPresenter.ItemProperty, new Binding("ModelItem.Body.Handler")
            {
                Mode = BindingMode.TwoWay
            });

            Content = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xC5, 0xCF, 0xD8)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFB, 0xFC)),
                Padding = new Thickness(2),
                Child = body
            };
        }
    }

    /// <summary>
    /// The designer shared by the calculating activities: the stock card, with an icon that marks it as a
    /// Business Time activity. Arguments stay in the properties panel, where Studio users expect them.
    /// </summary>
    public sealed class BusinessTimeActivityDesigner : ActivityDesigner
    {
        /// <summary>Builds the designer's visual tree.</summary>
        public BusinessTimeActivityDesigner()
        {
            Icon = Glyphs.Clock;
        }
    }

    /// <summary>The designer for the activities that build, load and save calendars.</summary>
    public sealed class BusinessCalendarActivityDesigner : ActivityDesigner
    {
        /// <summary>Builds the designer's visual tree.</summary>
        public BusinessCalendarActivityDesigner()
        {
            Icon = Glyphs.Calendar;
        }
    }
}
