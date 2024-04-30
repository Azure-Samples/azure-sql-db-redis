using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace BasicRedisLeaderboardDemoDotNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
            .ConfigureAppConfiguration(configuration => {
                configuration.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT")!), 
                    new ChainedTokenCredential(new AzureDeveloperCliCredential(), new DefaultAzureCredential()));
            })
            .Build()
            .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            // Accept the PORT environment variable to enable Cloud Run/Heroku support.
            var customPort = Environment.GetEnvironmentVariable("PORT");
            if (customPort != null)
            {
                string url = String.Concat("http://0.0.0.0:", customPort);

                return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls(url);
                });
            }
            else
            {

                return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
            }
        }
    }
}
