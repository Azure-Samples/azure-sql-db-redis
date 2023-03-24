using BasicRedisLeaderboardDemoDotNetCore.BLL;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Models;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services.Interfaces;
using BasicRedisLeaderboardDemoDotNetCore.BLL.DbContexts;
using BasicRedisLeaderboardDemoDotNetCore.Configs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BasicRedisLeaderboardDemoDotNetCore
{
    public class Startup
    {
        private const string KeySpaceChannel = $"__key*__*";
        private const string KeyEventChannel = "__keyevent@0__:*";
        private readonly string[] setCommands = { "hset", "hmset", "hincrbyfloat", "hincrby", "hsetnx", "change" };
        private readonly string[] delCommands = { "hdel", "del" };
        private readonly string _policyName = "CorsPolicy";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<LeaderboardDemoOptions>()
            .Bind(Configuration.GetSection(LeaderboardDemoOptions.Section))
            .Configure(options => {
                options.RedisHost = Configuration["REDIS_HOST"];
                options.RedisPort = Configuration["REDIS_PORT"];
                options.RedisPassword = Configuration[Configuration["REDIS_ACCESS_KEY"]];
                options.IsACRE = Convert.ToBoolean(Configuration["IS_ACRE"]);
                options.AllowAdmin = Convert.ToBoolean(Configuration["ALLOW_ADMIN"]);
                options.DeleteAllKeysOnLoad = Convert.ToBoolean(Configuration["DELETE_ALL_KEYS_ONLOAD"]);
                options.UseReadThrough = Convert.ToBoolean(Configuration["USE_READ_THROUGH"]);
                options.UseWriteBehind = Convert.ToBoolean(Configuration["USE_WRITE_BEHIND"]);
                options.ReadThroughFunctionBaseUrl = Configuration["READ_THROUGH_FUNCTION_BASE_URL"];
                options.LoadInitialData = Convert.ToBoolean(Configuration["LOAD_INITIAL_DATA"]);
            });

            var endpoint = GetRedisEndpoint(Configuration["REDIS_HOST"], 
                Configuration["REDIS_PORT"], Configuration[Configuration["REDIS_ACCESS_KEY"]], 
                Convert.ToBoolean(Configuration["IS_ACRE"]), Convert.ToBoolean(Configuration["ALLOW_ADMIN"]));

            var redisConnection = ConnectionMultiplexer.Connect(endpoint);

            services.AddSingleton<IConnectionMultiplexer>(redisConnection);

            if (Convert.ToBoolean(Configuration["USE_READ_THROUGH"]))
            {
                services.AddHttpClient<IAzureFunctionHttpClient, AzureFunctionHttpClient>(httpClient =>
                {
                    httpClient.BaseAddress = new Uri(Configuration["READ_THROUGH_FUNCTION_BASE_URL"]);
                });
                services.AddTransient<IRankService, RankServiceReadThrough>();
            }
            else
            {
                services.AddTransient<IRankService, RankService>();
            }

            ConfigureKeySpaceNotifications(redisConnection);
           
            Assembly.Load("BasicRedisLeaderboardDemoDotNetCore.BLL");
            //ServiceAutoConfig.Configure(services);

            services.AddControllers();

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            services.AddCors(opt =>
            {
                opt.AddPolicy(name: _policyName, builder =>
                {
                    builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            services.AddMvc(option => option.EnableEndpointRouting = false);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ICorsService corsService, ICorsPolicyProvider corsPolicyProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

           
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();
            app.UseCors(_policyName);

            app.Map(new PathString(""), client =>
            {
                var clientPath = Path.Combine(Directory.GetCurrentDirectory(), "./ClientApp/dist");
                StaticFileOptions clientAppDist = new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(clientPath)
                };
                client.UseSpaStaticFiles(clientAppDist);
                client.UseSpa(spa =>
                {
                    spa.Options.DefaultPageStaticFileOptions = clientAppDist;
                });

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
                });
            });

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                //Seed method
                AppDbInitializer.Seed(serviceScope);

            }
        }

        public void ConfigureKeySpaceNotifications(IConnectionMultiplexer connectionMultiplexer)
        {
            connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().Single())
            .ConfigSet("notify-keyspace-events", "KEA"); // KEA=everything

            //subscribe to the event
            connectionMultiplexer.GetSubscriber().Subscribe(KeySpaceChannel,
                async (channel, message) => {
                    if (setCommands.Any(x => x.Contains(message)))
                    {
                        // There was a set so we need to send it to process it async
                        Console.WriteLine($"received {message} on {channel}");
                        var db = connectionMultiplexer.GetDatabase();
                        var keyArr = channel.ToString().Split(":");
                        var key = keyArr[1];

                        if (keyArr.Length == 3)
                        {
                            key = $"{keyArr[1]}:{keyArr[2]}";
                        }
                        else
                        {
                            key = keyArr[1];
                        }


                        try
                        {
                            switch (message)
                            {
                                case "hset":
                                    HashEntry[] hashEntry = await db.HashGetAllAsync(key);
                                    var score = await db.SortedSetScoreAsync(LeaderboardDemoOptions.RedisKey, key);
                                    var rank = await db.SortedSetRankAsync(LeaderboardDemoOptions.RedisKey, key);
                                    hashEntry = hashEntry.Append(new HashEntry("marketcap", score)).ToArray();
                                    hashEntry = hashEntry.Append(new HashEntry("rank", rank)).ToArray();
                                    var wb = new WriteBehind(connectionMultiplexer, "company");
                                    wb.AddToStream(key, hashEntry);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    if (delCommands.Any(x => x.Contains(message)))
                    {
                        Console.WriteLine($"received {message} on {channel}");
                        // We want to keep the records in the SQL database, for this reason we don't sync deletes.

                    }
                });
        }

        private string GetRedisEndpoint(string RedisHost, string RedisPort, string RedisPassword, bool IsACRE, bool AllowAdmin)
        {
            if (string.IsNullOrEmpty(RedisHost))
            {
                RedisHost = "127.0.0.1";
                RedisPort = "6379";
            }

            if (IsACRE)
            { 
                return $"{RedisHost}:{RedisPort},abortConnect=false,ssl=true,password={RedisPassword},allowAdmin={AllowAdmin},syncTimeout=5000,connectTimeout=1000";                
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
