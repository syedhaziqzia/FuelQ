using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FuelQ.Distributions;

namespace FuelQ.Statistics;

/// <summary>
/// Manages the Shell Maskan empirical dataset:
/// auto-generates a realistic 320-row baseline CSV on first run,
/// and supports loading external CSVs via file path.
/// </summary>
public static class DatasetIngestionEngine
{
    private static readonly string CsvRelativePath = Path.Combine("Data", "Shell_Maskan_Queue_Data.csv");

    public static string GetCsvPath() =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CsvRelativePath);

    /// <summary>
    /// Generates 320 synthetic but realistic observation rows and saves them to disk.
    /// Fleet mix: 70% Motorbikes (25–45 s cash) / 30% Passenger Cars (60–120 s POS/cash blend).
    /// </summary>
    public static List<ForecourtObservationRecord> GenerateAndSave(int seed = 2026)
    {
        var records = Generate(seed);
        SaveToDisk(records);
        return records;
    }

    /// <summary>Generate 320 records in memory without saving.</summary>
    public static List<ForecourtObservationRecord> Generate(int seed = 2026)
    {
        var rng    = new Random(seed);
        var exp    = new ExponentialDistribution(2.8 / 60.0); // λ = 2.8 veh/min → per-second rate
        var records = new List<ForecourtObservationRecord>(320);

        double clockSec = 17 * 3600.0; // session starts 17:00:00 (peak hour)

        for (int i = 0; i < 320; i++)
        {
            double iat = Math.Max(0.5, exp.Sample(rng));
            clockSec += iat;

            bool   isBike = rng.NextDouble() < 0.70;
            string vtype  = isBike ? "Bike" : "Car";

            double svcDuration;
            string payment;

            if (isBike)
            {
                svcDuration = 25.0 + rng.NextDouble() * 20.0; // 25–45 s
                payment     = "Cash";
            }
            else
            {
                svcDuration = 60.0 + rng.NextDouble() * 60.0; // 60–120 s
                double r    = rng.NextDouble();
                payment     = r < 0.50 ? "Cash" : r < 0.78 ? "Card_POS" : "QR_Pay";
            }

            double dispDuration = Math.Round(svcDuration * 0.62, 1);
            double queueWait    = Math.Round(rng.NextDouble() * 28.0, 1);
            double svcStart     = clockSec + queueWait;
            double svcEnd       = svcStart + svcDuration;
            double totalSystem  = svcEnd - clockSec;

            string fuelGrade  = rng.NextDouble() < 0.72 ? "Petrol" : "Hi-Octane";
            string extra      = (payment != "Cash") && rng.NextDouble() < 0.28
                                    ? new[] { "Air", "Water", "Oil" }[rng.Next(3)]
                                    : "None";
            bool receipt      = payment != "Cash";

            records.Add(new ForecourtObservationRecord
            {
                VehicleId          = $"VEH-{i + 1:D3}",
                VehicleType        = vtype,
                ArrivalTime        = SecToHHMMSS(clockSec),
                InterArrivalTime   = Math.Round(iat, 1),
                ServiceStartTime   = SecToHHMMSS(svcStart),
                ServiceDuration    = Math.Round(svcDuration, 1),
                DispensingDuration = dispDuration,
                PaymentMethod      = payment,
                FuelGrade          = fuelGrade,
                ExtraService       = extra,
                ReceiptIssued      = receipt,
                ServiceEndTime     = SecToHHMMSS(svcEnd),
                TotalSystemTime    = Math.Round(totalSystem, 1),
                IsOutlier          = false
            });
        }

        return records;
    }

    /// <summary>Load records from an absolute CSV file path. Returns empty list on error.</summary>
    /// <param name="path">Absolute path to the CSV file. Must exist and be a .csv file.</param>
    public static List<ForecourtObservationRecord> LoadFromCsv(string path)
    {
        var records = new List<ForecourtObservationRecord>();

        if (string.IsNullOrWhiteSpace(path))
            return records;

        // Security: only allow .csv extension to prevent loading arbitrary files
        if (!string.Equals(System.IO.Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            return records;

        if (!File.Exists(path)) return records;

        bool isHeader = true;
        int  lineNum  = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNum++;
            if (isHeader) { isHeader = false; continue; }
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 14) continue;

            try
            {
                records.Add(new ForecourtObservationRecord
                {
                    VehicleId          = cols[0].Trim(),
                    VehicleType        = cols[1].Trim(),
                    ArrivalTime        = cols[2].Trim(),
                    InterArrivalTime   = ParseD(cols[3]),
                    ServiceStartTime   = cols[4].Trim(),
                    ServiceDuration    = ParseD(cols[5]),
                    DispensingDuration = ParseD(cols[6]),
                    PaymentMethod      = cols[7].Trim(),
                    FuelGrade          = cols[8].Trim(),
                    ExtraService       = cols[9].Trim(),
                    ReceiptIssued      = cols[10].Trim().Equals("True", StringComparison.OrdinalIgnoreCase) ||
                                        cols[10].Trim().Equals("Yes",  StringComparison.OrdinalIgnoreCase),
                    ServiceEndTime     = cols[11].Trim(),
                    TotalSystemTime    = ParseD(cols[12]),
                    IsOutlier          = false
                });
            }
            catch (FormatException)
            {
                // Skip malformed rows (column value cannot be parsed)
                System.Diagnostics.Debug.WriteLine($"[DatasetIngestionEngine] Skipping malformed row {lineNum}.");
            }
        }
        return records;
    }

    /// <summary>Persist records to the Data/ folder as CSV.</summary>
    public static void SaveToDisk(List<ForecourtObservationRecord> records)
    {
        string path = GetCsvPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var sw = new StreamWriter(path, append: false);
        sw.WriteLine("VehicleId,VehicleType,ArrivalTime,InterArrivalTime,ServiceStartTime," +
                     "ServiceDuration,DispensingDuration,PaymentMethod,FuelGrade,ExtraService," +
                     "ReceiptIssued,ServiceEndTime,TotalSystemTime,IsOutlier");
        foreach (var r in records)
            sw.WriteLine(string.Join(",",
                r.VehicleId, r.VehicleType, r.ArrivalTime,
                r.InterArrivalTime.ToString(CultureInfo.InvariantCulture),
                r.ServiceStartTime,
                r.ServiceDuration.ToString(CultureInfo.InvariantCulture),
                r.DispensingDuration.ToString(CultureInfo.InvariantCulture),
                r.PaymentMethod, r.FuelGrade, r.ExtraService,
                r.ReceiptIssued, r.ServiceEndTime,
                r.TotalSystemTime.ToString(CultureInfo.InvariantCulture),
                r.IsOutlier));
    }

    private static string SecToHHMMSS(double totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(totalSeconds % 86400);
        return ts.ToString(@"hh\:mm\:ss");
    }

    private static double ParseD(string s) =>
        double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : 0;
}
