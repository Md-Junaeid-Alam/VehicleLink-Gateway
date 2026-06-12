using Microsoft.AspNetCore.SignalR;

namespace VehicleLink.Gateway.Hubs;

public class TelemetryHub : Hub
{
    private readonly ILogger<TelemetryHub> _logger;

    public TelemetryHub(ILogger<TelemetryHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinVehicleGroup(string vehicleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, vehicleId);
        _logger.LogInformation(
            "Client {ConnectionId} joined group {VehicleId}",
            Context.ConnectionId, vehicleId);
    }

    public async Task LeaveVehicleGroup(string vehicleId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, vehicleId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Dashboard client connected: {ConnectionId}",
            Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Dashboard client disconnected: {ConnectionId}",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}