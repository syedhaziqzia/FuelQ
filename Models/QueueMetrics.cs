using System;

namespace FuelQ.Models;

/// <summary>
/// Unified immutable metrics record matching the PSO / Shell Maskan simulator outputs.
/// </summary>
public record QueueMetrics(
    double Lambda,
    double Mu,
    int S,
    double Rho,
    double TrafficIntensity,
    double Lq,
    double L,
    double Wq,
    double W,
    double P0,
    double Pwait,
    double Ca = 1.0,
    double Cs = 1.0,
    string ModelName = "M/M/1",
    string FormulaUsed = "")
{
    public static readonly QueueMetrics Empty = new(0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0);

    /// <summary>State probability P(n customers in system) for M/M/1 or M/M/s.</summary>
    public double Pn(int n)
    {
        if (n < 0 || Rho >= 1.0) return 0;
        if (S == 1)
        {
            // M/M/1: P(n) = (1-ρ)·ρⁿ
            return (1.0 - Rho) * Math.Pow(Rho, n);
        }
        else
        {
            // M/M/s
            double a = TrafficIntensity;
            if (n < S)
                return P0 * Math.Pow(a, n) / Factorial(n);
            else
                return P0 * Math.Pow(a, n) / (Factorial(S) * Math.Pow(S, n - S));
        }
    }

    public static double Factorial(int n)
    {
        if (n <= 1) return 1.0;
        double f = 1.0;
        for (int i = 2; i <= n; i++) f *= i;
        return f;
    }
}
