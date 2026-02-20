using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

public class Station
{
    public int Id { get; set; }
    public double Lon { get; set; }
    public double Lat { get; set; }
}

public class EvStationData
{
    public AddressInfo AddressInfo { get; set; }
}

public class AddressInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class SpatialGrid
{
    private readonly double _cellSize;
    private readonly Dictionary<(int x, int y), List<Station>> _cells = new();

    public SpatialGrid(double cellSize = 0.1)
    {
        _cellSize = cellSize;
    }

    public void Add(Station station)
    {
        var key = (x: (int)(station.Lon / _cellSize), y: (int)(station.Lat / _cellSize));
        if (!_cells.ContainsKey(key)) _cells[key] = new List<Station>();
        _cells[key].Add(station);
    }

    public int[] GetNearbyIndices(double lon, double lat, int maxStations = 50)
    {
        var key = (x: (int)(lon / _cellSize), y: (int)(lat / _cellSize));
        var candidates = new List<Station>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (_cells.TryGetValue((key.x + dx, key.y + dy), out var list))
                {
                    candidates.AddRange(list);
                }
            }
        }

        candidates.Sort((a, b) =>
        {
            double distA = (a.Lon - lon) * (a.Lon - lon) + (a.Lat - lat) * (a.Lat - lat);
            double distB = (b.Lon - lon) * (b.Lon - lon) + (b.Lat - lat) * (b.Lat - lat);
            return distA.CompareTo(distB);
        });

        int count = Math.Min(maxStations, candidates.Count);
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = candidates[i].Id;
        return indices;
    }
}

public unsafe class OSRMRouter : IDisposable
{
    private const string Lib = "osrm_wrapper";

    [DllImport(Lib)] private static extern IntPtr InitializeOSRM(string path);
    [DllImport(Lib)] private static extern void RegisterStations(IntPtr osrm, double[] coords, int numStations);
    [DllImport(Lib)] private static extern float* ComputeTableIndexed(IntPtr osrm, double evLon, double evLat, int[] indices, int numIndices, out int outSize);
    [DllImport(Lib)] private static extern void FreeMemory(IntPtr ptr);
    [DllImport(Lib)] private static extern void DeleteOSRM(IntPtr osrm);

    private readonly IntPtr _osrm;

    public OSRMRouter(string mapPath)
    {
        _osrm = InitializeOSRM(mapPath);
        if (_osrm == IntPtr.Zero) throw new Exception("OSRM init failed.");
    }

    public void InitStations(List<Station> stations)
    {
        double[] coords = new double[stations.Count * 2];
        for (int i = 0; i < stations.Count; i++)
        {
            stations[i].Id = i;
            coords[i * 2] = stations[i].Lon;
            coords[i * 2 + 1] = stations[i].Lat;
        }
        RegisterStations(_osrm, coords, stations.Count);
    }

    public float[] Query(double evLon, double evLat, int[] prunedIndices)
    {
        if (prunedIndices.Length == 0) return Array.Empty<float>();

        int size;
        float* ptr = ComputeTableIndexed(_osrm, evLon, evLat, prunedIndices, prunedIndices.Length, out size);
        if (ptr == null) return Array.Empty<float>();

        float[] result = new float[size];
        fixed (float* dest = result)
        {
            Buffer.MemoryCopy(ptr, dest, size * 4, size * 4);
        }

        FreeMemory((IntPtr)ptr);
        return result;
    }

    public void Dispose() => DeleteOSRM(_osrm);
}



public static class Program
{
    public static async Task Main()
    {
        string jsonPath = "denmark_ev_data_projected.json";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var evData = JsonSerializer.Deserialize<List<EvStationData>>(File.ReadAllText(jsonPath), options);

        var benchmarkStations = evData.Select((data, index) => new Station
        {
            Id = index,
            Lon = data.AddressInfo.Longitude,
            Lat = data.AddressInfo.Latitude
        }).ToList();

        int totalStations = benchmarkStations.Count;
        int[] allIndices = Enumerable.Range(0, totalStations).ToArray();

        string dockerBaseUrl = "http://localhost:5000/table/v1/car/";
        string stationsUrlSegment = string.Join(";", benchmarkStations.Select(s => $"{s.Lon:F6},{s.Lat:F6}"));

        using var router = new OSRMRouter("/home/mertz/Gemini/denmark-latest.osrm");
        router.InitStations(benchmarkStations);

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromMinutes(2);

        int nativeIterations = 1000;
        int dockerIterations = 10;
        var rnd = new Random();

        var evCoordinates = new (double Lon, double Lat)[nativeIterations];
        for (int i = 0; i < nativeIterations; i++)
        {
            evCoordinates[i] = (8.0 + rnd.NextDouble() * 4.5, 54.5 + rnd.NextDouble() * 3.0);
        }

        Console.WriteLine($"--- STRICT 1x{totalStations} MATRIX BENCHMARK ---");

        Console.WriteLine($"Executing Docker HTTP ({dockerIterations} iterations only due to extreme overhead)...");
        double dockerAvg = 0;
        try
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < dockerIterations; i++)
            {
                string fullUrl = $"{dockerBaseUrl}{evCoordinates[i].Lon:F6},{evCoordinates[i].Lat:F6};{stationsUrlSegment}?sources=0";
                using var resp = await http.GetAsync(fullUrl);
                resp.EnsureSuccessStatusCode();
            }
            sw.Stop();
            dockerAvg = sw.Elapsed.TotalMilliseconds / dockerIterations;
            Console.WriteLine($"DOCKER: {dockerAvg:F4}ms per query");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DOCKER FAILED: {ex.Message} (Likely exceeded OSRM max-table-size or URI length limits)");
        }

        Console.WriteLine($"Executing Native P/Invoke ({nativeIterations} iterations)...");
        var swNative = Stopwatch.StartNew();
        for (int i = 0; i < nativeIterations; i++)
        {
            float[] durations = router.Query(evCoordinates[i].Lon, evCoordinates[i].Lat, allIndices);
        }
        swNative.Stop();
        double nativeAvg = swNative.Elapsed.TotalMilliseconds / nativeIterations;
        Console.WriteLine($"NATIVE: {nativeAvg:F4}ms per query");

        if (dockerAvg > 0)
        {
            Console.WriteLine($"Performance Multiplier: {dockerAvg / nativeAvg:F2}x faster");
        }
    }
}
