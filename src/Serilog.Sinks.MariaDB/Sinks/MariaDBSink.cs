using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if MySqlConnector
using MySqlConnector;
#else
using MySql.Data.MySqlClient;
#endif
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MariaDB.Sinks
{
    public class MariaDBSink : PeriodicBatchingSink
    {
        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

        private readonly string _connectionString;
        private readonly MariaDBSinkCore _core;
        private readonly bool _useBulkInsert;

        public MariaDBSink(string connectionString,
            IFormatProvider formatProvider,
            int batchPostingLimit,
            int queueSizeLimit,
            TimeSpan period,
            MariaDBSinkOptions options,
            string tableName,
            bool autoCreateTable,
            bool useBulkInsert) : base(batchPostingLimit, period, queueSizeLimit)
        {
            _connectionString = connectionString;
            _useBulkInsert = useBulkInsert;

            _core = new MariaDBSinkCore(connectionString, formatProvider, options, tableName, autoCreateTable);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    if (_useBulkInsert)
                    {
                        await BulkInsert(events, connection).ConfigureAwait(false);
                    }
                    else
                    {
                        await Insert(events, connection).ConfigureAwait(false);
                    }
                }
                    
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to write {0} log events to the database due to following error: {1}", events.Count(), ex.Message);
            }
        }

        private async Task BulkInsert(IEnumerable<LogEvent> events, MySqlConnection connection)
        {
            var eventData = events.Select(i => _core.GetColumnsAndValues(i)).ToList();
            var commandText = _core.GetBulkInsertStatement(eventData);

            using (var cmd = new MySqlCommand(commandText, connection))
            {
                int i = 0;
                foreach (var columnValues in eventData)
                {
                    foreach (var columnValue in columnValues)
                    {
                        if (columnValue.Value != null)
                        {
                            cmd.Parameters.AddWithValue($"{columnValue.Key}{i}", columnValue.Value);
                        }
                    }
                    i++;
                }
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task Insert(IEnumerable<LogEvent> events, MySqlConnection connection)
        {
            foreach (var log in events)
            {
                try
                {
                    var columnValues = _core.GetColumnsAndValues(log).ToList();
                    var commandText = _core.GetInsertStatement(columnValues);

                    using (var cmd = new MySqlCommand(commandText, connection))
                    {
                        foreach (var columnValue in columnValues)
                        {
                            if (columnValue.Value != null)
                            {
                                cmd.Parameters.AddWithValue(columnValue.Key, columnValue.Value);
                            }
                        }

                        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Unable to write log event to the database due to following error: {0}", ex.Message);
                }
            }
        }
    }
}
