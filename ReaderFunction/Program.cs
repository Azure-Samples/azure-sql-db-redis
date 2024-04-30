using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace ReaderFunction
{
    public class Program
    {
        public static Task Main()
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config => 
                    config.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT")!), 
                                new ChainedTokenCredential(new AzureDeveloperCliCredential(), new DefaultAzureCredential())))
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    var endpoint = GetRedisEndpoint(context.Configuration["REDIS_HOST"], 
                        context.Configuration["REDIS_PORT"], 
                        context.Configuration[context.Configuration["REDIS_ACCESS_KEY"]], 
                        Convert.ToBoolean(context.Configuration["IS_ACRE"]));

                    services.AddDbContext<AppDbContext>(
                        options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, 
                            context.Configuration[context.Configuration["AZURE_SQL_CONNECTION_STRING_KEY"]]));

                    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(endpoint));
                    services.AddScoped<ISqlConnector, SqlConnector>();
                    services.AddScoped<IReadThrough, ReadThrough>();
                })
                .Build();
            
            return builder.RunAsync();
        }

        static string GetRedisEndpoint(string RedisHost, string RedisPort, string RedisPassword, bool IsACRE)
        {
            if (string.IsNullOrEmpty(RedisHost))
            {
                RedisHost = "127.0.0.1";
                RedisPort = "6379";
            }

            if (IsACRE)
            {
                return $"{RedisHost}:{RedisPort},ssl=true,password={RedisPassword}";
            }

            if (RedisPassword != null)
            {
                return $"{RedisPassword}@{RedisHost}:{RedisPort}";
            }
            else
            {
                return $"{RedisHost}:{RedisPort}";
            }
        }
    }
}

