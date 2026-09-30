using System;

namespace FuelQ.Models;

/// <summary>
/// M/G/1 Single-server queue with Poisson arrivals and General service distribution.
/// Evaluated via the exact Pollaczek-Khinchine (P-K) formula.
/// </summary>
public static class MG1Calculator
{
    public static (QueueMetrics? Metrics, string? Error) Calculate(double lambda, double mu, double serviceVariance)
    {
        if (lambda <= 0 || mu <= 0)
            return (null, "Both λ and μ must be positive numbers.");
        if (lambda >= mu)
            return (null, $"System is UNSTABLE: Arrival rate λ ({lambda:F4}) ≥ Service rate μ ({mu:F4}). Utilization ρ ≥ 1.");
        if (serviceVariance < 0)
            serviceVariance = 0;

        double meanS = 1.0 / mu;
        double es2   = (meanS * meanS) + serviceVariance; // E[S²] = E[S]² + Var(S)
        double cs    = Math.Sqrt(serviceVariance) / meanS;

        double rho = lambda / mu;
        double a   = rho;

        // Pollaczek-Khinchine Formula: Wq = λ·E[S²] / [2·(1−ρ)]
        double wq = (lambda * es2) / (2.0 * (1.0 - rho));
        double lq = lambda * wq;
        double w  = wq + meanS;
        double l  = lambda * w;
        double p0 = 1.0 - rho;
        double pw = rho;

        string formula = "Pollaczek-Khinchine: Wq = λ·E[S²] / [2(1−ρ)], Lq = λ·Wq, W = Wq + 1/μ, L = λ·W, P₀ = 1−ρ, Pw = ρ";

        var metrics = new QueueMetrics(
            Lambda: lambda,
            Mu: mu,
            S: 1,
            Rho: rho,
            TrafficIntensity: a,
            Lq: lq,
            L: l,
            Wq: wq,
            W: w,
            P0: p0,
            Pwait: pw,
            Ca: 1.0,
            Cs: cs,
            ModelName: "M/G/1",
            FormulaUsed: formula
        );

        return (metrics, null);
    }
}
