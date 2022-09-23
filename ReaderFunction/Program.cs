using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace ReaderFunction
{
    public class Program
    {
        private const string _jsonFileName = "local.settings.json";

        public static Task Main()
        {
            var builder = new HostBuilder()                                                       
                         .ConfigureAppConfiguration(config =>
                         {                                           
                             config.AddJsonFile(_jsonFileName, optional:true, reloadOnChange: true)
                             .AddEnvironmentVariables();   
                         })
                         .ConfigureFunctionsWorkerDefaults()     
                         .ConfigureServices((context, services) =>
                         {
                             var settings = context.Configuration.GetSection(ReaderFunctionOptions.Section)
                            .Get<ReaderFunctionOptions>();

                            var endpoint = settings.GetRedisEnpoint();

                             services.AddDbContext<AppDbContext>(
                                 options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, settings.SQLConnectionString));

                             services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(endpoint));
                             services.AddScoped<ISqlConnector, SqlConnector>();
                             services.AddScoped<IReadThrough, ReadThrough>();
                         })
                         .Build();
            
            return builder.RunAsync();
        }
    }
}

