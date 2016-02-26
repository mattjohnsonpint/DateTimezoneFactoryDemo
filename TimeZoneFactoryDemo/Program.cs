using System;
using System.Configuration;

namespace TimeZoneFactoryDemo
{
    class Program
    {
        private static DateTimeZoneFactory _factory;

        static void Main(string[] args)
        {
            // Check http://nodatime.org/tzdb/latest.txt for the latest data file.
            // See http://nodatime.org/tzdb/ for more details.
            
            var dataFile = ConfigurationManager.AppSettings["NodaTimeDataFile"];
            var mappingFile = ConfigurationManager.AppSettings["MappingOverridesFile"];

            _factory = DateTimeZoneFactory.CreateFromFile(dataFile, mappingFile);


            Test("Pacific Standard Time");     // America/Los_Angeles  (Windows to IANA)
            Test("America/New_York");          // America/New_York     (IANA)
            Test("Australia/Lord_Howe");       // Australia/Lord_Howe  (IANA only)
            Test("E. Europe Standard Time");   // Europe/Chisinau  (from override)
            Test("Foo");                       // null (not found)
        }

        private static void Test(string input)
        {
            var tz = _factory.GetDateTimeZone(input);
            Console.WriteLine("{0} => {1}", input, tz != null ? tz.Id : "(null)");
        }
    }
}
