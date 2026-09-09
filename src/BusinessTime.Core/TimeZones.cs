using System;
using System.Linq;

namespace BusinessTime
{
    /// <summary>Helpers for turning the time zone identifiers used in workflows into <see cref="TimeZoneInfo"/>.</summary>
    public static class TimeZones
    {
        /// <summary>
        /// Resolves a time zone identifier. An empty value means the machine's own zone; <c>UTC</c> and
        /// <c>Local</c> are understood, as are Windows identifiers (<c>W. Europe Standard Time</c>) and,
        /// on .NET 6 and later, IANA identifiers (<c>Europe/Berlin</c>).
        /// </summary>
        /// <exception cref="BusinessTimeException">The identifier does not match a known time zone.</exception>
        public static TimeZoneInfo Resolve(string timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return TimeZoneInfo.Local;

            string id = timeZoneId.Trim();

            if (id.Equals("local", StringComparison.OrdinalIgnoreCase))
                return TimeZoneInfo.Local;
            if (id.Equals("utc", StringComparison.OrdinalIgnoreCase) || id.Equals("gmt", StringComparison.OrdinalIgnoreCase))
                return TimeZoneInfo.Utc;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
            {
                TimeZoneInfo match = TimeZoneInfo.GetSystemTimeZones()
                    .FirstOrDefault(zone =>
                        zone.DisplayName.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                        zone.StandardName.Equals(id, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return match;

                throw new BusinessTimeException(
                    $"'{timeZoneId}' is not a time zone known to this machine. Use an identifier such as 'UTC', " +
                    "'Europe/Berlin' or 'W. Europe Standard Time', or leave it empty to use the machine's own zone.", ex);
            }
        }
    }
}
