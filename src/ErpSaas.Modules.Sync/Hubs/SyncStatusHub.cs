using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ErpSaas.Modules.Sync.Hubs;

[Authorize]
public sealed class SyncStatusHub : Hub
{
    public static string HubPath => "/hubs/sync-status";

    public async Task JoinShopGroup(string shopId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{shopId}");

    public async Task LeaveShopGroup(string shopId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"shop_{shopId}");
}

public static class SyncStatusNotifier
{
    public static async Task BroadcastDeviceStatusAsync(
        IHubContext<SyncStatusHub> hub,
        long shopId,
        string deviceId,
        bool isOnline,
        DateTime? lastSeenAtUtc = null)
    {
        await hub.Clients
            .Group($"shop_{shopId}")
            .SendAsync("DeviceStatusChanged", new
            {
                deviceId,
                isOnline,
                lastSeenAtUtc = lastSeenAtUtc ?? DateTime.UtcNow,
            });
    }

    public static async Task BroadcastSyncJobAsync(
        IHubContext<SyncStatusHub> hub,
        long shopId,
        string deploymentId,
        string status,
        int rowsTransferred,
        int rowsConflicted)
    {
        await hub.Clients
            .Group($"shop_{shopId}")
            .SendAsync("ReplicationJobUpdated", new
            {
                deploymentId,
                status,
                rowsTransferred,
                rowsConflicted,
                updatedAtUtc = DateTime.UtcNow,
            });
    }
}
