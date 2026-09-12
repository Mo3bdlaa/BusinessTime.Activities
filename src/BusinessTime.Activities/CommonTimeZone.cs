using System;
using System.Collections.Generic;
using BusinessTime;

namespace BusinessTime.Activities
{
    /// <summary>
    /// The time zones offered as a drop-down on the calendar activities, named by a city in each so they can
    /// be recognised at a glance.
    /// </summary>
    /// <remarks>
    /// The list covers the usual business centres rather than every zone in the world. Anything missing is
    /// still reachable: pick <see cref="Custom"/> and give the identifier on the activity's
    /// <c>Time zone id</c> property.
    /// </remarks>
    public enum CommonTimeZone
    {
        /// <summary>Whatever zone the robot itself is set to.</summary>
        MachineLocal = 0,

        /// <summary>Coordinated Universal Time.</summary>
        UTC,

        // Europe, west to east.
        /// <summary>United Kingdom and Ireland.</summary>
        London,
        /// <summary>Portugal.</summary>
        Lisbon,
        /// <summary>France, Spain and the western Mediterranean.</summary>
        Paris,
        /// <summary>Germany, the Netherlands, Italy and Switzerland.</summary>
        Berlin,
        /// <summary>Poland, Czechia, Hungary and the Balkans.</summary>
        Warsaw,
        /// <summary>Greece, Finland, Romania and Bulgaria.</summary>
        Athens,
        /// <summary>Turkey.</summary>
        Istanbul,
        /// <summary>Western Russia.</summary>
        Moscow,

        // Africa and the Middle East.
        /// <summary>Morocco.</summary>
        Casablanca,
        /// <summary>Nigeria and West Africa.</summary>
        Lagos,
        /// <summary>Egypt.</summary>
        Cairo,
        /// <summary>South Africa.</summary>
        Johannesburg,
        /// <summary>Kenya and East Africa.</summary>
        Nairobi,
        /// <summary>Israel.</summary>
        Jerusalem,
        /// <summary>Saudi Arabia, Iraq and Kuwait.</summary>
        Riyadh,
        /// <summary>The United Arab Emirates.</summary>
        Dubai,

        // Asia, west to east.
        /// <summary>Pakistan.</summary>
        Karachi,
        /// <summary>India and Sri Lanka.</summary>
        Mumbai,
        /// <summary>Bangladesh.</summary>
        Dhaka,
        /// <summary>Thailand and Vietnam.</summary>
        Bangkok,
        /// <summary>Western Indonesia.</summary>
        Jakarta,
        /// <summary>Singapore and Malaysia.</summary>
        Singapore,
        /// <summary>Hong Kong.</summary>
        HongKong,
        /// <summary>Mainland China.</summary>
        Shanghai,
        /// <summary>Taiwan.</summary>
        Taipei,
        /// <summary>The Philippines.</summary>
        Manila,
        /// <summary>South Korea.</summary>
        Seoul,
        /// <summary>Japan.</summary>
        Tokyo,

        // Oceania.
        /// <summary>Western Australia.</summary>
        Perth,
        /// <summary>South Australia.</summary>
        Adelaide,
        /// <summary>Queensland.</summary>
        Brisbane,
        /// <summary>New South Wales and Victoria.</summary>
        Sydney,
        /// <summary>New Zealand.</summary>
        Auckland,

        // The Americas, east to west.
        /// <summary>Brazil.</summary>
        SaoPaulo,
        /// <summary>Argentina, Uruguay and Chile's east.</summary>
        BuenosAires,
        /// <summary>Chile.</summary>
        Santiago,
        /// <summary>Colombia, Peru and Ecuador.</summary>
        Bogota,
        /// <summary>The United States and Canada, Eastern.</summary>
        NewYork,
        /// <summary>The United States and Canada, Central.</summary>
        Chicago,
        /// <summary>Mexico.</summary>
        MexicoCity,
        /// <summary>The United States and Canada, Mountain.</summary>
        Denver,
        /// <summary>The United States and Canada, Pacific.</summary>
        LosAngeles,
        /// <summary>Alaska.</summary>
        Anchorage,
        /// <summary>Hawaii.</summary>
        Honolulu,

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
            { CommonTimeZone.London, "GMT Standard Time" },
            { CommonTimeZone.Lisbon, "GMT Standard Time" },
            { CommonTimeZone.Paris, "Romance Standard Time" },
            { CommonTimeZone.Berlin, "W. Europe Standard Time" },
            { CommonTimeZone.Warsaw, "Central European Standard Time" },
            { CommonTimeZone.Athens, "GTB Standard Time" },
            { CommonTimeZone.Istanbul, "Turkey Standard Time" },
            { CommonTimeZone.Moscow, "Russian Standard Time" },

            { CommonTimeZone.Casablanca, "Morocco Standard Time" },
            { CommonTimeZone.Lagos, "W. Central Africa Standard Time" },
            { CommonTimeZone.Cairo, "Egypt Standard Time" },
            { CommonTimeZone.Johannesburg, "South Africa Standard Time" },
            { CommonTimeZone.Nairobi, "E. Africa Standard Time" },
            { CommonTimeZone.Jerusalem, "Israel Standard Time" },
            { CommonTimeZone.Riyadh, "Arab Standard Time" },
            { CommonTimeZone.Dubai, "Arabian Standard Time" },

            { CommonTimeZone.Karachi, "Pakistan Standard Time" },
            { CommonTimeZone.Mumbai, "India Standard Time" },
            { CommonTimeZone.Dhaka, "Bangladesh Standard Time" },
            { CommonTimeZone.Bangkok, "SE Asia Standard Time" },
            { CommonTimeZone.Jakarta, "SE Asia Standard Time" },
            { CommonTimeZone.Singapore, "Singapore Standard Time" },
            { CommonTimeZone.HongKong, "China Standard Time" },
            { CommonTimeZone.Shanghai, "China Standard Time" },
            { CommonTimeZone.Taipei, "Taipei Standard Time" },
            { CommonTimeZone.Manila, "Singapore Standard Time" },
            { CommonTimeZone.Seoul, "Korea Standard Time" },
            { CommonTimeZone.Tokyo, "Tokyo Standard Time" },

            { CommonTimeZone.Perth, "W. Australia Standard Time" },
            { CommonTimeZone.Adelaide, "Cen. Australia Standard Time" },
            { CommonTimeZone.Brisbane, "E. Australia Standard Time" },
            { CommonTimeZone.Sydney, "AUS Eastern Standard Time" },
            { CommonTimeZone.Auckland, "New Zealand Standard Time" },

            { CommonTimeZone.SaoPaulo, "E. South America Standard Time" },
            { CommonTimeZone.BuenosAires, "Argentina Standard Time" },
            { CommonTimeZone.Santiago, "Pacific SA Standard Time" },
            { CommonTimeZone.Bogota, "SA Pacific Standard Time" },
            { CommonTimeZone.NewYork, "Eastern Standard Time" },
            { CommonTimeZone.Chicago, "Central Standard Time" },
            { CommonTimeZone.MexicoCity, "Central Standard Time (Mexico)" },
            { CommonTimeZone.Denver, "Mountain Standard Time" },
            { CommonTimeZone.LosAngeles, "Pacific Standard Time" },
            { CommonTimeZone.Anchorage, "Alaskan Standard Time" },
            { CommonTimeZone.Honolulu, "Hawaiian Standard Time" }
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
