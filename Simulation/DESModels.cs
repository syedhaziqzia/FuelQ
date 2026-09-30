using System.Collections.Generic;

namespace FuelQ.Simulation;

public enum TimeUnitType { Seconds, Minutes, Hours }
public enum DistType { Exponential, Uniform, Normal, Gamma }

public record DistConfig(
    DistType Type,
    double Param1 = 0, // Mean, or Min, or Shape (k)
    double Param2 = 0  // StdDev, or Max, or Scale (theta)
);

public record DESSimConfig(
    int NumServers,
    TimeUnitType TimeUnit,
    int CustomerLimit,
    double DurationLimit,
    int RandomSeed,
    DistConfig ArrivalDist,
    DistConfig ServiceDist
);

public class CustomerEventRow
{
    public int CustomerNumber { get; set; }
    public double ArrivalRandom { get; set; }
    public double InterarrivalTime { get; set; }
    public double ArrivalTime { get; set; }
    public double ServiceRandom { get; set; }
    public double ServiceTime { get; set; }
    public double TimeServiceBegins { get; set; }
    public double TimeServiceEnds { get; set; }
    public double WaitingTime { get; set; }      // Wq
    public double TimeInSystem { get; set; }     // W
    public int QueueLengthOnArrival { get; set; }
    public int AssignedServer { get; set; }
    public double IdleTimeOfServer { get; set; }
}

public class HourlySummaryRow
{
    public string HourPeriod { get; set; } = string.Empty;
    public int Arrivals { get; set; }
    public int Departures { get; set; }
    public double AvgWait { get; set; }
    public double AvgQueueLength { get; set; }
    public double PeakQueueLength { get; set; }
    public double ServerUtilization { get; set; }
}

public class TheoreticalComparisonRow
{
    public string MetricName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string SimulatedValue { get; set; } = string.Empty;
    public string TheoreticalValue { get; set; } = string.Empty;
    public string ErrorDelta { get; set; } = string.Empty;
}

public record DESSimulationResult(
    List<CustomerEventRow> CustomerRows,
    List<HourlySummaryRow> HourlyRows,
    List<TheoreticalComparisonRow> ComparisonRows,
    List<(double Time, int QLen)> QueueTimeline,
    List<(double WaitBin, int Count, double Pct)> WaitDistribution,
    double AvgInterarrivalTime,
    double AvgServiceTime,
    double AvgWaitingTime,
    double AvgSystemTime,
    double AvgQueueLength,
    double AvgSystemLength,
    int MaxQueueLength,
    double MaxWaitingTime,
    double ServerUtilization,
    double Throughput,
    int TotalCustomersServed,
    double TotalSimTime,
    string TimeUnitStr
);
