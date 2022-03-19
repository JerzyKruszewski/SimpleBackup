using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Npgsql;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using SimpleBackup.Core;

namespace SimpleBackup.Npgsql.Core
{
    public class DatabaseHandler : IDatabaseHandler
    {
        private readonly IConfiguration _config;
        private readonly IConfiguration _internalConfig = new ConfigurationBuilder().AddJsonFile("./npgsql_config.json").Build();
        private readonly string _connectionString;

        private const string GetSchemaNamesQuery = @"SELECT schema_name 
FROM information_schema.schemata
WHERE schema_name NOT IN ({0});";
        private const string GetTableNamesQuery = @"SELECT table_name
FROM information_schema.tables
WHERE table_type = 'BASE TABLE' AND table_schema = '{0}';";
        private const string GetTableDataQuery = "SELECT * FROM {0}.\"{1}\";";
        private const string GetInsertQueryHeader = "INSERT INTO {0}.\"{1}\" VALUES ";

        public DatabaseHandler(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSQL");
        }

        public IDictionary<string, string> GetBackupQueries()
        {
            StringBuilder query = new StringBuilder();
            Dictionary<string, string> queries = new Dictionary<string, string>();
            ICollection<TableData> data = HandleBackup();

            foreach (TableData tableData in data)
            {
                if (tableData.Data.Rows.Count == 0 || tableData.Data.Columns.Count == 0) //Empty 
                {
                    continue;
                }

                query.Clear();
                query.AppendLine(string.Format(GetInsertQueryHeader, tableData.SchemaName, tableData.TableName));

                for (int row = 0; row < tableData.Data.Rows.Count; row++)
                {
                    query.Append("(");
                    for (int col = 0; col < tableData.Data.Columns.Count; col++)
                    {
                        object? cell = tableData.Data.Rows[row][col];
                        if (cell is null || cell is DBNull)
                        {
                            query.Append("NULL, ");
                            continue;
                        }
                        if (cell is int)
                        {
                            query.Append($"{cell}, ");
                            continue;
                        }
                        query.Append($"'{cell}', ");
                    }
                    query.Length -= 2;
                    query.AppendLine("),");
                }

                query.Length -= (Environment.NewLine.Length + 1);
                query.AppendLine(";");
                queries.Add($"{tableData.SchemaName}-{tableData.TableName}-{DateTime.UtcNow}", query.ToString());
            }

            return queries;
        }

        public ICollection<TableData> HandleBackup()
        {
            List<TableData>? data = new List<TableData>();

            foreach (string schema in GetSchemas())
            {
                foreach (string tableName in GetTables(schema))
                {
                    data.Add(new TableData()
                    {
                        SchemaName = schema,
                        TableName = tableName,
                        Data = GetDataFromTable(schema, tableName)
                    });
                }
            }

            return data;
        }

        private DataTable GetDataFromTable(string schema, string tableName)
        {
            return GetData(string.Format(GetTableDataQuery, schema, tableName));
        }

        private ICollection<string> GetTables(string schema)
        {
            List<string> result = new List<string>();

            string query = string.Format(GetTableNamesQuery, schema);

            DataTable table = GetData(query);
            for (int row = 0; row <= table.Rows.Count - 1; row++)
            {
                object? cell = table.Rows[row][0];
                if (cell is string)
                {
                    result.Add(cell.ToString());
                }
            }

            return result;
        }

        private ICollection<string> GetSchemas()
        {
            List<string> result = new List<string>();
            StringBuilder ignoredSchemas = new StringBuilder();
            foreach (IConfigurationSection section in _internalConfig.GetSection("SchematasToIgnore").GetChildren())
            {
                ignoredSchemas.Append($"'{section.Value}', ");
            }
            ignoredSchemas.Length -= 2;
            string query = string.Format(GetSchemaNamesQuery, ignoredSchemas.ToString());

            DataTable table = GetData(query);
            for (int row = 0; row <= table.Rows.Count - 1; row++)
            {
                object? cell = table.Rows[row][0];
                if (cell is string)
                {
                    result.Add(cell.ToString());
                }
            }

            return result;
        }

        private DataTable GetData(string query)
        {
            DataTable dataTable = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
                conn.Close();
            }
            return dataTable;
        }
    }
}
