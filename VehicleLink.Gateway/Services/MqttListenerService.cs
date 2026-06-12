using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using MQTTnet;
using System.Buffers;
using VehicleLink.Core.Models;

namespace VehicleLink.Gateway.Services;

public class MqttListenerService : BackgroundService
{
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<MqttListenerService> _logger;

    public MqttListenerService(
        IKafkaProducerService kafkaProducer,
        ILogger<MqttListenerService> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        // Load CA cert
        var caChain = new X509Certificate2Collection();
        caChain.ImportFromPemFile("certs/ca.crt");

        // Load client cert from PEM files
        var certPem = File.ReadAllText("certs/gateway.crt");
        var keyPem = File.ReadAllText("certs/gateway.key");
        var rawCert = X509Certificate2.CreateFromPem(certPem, keyPem);
        var clientCert = X509CertificateLoader.LoadPkcs12(rawCert.Export(X509ContentType.Pfx),
        password: null);

        var clientCerts = new X509Certificate2Collection();
        clientCerts.Add(clientCert);

        var tlsOptions = new MqttClientTlsOptionsBuilder()
            .WithTrustChain(caChain)
            .WithClientCertificates(clientCerts)
            .WithIgnoreCertificateRevocationErrors()
            .Build();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 8883)
            .WithTlsOptions(tlsOptions)
            .Build();

        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                // v5 uses Payload as ReadOnlyMemory<byte>
                var payload = Encoding.UTF8.GetString(
           e.ApplicationMessage.Payload.ToArray());

                var telemetry = JsonSerializer
                    .Deserialize<VehicleTelemetry>(payload);

                if (telemetry is not null)
                {
                    _logger.LogInformation(
                        "Received from {VehicleId} speed={Speed}kmh event={Event}",
                        telemetry.VehicleId,
                        telemetry.SpeedKmh,
                        telemetry.EventType);

                    await _kafkaProducer.PublishAsync(
                        "vehicle-telemetry", telemetry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MQTT message");
            }
        };

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await mqttClient.ConnectAsync(mqttClientOptions, ct);

                await mqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder()
                        .WithTopic("v2x/telemetry/#")
                        .Build(), ct);

                _logger.LogInformation(
                    "MQTT connected — listening on v2x/telemetry/#");

                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "MQTT connection lost — retrying in 5s");
                await Task.Delay(5000, ct);
            }
        }

        if (mqttClient.IsConnected)
            await mqttClient.DisconnectAsync();
    }
}