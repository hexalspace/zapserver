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
        public static readonly string valueColumnName = "VALUE";
        public static readonly string createKeyValueTable =  $"CREATE TABLE `{keyValueTabelName}` (`{keyColumnName}` TEXT, `{valueColumnName}` TEXT, PRIMARY KEY(`{keyColumnName}`))";

        public SqliteKeyValueStore(string pathToSqlite)
        {
            m_dbConnection = new SQLiteConnection($"Data Source='{pathToSqlite}';Version=3;");
            m_dbConnection.Open();
            try
            {
                SQLiteCommand command = new SQLiteCommand(createKeyValueTable, m_dbConnection);
                command.ExecuteNonQuery();
            }
            catch
            {
                ;
            }
        }

        public string getValue(string key)
        {
            string sql = $"SELECT {valueColumnName} from {keyValueTabelName} where {keyColumnName}='{key}'";
            using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }
                    return reader[valueColumnName].ToString();
                }
            }
        }

        public int? getValueAsNullableInt(string key)
        {
            int? value = null;

            string parseString = getValue(key);
            if (parseString == null)
            {
                return null;
            }

            int v;
            if (int.TryParse(parseString, out v))
            {
                value = v;
            }

            return value;
        }


        public void setValue(string key, string value)
        {
            string sql = $"INSERT OR REPLACE INTO {keyValueTabelName}({keyColumnName}, {valueColumnName}) VALUES ('{key}', '{value}')";
            using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        public void remove(string key)
        {
            string sql = $"DELETE FROM {keyValueTabelName} WHERE {keyColumnName}='{key}'";
            using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        SQLiteConnection m_dbConnection;
    }
}
