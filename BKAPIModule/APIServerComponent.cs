using Microsoft.AspNetCore;
using Newtonsoft.Json;
using BKServerBase.Component;
using BKServerBase.Config;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKNetwork.Discovery;
using BKWebAPIComponent.Service.Initialize;

namespace BKWebAPIComponent
{
    public class APIServerComponent : BaseSingleton<APIServerComponent>, IComponent
    {
        IWebHost? app = null;
        

        private APIServerComponent()
        {
            
        }

        public (bool success, OnComponentInitializedHandler? InitDoneFunc) Initialize()
        {
            //TODO 주키퍼 등록용 매니저 추가
            //하단의 서비스가 기동되기전에 먼저 해야 함.
            //그외 웹서버에서 스레드를 더 따던가 해서 해야되는 서비스들은 여기서 처리

            app = CreateHostBuilder().Build();

            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DBInitializeService>();
                context.CheckSchemaChanged().ContinueWith((result) =>
                {
                });
            }

            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ZKRegistrationService>();
                context.Register();
            }

            app.RunAsync().ContinueWith((Result) =>
            {
                CoreLog.Critical.LogFatal($"{Result.Exception}/{Result.IsFaulted}: Terminated");
            });
            return (true, () =>
            {
                //타이머 등록 등
            });
        }

        public bool OnUpdate(double delta)
        {
            return true;
        }

        public bool Shutdown()
        {
            app?.StopAsync().Wait();
            return true;
        }

        public IWebHostBuilder CreateHostBuilder()
        {
            var config = MakeConfiguration();
            //NOTE https://learn.microsoft.com/ko-kr/aspnet/core/fundamentals/host/web-host?view=aspnetcore-6.0
            //웹 호스트 Default 빌더를 쓴다. Startup 함수를 쓰기 위해서.
            //단, 하기의 사항을 참조하길 바람.
            //1. 환경 변수를 안쓸 예정 (단일머신에서 혹시라도 환경이 다른 두개의 바이너리가 켜질 경우 대형 사고 발생). 
            //   환경변수에 혹시라도 써놨더라도 아래 항목에서 최종으로 덮어쓰게 됨.
            //2. 명령줄 인수는 서버 전체항목으로 쓰고, asp용 인수는 안쓰는 쪽으로 (--environment 등)
            //3. appsettings.json 안쓸 예정
            //4. launchsettings.json 안쓸 예정 (애당초 VS에서만 동작하므로 라이브에서 쓸수 없음. gitignore 제외)
            //5. IIS 사용안함. 크로스플랫폼 대응 X
            var port = ConfigManager.Instance.APIServerConf!.Port;
            var builder = WebHost.CreateDefaultBuilder()
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .UseUrls($"http://0.0.0.0:{port}",$"https://0.0.0.0:{port+1}")
                .UseConfiguration(config)
                .UseEnvironment(Environments.Development)
                .UseStartup<Startup>();
            CoreLog.Normal.LogDebug($"Application Name: {builder.GetSetting(WebHostDefaults.ApplicationKey)}");
            return builder;
        }

        private IConfiguration MakeConfiguration()
        {
            return new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("kestrelsetting.json", optional: true) //kestrel 등에 대한 세팅을 여기에 한다.
            .Build();
        }
    }
}
