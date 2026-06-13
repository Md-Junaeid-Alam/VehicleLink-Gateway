using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using MQTTnet;
using VehicleLink.Core.Models;

Console.WriteLine("VehicleLink Vehicle Simulator starting...");

// Resolve certs path relative to solution root
var certsPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "certs"));

Console.WriteLine($"Loading certs from {certsPath}");

// Load CA cert
var caChain = new X509Certificate2Collection();
caChain.ImportFromPemFile(Path.Combine(certsPath, "ca.crt"));

// Load client cert + key
var certPem = File.ReadAllText(Path.Combine(certsPath, "client.crt"));
var keyPem = File.ReadAllText(Path.Combine(certsPath, "client.key"));
var rawCert = X509Certificate2.CreateFromPem(certPem, keyPem);
var clientCert = new X509Certificate2(
    rawCert.Export(X509ContentType.Pfx));

var clientCerts = new X509Certificate2Collection();
clientCerts.Add(clientCert);

// Build MQTT client
var mqttFactory = new MqttClientFactory();
using var mqttClient = mqttFactory.CreateMqttClient();

var tlsOptions = new MqttClientTlsOptionsBuilder()
    .WithTrustChain(caChain)
    .WithClientCertificates(clientCerts)
    .WithIgnoreCertificateRevocationErrors()
    .WithCertificateValidationHandler(_ => true)
    .Build();

var mqttOptions = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 8883)
    .WithTlsOptions(tlsOptions)
    .Build();

// Connect
await mqttClient.ConnectAsync(mqttOptions);
Console.WriteLine("Connected to Mosquitto broker on port 8883 via mTLS");
Console.WriteLine("Publishing telemetry every 1 second. Press Ctrl+C to stop.");
Console.WriteLine();

// Simulate multiple vehicles
var vehicles = new[]
{
    new { Id = "VH-001", BaseLat = 42.3314, BaseLon = -83.0458 },
    new { Id = "VH-002", BaseLat = 42.3400, BaseLon = -83.0500 },
    new { Id = "VH-003", BaseLat = 42.3250, BaseLon = -83.0400 }
};

var random = new Random();
var eventTypes = new[] { "NORMAL", "NORMAL", "NORMAL", "HAZARD", "EMERGENCY" };

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var vehicleIndex = 0;

while (!cts.Token.IsCancellationRequested)
{
    var vehicle = vehicles[vehicleIndex % vehicles.Length];
    vehicleIndex++;

    var telemetry = new VehicleTelemetry
    {
        VehicleId = vehicle.Id,
        Latitude = vehicle.BaseLat + random.NextDouble() * 0.01,
        Longitude = vehicle.BaseLon + random.NextDouble() * 0.01,
        SpeedKmh = Math.Round(random.NextDouble() * 120, 1),
        EventType = eventTypes[random.Next(eventTypes.Length)],
        Timestamp = DateTime.UtcNow
    };

    var payload = JsonSerializer.Serialize(telemetry);
    var topic = $"v2x/telemetry/{vehicle.Id}";

    var message = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(Encoding.UTF8.GetBytes(payload))
        .Build();

    await mqttClient.PublishAsync(message, cts.Token);

    Console.WriteLine(
        $"[{telemetry.Timestamp:HH:mm:ss}] {telemetry.VehicleId} " +
        $"lat={telemetry.Latitude:F4} lon={telemetry.Longitude:F4} " +
        $"speed={telemetry.SpeedKmh}kmh event={telemetry.EventType}");

    await Task.Delay(1000, cts.Token);
}

await mqttClient.DisconnectAsync();
Console.WriteLine("Simulator stopped.");