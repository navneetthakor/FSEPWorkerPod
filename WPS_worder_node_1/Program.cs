
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using WPS_worder_node_1.Repositories;
using DotNetEnv;

namespace WPS_worder_node_1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //loading .env file 
            Env.Load($"{Directory.GetCurrentDirectory()}/.env");
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            Startup startup = new Startup(builder.Configuration);

            startup.ConfigureServices(builder.Services);

            WebApplication app = builder.Build();
            IRecurringJobManager recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
            startup.Configure(app, builder.Environment,recurringJobManager);


            app.MapGet("/", () => "Hello World!");

            app.Run();
        }
    }
}
