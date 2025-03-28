using Hangfire;
using Hangfire.MySql;
using WPS_worder_node_1.Repositories;
using WPS_worder_node_1.Repositories.Interface;

namespace WPS_worder_node_1
{
    /// <summary>
    /// Class responsible for configuring the application during startup. 
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration property containing the application's configuration
        /// </summary>
        public IConfiguration Configuration { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            string connectionString = "server=localhost;database=hangfire_db;uid=root;pwd=N@vneet2810;Allow User Variables=True"; // Connection string to the database

            // 1. Configure Hangfire storage (e.g., SQL Server, Redis)
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(
                new MySqlStorage(connectionString, new MySqlStorageOptions())));

            services.AddHangfireServer();

            //ServerListRepo configuration 
            services.AddSingleton<IServerListRepo, ServerListRepo>();
            services.AddSingleton<IMyJobServices, MyJobServices>();
            services.AddSingleton<IHeartBitService, HeartBitService>();
            
        }

        public void Configure(WebApplication app, IWebHostEnvironment env, IRecurringJobManager recurringJobManager)
        {
            // Schedule a recurring job that pushes metrics
            //IMyJobServices job = app.Services.GetRequiredService<IMyJobServices>();
            //recurringJobManager.AddOrUpdate("Checking-health", () => job.InvokCheck(), Cron.Minutely);

            //Schedule a recurring job that sends a heartbeat
            IHeartBitService heartBitService = app.Services.GetRequiredService<IHeartBitService>();
            recurringJobManager.AddOrUpdate("HeartBit", () => heartBitService.HeartBit(), Cron.Minutely);

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }   

            // Redirect HTTP requests to HTTPS
            app.UseHttpsRedirection();

            // Enable Hangfire Dashboard (optional)
            app.UseHangfireDashboard("/hangfire");

            // Map incoming requests to controller actions
            app.MapControllers();


            // End the request pipeline 
            app.Run();
        }
    }
}