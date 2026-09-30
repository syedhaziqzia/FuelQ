namespace FuelQ.Models;

/// <summary>
/// M/D/1 deterministic-service queuing calculator (σ = 0 special case of M/G/1).
/// Service time is constant D, so Var(S) = 0 and Lq is exactly half of M/M/1.
/// </summary>
public static class MD1Calculator
{
    /// <param name="lambda">Arrival rate λ (customers per time unit)</param>
    /// <param name="meanService">Constant service time D = 1/μ (time units per customer, σ = 0)</param>
    public static (QueueMetrics? Metrics, string? Error) Calculate(double lambda, double meanService)
    {
        if (lambda <= 0)
            return (null, "Arrival rate λ must be positive.");
        if (meanService <= 0)
            return (null, "Service time D must be positive.");

        double mu  = 1.0 / meanService;
        double rho = lambda * meanService; // ρ = λ/μ = λ·D

        if (rho >= 1.0)
            return (null, $"System is UNSTABLE: ρ = {rho:F4} ≥ 1.0. Arrival rate λ ({lambda:F4}) ≥ service rate μ ({mu:F4}).");

        // P-K formula with σ² = 0: Lq = ρ² / [2(1−ρ)]
        double lq = (rho * rho) / (2.0 * (1.0 - rho));
        double l  = lq + rho;
        double wq = lq / lambda;
        double w  = wq + meanService;
        double p0 = 1.0 - rho;

        const string formula = "P-K (σ=0): Lq = ρ²/[2(1−ρ)], Wq = Lq/λ, W = Wq + D, L = Lq + ρ, P₀ = 1−ρ";
        return (new QueueMetrics(lambda, mu, 1, rho, rho, lq, l, wq, w, p0, rho, 1.0, 0.0, "M/D/1", formula), null);
    }
}
