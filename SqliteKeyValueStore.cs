using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zapserver
{
    class SqliteKeyValueStore
    {
        public static readonly string keyValueTabelName = "KEYVALUE";
        public static readonly string keyColumnName = "KEY";
        public static readonly string keyValueName = "VALUE";

        public SqliteKeyValueStore(string pathToSqlite)
        {
            m_dbConnection = new SQLiteConnection($"Data Source='{pathToSqlite}';Version=3;");
            m_dbConnection.Open();
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
