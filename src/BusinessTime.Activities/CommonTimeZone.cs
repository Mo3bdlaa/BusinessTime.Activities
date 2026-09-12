using System;
using System.Collections.Generic;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>
    /// The time zones offered as a drop-down on the calendar activities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each is named by its standard offset from UTC and a city in it, so the list can be read either way
    /// and sorts west to east. The offsets are standard time: a zone that keeps summer time is an hour
    /// further ahead for part of the year, which the calculations account for on their own.
    /// </para>
    /// <para>
    /// The list covers the usual business centres rather than every zone in the world. Anything missing is
    /// still reachable through <see cref="Custom"/> and an identifier.
    /// </para>
    /// </remarks>
    public enum CommonTimeZone
    {
        /// <summary>
        /// Whatever zone the robot running the process is set to. A calendar saved with this follows each
        /// robot rather than fixing the hours to one place.
        /// </summary>
        MachineLocal = 0,

        /// <summary>Coordinated Universal Time.</summary>
        UTC = 1,

        /// <summary>Honolulu, standard offset UTC-10.</summary>
        UTC_minus_10_Honolulu = 10,
        /// <summary>Anchorage, standard offset UTC-09.</summary>
        UTC_minus_09_Anchorage = 11,
        /// <summary>LosAngeles, standard offset UTC-08.</summary>
        UTC_minus_08_LosAngeles = 12,
        /// <summary>Denver, standard offset UTC-07.</summary>
        UTC_minus_07_Denver = 13,
        /// <summary>Chicago, standard offset UTC-06.</summary>
        UTC_minus_06_Chicago = 14,
        /// <summary>MexicoCity, standard offset UTC-06.</summary>
        UTC_minus_06_MexicoCity = 15,
        /// <summary>NewYork, standard offset UTC-05.</summary>
        UTC_minus_05_NewYork = 16,
        /// <summary>Bogota, standard offset UTC-05.</summary>
        UTC_minus_05_Bogota = 17,
        /// <summary>Santiago, standard offset UTC-04.</summary>
        UTC_minus_04_Santiago = 18,
        /// <summary>SaoPaulo, standard offset UTC-03.</summary>
        UTC_minus_03_SaoPaulo = 19,
        /// <summary>BuenosAires, standard offset UTC-03.</summary>
        UTC_minus_03_BuenosAires = 20,
        /// <summary>London, standard offset UTC+00.</summary>
        UTC_00_London = 21,
        /// <summary>Lisbon, standard offset UTC+00.</summary>
        UTC_00_Lisbon = 22,
        /// <summary>Casablanca, standard offset UTC+01.</summary>
        UTC_plus_01_Casablanca = 23,
        /// <summary>Paris, standard offset UTC+01.</summary>
        UTC_plus_01_Paris = 24,
        /// <summary>Berlin, standard offset UTC+01.</summary>
        UTC_plus_01_Berlin = 25,
        /// <summary>Warsaw, standard offset UTC+01.</summary>
        UTC_plus_01_Warsaw = 26,
        /// <summary>Lagos, standard offset UTC+01.</summary>
        UTC_plus_01_Lagos = 27,
        /// <summary>Athens, standard offset UTC+02.</summary>
        UTC_plus_02_Athens = 28,
        /// <summary>Cairo, standard offset UTC+02.</summary>
        UTC_plus_02_Cairo = 29,
        /// <summary>Johannesburg, standard offset UTC+02.</summary>
        UTC_plus_02_Johannesburg = 30,
        /// <summary>Jerusalem, standard offset UTC+02.</summary>
        UTC_plus_02_Jerusalem = 31,
        /// <summary>Istanbul, standard offset UTC+03.</summary>
        UTC_plus_03_Istanbul = 32,
        /// <summary>Moscow, standard offset UTC+03.</summary>
        UTC_plus_03_Moscow = 33,
        /// <summary>Nairobi, standard offset UTC+03.</summary>
        UTC_plus_03_Nairobi = 34,
        /// <summary>Riyadh, standard offset UTC+03.</summary>
        UTC_plus_03_Riyadh = 35,
        /// <summary>Dubai, standard offset UTC+04.</summary>
        UTC_plus_04_Dubai = 36,
        /// <summary>Karachi, standard offset UTC+05.</summary>
        UTC_plus_05_Karachi = 37,
        /// <summary>Mumbai, standard offset UTC+05:30.</summary>
        UTC_plus_05_30_Mumbai = 38,
        /// <summary>Dhaka, standard offset UTC+06.</summary>
        UTC_plus_06_Dhaka = 39,
        /// <summary>Bangkok, standard offset UTC+07.</summary>
        UTC_plus_07_Bangkok = 40,
        /// <summary>Jakarta, standard offset UTC+07.</summary>
        UTC_plus_07_Jakarta = 41,
        /// <summary>Singapore, standard offset UTC+08.</summary>
        UTC_plus_08_Singapore = 42,
        /// <summary>Manila, standard offset UTC+08.</summary>
        UTC_plus_08_Manila = 43,
        /// <summary>HongKong, standard offset UTC+08.</summary>
        UTC_plus_08_HongKong = 44,
        /// <summary>Shanghai, standard offset UTC+08.</summary>
        UTC_plus_08_Shanghai = 45,
        /// <summary>Taipei, standard offset UTC+08.</summary>
        UTC_plus_08_Taipei = 46,
        /// <summary>Perth, standard offset UTC+08.</summary>
        UTC_plus_08_Perth = 47,
        /// <summary>Seoul, standard offset UTC+09.</summary>
        UTC_plus_09_Seoul = 48,
        /// <summary>Tokyo, standard offset UTC+09.</summary>
        UTC_plus_09_Tokyo = 49,
        /// <summary>Adelaide, standard offset UTC+09:30.</summary>
        UTC_plus_09_30_Adelaide = 50,
        /// <summary>Brisbane, standard offset UTC+10.</summary>
        UTC_plus_10_Brisbane = 51,
        /// <summary>Sydney, standard offset UTC+10.</summary>
        UTC_plus_10_Sydney = 52,
        /// <summary>Auckland, standard offset UTC+12.</summary>
        UTC_plus_12_Auckland = 53,

        /// <summary>
        /// Any other zone. Give the identifier on the activity's <c>Time zone id</c> property, for example
        /// <c>Europe/Oslo</c> or <c>Central Asia Standard Time</c>.
        /// </summary>
        Custom = 9999
    }

    /// <summary>Turns the drop-down choice into a real time zone.</summary>
    internal static class CommonTimeZones
    {
        /// <summary>
        /// Windows identifiers, which resolve on .NET Framework and, through the bundled zone data, on .NET 6
        /// and later on any operating system. That makes one table correct for every target this package has.
        /// </summary>
        private static readonly Dictionary<CommonTimeZone, string> WindowsIds = new Dictionary<CommonTimeZone, string>
        {
            { CommonTimeZone.UTC_minus_10_Honolulu, "Hawaiian Standard Time" },
            { CommonTimeZone.UTC_minus_09_Anchorage, "Alaskan Standard Time" },
            { CommonTimeZone.UTC_minus_08_LosAngeles, "Pacific Standard Time" },
            { CommonTimeZone.UTC_minus_07_Denver, "Mountain Standard Time" },
            { CommonTimeZone.UTC_minus_06_Chicago, "Central Standard Time" },
            { CommonTimeZone.UTC_minus_06_MexicoCity, "Central Standard Time (Mexico)" },
            { CommonTimeZone.UTC_minus_05_NewYork, "Eastern Standard Time" },
            { CommonTimeZone.UTC_minus_05_Bogota, "SA Pacific Standard Time" },
            { CommonTimeZone.UTC_minus_04_Santiago, "Pacific SA Standard Time" },
            { CommonTimeZone.UTC_minus_03_SaoPaulo, "E. South America Standard Time" },
            { CommonTimeZone.UTC_minus_03_BuenosAires, "Argentina Standard Time" },
            { CommonTimeZone.UTC_00_London, "GMT Standard Time" },
            { CommonTimeZone.UTC_00_Lisbon, "GMT Standard Time" },
            { CommonTimeZone.UTC_plus_01_Casablanca, "Morocco Standard Time" },
            { CommonTimeZone.UTC_plus_01_Paris, "Romance Standard Time" },
            { CommonTimeZone.UTC_plus_01_Berlin, "W. Europe Standard Time" },
            { CommonTimeZone.UTC_plus_01_Warsaw, "Central European Standard Time" },
            { CommonTimeZone.UTC_plus_01_Lagos, "W. Central Africa Standard Time" },
            { CommonTimeZone.UTC_plus_02_Athens, "GTB Standard Time" },
            { CommonTimeZone.UTC_plus_02_Cairo, "Egypt Standard Time" },
            { CommonTimeZone.UTC_plus_02_Johannesburg, "South Africa Standard Time" },
            { CommonTimeZone.UTC_plus_02_Jerusalem, "Israel Standard Time" },
            { CommonTimeZone.UTC_plus_03_Istanbul, "Turkey Standard Time" },
            { CommonTimeZone.UTC_plus_03_Moscow, "Russian Standard Time" },
            { CommonTimeZone.UTC_plus_03_Nairobi, "E. Africa Standard Time" },
            { CommonTimeZone.UTC_plus_03_Riyadh, "Arab Standard Time" },
            { CommonTimeZone.UTC_plus_04_Dubai, "Arabian Standard Time" },
            { CommonTimeZone.UTC_plus_05_Karachi, "Pakistan Standard Time" },
            { CommonTimeZone.UTC_plus_05_30_Mumbai, "India Standard Time" },
            { CommonTimeZone.UTC_plus_06_Dhaka, "Bangladesh Standard Time" },
            { CommonTimeZone.UTC_plus_07_Bangkok, "SE Asia Standard Time" },
            { CommonTimeZone.UTC_plus_07_Jakarta, "SE Asia Standard Time" },
            { CommonTimeZone.UTC_plus_08_Singapore, "Singapore Standard Time" },
            { CommonTimeZone.UTC_plus_08_Manila, "Singapore Standard Time" },
            { CommonTimeZone.UTC_plus_08_HongKong, "China Standard Time" },
            { CommonTimeZone.UTC_plus_08_Shanghai, "China Standard Time" },
            { CommonTimeZone.UTC_plus_08_Taipei, "Taipei Standard Time" },
            { CommonTimeZone.UTC_plus_08_Perth, "W. Australia Standard Time" },
            { CommonTimeZone.UTC_plus_09_Seoul, "Korea Standard Time" },
            { CommonTimeZone.UTC_plus_09_Tokyo, "Tokyo Standard Time" },
            { CommonTimeZone.UTC_plus_09_30_Adelaide, "Cen. Australia Standard Time" },
            { CommonTimeZone.UTC_plus_10_Brisbane, "E. Australia Standard Time" },
            { CommonTimeZone.UTC_plus_10_Sydney, "AUS Eastern Standard Time" },
            { CommonTimeZone.UTC_plus_12_Auckland, "New Zealand Standard Time" },
        };

        /// <summary>
        /// Resolves the chosen zone. <paramref name="customId"/> is used when <see cref="CommonTimeZone.Custom"/>
        /// is picked, and is also honoured on its own so an existing workflow that only sets the identifier
        /// keeps working.
        /// </summary>
        internal static TimeZoneInfo Resolve(CommonTimeZone choice, string customId)
        {
            if (choice == CommonTimeZone.Custom)
            {
                if (string.IsNullOrWhiteSpace(customId))
                    throw new BusinessTimeException(
                        "The time zone is set to Custom, so the 'Time zone id' property needs an identifier such as 'Europe/Oslo'.");

                return TimeZones.Resolve(customId);
            }

            // An identifier on its own still wins, so nothing that already works has to be rewritten.
            if (!string.IsNullOrWhiteSpace(customId))
                return TimeZones.Resolve(customId);

            switch (choice)
            {
                case CommonTimeZone.MachineLocal:
                    return TimeZoneInfo.Local;
                case CommonTimeZone.UTC:
                    return TimeZoneInfo.Utc;
                default:
                    return TimeZones.Resolve(WindowsIds[choice]);
            }
        }

        /// <summary>
        /// Applies the chosen zone to a calendar being built. <see cref="CommonTimeZone.MachineLocal"/>
        /// records that the hours follow the machine, rather than pinning them to the zone this happens to
        /// be running in.
        /// </summary>
        internal static void ApplyTo(BusinessCalendarBuilder builder, CommonTimeZone choice, string customId)
        {
            if (choice == CommonTimeZone.MachineLocal && string.IsNullOrWhiteSpace(customId))
                builder.WithMachineTimeZone();
            else
                builder.WithTimeZone(Resolve(choice, customId));
        }

        /// <summary>The identifier behind a drop-down choice, for logging and for the tests.</summary>
        internal static string IdentifierFor(CommonTimeZone choice)
        {
            switch (choice)
            {
                case CommonTimeZone.MachineLocal:
                    return TimeZoneInfo.Local.Id;
                case CommonTimeZone.UTC:
                    return "UTC";
                case CommonTimeZone.Custom:
                    return null;
                default:
                    return WindowsIds[choice];
            }
        }

        /// <summary>Every choice that maps to a fixed zone, for the tests that check the table.</summary>
        internal static IEnumerable<CommonTimeZone> Mapped => WindowsIds.Keys;
    }
}
