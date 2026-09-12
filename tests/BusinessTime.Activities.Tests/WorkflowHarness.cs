using System;
using System.Activities;
using System.Collections.Generic;

namespace BusinessTime.Activities.Tests
{
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
    }
}
