namespace VehicleLink.Core.Models;

public class VehicleTelemetry
{
    public string VehicleId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKmh { get; set; }
    public string EventType { get; set; } 
    public DateTime Timestamp { get; set; }
}