using System;

namespace BusinessTime
{
    /// <summary>
    /// Raised when a business calendar is misconfigured or a calculation cannot be completed
    /// (for example when a calendar defines no working time at all).
    /// </summary>
    public class BusinessTimeException : Exception
    {
        /// <summary>Creates a new <see cref="BusinessTimeException"/>.</summary>
        public BusinessTimeException(string message) : base(message)
        {
        }

        /// <summary>Creates a new <see cref="BusinessTimeException"/> wrapping an inner exception.</summary>
        public BusinessTimeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
