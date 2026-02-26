using System.Diagnostics;
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
  required public AddressInfo AddressInfo { get; set; }
}

public class AddressInfo
{
  public double Latitude { get; set; }
  public double Longitude { get; set; }
}

public unsafe class OSRMRouter : IDisposable
{
  private const string Lib = "osrm_wrapper";

  [DllImport(Lib)] private static extern IntPtr InitializeOSRM(string path);
  [DllImport(Lib)] private static extern void RegisterStations(IntPtr osrm, double[] coords, int numStations);
  [DllImport(Lib)]
  private static extern float* ComputeTableIndexed(
      IntPtr osrm,
      double evLon,
      double evLat,
      int[] indices,
      int numIndices,
      out int outSize);

  [DllImport(Lib)] private static extern void FreeMemory(IntPtr ptr);
  [DllImport(Lib)] private static extern void DeleteOSRM(IntPtr osrm);

  private readonly IntPtr _osrm;

  public OSRMRouter(string mapPath)
  {
    _osrm = InitializeOSRM(mapPath);
    if (_osrm == IntPtr.Zero)
      throw new Exception("OSRM init failed.");
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

  public float[] QueryStations(double evLon, double evLat, int[] indices)
  {
    if (indices.Length == 0)
      return [];

    float* ptr = ComputeTableIndexed(_osrm, evLon, evLat, indices, indices.Length, out int size);

    if (ptr == null || size <= 0)
      return [];

    float[] result = new float[size];

    fixed (float* dest = result)
    {
      Buffer.MemoryCopy(ptr, dest, size * sizeof(float), size * sizeof(float));
    }

    FreeMemory((IntPtr)ptr);
    return result;
  }

  public void Dispose()
  {
    DeleteOSRM(_osrm);
  }
}

public static class Program
{
  public static void Main()
  {
    string jsonPath = "denmark_ev_data_projected.json";
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    var evData = JsonSerializer.Deserialize<List<EvStationData>>(
        File.ReadAllText(jsonPath),
        options) ?? [];

    int targetStationCount = evData.Count; // Use all stations from the JSON file

    var stations = evData
        .Take(targetStationCount)
        .Select((data, index) => new Station
        {
          Id = index,
          Lon = data.AddressInfo.Longitude,
          Lat = data.AddressInfo.Latitude,
        })
        .ToList();

    int[] indices = Enumerable.Range(0, stations.Count).ToArray();

    using var router = new OSRMRouter(
        "/home/mertz/Coding/OSRM_FFI/EVSimulation/data/denmark-latest.osrm");

    router.InitStations(stations);

    var evCoordinates = new (double Lon, double Lat)[]
    {
            (9.9410, 57.2706),  // Brønderslev
            (9.9217, 57.0488),  // Aalborg
            (10.0364, 56.4606), // Randers
            (10.2039, 56.1629), // Aarhus
            (12.5683, 55.6761), // København
    };

    var minusOneStations = new List<(int EvIndex, Station Station)>();
    uint numberOfMinus1 = 0;

    for (int i = 0; i < evCoordinates.Length; i++)
    {
      var (lon, lat) = evCoordinates[i];

      float[] durations = router.QueryStations(lon, lat, indices);

      Console.WriteLine($"Query {i + 1} ({lon}, {lat}):");

      for (int j = 0; j < durations.Length; j++)
      {
        if (durations[j] < 0)
        {
          minusOneStations.Add((i, stations[j]));
          numberOfMinus1++;
        }
      }
    }

    Console.WriteLine($"Total number of -1 durations: {numberOfMinus1}");
    Console.WriteLine("EV coordinate → stations with -1 durations:");

    foreach (var group in minusOneStations.GroupBy(e => e.EvIndex))
    {
      var (Lon, Lat) = evCoordinates[group.Key];
      Console.WriteLine($"EV {group.Key} ({Lat}, {Lon}):");
      foreach (var entry in group)
      {
        var s = entry.Station;
        Console.WriteLine($"  Station {s.Id}: ({s.Lat}, {s.Lon})");
      }
    }



    var sw = Stopwatch.StartNew();
    var chargingModel = new ChargingModel(0.0001);
    sw.Stop();

    Console.WriteLine($"Build time: {sw.Elapsed.TotalMilliseconds:F3} ms");

    //---------------------------------
    // USER EXAMPLE
    //---------------------------------
    double socStart = 0.05;
    double socEnd = 1;
    double capacity = 83.9;
    double charger = 205;

    double hours = chargingModel.GetChargingTimeHours(
        socStart,
        socEnd,
        capacity,
        charger);

    Console.WriteLine($"Charging time: {hours * 60:F1} minutes");

    //---------------------------------
    // SINGLE CALL BENCHMARK
    //---------------------------------
    sw.Restart();

    for (int i = 0; i < 1_000_000; i++)
    {
      chargingModel.GetChargingTimeHours(
          socStart, socEnd, capacity, charger);
    }

    sw.Stop();

    Console.WriteLine(
        $"1M calls: {sw.Elapsed.TotalMilliseconds:F2} ms");

    //---------------------------------
    // LARGE PERFORMANCE TEST
    //---------------------------------
    const int N = 10_000_000;

    sw.Restart();

    double sum = 0; // prevents optimizer removing calls

    for (int i = 0; i < N; i++)
    {
      sum += chargingModel.GetChargingTimeHours(
          socStart, socEnd, capacity, charger);
    }

    sw.Stop();

    double nsPerCall =
        sw.Elapsed.TotalMilliseconds * 1_000_000 / N;

    Console.WriteLine($"10M calls: {sw.Elapsed.TotalSeconds:F3} s");
    Console.WriteLine($"Avg time per call: {nsPerCall:F1} ns");

    Console.WriteLine(sum); // anti-optimization
  }
}
