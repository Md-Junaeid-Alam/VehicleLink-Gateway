using Confluent.Kafka;
using System.Text.Json;
using VehicleLink.Core.Models;

namespace VehicleLink.Gateway.Services;

public class KafkaProducerService : IKafkaProducerService
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(
        IConfiguration config,
        ILogger<KafkaProducerService> logger)
    {
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
                               ?? "localhost:9092"
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .Build();
    }

    public async Task PublishAsync(string topic, VehicleTelemetry telemetry)
    {
        var message = new Message<string, string>
        {
            Key = telemetry.VehicleId,
            Value = JsonSerializer.Serialize(telemetry)
        };

        var result = await _producer.ProduceAsync(topic, message);

        _logger.LogInformation(
            "Published to Kafka topic={Topic} partition={Partition} offset={Offset}",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }
}
