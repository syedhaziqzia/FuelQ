namespace FuelQ.Simulation;

/// <summary>Configuration record for the ForecourtSimulator DES engine.</summary>
public record ForecourtConfig(
    int    NumServers,
    double ArrivalRate,       // λ vehicles/second
    double BikeServiceRate,   // μ_bike vehicles/second (mean: 1/35 s⁻¹)
    double CarServiceRate,    // μ_car  vehicles/second (mean: 1/85 s⁻¹)
    double SimDurationSeconds,
    double BikeFraction,      // 0.70
    int    RandomSeed)         // 2026
{
    public static ForecourtConfig Default => new(
        NumServers:          3,
        ArrivalRate:         2.8 / 60.0,   // 2.8 veh/min → per second
        BikeServiceRate:     1.0 / 35.0,
        CarServiceRate:      1.0 / 85.0,
        SimDurationSeconds:  3600,
        BikeFraction:        0.70,
        RandomSeed:          2026);
}
