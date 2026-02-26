public class ChargingModel
{
  private readonly double[] socGrid;
  private readonly double[] tRef;
  private readonly double ds;


  public ChargingModel(double stepSize = 0.001)
  {
    ds = stepSize;

    int n = (int)((1.0 / ds) + 1);

    socGrid = new double[n];
    tRef = new double[n];

    BuildReferenceTable();
  }

  // ---------------------------------
  // Charging curve shape f(s)
  // Returns fraction of max power
  // ---------------------------------
  private double PowerFraction(double soc)
  {
    if (soc < 0.1)
    {
      return 0.5 + 5 * soc;          // ramp up
    }

    if (soc < 0.8)
    {
      return 1.0;                    // constant region
    }

    double taper = 1.0 - 3 * (soc - 0.8);
    return Math.Max(0.2, taper);       // taper down
  }

  private void BuildReferenceTable()
  {
    tRef[0] = 0.0;
    socGrid[0] = 0.0;
    for (int i = 0; i < socGrid.Length - 1; i++)
    {
      double s = i * ds;
      socGrid[i] = s;

      double powerFrac = PowerFraction(s);

      double dt = ds / powerFrac;

      tRef[i + 1] = tRef[i] + dt;
    }

    socGrid[^1] = 1.0;
  }

  public double GetChargingTimeHours(
      double socStart,
      double socEnd,
      double batteryCapacityKWh,
      double chargerPowerKW)
  {
    if (socEnd <= socStart)
      return 0.0;

    int i1 = (int)(socStart / ds);
    int i2 = (int)(socEnd / ds);

    i1 = Math.Clamp(i1, 0, tRef.Length - 1);
    i2 = Math.Clamp(i2, 0, tRef.Length - 1);

    double referenceTime = tRef[i2] - tRef[i1];


    return batteryCapacityKWh / chargerPowerKW * referenceTime;
  }
}
