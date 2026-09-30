using System;

namespace FuelQ.Models;

/// <summary>
/// M/G/s Multi-server queue with Poisson arrivals and General service distribution.
/// Evaluated via the Allen-Cunneen approximation: Wq ≈ [(1 + Cs²)/2] × Wq(M/M/s).
/// </summary>
public static class MGsCalculator
{
    public static (QueueMetrics? Metrics, string? Error) Calculate(double lambda, double mu, int s, double serviceVariance)
    {
        if (lambda <= 0 || mu <= 0)
            return (null, "Both λ and μ must be positive numbers.");
        if (s < 1)
            return (null, "Number of servers s must be ≥ 1.");
        if (lambda >= s * mu)
            return (null, $"System is UNSTABLE: Total arrival rate λ ({lambda:F4}) ≥ Total service capacity s·μ ({s * mu:F4}). Utilization ρ ≥ 1.");
        if (serviceVariance < 0)
            serviceVariance = 0;

        var (mmsMetrics, err) = MMsCalculator.Calculate(lambda, mu, s);
        if (err != null || mmsMetrics == null)
            return (null, err);

        double meanS = 1.0 / mu;
        double csSq  = serviceVariance / (meanS * meanS);
        double cs    = Math.Sqrt(csSq);

        // Allen-Cunneen approximation
        double wq = ((1.0 + csSq) / 2.0) * mmsMetrics.Wq;
        double lq = lambda * wq;
        double w  = wq + meanS;
        double l  = lambda * w;

        string formula = "Allen-Cunneen: Wq ≈ [(1 + Cs²)/2] × Wq(M/M/s), Lq = λ·Wq, W = Wq + 1/μ, L = λ·W, P₀ = P₀(M/M/s), Pw = Pw(M/M/s)";

        var metrics = new QueueMetrics(
            Lambda: lambda,
            Mu: mu,
            S: s,
            Rho: mmsMetrics.Rho,
            TrafficIntensity: mmsMetrics.TrafficIntensity,
            Lq: lq,
            L: l,
            Wq: wq,
            W: w,
            P0: mmsMetrics.P0,
            Pwait: mmsMetrics.Pwait,
            Ca: 1.0,
            Cs: cs,
            ModelName: $"M/G/{s}",
            FormulaUsed: formula
        );

        return (metrics, null);
    }
}
