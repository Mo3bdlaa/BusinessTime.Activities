using System;
using System.Globalization;

namespace BusinessTime
{
    /// <summary>
    /// A continuous stretch of working time, returned when a calendar's working windows are enumerated.
    /// </summary>
    public readonly struct BusinessTimeInterval : IEquatable<BusinessTimeInterval>
    {
        /// <summary>Creates an interval.</summary>
        public BusinessTimeInterval(DateTime start, DateTime end)
        {
            if (end < start)
                throw new BusinessTimeException("A working interval cannot end before it starts.");

            Start = start;
            End = end;
        }

        /// <summary>First moment of the interval.</summary>
        public DateTime Start { get; }

        /// <summary>End of the interval; this instant itself is not working time.</summary>
        public DateTime End { get; }

        /// <summary>Length of the interval.</summary>
        public TimeSpan Duration => End - Start;

        /// <summary>True when <paramref name="moment"/> falls inside the half-open interval.</summary>
        public bool Contains(DateTime moment) => moment >= Start && moment < End;

        /// <inheritdoc />
        public override string ToString() =>
            Start.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " -> " +
            End.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public bool Equals(BusinessTimeInterval other) => Start == other.Start && End == other.End;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is BusinessTimeInterval other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        /// <summary>Equality operator.</summary>
        public static bool operator ==(BusinessTimeInterval left, BusinessTimeInterval right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(BusinessTimeInterval left, BusinessTimeInterval right) => !left.Equals(right);
    }
}
