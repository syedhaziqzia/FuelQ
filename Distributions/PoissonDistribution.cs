using System;

namespace FuelQ.Distributions;

/// <summary>Poisson distribution — number of events in a fixed interval.</summary>
public sealed class PoissonDistribution : IDistribution
{
    private readonly double _lambda;

    public PoissonDistribution(double lambda)
    {
        if (lambda <= 0) throw new ArgumentException("λ must be positive.", nameof(lambda));
        _lambda = lambda;
    }

    public double Mean     => _lambda;
    public double Variance => _lambda;

    /// <summary>Knuth algorithm: returns integer count as double.</summary>
    public double Sample(Random rng)
    {
        double L = Math.Exp(-_lambda);
        int k = 0;
        double p = 1.0;
        do
        {
            k++;
            p *= rng.NextDouble();
        } while (p > L);
        return k - 1;
    }

    public double PDF(double x)
    {
        int n = (int)Math.Round(x);
        if (n < 0) return 0;
        return Math.Exp(-_lambda) * Math.Pow(_lambda, n) / Factorial(n);
    }

    public double CDF(double x)
    {
        int n = (int)Math.Floor(x);
        double sum = 0;
        for (int k = 0; k <= n; k++) sum += PDF(k);
        return sum;
    }

    private static double Factorial(int n)
    {
        double f = 1;
        for (int i = 2; i <= n; i++) f *= i;
        return f;
    }
}
