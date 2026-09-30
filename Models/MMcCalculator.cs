namespace FuelQ.Models;

/// <summary>
/// Alias / wrapper for MMsCalculator.
/// </summary>
public static class MMcCalculator
{
    public static (QueueMetrics? Metrics, string? Error) Calculate(double lambda, double mu, int c)
        => MMsCalculator.Calculate(lambda, mu, c);
}
