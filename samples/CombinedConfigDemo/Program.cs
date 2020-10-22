using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using Serilog.Sinks.MariaDB.Extensions;
using Serilog.Sinks.MariaDB;
using System.Threading;
using System.Collections.Generic;

namespace CombinedConfigDemo
{
    class Program
    {

        private const string _tableName = "console_logevents";

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();
            var sinkOptionsSection = configuration.GetSection("Serilog:SinkOptions");
            var LogDatabase = configuration.GetSection("ConnectionStrings:LogDatabase");

            // New SinkOptions based interface
            Log.Logger = new LoggerConfiguration()
                .AuditTo.MariaDB(
                    connectionString: LogDatabase.Value,
                    tableName: _tableName,
                    autoCreateTable: true,
                    options: new MariaDBSinkOptions
                    {
                        PropertiesToColumnsMapping = new Dictionary<string, string>
                        {
                            ["Exception"] = "exception",
                            ["Level"] = "level",
                            ["Message"] = "message",
                            ["MessageTemplate"] = "message_template",
                            ["Properties"] = "properties",
                            ["Timestamp"] = "timestamp",
                        },
                        TimestampInUtc = false,
                        ExcludePropertiesWithDedicatedColumn = true,
                        EnumsAsInts = true,
                        LogRecordsCleanupFrequency = TimeSpan.FromHours(0.02),//default is 12 mins
                        LogRecordsExpiration = TimeSpan.FromDays(32) //not set by default ,if you set ,tries to periodically delete rows
                    })
                .CreateLogger();

            Log.Information("Hello {Name} from thread {ThreadId}", Environment.GetEnvironmentVariable("USERNAME"), Thread.CurrentThread.ManagedThreadId);

            Log.Warning("No coins remain at position {@Position}", new { Lat = 25, Long = 134 });

            Log.Error("Error {@Position}", new { Lat = 25, Long = 134 });

            Log.Error(new Exception("Error Message"),"error message template");

            Log.CloseAndFlush();
        }
    }
}
