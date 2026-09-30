using System;

namespace FuelQ.Models;

/// <summary>
/// M/M/s Multi-server Markovian queuing model using exact Erlang-C formulas.
/// </summary>
public static class MMsCalculator
{
    public static (QueueMetrics? Metrics, string? Error) Calculate(double lambda, double mu, int s)
    {
        if (lambda <= 0 || mu <= 0)
            return (null, "Both λ and μ must be positive numbers.");
        if (s < 1)
            return (null, "Number of servers s must be ≥ 1.");
        if (s > 50)
            return (null, "Number of servers s must be ≤ 50 (Factorial overflow above this limit).");
        if (lambda >= s * mu)
            return (null, $"System is UNSTABLE: Total arrival rate λ ({lambda:F4}) ≥ Total service capacity s·μ ({s * mu:F4}). Utilization ρ ≥ 1.");

        double a   = lambda / mu;             // Traffic intensity / offered load
        double rho = lambda / (s * mu);       // Server utilization

        // Compute P0 via Erlang-C state equations
        double sum = 0.0;
        for (int n = 0; n < s; n++)
        {
            sum += Math.Pow(a, n) / QueueMetrics.Factorial(n);
        }
        sum += Math.Pow(a, s) / (QueueMetrics.Factorial(s) * (1.0 - rho));
        double p0 = 1.0 / sum;

        // Erlang-C delay probability P(wait > 0)
        double pw = (Math.Pow(a, s) / (QueueMetrics.Factorial(s) * (1.0 - rho))) * p0;

        // Expected queue length Lq
        double lq = pw * rho / (1.0 - rho);
        double wq = lq / lambda;
        double w  = wq + (1.0 / mu);
        double l  = lambda * w; // or lq + a

        string formula = "P₀ = [∑(aⁿ/n!) + (aˢ/s!(1−ρ))]⁻¹, Pw = C(s,a) = [aˢ/s!(1−ρ)]·P₀, Lq = Pw·ρ/(1−ρ), Wq = Lq/λ, W = Wq + 1/μ, L = λ·W";

        var metrics = new QueueMetrics(
            Lambda: lambda,
            Mu: mu,
            S: s,
            Rho: rho,
            TrafficIntensity: a,
            Lq: lq,
            L: l,
            Wq: wq,
            W: w,
            P0: p0,
            Pwait: pw,
            Ca: 1.0,
            Cs: 1.0,
            ModelName: $"M/M/{s}",
            FormulaUsed: formula
        );

        return (metrics, null);
    }
}
