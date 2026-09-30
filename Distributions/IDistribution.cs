using System;

namespace FuelQ.Distributions;

/// <summary>Contract for all probability distribution implementations.</summary>
public interface IDistribution
{
    /// <summary>Draw one random sample using inverse-transform or dedicated algorithm.</summary>
    double Sample(Random rng);

    /// <summary>Probability density function at x.</summary>
    double PDF(double x);

    /// <summary>Cumulative distribution function at x.</summary>
    double CDF(double x);

    double Mean     { get; }
    double Variance { get; }
}
