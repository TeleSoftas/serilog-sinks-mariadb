#if MySqlConnector
using MySqlConnector;
#else
using MySql.Data.MySqlClient;
#endif

using System.Collections.Generic;
using System.Text;

namespace Serilog.Sinks.MariaDB
{
    internal class SqlTableCreator
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly IReadOnlyCollection<string> _columns;

        public SqlTableCreator(string connectionString, string tableName, IReadOnlyCollection<string> columns)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _columns = columns;
        }

        public int CreateTable()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = GetSqlForTable();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    conn.Open();

                    return cmd.ExecuteNonQuery();
                }
            }
        }

        private string GetSqlForTable()
        {
            var sql = new StringBuilder();
            var i = 1;

            sql.AppendLine($"CREATE TABLE IF NOT EXISTS `{_tableName}` ( ");
            sql.AppendLine("`Id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,");

            foreach (var column in _columns)
            {
                sql.Append($"`{column}` TEXT NULL");

                if (_columns.Count > i++)
                {
                    sql.Append(",");
                }

                sql.AppendLine();
            }

            sql.AppendLine(");");

            return sql.ToString();
        }
    }

}
