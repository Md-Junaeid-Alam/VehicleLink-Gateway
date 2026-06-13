using Microsoft.EntityFrameworkCore;
using VehicleLink.Gateway.Hubs;
using VehicleLink.Gateway.Services;
using VehicleLink.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHostedService<MqttListenerService>();
builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddDbContext<VehicleLinkDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        policy.WithOrigins(
    "http://localhost:4200",
    "null",              // ← allows file opened HTML files
    "http://localhost");
    });
});

var app = builder.Build();

app.UseCors("DashboardPolicy");
app.MapControllers();
app.MapHub<TelemetryHub>("/hubs/telemetry");
app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/dashboard.html"));

app.Run();