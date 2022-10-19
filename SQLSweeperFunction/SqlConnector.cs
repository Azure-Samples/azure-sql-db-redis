using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SQLSweeperFunction
{
    public class SqlConnector
    {
        private readonly string _tableName;
        private readonly SqlConnection _sqlConnection;
        private readonly string _pk;
        private string _addQuery;
        private string _deleteQuery;
        private readonly ILogger _logger;
        private readonly IDatabase _database;
        private TimeSpan ackExpireSeconds = new TimeSpan(0,0,10);

        public SqlConnector(SqlConnection connection, string tableName, string pk, ILogger logger, IDatabase database)
        {
            _sqlConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _pk = pk ?? throw new ArgumentNullException(nameof(tableName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public void PrepereQueries(NameValueEntry[] entries, RedisKey key)
        {
            //The last value after the second hyphen is the key name.
            //We can also add a new property in the hash to store the key name to avoid this logic.

            string pkValue = key.ToString().Split("-").Skip(2).ToArray()[0].ToString();
            
            //Check if the pkValue has Redis Standard convention. If it does we need to normalize to SQL pk
             if(pkValue.Contains(":"))
             {
                pkValue = pkValue.Split(":").Skip(1).ToArray()[0].ToString();
             }

            _addQuery = GetUpdateQuery(_tableName, entries, _pk, pkValue);
            _deleteQuery = $"DELETE FROM {_tableName} WHERE {nameof(_pk)}={_pk}";
                    
        }

        private string GetUpdateQuery(string tableName, NameValueEntry[] entries, string pk, string pkValue)
        {
            var names = string.Join(",", entries.Select(x => x.Name));
            var values = string.Join(",", entries.Select(x => $"'{x.Value}'"));

            var merge_into = $"MERGE {tableName} AS Target " +
                             $"USING (VALUES ('{pkValue}')) AS Source (key1) " +
                             $"ON (Target.{pk} = Source.key1)";

            var not_matched = $"WHEN NOT MATCHED BY TARGET " +
                              $"THEN INSERT ({pk}, {names}) VALUES ('{pkValue}', {values})";

            var matched = $"WHEN MATCHED " +
                          $"THEN UPDATE SET {string.Join(",", entries.Select(x => $"Target.{x.Name}='{x.Value}'"))}";

            return $"{merge_into} {not_matched} {matched};";
        }

        public string TableName()
        {
            return _tableName;
        }

        public string PrimaryKey()
        {
            return _pk;
        }

        public async Task WriteData(NameValueEntry[] entries, RedisKey key)
        {
            if (entries.Length == 0)
            {
               _logger.LogInformation("Warning, got an empty batch");
               return;
            }

            string query = "";
            var idsToAck = new List<string>();
           
    
            await _sqlConnection.OpenAsync();
            using var trans = await _sqlConnection.BeginTransactionAsync();
           
            try
            {
                var isAdd = true;
                query = isAdd ? _addQuery : _deleteQuery;
 
                using(SqlCommand cmd = new SqlCommand(query, _sqlConnection, trans as SqlTransaction))
                {
                    await cmd.ExecuteNonQueryAsync();
                    _database.KeyExpire(key, ackExpireSeconds);
                }

                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                string msg = "";
                try
                {
                    msg = $"Got exception when writing to DB, query= {query}, error={ex.Message}";
                    _logger.LogError(msg);

                    await trans.RollbackAsync();
                }
                catch (Exception e)
                {
                    msg = $"Got exception trying to roll back transaction, error={e.Message}";
                    _logger.LogError(msg);
                }
            }

            _sqlConnection.Close();
        }

    }
}

