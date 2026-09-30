using System;

namespace FuelQ.Distributions;

/// <summary>Exponential distribution — memoryless inter-arrival times in a Poisson process.</summary>
public sealed class ExponentialDistribution : IDistribution
{
    private readonly double _lambda;

    public ExponentialDistribution(double lambda)
    {
        if (lambda <= 0) throw new ArgumentException("λ must be positive.", nameof(lambda));
        _lambda = lambda;
    }

    public double Mean     => 1.0 / _lambda;
    public double Variance => 1.0 / (_lambda * _lambda);

    /// <summary>Inverse-transform sampling: X = -ln(1-U)/λ</summary>
    public double Sample(Random rng)
    {
        double u = rng.NextDouble();
        if (u == 0.0) u = 1e-14;
        return -Math.Log(u) / _lambda;
    }

    public double PDF(double x) => x < 0 ? 0.0 : _lambda * Math.Exp(-_lambda * x);
    public double CDF(double x) => x < 0 ? 0.0 : 1.0 - Math.Exp(-_lambda * x);
}
