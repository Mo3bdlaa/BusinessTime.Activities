using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;

namespace BusinessTime.Activities.Tests
{
    /// <summary>Records the value handed to it, so a test can read a variable set inside a scope.</summary>
    internal sealed class Capture<T> : CodeActivity
    {
        public InArgument<T> Value { get; set; }

        public List<T> Sink { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            Sink.Add(Value.Get(context));
        }
    }

    /// <summary>Runs activities through the real workflow runtime, the way Studio does.</summary>
    internal static class WorkflowHarness
    {
        /// <summary>
        /// Binds a value to an argument. A plain <c>new InArgument&lt;T&gt;(value)</c> compiles to a
        /// <c>Literal</c>, which the runtime only accepts for value types and strings, so reference types
        /// have to arrive the way they do in a real workflow: as the result of an expression.
        /// </summary>
        internal static InArgument<T> Arg<T>(T value) => new InArgument<T>(context => value);

        /// <summary>Runs a single activity and returns its output arguments.</summary>
        internal static IDictionary<string, object> Run(Activity activity) =>
            WorkflowInvoker.Invoke(activity);

        /// <summary>Runs a single activity and returns its <c>Result</c>.</summary>
        internal static T RunFor<T>(Activity<T> activity) => WorkflowInvoker.Invoke(activity);

        /// <summary>
        /// Runs an activity inside a calendar scope and returns the value it produced, which is how a
        /// workflow author gets at a result computed inside a scope body.
        /// </summary>
        internal static T RunInScope<T>(BusinessCalendarScope scope, Activity<T> inner)
        {
            var captured = new Variable<T>("captured");
            var sink = new List<T>();

            inner.Result = new OutArgument<T>(captured);
            scope.Body.Handler = inner;

            var root = new Sequence
            {
                Variables = { captured },
                Activities =
                {
                    scope,
                    new Capture<T> { Value = new InArgument<T>(captured), Sink = sink }
                }
            };

            WorkflowInvoker.Invoke(root);
            return sink[0];
        }
    }
}
