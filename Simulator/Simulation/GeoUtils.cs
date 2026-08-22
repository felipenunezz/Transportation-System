namespace Simulator.Simulation;

public static class GeoUtils
{
    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusM = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusM * c;
    }
    public static ((double Lat, double Lon) Point, int NewIndex, bool ReachedEnd) WalkPolyline(
        List<(double Lat, double Lon)> shape, int index, double metersToTravel)
    {
        if (shape.Count == 0) return ((0, 0), index, true);
        if (index >= shape.Count - 1) return (shape[^1], shape.Count - 1, true);

        var remaining = metersToTravel;
        var i = index;
        while (i < shape.Count - 1)
        {
            var segmentLength = DistanceMeters(shape[i].Lat, shape[i].Lon, shape[i + 1].Lat, shape[i + 1].Lon);
            if (segmentLength >= remaining || segmentLength == 0)
                return (shape[i + 1], i + 1, i + 1 >= shape.Count - 1);

            remaining -= segmentLength;
            i++;
        }
        return (shape[^1], shape.Count - 1, true);
    }
}