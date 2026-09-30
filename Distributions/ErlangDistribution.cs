using System;

namespace FuelQ.Distributions;

/// <summary>Erlang-k distribution — sum of k independent Exponential(λ) phases.</summary>
public sealed class ErlangDistribution : IDistribution
{
    private readonly int    _k;
    private readonly double _lambda;

    public ErlangDistribution(int k, double lambda)
    {
        if (k < 1)      throw new ArgumentException("k must be ≥ 1.", nameof(k));
        if (lambda <= 0) throw new ArgumentException("λ must be positive.", nameof(lambda));
        _k      = k;
        _lambda = lambda;
    }

    public double Mean     => _k / _lambda;
    public double Variance => _k / (_lambda * _lambda);

    /// <summary>Product of k uniform samples: X = -ln(∏U_i)/λ</summary>
    public double Sample(Random rng)
    {
        double prod = 1.0;
        for (int i = 0; i < _k; i++)
        {
            double u = rng.NextDouble();
            if (u == 0.0) u = 1e-14;
            prod *= u;
        }
        return -Math.Log(prod) / _lambda;
    }

    public double PDF(double x)
    {
        if (x < 0) return 0;
        return Math.Pow(_lambda, _k) * Math.Pow(x, _k - 1) *
               Math.Exp(-_lambda * x) / Factorial(_k - 1);
    }

    public double CDF(double x)
    {
        if (x < 0) return 0;
        double sum = 0;
        for (int i = 0; i < _k; i++)
            sum += Math.Exp(-_lambda * x) * Math.Pow(_lambda * x, i) / Factorial(i);
        return 1.0 - sum;
    }

    private static double Factorial(int n)
    {
        double f = 1;
        for (int i = 2; i <= n; i++) f *= i;
        return f;
    }
}
