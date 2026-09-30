using System;
using System.Collections.Generic;
using System.Linq;

namespace FuelQ.Statistics;

/// <summary>Chi-Square Goodness-of-Fit test for exponential inter-arrival distribution.</summary>
public static class ChiSquareGoodnessOfFit
{
    public record Result(
        int    Bins,
        int    DegreesOfFreedom,
        double ChiSquareStat,
        double CriticalValue,
        double PValue,
        bool   H0Accepted,
        double EstimatedLambda,
        double[] Observed,
        double[] Expected,
        double[] BinEdges);

    /// <param name="records">Clean (non-outlier) records to test.</param>
    /// <param name="k">Number of equal-probability bins (default 10, requires k-1-p df).</param>
    /// <param name="alpha">Significance level (default 0.05).</param>
    public static Result Run(
        IList<ForecourtObservationRecord> records,
        int k = 10, double alpha = 0.05)
    {
        var clean = records.Where(r => !r.IsOutlier).ToList();
        int n     = clean.Count;

        double[] iats = [.. clean.Select(r => r.InterArrivalTime).OrderBy(x => x)];
        double mean   = iats.Length > 0 ? iats.Average() : 1.0;
        double lambda = mean > 0 ? 1.0 / mean : 1.0;

        // Equal-probability bins: each bin contains expected n/k observations
        // Bin edges from exponential quantiles
        var edges = new double[k + 1];
        edges[0] = 0;
        for (int i = 1; i < k; i++)
            edges[i] = -Math.Log(1.0 - (double)i / k) / lambda;
        edges[k] = double.PositiveInfinity;

        var observed = new double[k];
        foreach (double t in iats)
        {
            for (int b = 0; b < k; b++)
            {
                if (t >= edges[b] && (t < edges[b + 1] || double.IsPositiveInfinity(edges[b + 1])))
                {
                    observed[b]++;
                    break;
                }
            }
        }

        double expected    = (double)n / k;
        double chiSqStat   = 0;
        for (int b = 0; b < k; b++)
            chiSqStat += Math.Pow(observed[b] - expected, 2) / expected;

        int df            = k - 1 - 1; // p = 1 estimated parameter (λ)
        double critical   = ChiSquareCritical(df, alpha);
        double pValue     = 1.0 - ChiSquareCDF(chiSqStat, df);
        bool   h0         = chiSqStat < critical;

        var expectedArr = new double[k];
        for (int b = 0; b < k; b++) expectedArr[b] = expected;

        return new Result(k, df, chiSqStat, critical, pValue, h0, lambda, observed, expectedArr, edges);
    }

    // Wilson-Hilferty approximation for chi-square CDF
    public static double ChiSquareCDF(double x, int df)
    {
        if (x <= 0) return 0;
        double mu     = df;
        double h      = Math.Pow(x / mu, 1.0 / 3.0) - (1.0 - 2.0 / (9.0 * mu));
        double denom  = Math.Sqrt(2.0 / (9.0 * mu));
        double z      = h / denom;
        return NormalCDF(z);
    }

    // chi-square critical value via binary search
    public static double ChiSquareCritical(int df, double alpha)
    {
        double lo = 0, hi = 200;
        for (int i = 0; i < 60; i++)
        {
            double mid = (lo + hi) / 2;
            if (ChiSquareCDF(mid, df) < 1 - alpha)
                lo = mid;
            else
                hi = mid;
        }
        return Math.Round((lo + hi) / 2, 3);
    }

    private static double NormalCDF(double z)
    {
        return 0.5 * (1.0 + Erf(z / Math.Sqrt(2)));
    }

    private static double Erf(double z)
    {
        double t = 1.0 / (1.0 + 0.3275911 * Math.Abs(z));
        double poly = t * (0.254829592 + t * (-0.284496736 +
                     t * (1.421413741 + t * (-1.453152027 + t * 1.061405429))));
        double result = 1.0 - poly * Math.Exp(-z * z);
        return z < 0 ? -result : result;
    }
}
