using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using System.Data;


namespace Trigger.Generator
{
    internal static class DataManager
    {
        internal static OracleConnection CreateOracleConnection()
        {
            string connectionString =
                ConfigurationManager.ConnectionStrings[SettingNames.Orcl].ConnectionString;
            return new OracleConnection(connectionString);
        }

        public static int ExecuteNonQuery(string commandText)
        {
            using (var connection = CreateOracleConnection())
            {
                connection.Open();
                using (var command = new OracleCommand(commandText, connection))
                {
                    return command.ExecuteNonQuery();
                }
            }
        }

        public static DataTable ExecuteCommand(string commandText)
        {
            using (var connection = CreateOracleConnection())
            {
                connection.Open();
                using (var command = new OracleCommand(commandText, connection))
                {
                    using (var dataReader = command.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(dataReader);
                        return dataTable;
                    }
                }
            }
        }

        public static DataTable GetTableList()
        {
            using (var connection = CreateOracleConnection())
            {
                connection.Open();
                OracleConnectionStringBuilder builder = new OracleConnectionStringBuilder(connection.ConnectionString);
                var dataTableColumns = connection.GetSchema(CollectionNames.Tables, new[] { builder.UserID, null });
                return dataTableColumns;
            }
        }

        public static DataTable GetPrimaryKeysForTable(string ownerName, string tableName)
        {
            using (var connection = CreateOracleConnection())
            {
                connection.Open();
                var dataTable = connection.GetSchema(CollectionNames.PrimaryKeys, new[] { ownerName, tableName, null });
                return dataTable;
            }
        }

        public static DataTable GetIndexColumns(string ownerName, string indexName)
        {
            using (var connection = CreateOracleConnection())
            {
                connection.Open();
                var dataTable = connection.GetSchema(CollectionNames.IndexColumns, new[] { ownerName, indexName, null });
                return dataTable;
            }
        }

        public static DataTable GetColumnsForTable(string ownerName, string tableName)
        {
            using (var connection = CreateOracleConnection())
            {
                connection.Open();
                var dataTableColumns = connection.GetSchema(CollectionNames.Columns, new[] { ownerName, tableName, null });
                return dataTableColumns;
            }
        }
    }
}
