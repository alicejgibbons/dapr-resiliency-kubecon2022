using Serilog;
using Serilog.Core;
using Serilog.Events;
using CustomerOrderService.Model;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Destructure.ByTransforming<CustomerOrder>(co => new { CustomerId = co.CustomerId, OrderTotal = co.OrderTotal, LoyaltyPoints = co.LoyaltyPoints, OrderCount = co.OrderCount })
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Starting up");

try {
var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.

builder.Services.AddHttpClient();
builder.Services.AddControllers().AddDapr(); // <-- Add Dapr support
//builder.Services.AddApplicationInsightsTelemetry();


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin();
        builder.AllowAnyHeader();
        builder.AllowAnyMethod();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSerilogRequestLogging(); // <-- Add Serilog support
app.UseSwagger(); 
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CustomerOrderService v1"));
app.UseSerilogRequestLogging();
app.UseRouting(); 
app.UseCloudEvents();
app.UseCors(); 
app.UseAuthorization(); 
app.UseEndpoints(endpoints =>
{
    // endpoints.MapSubscribeHandler();
    endpoints.MapControllers();
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}

class UtcTimestampEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory pf)
    {
        logEvent.AddPropertyIfAbsent(pf.CreateProperty("UtcTimestamp", logEvent.Timestamp.UtcDateTime));
    }
}