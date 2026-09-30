namespace FuelQ.Simulation;

/// <summary>Per-vehicle entity tracking all timing and routing fields.</summary>
public class VehicleEntity
{
    public int    Id            { get; init; }
    public string VehicleType  { get; init; } = "Bike";  // "Bike" | "Car"
    public string PaymentBranch{ get; init; } = "4B";    // "4B" (Cash) | "4A5" (POS/receipt)
    public string PaymentMethod{ get; init; } = "Cash";

    public double ArrivalTime     { get; set; }
    public double ServiceStart    { get; set; }
    public double ServiceEnd      { get; set; }
    public double ServiceDuration { get; set; }
    public int    AssignedPump    { get; set; }

    public double WaitingTime    => ServiceStart - ArrivalTime;
    public double SystemTime     => ServiceEnd   - ArrivalTime;
}
