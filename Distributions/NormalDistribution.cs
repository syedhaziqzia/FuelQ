using System;

namespace FuelQ.Distributions;

/// <summary>
/// Normal (Gaussian) distribution using the Box-Muller transform.
/// This implementation is stateless (no spare sample caching) to ensure
/// thread safety and deterministic reproducibility per Random instance.
/// </summary>
public sealed class NormalDistribution : IDistribution
{
    private readonly double _mean;
    private readonly double _std;

    public NormalDistribution(double mean, double std)
    {
        if (std < 0) throw new ArgumentException("Standard deviation σ must be ≥ 0.", nameof(std));
        _mean = mean;
        _std  = std;
    }

    public double Mean     => _mean;
    public double Variance => _std * _std;

    /// <summary>
    /// Box-Muller transform. Returns Max(0, sample) to prevent negative service times.
    /// Each call is independent — no spare caching to ensure thread safety.
    /// </summary>
    public double Sample(Random rng)
    {
        // Marsaglia polar method — rejection-samples until inside unit circle
        double u, v, s;
        do
        {
            u = rng.NextDouble() * 2.0 - 1.0;
            v = rng.NextDouble() * 2.0 - 1.0;
            s = u * u + v * v;
        } while (s >= 1.0 || s == 0.0);

        double mul = Math.Sqrt(-2.0 * Math.Log(s) / s);
        return Math.Max(0.0, u * mul * _std + _mean);
    }

    public double PDF(double x)
    {
        if (_std <= 0) return x == _mean ? double.PositiveInfinity : 0.0;
        double z = (x - _mean) / _std;
        return Math.Exp(-0.5 * z * z) / (_std * Math.Sqrt(2.0 * Math.PI));
    }

    public double CDF(double x)
    {
        if (_std <= 0) return x >= _mean ? 1.0 : 0.0;
        return 0.5 * (1.0 + Erf((x - _mean) / (_std * Math.Sqrt(2.0))));
    }

    // Abramowitz & Stegun approximation — max error 1.5×10⁻⁷
    private static double Erf(double z)
    {
        double t    = 1.0 / (1.0 + 0.3275911 * Math.Abs(z));
        double poly = t * (0.254829592 + t * (-0.284496736 +
                     t * (1.421413741 + t * (-1.453152027 + t * 1.061405429))));
        double result = 1.0 - poly * Math.Exp(-z * z);
        return z < 0 ? -result : result;
    }
}
