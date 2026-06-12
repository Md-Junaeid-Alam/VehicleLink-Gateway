using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using VehicleLink.Core.Models;
using VehicleLink.Gateway.Hubs;

namespace VehicleLink.Gateway.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly ILogger<KafkaConsumerService> _logger;

    public KafkaConsumerService(
        IHubContext<TelemetryHub> hubContext,
        IConfiguration config,
        ILogger<KafkaConsumerService> logger)
    {
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"]
                               ?? "localhost:9092",
            GroupId = "vehiclelink-gateway",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        using var consumer =
            new ConsumerBuilder<string, string>(consumerConfig).Build();

        consumer.Subscribe("vehicle-telemetry");

        _logger.LogInformation(
            "Kafka consumer started — listening on vehicle-telemetry");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(ct);

                var telemetry = JsonSerializer
                    .Deserialize<VehicleTelemetry>(result.Message.Value);

                if (telemetry is not null)
                {
                    // Broadcast to ALL connected dashboard clients
                    await _hubContext.Clients.All
                        .SendAsync("ReceiveTelemetry", telemetry, ct);

                    // Also send to vehicle-specific group
                    await _hubContext.Clients
                        .Group(telemetry.VehicleId)
                        .SendAsync("ReceiveTelemetry", telemetry, ct);

                    _logger.LogInformation(
                        "Pushed to dashboard: {VehicleId} speed={Speed}kmh",
                        telemetry.VehicleId, telemetry.SpeedKmh);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka consumer error");
                await Task.Delay(1000, ct);
            }
        }

        consumer.Close();
    }
}
