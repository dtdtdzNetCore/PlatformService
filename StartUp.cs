using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using PlatformService.Data;
using Microsoft.AspNetCore.Hosting.Server.Features;
using PlatformService.SyncDataServices.Http;
using PlatformService.AsyncDataServices;
using PlatformService.SyncDataServices.Grpc;

namespace PlatformService
{
    public class Startup
    {


        public IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment _env;
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (_env.IsProduction())
            {
                System.Console.WriteLine("--> Using SqlServer Db");
                services.AddDbContext<AppDbContext>(opt => 
                    opt.UseSqlServer(Configuration.GetConnectionString("PlatformsConn")));
            }
            else
            {
                System.Console.WriteLine("--> Using InMem Db");
                // Register the DbContext with dependency injection
                services.AddDbContext<AppDbContext>(opt => 
                    opt.UseInMemoryDatabase("InMem"));
            }

            services.AddControllers();
            // AutoMapper 등록
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // 레포지토리 패턴을 위한 서비스 등록
            services.AddScoped<IPlatformRepo, PlatformRepo>();
            services.AddSingleton<IMessageBusClient, MessageBusClient>();
            services.AddGrpc();
            // Register Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PlatformService API", Version = "v1" });
            });
            services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();

            System.Console.WriteLine($"--> CommandService EndPoint {Configuration["CommandService"]}");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();

            // Enable middleware to serve Swagger UI
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlatformService API V1");
                c.RoutePrefix = string.Empty;
            });

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<GrpcPlatformService>();
                endpoints.MapGet("/protos/platforms.proto", async context => 
                {
                    await context.Response.WriteAsync(File.ReadAllText("Protos/platforms.proto"));
                });
            });


            PrepDb.PrepPopulation(app, env.IsProduction());
        }
    }
}
