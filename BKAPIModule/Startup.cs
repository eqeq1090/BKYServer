using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System.Diagnostics;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Util;
using BKNetwork.ConstEnum;
using BKNetwork.Discovery;
using BKWebAPIComponent.Common.ServiceFactory.Database;
using BKWebAPIComponent.External;
using BKWebAPIComponent.Formatter;
using BKWebAPIComponent.Manager;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Middleware;
using BKWebAPIComponent.Redis;
using BKWebAPIComponent.Service;
using BKWebAPIComponent.Service.Initialize;
using static System.Net.Mime.MediaTypeNames;

namespace BKWebAPIComponent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // aspnetrun dependencies
            ConfigureAspnetRunServices(services);

            services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, new VillInputFormatter());
                options.OutputFormatters.Insert(0, new VillOutputFormatter());
            });
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder builder, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                builder.UseDeveloperExceptionPage();
                builder.UseSwagger();
                builder.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                    options.RoutePrefix = string.Empty;
                });
            }
            else
            {
                builder.UseHsts();
            }
            builder.UseStatusCodePages();


            //NOTE DB 설정 추가
            var queryMapper = builder.ApplicationServices.GetService<DBQueryMapper>();
            if (queryMapper is null)
            {
                throw new Exception($"DBQueryMapper is null");
            }
            ConfigureDatabases(queryMapper);

            

            /*
            builder.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = Text.Plain;

                    await context.Response.WriteAsync("An exception was thrown.");

                    var exceptionHandlerPathFeature =
                        context.Features.Get<IExceptionHandlerPathFeature>();

                    if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
                    {
                        await context.Response.WriteAsync(" The file was not found.");
                    }

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        CoreLog.Critical.LogError(contextFeature.Error);
                    }
                    await context.
                });
            });
            */

            //builder.UseHttpsRedirection();

            // builder.UseRedisInformation();

            builder.UseRouting();

            //builder.UseAuthorization();

            builder.UseMiddleware<SessionCheckMiddleware>();
            
            builder.UseMiddleware<BKDeveloperExceptionHandlerMiddleware>();

            builder.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });   
        }

        private void ConfigureAspnetRunServices(IServiceCollection services)
        {
            //NOTE 싱글턴 서비스 계열들 모두 추가
            services.AddHttpContextAccessor();

            services.AddSingleton<DBQueryMapper>();

            services.AddSingleton<ShardNumCheckService>();

            services.AddSingleton<DBServiceFactoryManager>();

            services.AddSingleton((serviceProvider) =>
            {
                var IPv4List = new List<string>();
                var IPv6List = new List<string>();
                CommonUtil.GetNics(ref IPv4List, ref IPv6List);
                if (IPv4List.Count == 0)
                {
                    throw new Exception("GET IPV4 ADDR FAILED");
                }
                int port = ConfigManager.Instance.APIServerConf!.Port;

                //디스커버리 서버는 게임서버가 궁금하다.
                return new ZKRegistrationService(IPv4List[0], port, ServerMode.APIServer, new Dictionary<ServiceDiscoveryInfoType, bool>(),
                    !ConfigManager.Instance.APIServerConf.UseLB);
            });

            services.AddSingleton<ExternalHttpClientService>();
            
            services.AddSingleton<DBInitializeService>();

            services.AddSingleton<RedisDataService>();

            services.AddSingleton<RewardManager>((provider) =>
            {
                var serviceFactoryManager = provider.GetService<DBServiceFactoryManager>();
                if (serviceFactoryManager != null)
                {
                    var newService = new RewardManager(serviceFactoryManager);
                    return newService;
                }
                else
                {
                    throw new Exception("RewardManager not initialized");
                }
            });

            services.AddSingleton<PlayerManager>((provider) =>
            {
                var serviceFactoryManager = provider.GetService<DBServiceFactoryManager>();
                var rewardManager = provider.GetService<RewardManager>();
                var clientService = provider.GetService<ExternalHttpClientService>();
                if (rewardManager != null && serviceFactoryManager != null && clientService != null)
                {
                    var newService = new PlayerManager(serviceFactoryManager, rewardManager, clientService);
                    return newService;
                }
                else
                {
                    throw new Exception("DBQueryMapper not initialized");
                }
            });

            services.AddRouting(options => options.LowercaseUrls = true);
            //미들웨어 추가 
        }

        public void ConfigureDatabases(DBQueryMapper queryMapper)
        {
            //커넥션 정보 로딩해서 커넥션 프로퍼티 초기화. net core config 말고 configuration 매니저 쓸것.
            // IDBService.Initialize(queryMapper);
        }
    }
}
