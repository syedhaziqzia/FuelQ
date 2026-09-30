namespace FuelQ.Models;

/// <summary>
/// M/M/1 Single-server Markovian queuing model.
/// Poisson arrivals + Exponential service.
/// </summary>
public static class MM1Calculator
{
    public static (QueueMetrics? Metrics, string? Error) Calculate(double lambda, double mu)
    {
        if (lambda <= 0 || mu <= 0)
            return (null, "Both λ and μ must be positive numbers.");
        if (lambda >= mu)
            return (null, $"System is UNSTABLE: Arrival rate λ ({lambda:F4}) ≥ Service rate μ ({mu:F4}). Utilization ρ ≥ 1.");

        double rho = lambda / mu;
        double a   = rho; // Traffic intensity
        double lq  = (rho * rho) / (1.0 - rho);
        double l   = rho / (1.0 - rho);
        double wq  = lq / lambda;
        double w   = wq + (1.0 / mu);
        double p0  = 1.0 - rho;
        double pw  = rho;

        string formula = "Lq = ρ²/(1−ρ), Wq = Lq/λ, L = Lq + a, W = Wq + 1/μ, P₀ = 1−ρ, Pw = ρ";

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
            Cs: 1.0,
            ModelName: "M/M/1",
            FormulaUsed: formula
        );

        return (metrics, null);
    }
}
