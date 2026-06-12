using VehicleLink.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Register Kafka producer as singleton
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// Register MQTT listener as hosted background service
builder.Services.AddHostedService<MqttListenerService>();

var app = builder.Build();

app.MapControllers();

app.Run();