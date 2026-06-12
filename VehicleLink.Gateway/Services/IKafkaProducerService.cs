using VehicleLink.Core.Models;
namespace VehicleLink.Gateway.Services
{
    public interface IKafkaProducerService
    {
        Task PublishAsync(string topic, VehicleTelemetry telemetry);
    }
}
