using System;

namespace FuelQ.Distributions;

/// <summary>
/// Gamma distribution (shape k / alpha, scale theta / beta).
/// Mean = k * theta, Variance = k * theta^2.
/// Sampled via Marsaglia and Tsang (2000) method.
/// </summary>
public sealed class GammaDistribution : IDistribution
{
    private readonly double _shape; // k or alpha
    private readonly double _scale; // theta or 1/beta

    public GammaDistribution(double shape, double scale)
    {
        if (shape <= 0) throw new ArgumentException("Shape k must be > 0.", nameof(shape));
        if (scale <= 0) throw new ArgumentException("Scale theta must be > 0.", nameof(scale));
        _shape = shape;
        _scale = scale;
    }

    public double Mean     => _shape * _scale;
    public double Variance => _shape * _scale * _scale;

    public double Sample(Random rng)
    {
        // Marsaglia and Tsang's method for k >= 1
        if (_shape < 1.0)
        {
            // Boost using Weibull / power of uniform
            double u = rng.NextDouble();
            if (u == 0.0) u = 1e-14;
            return SampleMarsaglia(1.0 + _shape, rng) * Math.Pow(u, 1.0 / _shape) * _scale;
        }

        return SampleMarsaglia(_shape, rng) * _scale;
    }

    private static double SampleMarsaglia(double k, Random rng)
    {
        double d = k - 1.0 / 3.0;
        double c = 1.0 / Math.Sqrt(9.0 * d);

        while (true)
        {
            double z = SampleStandardNormal(rng);
            double v = 1.0 + c * z;
            if (v <= 0) continue;

            v = v * v * v;
            double u = rng.NextDouble();
            if (u == 0.0) u = 1e-14;

            if (u < 1.0 - 0.0331 * z * z * z * z)
                return d * v;

            if (Math.Log(u) < 0.5 * z * z + d * (1.0 - v + Math.Log(v)))
                return d * v;
        }
    }

    private static double SampleStandardNormal(Random rng)
    {
        double u1 = rng.NextDouble();
        double u2 = rng.NextDouble();
        if (u1 == 0.0) u1 = 1e-14;
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    public double PDF(double x)
    {
        if (x <= 0) return 0;
        // f(x) = (1 / (gamma(k) * theta^k)) * x^(k-1) * exp(-x/theta)
        return Math.Exp((_shape - 1.0) * Math.Log(x) - (x / _scale) - LogGamma(_shape) - _shape * Math.Log(_scale));
    }

    public double CDF(double x)
    {
        if (x <= 0) return 0;
        // Incomplete gamma ratio approximation
        return LowerIncompleteGammaRatio(_shape, x / _scale);
    }

    public static double LogGamma(double x)
    {
        // Lanczos approximation
        double[] c = [76.18009172947146, -86.50532032941677, 24.01409824083091,
                      -1.231739572450155, 0.1208650973866179e-2, -0.5395239384953e-5];
        double y = x;
        double tmp = x + 5.5;
        tmp -= (x + 0.5) * Math.Log(tmp);
        double ser = 1.000000000190015;
        for (int j = 0; j <= 5; j++) ser += c[j] / ++y;
        return -tmp + Math.Log(2.5066282746310005 * ser / x);
    }

    private static double LowerIncompleteGammaRatio(double a, double x)
    {
        if (x <= 0) return 0;
        if (x < a + 1.0)
        {
            // Series representation
            double ap = a;
            double sum = 1.0 / a;
            double del = sum;
            for (int n = 1; n <= 100; n++)
            {
                ap += 1.0;
                del *= x / ap;
                sum += del;
                if (Math.Abs(del) < Math.Abs(sum) * 1e-10) break;
            }
            return sum * Math.Exp(-x + a * Math.Log(x) - LogGamma(a));
        }
        else
        {
            // Continued fraction
            double b = x + 1.0 - a;
            double c = 1.0 / 1e-30;
            double d = 1.0 / b;
            double h = d;
            for (int i = 1; i <= 100; i++)
            {
                double an = -i * (i - a);
                b += 2.0;
                d = an * d + b;
                if (Math.Abs(d) < 1e-30) d = 1e-30;
                c = b + an / c;
                if (Math.Abs(c) < 1e-30) c = 1e-30;
                d = 1.0 / d;
                double del = d * c;
                h *= del;
                if (Math.Abs(del - 1.0) < 1e-10) break;
            }
            return 1.0 - Math.Exp(-x + a * Math.Log(x) - LogGamma(a)) * h;
        }
    }
}
