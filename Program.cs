using PlatformService;

var builder = WebApplication.CreateBuilder(args);

// Startup 클래스 인스턴스 생성 및 서비스 등록 메서드 호출
var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// HTTP 요청 파이프라인 구성 메서드 호출
startup.Configure(app, builder.Environment);

app.Run();
