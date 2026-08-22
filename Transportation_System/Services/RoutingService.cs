using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Transportation_System.Models.Domain;

namespace Transportation_System.Services;

public class RoutingService (HttpClient httpClient, ILogger<RoutingService>  logger)
{
    //sets valhalla to use a bigger kind of vehicle instead of the default one.
    private const string Costing = "bus";
    
    //call for valhalla to create a route folowing the stops in order.
    public async Task<List<(double Latitude, double Longitude)>?> GetRouteShapeAsync (IReadOnlyList<(double Latitude, double Longitude)> _points) 
    {
        if (_points.Count < 2) return null;
        
        var request = new ValhallaRouteRequest(
            _points.Select(p => new ValhallaLocation(p.Latitude, p.Longitude)).ToList(),
            Costing);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("route", request);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(e, "valhalla routing request failed");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Valhalla returned {StatusCode} for a route request", response.StatusCode);
            return null;
        }

        ValhallaRouteResponse? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<ValhallaRouteResponse>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not parse Valhalla route response");
            return null;
        }

        if (result?.Trip?.Legs is not { Count: > 0 } legs) return null;

        var shape = new List<(double Lat, double Lon)>();
        foreach (var points in legs.Select(leg => DecodePolyline6(leg.Shape)))
        {
            shape.AddRange(shape.Count == 0 ? points : points.Skip(1));
        }

        return shape;
    }
    
    //conversion from polyline6 to 5, so that is compatible with MapLibre's Polyline algorithm. 
    private static List<(double Lat, double Lon)> DecodePolyline6(string encoded)
    {
        var poly = new List<(double, double)>();
        int index = 0, lat = 0, lon = 0;

        while (index < encoded.Length)
        {
            lat += DecodeNext(encoded, ref index);
            lon += DecodeNext(encoded, ref index);
            poly.Add((lat / 1e6, lon / 1e6));
        }

        return poly;
    }

    private static int DecodeNext(string encoded, ref int index)
    {
        int b, shift = 0, result = 0;
        do
        {
            b = encoded[index++] - 63;
            result |= (b & 0x1f) << shift;
            shift += 5;
        } while (b >= 0x20);
        return (result & 1) != 0 ? ~(result >> 1) : (result >> 1);
    }

    //Records that assing the structure needed by valhalla with JsonPropertyNames so that is easier to use in the code above. 
    private record ValhallaRouteRequest(
        [property: JsonPropertyName("locations")] List<ValhallaLocation> Locations,
        [property: JsonPropertyName("costing")] string Costing);

    private record ValhallaLocation(
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lon")] double Lon);

    private record ValhallaRouteResponse([property: JsonPropertyName("trip")] ValhallaTrip? Trip);
    private record ValhallaTrip([property: JsonPropertyName("legs")] List<ValhallaLeg>? Legs);
    private record ValhallaLeg([property: JsonPropertyName("shape")] string Shape);
}