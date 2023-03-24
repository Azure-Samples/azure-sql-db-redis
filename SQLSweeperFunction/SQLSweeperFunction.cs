using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System.Collections.Generic;

namespace SQLSweeperFunction
{
    public class SQLSweeperFunction 
    {
        [FunctionName("SQLSweeperFunction")]
        public static async Task Run([TimerTrigger("%TimerInterval%")]TimerInfo myTimer,
        ILogger log)
        {
            await new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT")!), 
                        new ChainedTokenCredential(new AzureDeveloperCliCredential(), new DefaultAzureCredential()));
                })
                .ConfigureServices(async (context, services) => {
                    var sqlConnectionString = context.Configuration[Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING_KEY")];
                    var redisConnectionString = context.Configuration[Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING_KEY")];
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

                })
                .Build()
                .RunAsync();
        }

        private static IEnumerable<RedisKey> GetKeysByPattern(string pattern, IConnectionMultiplexer redisConnection)
        {
            var server = redisConnection.GetServer(redisConnection.GetEndPoints().Single());
            var keys = server.Keys(pattern: pattern);

            return keys;        
        }
    }
}

