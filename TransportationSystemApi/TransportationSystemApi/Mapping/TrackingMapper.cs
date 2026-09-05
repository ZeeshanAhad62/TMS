using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;
using TransportationSystemApi.Services;

namespace TransportationSystemApi.Mapping;

public static class TrackingMapper
{
    public static VehiclePositionDto ToDto(VehiclePosition p) => new()
    {
        Id = p.Id,
        VehicleId = p.VehicleId,
        Latitude = p.Latitude,
        Longitude = p.Longitude,
        SpeedKph = p.SpeedKph,
        Heading = p.Heading,
        Ignition = p.Ignition,
        DeviceTimeUtc = p.DeviceTimeUtc,
        Source = p.Source
    };

    public static LiveVehicleDto ToLiveDto(Vehicle v, VehiclePosition? latest, TrackingOptions opts, DateTime nowUtc)
    {
        var dto = new LiveVehicleDto
        {
            VehicleId = v.Id,
            VehicleCode = v.VehicleCode,
            RegistrationNumber = v.RegistrationNumber,
            CurrentStatus = v.CurrentStatus,
            HasPosition = latest is not null
        };

        if (latest is null)
        {
            dto.MovementState = VehicleMovementState.Offline;
            return dto;
        }

        var ageMinutes = (nowUtc - latest.DeviceTimeUtc).TotalMinutes;
        dto.Latitude = latest.Latitude;
        dto.Longitude = latest.Longitude;
        dto.SpeedKph = latest.SpeedKph;
        dto.Heading = latest.Heading;
        dto.Ignition = latest.Ignition;
        dto.DeviceTimeUtc = latest.DeviceTimeUtc;
        dto.MinutesSinceReport = Math.Round(ageMinutes, 1);

        dto.MovementState = ageMinutes > opts.OfflineAfterMinutes
            ? VehicleMovementState.Offline
            : (latest.SpeedKph ?? 0m) >= opts.MovingSpeedKph
                ? VehicleMovementState.Moving
                : VehicleMovementState.Idle;

        return dto;
    }

    public static GeofenceListItemDto ToListItemDto(Geofence g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Shape = g.Shape,
        IsActive = g.IsActive,
        CenterLat = g.CenterLat,
        CenterLng = g.CenterLng,
        RadiusMeters = g.RadiusMeters,
        PointCount = g.Shape == GeofenceShape.Polygon ? GeoGeometry.ParsePolygon(g.PolygonJson).Count : 0
    };

    public static GeofenceDetailDto ToDetailDto(Geofence g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Shape = g.Shape,
        CenterLat = g.CenterLat,
        CenterLng = g.CenterLng,
        RadiusMeters = g.RadiusMeters,
        PolygonJson = g.PolygonJson,
        IsActive = g.IsActive,
        Notes = g.Notes,
        CreatedAt = g.CreatedAt,
        UpdatedAt = g.UpdatedAt
    };

    public static void ApplyUpsert(Geofence g, GeofenceUpsertDto dto)
    {
        g.Name = dto.Name.Trim();
        g.Shape = dto.Shape;
        g.IsActive = dto.IsActive;
        g.Notes = dto.Notes;

        if (dto.Shape == GeofenceShape.Circle)
        {
            g.CenterLat = dto.CenterLat;
            g.CenterLng = dto.CenterLng;
            g.RadiusMeters = dto.RadiusMeters;
            g.PolygonJson = null;
        }
        else
        {
            g.PolygonJson = dto.PolygonJson;
            g.CenterLat = null;
            g.CenterLng = null;
            g.RadiusMeters = null;
        }
    }

    public static GeofenceEventDto ToEventDto(GeofenceEvent e) => new()
    {
        Id = e.Id,
        GeofenceId = e.GeofenceId,
        GeofenceName = e.Geofence?.Name ?? string.Empty,
        VehicleId = e.VehicleId,
        VehicleCode = e.Vehicle?.VehicleCode ?? string.Empty,
        RegistrationNumber = e.Vehicle?.RegistrationNumber ?? string.Empty,
        EventType = e.EventType,
        OccurredAtUtc = e.OccurredAtUtc,
        Latitude = e.Latitude,
        Longitude = e.Longitude
    };
}
