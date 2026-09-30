using System;

namespace FuelQ.Distributions;

/// <summary>Continuous uniform distribution on [a, b].</summary>
public sealed class UniformDistribution : IDistribution
{
    private readonly double _a;
    private readonly double _b;

    public UniformDistribution(double a, double b)
    {
        if (b <= a) throw new ArgumentException("b must be > a.", nameof(b));
        _a = a;
        _b = b;
    }

    public double Mean     => (_a + _b) / 2.0;
    public double Variance => (_b - _a) * (_b - _a) / 12.0;

    public double Sample(Random rng) => _a + rng.NextDouble() * (_b - _a);

    public double PDF(double x) => (x < _a || x > _b) ? 0.0 : 1.0 / (_b - _a);

    public double CDF(double x)
    {
        if (x < _a) return 0.0;
        if (x > _b) return 1.0;
        return (x - _a) / (_b - _a);
    }
}
