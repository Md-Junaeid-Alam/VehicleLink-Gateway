namespace VehicleLink.Core.Models;

public class TelemetryLog
{
    public int Id { get; set; }
    public string VehicleId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKmh { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}