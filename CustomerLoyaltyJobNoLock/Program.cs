using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using CustomerLoyaltyJobNoLock.Model;

namespace CustomerLoyaltyJobNoLock
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Destructure.ByTransforming<CustomerOrder>(co => new { CustomerId = co.CustomerId, OrderTotal = co.OrderTotal, LoyaltyPoints = co.LoyaltyPoints, OrderCount = co.OrderCount })
            .WriteTo.Console()
            .CreateLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
