namespace TransportationSystemApi.Services;

public class TrackingOptions
{
    // Shared secret for the ingest webhook. Empty (the dev default) = open:
    // POST api/tracking/ingest accepts any caller. When set, callers must send
    // it as the X-Tracking-Key header.
    public string IngestKey { get; set; } = string.Empty;

    // A vehicle whose latest fix is older than this reads as Offline.
    public int OfflineAfterMinutes { get; set; } = 15;

    // At/above this speed a (non-offline) vehicle reads as Moving, else Idle.
    public decimal MovingSpeedKph { get; set; } = 5m;

    // Positions kept per vehicle; older rows are pruned on ingest.
    public int MaxHotPositionsPerVehicle { get; set; } = 500;
}
