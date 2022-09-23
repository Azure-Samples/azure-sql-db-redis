﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SQLSweeperFunction.Models;
using StackExchange.Redis;
using System.Collections.Generic;

namespace SQLSweeperFunction
{
    public class SQLSweeperFunction 
    {
        [FunctionName("SQLSweeperFunction")]
        public static async Task Run([TimerTrigger("%TimerInterval%")]TimerInfo myTimer, ILogger log)
        {
            //Get all the keys that need to be worked on
            var sqlConnectionString = GetAzureSqlConnectionString("SQLConnectionString", log);
            var redisConnectionString = GetRedisConnectionstring("RedisConnectionString", log);

            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                log.LogInformation("The SQL connection string is empty");
                throw new NullReferenceException(nameof(sqlConnectionString));
            }
            
            if(string.IsNullOrEmpty(redisConnectionString))
            {
                log.LogInformation("The Redis connection string is empty");
                 throw new NullReferenceException(nameof(redisConnectionString));
            }
               
            try
            {
                IConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

                IDatabase db = redisConnection.GetDatabase();

                var sqlConnector = new SqlConnector(new SqlConnection(sqlConnectionString), "company", "symbol", log, db);

                //TODO: Read from stream, depending on the type insert or update
                var preFix = $"_{sqlConnector.TableName()}-stream-*";
                IEnumerable<RedisKey> keys = GetKeysByPattern(preFix, redisConnection);
            
                foreach(var key in keys)
                {
                    //Get the last entry
                    StreamEntry[] results = db.StreamRange(key, maxId: "+", count:1, messageOrder:Order.Descending);
                    NameValueEntry[] values = results[0].Values;

                    sqlConnector.PrepereQueries(values, key);
                    await sqlConnector.WriteData(values, key);
                }
            }
            catch(Exception ex)
            {
                log.LogInformation(ex.Message);
            }

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private static string GetAzureSqlConnectionString(string name, ILogger log)
        { 
            log.LogInformation($"Getting information from ConnectionStrings:{name}");
            string conStr = System.Environment.GetEnvironmentVariable($"ConnectionStrings:{name}");
            log.LogInformation($"The conStr value is:{conStr}");

            if (string.IsNullOrEmpty(conStr) && name == "SQLConnectionString") // Azure Functions App Service naming convention
            {
                log.LogInformation($"Getting information from SQLAZURECONNSTR_{name}");
                conStr = System.Environment.GetEnvironmentVariable($"SQLAZURECONNSTR_{name}");
                log.LogInformation($"The conStr value is:{conStr}");
            }
            return conStr;
        }

        private static string GetRedisConnectionstring(string name, ILogger log)
        {
            log.LogInformation($"Getting information from ConnectionStrings:{name}");
            string conStr = System.Environment.GetEnvironmentVariable($"ConnectionStrings:{name}");
            log.LogInformation($"The conStr value is:{conStr}");

            if (string.IsNullOrEmpty(conStr) && name == "RedisConnectionString") // Azure Functions App Service naming convention
            {
                  log.LogInformation($"Getting information from REDISCONNSTR_{name}");
                  conStr = System.Environment.GetEnvironmentVariable($"REDISCONNSTR_{name}");
                  log.LogInformation($"The conStr value is:{conStr}");
            }
                
            return conStr;
        }

        private static IEnumerable<RedisKey> GetKeysByPattern(string pattern, IConnectionMultiplexer redisConnection)
        {
            var server = redisConnection.GetServer(redisConnection.GetEndPoints().Single());
            var keys = server.Keys(pattern: pattern);

            return keys;        
        }
    }
}

