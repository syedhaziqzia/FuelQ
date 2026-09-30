using System;
using System.Collections.Generic;
using System.Linq;

namespace FuelQ.Statistics;

/// <summary>IQR-based outlier detection for empirical forecourt records.</summary>
public static class OutlierDetector
{
    /// <summary>
    /// Flags records whose ServiceDuration or InterArrivalTime lie outside
    /// [Q1 − 1.5·IQR, Q3 + 1.5·IQR]. Modifies records in-place and returns
    /// the count of outliers flagged.
    /// </summary>
    public static int Detect(IList<ForecourtObservationRecord> records)
    {
        if (records.Count == 0) return 0;

        // Reset flags
        foreach (var r in records) r.IsOutlier = false;

        // Compute IQR fences for ServiceDuration
        double[] svc = [.. records.Select(r => r.ServiceDuration).OrderBy(x => x)];
        (double svcLow, double svcHigh) = Fences(svc);

        // Compute IQR fences for InterArrivalTime
        double[] iat = [.. records.Select(r => r.InterArrivalTime).OrderBy(x => x)];
        (double iatLow, double iatHigh) = Fences(iat);

        int count = 0;
        foreach (var r in records)
        {
            if (r.ServiceDuration < svcLow || r.ServiceDuration > svcHigh ||
                r.InterArrivalTime < iatLow || r.InterArrivalTime > iatHigh)
            {
                r.IsOutlier = true;
                count++;
            }
        }
        return count;
    }

    public static (double Q1, double Q3, double IQR, double Lower, double Upper)
        ComputeFences(double[] sorted)
    {
        (double lo, double hi) = Fences(sorted);
        double q1  = Percentile(sorted, 25);
        double q3  = Percentile(sorted, 75);
        double iqr = q3 - q1;
        return (q1, q3, iqr, lo, hi);
    }

    private static (double lower, double upper) Fences(double[] sorted)
    {
        double q1  = Percentile(sorted, 25);
        double q3  = Percentile(sorted, 75);
        double iqr = q3 - q1;
        return (q1 - 1.5 * iqr, q3 + 1.5 * iqr);
    }

    private static double Percentile(double[] sorted, double pct)
    {
        if (sorted.Length == 0) return 0;
        double idx = (pct / 100.0) * (sorted.Length - 1);
        int lo = (int)Math.Floor(idx);
        int hi = (int)Math.Ceiling(idx);
        if (lo == hi) return sorted[lo];
        return sorted[lo] * (hi - idx) + sorted[hi] * (idx - lo);
    }
}
