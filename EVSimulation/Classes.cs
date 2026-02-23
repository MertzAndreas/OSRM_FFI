// Entities

using Time = int;


class Stationn(ushort id,
               string name,
               string address,
               Position position,
               List<Charger> chargers)
{
  private readonly ushort id = id;
  private readonly string name = name;
  private readonly string address = address;
  private readonly Position position = position;
  private List<Charger> chargers = chargers;
}

class Charger(int id, int PowerKW, Socket socket)
{
  private readonly int id = id;
  private readonly int PowerKW = PowerKW;
  private readonly Socket socket = socket;
}

class EV(uint id, Position position, Battery battery, Preferences preferences)
{
  public readonly uint id = id;
  private Position position = position;
  private Battery battery = battery;
  public readonly Preferences Preferences = preferences;

  // Methods that update battery
}



class Preferences(float priceSensitivity)
{
  public readonly float priceSensitivity = priceSensitivity;
}

class Battery(float capacity, float maxChargeRate, float currentCharge, Socket socket)
{
  public readonly float capacity = capacity;
  public readonly float maxChargeRate = maxChargeRate;
  public float CurrentCharge { get; } = currentCharge;
  public readonly Socket Socket = socket;


}

class Journey(Time depature, Path path)
{
  public readonly Time depature = depature;
  public required Path Path { get; set; } = path;

  //MAKE ETA METHOD
}

class Path(List<Position> waypoints)
{
  public List<Position> Waypoints { get; } = waypoints;
}

enum Socket
{
  CHADEMO,
  CCS,
  Type2,
  Tesla_ModelSX,
  Tesla_Model3
}


class City(string name, float spawnChance, Position position)
{
  public readonly string name = name;
  public readonly float spawnChance = spawnChance;
  public readonly Position position = position;
}

struct Position(double longitude, double latitude)
{
  public double longitude = longitude;
  public double latitude = latitude;
}




// Event
// Scheduling

interface IEvent
{

}

public readonly record struct ReservationRequest(uint EVId, ushort StationId, Time Time) : IEvent;
public readonly record struct CancelRequest(uint EVId, ushort StationId, Time Time) : IEvent;
public readonly record struct ArriveAtStation(uint EVId, ushort StationId, Time Time) : IEvent;
public readonly record struct StartCharging(uint EVId, int ChargerId, Time Time) : IEvent;
public readonly record struct EndCharging(uint EVId, int ChargerId, Time Time) : IEvent;
public readonly record struct ArriveAtDestination(uint EVId, Time Time) : IEvent;

