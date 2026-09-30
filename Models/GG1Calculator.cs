using System;

namespace FuelQ.Models;

/// <summary>
/// G/G/1 Single-server queue with General arrivals and General service distributions.
/// Evaluated via Kingman's approximation: Wq ≈ [(Ca² + Cs²)/2] × [ρ/(1−ρ)] × E[S].
/// </summary>
public static class GG1Calculator
{
    public static (QueueMetrics? Metrics, string? Error) Calculate(
        double lambda, double mu, double arrivalVariance, double serviceVariance)
    {
        if (lambda <= 0 || mu <= 0)
            return (null, "Both λ and μ must be positive numbers.");
        if (lambda >= mu)
            return (null, $"System is UNSTABLE: Arrival rate λ ({lambda:F4}) ≥ Service rate μ ({mu:F4}). Utilization ρ ≥ 1.");
        if (arrivalVariance < 0) arrivalVariance = 0;
        if (serviceVariance < 0) serviceVariance = 0;

        double meanA = 1.0 / lambda;
        double meanS = 1.0 / mu;

        double caSq  = arrivalVariance / (meanA * meanA);
        double csSq  = serviceVariance / (meanS * meanS);
        double ca    = Math.Sqrt(caSq);
        double cs    = Math.Sqrt(csSq);

        double rho = lambda / mu;
        double a   = rho;

        // Kingman's approximation for G/G/1
        double wq = ((caSq + csSq) / 2.0) * (rho / (1.0 - rho)) * meanS;
        double lq = lambda * wq;
        double w  = wq + meanS;
        double l  = lambda * w;
        double p0 = 1.0 - rho;
        double pw = rho;

        string formula = "Kingman: Wq ≈ [(Ca² + Cs²)/2] × [ρ/(1−ρ)] × E[S], Lq = λ·Wq, W = Wq + 1/μ, L = λ·W, P₀ = 1−ρ, Pw = ρ";

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
            Ca: ca,
            Cs: cs,
            ModelName: "G/G/1",
            FormulaUsed: formula
        );

        return (metrics, null);
    }
}
