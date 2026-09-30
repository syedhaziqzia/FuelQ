namespace FuelQ.Statistics;

/// <summary>Single row of empirical forecourt observation data (14 columns per brief spec).</summary>
public class ForecourtObservationRecord
{
    public string VehicleId          { get; set; } = string.Empty;
    public string VehicleType        { get; set; } = "Bike";       // Bike | Car
    public string ArrivalTime        { get; set; } = string.Empty; // HH:mm:ss
    public double InterArrivalTime   { get; set; }                  // seconds
    public string ServiceStartTime   { get; set; } = string.Empty; // HH:mm:ss
    public double ServiceDuration    { get; set; }                  // seconds
    public double DispensingDuration { get; set; }                  // seconds
    public string PaymentMethod      { get; set; } = "Cash";       // Cash | Card_POS | QR_Pay
    public string FuelGrade          { get; set; } = "Petrol";     // Petrol | Hi-Octane
    public string ExtraService       { get; set; } = "None";       // None | Air | Water | Oil
    public bool   ReceiptIssued      { get; set; }
    public string ServiceEndTime     { get; set; } = string.Empty; // HH:mm:ss
    public double TotalSystemTime    { get; set; }                  // seconds
    public bool   IsOutlier          { get; set; }

    // Formatted display helpers
    public string ReceiptStr => ReceiptIssued ? "Yes" : "No";
    public string OutlierStr => IsOutlier ? "⚠ Yes" : "No";
}
