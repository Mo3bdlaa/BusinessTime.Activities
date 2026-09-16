// Stand-ins for the real assembly Studio supplies. Signatures must match it exactly; see ../README.md.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace System.Activities.Presentation
{
    /// <summary>Base of everything the designer draws on the canvas.</summary>
    public class WorkflowViewElement : ContentControl
    {
    }

    /// <summary>The card an activity is drawn as.</summary>
    public class ActivityDesigner : WorkflowViewElement
    {
        /// <summary>The glyph shown on the card.</summary>
        public DrawingBrush Icon { get; set; }

        /// <summary>Called when the designer learns which activity it belongs to.</summary>
        protected virtual void OnModelItemChanged(object newItem)
        {
        }
    }
}

namespace System.Activities.Presentation.View
{
    /// <summary>The editor an argument is typed into on a card.</summary>
    public class ExpressionTextBox : UserControl
    {
        /// <summary>Backs <see cref="Expression"/>.</summary>
        public static readonly DependencyProperty ExpressionProperty =
            DependencyProperty.Register(nameof(Expression), typeof(object), typeof(ExpressionTextBox));

        /// <summary>Backs <see cref="OwnerActivity"/>.</summary>
        public static readonly DependencyProperty OwnerActivityProperty =
            DependencyProperty.Register(nameof(OwnerActivity), typeof(object), typeof(ExpressionTextBox));

        /// <summary>The argument being edited.</summary>
        public object Expression { get; set; }

        /// <summary>The activity the argument belongs to.</summary>
        public object OwnerActivity { get; set; }

        /// <summary>The type the expression must produce.</summary>
        public Type ExpressionType { get; set; }

        /// <summary>True when the expression is somewhere to write to rather than a value to read.</summary>
        public bool UseLocationExpression { get; set; }

        /// <summary>Greyed-out text shown while the box is empty.</summary>
        public string HintText { get; set; }
    }
}

namespace System.Activities.Presentation.Converters
{
    /// <summary>Exposes an argument through the expression it holds.</summary>
    public class ArgumentToExpressionConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
