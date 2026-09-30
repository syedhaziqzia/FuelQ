using System;

namespace FuelQ.Models;

/// <summary>M/Ek/1 Erlang-k service queuing calculator.</summary>
public static class MEk1Calculator
{
    /// <param name="lambda">Arrival rate λ</param>
    /// <param name="meanService">Mean service time E[S]</param>
    /// <param name="k">Erlang shape parameter k (number of phases, k ≥ 1)</param>
    public static (QueueMetrics? Metrics, string? Error) Calculate(double lambda, double meanService, int k)
    {
        if (lambda <= 0 || meanService <= 0)
            return (null, "λ and mean service time E[S] must be positive.");
        if (k < 1)
            return (null, "Erlang shape k must be ≥ 1.");

        double mu  = 1.0 / meanService;
        double rho = lambda * meanService;

        if (rho >= 1.0)
            return (null, $"System Unstable: ρ = {rho:F4} ≥ 1.0.");

        // Erlang-k: Lq = (1+k)/(2k) · ρ²/(1-ρ)
        double lq = ((1.0 + k) / (2.0 * k)) * (rho * rho) / (1.0 - rho);
        double l  = lq + rho;
        double wq = lq / lambda;
        double w  = wq + meanService;
        double p0 = 1.0 - rho;

        return (new QueueMetrics(lambda, mu, 1, rho, rho, lq, l, wq, w, p0, rho, 1.0, 1.0 / Math.Sqrt(k), $"M/E{k}/1", "Lq = [(1+k)/(2k)]·[ρ²/(1−ρ)]"), null);
    }
}
