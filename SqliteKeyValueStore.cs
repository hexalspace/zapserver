using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zapserver
{
    class SqliteKeyValueStore : IKeyValueStore
    {
        public static readonly string keyValueTabelName = "KEYVALUE";
        public static readonly string keyColumnName = "KEY";
        public static readonly string keyValueName = "VALUE";
        public static readonly string createKeyValueTable =  $"CREATE TABLE `{keyValueTabelName}` (`{keyColumnName}` TEXT, `{keyValueName}` TEXT, PRIMARY KEY(`{keyColumnName}`))";

        public SqliteKeyValueStore(string pathToSqlite)
        {
            m_dbConnection = new SQLiteConnection($"Data Source='{pathToSqlite}';Version=3;");

            try
            {
                m_dbConnection.Open();
                return;
            }
            catch
            {
                SQLiteConnection.CreateFile(pathToSqlite);
                m_dbConnection = new SQLiteConnection($"Data Source='{pathToSqlite}';Version=3;");
                m_dbConnection.Open();
                SQLiteCommand command = new SQLiteCommand(createKeyValueTable, m_dbConnection);
                command.ExecuteNonQuery();
            }
        }

        public string getValue(string key)
        {
            string sql = $"SELECT {keyValueName} from {keyValueTabelName} where {keyValueName}='{key}'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            reader.Read();
            return reader[keyValueName].ToString();
        }

        public void setValue(string key, string value)
        {
            string sql = $"INSERT INTO {keyValueTabelName}({keyColumnName}, {keyValueName}) VALUES ('{key}', '{value}')";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        SQLiteConnection m_dbConnection;
    }
}
