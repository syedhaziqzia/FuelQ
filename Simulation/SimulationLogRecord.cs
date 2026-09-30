namespace FuelQ.Simulation;

/// <summary>Single event-log entry produced by the DES engine.</summary>
public class SimulationLogRecord
{
    public string ClockTime   { get; set; } = string.Empty;   // HH:mm:ss
    public string EventType   { get; set; } = string.Empty;   // Arrival | ServiceStart | ServiceEnd
    public string VehicleId   { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string PumpId      { get; set; } = "—";
    public int    QueueLength { get; set; }
    public string PaymentPath { get; set; } = string.Empty;   // 4B-Cash | 4A-POS
    public string Notes       { get; set; } = string.Empty;
}
