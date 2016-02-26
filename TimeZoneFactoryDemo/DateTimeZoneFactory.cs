using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NodaTime;
using NodaTime.TimeZones;

namespace TimeZoneFactoryDemo
{
    public class DateTimeZoneFactory
    {
        private readonly IDateTimeZoneProvider _provider;
        private readonly IDictionary<string, string> _mappings;

        private DateTimeZoneFactory(IDateTimeZoneProvider provider, IDictionary<string, string> mappings)
        {
            _provider = provider;
            _mappings = mappings;
        }

        public static DateTimeZoneFactory CreateFromFile(string pathToDataFile, string pathToOverridesFile)
        {
            // See http://nodatime.org/userguide/tzdb.html

            using (var stream = File.OpenRead(pathToDataFile))
            {
                var source = TzdbDateTimeZoneSource.FromStream(stream);
                var provider = new DateTimeZoneCache(source);
                var mappings = GetMappings(source, pathToOverridesFile);

                return new DateTimeZoneFactory(provider, mappings);
            }
        }

        private static IDictionary<string, string> GetMappings(TzdbDateTimeZoneSource source, string pathToOverridesFile)
        {
            // start from the overrides file
            var mappings = LoadOverrides(pathToOverridesFile);

            // merge the CLDR mappings from the source data
            foreach (var mapping in source.WindowsMapping.PrimaryMapping)
            {
                if (!mappings.ContainsKey(mapping.Key))
                {
                    // Ensure all IANA zone ids are canonical by IANA (as CLDR canonical can differ)
                    var value = source.CanonicalIdMap[mapping.Value];
                    mappings.Add(mapping.Key, value);
                }
            }

            return mappings;
        }


        public DateTimeZone GetDateTimeZone(string timeZoneId)
        {
            // try loading the IANA zone directly
            var tz = _provider.GetZoneOrNull(timeZoneId);
            if (tz != null)
            {
                return tz;
            }

            // try mapping it from a Windows ID
            var mappedId = WindowsToIana(timeZoneId);
            if (mappedId != null)
            {
                tz = _provider.GetZoneOrNull(mappedId);
                if (tz != null)
                {
                    return tz;
                }
            }

            // if all else failed, try from the registry before giving up
            tz = DateTimeZoneProviders.Bcl.GetZoneOrNull(timeZoneId);

            return tz;
        }

        private static IDictionary<string, string> LoadOverrides(string path)
        {
            // The overrides should include mappings that are not yet in the current published CLDR.
            // This is necessary due to the bi-annual release cadence of CLDR, vs. the as-needed releases of IANA and Windows data.

            var lines = File.ReadAllLines(path).Where(x => !string.IsNullOrWhiteSpace(x));
            var overrides = lines.Select(x => x.Split(',')).ToDictionary(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);

            // Make sure UTC is always mapped to Etc/UTC, as it is the most correct form.
            if (!overrides.ContainsKey("UTC"))
            {
                overrides.Add("UTC", "Etc/UTC");
            }

            return overrides;
        }

        private string WindowsToIana(string windowsZoneId)
        {
            // note, returns null if the mapping was not found

            string result;
            _mappings.TryGetValue(windowsZoneId, out result);
            return result;
        }
    }
}