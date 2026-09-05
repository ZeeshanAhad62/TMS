using System.Text.Json;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

// Plain geometry helpers for geofence containment. Small scale (fleet ops,
// not GIS), so a spherical-earth haversine for circles and a planar
// ray-cast for polygons are accurate enough.
public static class GeoGeometry
{
    private const double EarthRadiusMeters = 6_371_000d;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static List<GeoPoint> ParsePolygon(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<GeoPoint>>(json, JsonOpts) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    // True when (lat,lng) is inside the geofence. Inactive or malformed
    // geofences never contain anything.
    public static bool Contains(Geofence g, double lat, double lng)
    {
        if (!g.IsActive) return false;

        if (g.Shape == GeofenceShape.Circle)
        {
            if (g.CenterLat is not decimal cLat || g.CenterLng is not decimal cLng || g.RadiusMeters is not decimal r)
                return false;
            return DistanceMeters((double)cLat, (double)cLng, lat, lng) <= (double)r;
        }

        var pts = ParsePolygon(g.PolygonJson);
        if (pts.Count < 3) return false;
        return PointInPolygon(lat, lng, pts);
    }

    private static bool PointInPolygon(double lat, double lng, List<GeoPoint> poly)
    {
        var inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            double yi = (double)poly[i].Lat, xi = (double)poly[i].Lng;
            double yj = (double)poly[j].Lat, xj = (double)poly[j].Lng;

            var intersect = ((yi > lat) != (yj > lat)) &&
                            (lng < (xj - xi) * (lat - yi) / (yj - yi) + xi);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180d;
}
