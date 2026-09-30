using System;
using System.Collections.Generic;
using System.Linq;
using FuelQ.Distributions;
using FuelQ.Models;

namespace FuelQ.Simulation;

public static class GeneralDESimulator
{
    public static DESSimulationResult Run(DESSimConfig cfg)
    {
        var rng = new Random(cfg.RandomSeed);
        int numServers = Math.Max(1, cfg.NumServers);
        int maxCustomers = Math.Max(1, cfg.CustomerLimit);
        double maxDuration = cfg.DurationLimit > 0 ? cfg.DurationLimit : double.MaxValue;

        string timeUnitStr = cfg.TimeUnit switch
        {
            TimeUnitType.Seconds => "sec",
            TimeUnitType.Hours => "hr",
            _ => "min"
        };

        // Server states: when each server is free next
        double[] serverFreeAt = new double[numServers];
        double[] serverBusyDuration = new double[numServers];

        var customerRows = new List<CustomerEventRow>(maxCustomers);
        var timeline     = new List<(double Time, int QLen)>(maxCustomers * 2 + 1);

        // Build distribution instances once — reused across all events
        IDistribution? arrDistInst = BuildDistInstance(cfg.ArrivalDist);
        IDistribution? svcDistInst = BuildDistInstance(cfg.ServiceDist);

        double currentArrival = 0;
        // track queue length with a counter instead of O(n) LINQ scan
        int queueCounter = 0;

        timeline.Add((0, 0));

        for (int i = 1; i <= maxCustomers; i++)
        {
            // Sample Interarrival Time with zero-guard applied after rounding
            double rArr = Math.Round(rng.NextDouble(), 4);
            if (rArr <= 0) rArr = 0.0001;
            double iat = SampleDist(cfg.ArrivalDist, rArr, rng, arrDistInst);

            if (i == 1)
                currentArrival = iat;
            else
                currentArrival += iat;

            if (currentArrival > maxDuration)
                break;

            // Sample Service Time (same post-round guard)
            double rSvc = Math.Round(rng.NextDouble(), 4);
            if (rSvc <= 0) rSvc = 0.0001;
            double svc = SampleDist(cfg.ServiceDist, rSvc, rng, svcDistInst);

            // Find earliest available server
            int bestServer = 0;
            double earliestFree = serverFreeAt[0];
            for (int s = 1; s < numServers; s++)
            {
                if (serverFreeAt[s] < earliestFree)
                {
                    earliestFree = serverFreeAt[s];
                    bestServer = s;
                }
            }

            double svcBegins = Math.Max(currentArrival, earliestFree);
            double waitInQueue = svcBegins - currentArrival;
            double svcEnds = svcBegins + svc;
            double timeInSystem = waitInQueue + svc;

            // Queue length at arrival = current counter value (customers waiting or being served)
            // If this customer waits, increment counter; it decrements when service begins
            int qLenAtArrival = queueCounter;
            if (waitInQueue > 0)
                queueCounter++; // customer joins queue

            double idleTime = Math.Max(0, currentArrival - earliestFree);

            serverFreeAt[bestServer] = svcEnds;
            serverBusyDuration[bestServer] += svc;

            var row = new CustomerEventRow
            {
                CustomerNumber = i,
                ArrivalRandom = rArr,
                InterarrivalTime = Math.Round(iat, 4),
                ArrivalTime = Math.Round(currentArrival, 4),
                ServiceRandom = rSvc,
                ServiceTime = Math.Round(svc, 4),
                TimeServiceBegins = Math.Round(svcBegins, 4),
                TimeServiceEnds = Math.Round(svcEnds, 4),
                WaitingTime = Math.Round(waitInQueue, 4),
                TimeInSystem = Math.Round(timeInSystem, 4),
                QueueLengthOnArrival = qLenAtArrival,
                AssignedServer = bestServer + 1,
                IdleTimeOfServer = Math.Round(idleTime, 4)
            };

            customerRows.Add(row);
            if (waitInQueue > 0 && queueCounter > 0)
                queueCounter--; // customer is now being served, leaves queue

            // Record timeline points
            timeline.Add((currentArrival, qLenAtArrival + (waitInQueue > 0 ? 1 : 0)));
            timeline.Add((svcBegins, qLenAtArrival));
        }

        // Timeline is already in chronological order (events appended sequentially).
        // Sorting would be O(n log n) for no benefit — skip it.

        int totalServed = customerRows.Count;
        double totalSimTime = totalServed > 0 ? customerRows.Max(c => c.TimeServiceEnds) : 1.0;
        if (totalSimTime <= 0) totalSimTime = 1.0;

        double avgIat = totalServed > 0 ? customerRows.Average(c => c.InterarrivalTime) : 0;
        double avgSvc = totalServed > 0 ? customerRows.Average(c => c.ServiceTime) : 0;
        double avgWq  = totalServed > 0 ? customerRows.Average(c => c.WaitingTime) : 0;
        double avgW   = totalServed > 0 ? customerRows.Average(c => c.TimeInSystem) : 0;
        double maxWq  = totalServed > 0 ? customerRows.Max(c => c.WaitingTime) : 0;
        int maxQLen   = totalServed > 0 ? customerRows.Max(c => c.QueueLengthOnArrival) : 0;

        // Time-average queue length and system length
        double simLambda = avgIat > 0 ? 1.0 / avgIat : 0;
        double simLq = simLambda * avgWq;
        double simL  = simLambda * avgW;

        double totalBusy = serverBusyDuration.Sum();
        double avgUtil = (totalBusy / (numServers * totalSimTime));
        if (avgUtil > 1.0) avgUtil = 1.0;

        double throughput = totalSimTime > 0 ? totalServed / totalSimTime : 0;

        // Hourly breakdown
        var hourlyRows = GenerateHourlySummary(customerRows, numServers, timeUnitStr);

        // Theoretical Comparison
        var compRows = GenerateTheoreticalComparison(cfg, avgIat, avgSvc, avgWq, avgW, simLq, simL, avgUtil, timeUnitStr);

        // Waiting Time Distribution
        var waitDist = GenerateWaitDistribution(customerRows);

        return new DESSimulationResult(
            CustomerRows: customerRows,
            HourlyRows: hourlyRows,
            ComparisonRows: compRows,
            QueueTimeline: timeline,
            WaitDistribution: waitDist,
            AvgInterarrivalTime: avgIat,
            AvgServiceTime: avgSvc,
            AvgWaitingTime: avgWq,
            AvgSystemTime: avgW,
            AvgQueueLength: simLq,
            AvgSystemLength: simL,
            MaxQueueLength: maxQLen,
            MaxWaitingTime: maxWq,
            ServerUtilization: avgUtil,
            Throughput: throughput,
            TotalCustomersServed: totalServed,
            TotalSimTime: totalSimTime,
            TimeUnitStr: timeUnitStr
        );
    }

    private static double SampleDist(DistConfig dist, double u, Random rng, IDistribution? distInstance)
    {
        // For distributions that need rng (Normal, Gamma), use the pre-built instance.
        // For inverse-transform distributions (Exp, Uniform), use u directly for reproducibility.
        switch (dist.Type)
        {
            case DistType.Exponential:
                double mean = dist.Param1 > 0 ? dist.Param1 : 1.0;
                return -mean * Math.Log(u);

            case DistType.Uniform:
                double min = dist.Param1;
                double max = dist.Param2 > min ? dist.Param2 : min + 1.0;
                return min + u * (max - min);

            case DistType.Normal:
            case DistType.Gamma:
                // distInstance is guaranteed non-null for these cases
                return Math.Max(0.01, distInstance!.Sample(rng));

            default:
                return 1.0;
        }
    }

    private static IDistribution? BuildDistInstance(DistConfig dist)
    {
        return dist.Type switch
        {
            DistType.Normal => new NormalDistribution(
                dist.Param1,
                dist.Param2 > 0 ? dist.Param2 : 1.0),
            DistType.Gamma => new GammaDistribution(
                dist.Param1 > 0 ? dist.Param1 : 2.0,
                dist.Param2 > 0 ? dist.Param2 : 1.0),
            _ => null
        };
    }

    private static List<HourlySummaryRow> GenerateHourlySummary(
        List<CustomerEventRow> rows, int servers, string unit)
    {
        if (rows.Count == 0) return [];

        double maxTime = rows.Max(r => r.TimeServiceEnds);
        double interval = unit == "hr" ? 1.0 : unit == "sec" ? 3600.0 : 60.0;
        int totalPeriods = Math.Max(1, (int)Math.Ceiling(maxTime / interval));

        // Pre-allocate per-period accumulators for a single O(n) pass
        var arrCount  = new int[totalPeriods];
        var depCount  = new int[totalPeriods];
        var waitSum   = new double[totalPeriods];
        var qSum      = new double[totalPeriods];
        var peakQ     = new double[totalPeriods];
        var busySum   = new double[totalPeriods];

        foreach (var r in rows)
        {
            int arrBin = Math.Min((int)(r.ArrivalTime / interval), totalPeriods - 1);
            int depBin = Math.Min((int)(r.TimeServiceEnds / interval), totalPeriods - 1);

            arrCount[arrBin]++;
            qSum[arrBin] += r.QueueLengthOnArrival;
            if (r.QueueLengthOnArrival > peakQ[arrBin])
                peakQ[arrBin] = r.QueueLengthOnArrival;

            depCount[depBin]++;
            waitSum[depBin] += r.WaitingTime;

            // Clip service overlap to each period
            for (int p = Math.Min(arrBin, depBin); p <= depBin && p < totalPeriods; p++)
            {
                double tStart = p * interval;
                double tEnd   = (p + 1) * interval;
                busySum[p] += Math.Max(0.0, Math.Min(r.TimeServiceEnds, tEnd)
                                          - Math.Max(r.TimeServiceBegins, tStart));
            }
        }

        var list = new List<HourlySummaryRow>(totalPeriods);
        for (int h = 0; h < totalPeriods; h++)
        {
            double tStart = h * interval;
            double tEnd   = (h + 1) * interval;
            double avgW   = depCount[h] > 0 ? waitSum[h] / depCount[h] : 0;
            double avgQ   = arrCount[h] > 0 ? qSum[h]  / arrCount[h]  : 0;
            double util   = Math.Min(1.0, busySum[h] / (servers * interval));

            list.Add(new HourlySummaryRow
            {
                HourPeriod        = $"Period {h + 1} ({tStart:F0}\u2013{tEnd:F0} {unit})",
                Arrivals          = arrCount[h],
                Departures        = depCount[h],
                AvgWait           = Math.Round(avgW, 2),
                AvgQueueLength    = Math.Round(avgQ, 2),
                PeakQueueLength   = (int)peakQ[h],
                ServerUtilization = Math.Round(util * 100.0, 1)
            });
        }
        return list;
    }

    private static List<TheoreticalComparisonRow> GenerateTheoreticalComparison(
        DESSimConfig cfg, double avgIat, double avgSvc, double simWq, double simW,
        double simLq, double simL, double simRho, string unit)
    {
        var list = new List<TheoreticalComparisonRow>();

        double theoLambda, theoMu;
        double arrVar, svcVar;

        // Extract theoretical distribution parameters
        if (cfg.ArrivalDist.Type == DistType.Exponential)
        {
            double mean = cfg.ArrivalDist.Param1 > 0 ? cfg.ArrivalDist.Param1 : 1.0;
            theoLambda = 1.0 / mean;
            arrVar = mean * mean;
        }
        else if (cfg.ArrivalDist.Type == DistType.Uniform)
        {
            double a = cfg.ArrivalDist.Param1, b = cfg.ArrivalDist.Param2;
            double mean = (a + b) / 2.0;
            theoLambda = mean > 0 ? 1.0 / mean : 1.0;
            arrVar = Math.Pow(b - a, 2) / 12.0;
        }
        else
        {
            double mean = cfg.ArrivalDist.Param1 > 0 ? cfg.ArrivalDist.Param1 : 1.0;
            theoLambda = 1.0 / mean;
            arrVar = Math.Pow(cfg.ArrivalDist.Param2, 2);
        }

        if (cfg.ServiceDist.Type == DistType.Exponential)
        {
            double mean = cfg.ServiceDist.Param1 > 0 ? cfg.ServiceDist.Param1 : 1.0;
            theoMu = 1.0 / mean;
            svcVar = mean * mean;
        }
        else if (cfg.ServiceDist.Type == DistType.Uniform)
        {
            double a = cfg.ServiceDist.Param1, b = cfg.ServiceDist.Param2;
            double mean = (a + b) / 2.0;
            theoMu = mean > 0 ? 1.0 / mean : 1.0;
            svcVar = Math.Pow(b - a, 2) / 12.0;
        }
        else if (cfg.ServiceDist.Type == DistType.Gamma)
        {
            double k = cfg.ServiceDist.Param1, theta = cfg.ServiceDist.Param2;
            double mean = k * theta;
            theoMu = mean > 0 ? 1.0 / mean : 1.0;
            svcVar = k * theta * theta;
        }
        else
        {
            double mean = cfg.ServiceDist.Param1 > 0 ? cfg.ServiceDist.Param1 : 1.0;
            theoMu = 1.0 / mean;
            svcVar = Math.Pow(cfg.ServiceDist.Param2, 2);
        }

        // Calculate theoretical model
        QueueMetrics? theo;
        if (cfg.NumServers == 1 && cfg.ArrivalDist.Type == DistType.Exponential && cfg.ServiceDist.Type == DistType.Exponential)
            (theo, _) = MM1Calculator.Calculate(theoLambda, theoMu);
        else if (cfg.NumServers > 1 && cfg.ArrivalDist.Type == DistType.Exponential && cfg.ServiceDist.Type == DistType.Exponential)
            (theo, _) = MMsCalculator.Calculate(theoLambda, theoMu, cfg.NumServers);
        else if (cfg.NumServers == 1 && cfg.ArrivalDist.Type == DistType.Exponential)
            (theo, _) = MG1Calculator.Calculate(theoLambda, theoMu, svcVar);
        else if (cfg.NumServers > 1 && cfg.ArrivalDist.Type == DistType.Exponential)
            (theo, _) = MGsCalculator.Calculate(theoLambda, theoMu, cfg.NumServers, svcVar);
        else if (cfg.NumServers == 1)
            (theo, _) = GG1Calculator.Calculate(theoLambda, theoMu, arrVar, svcVar);
        else
            (theo, _) = GGsCalculator.Calculate(theoLambda, theoMu, cfg.NumServers, arrVar, svcVar);

        double simLambda = avgIat > 0 ? 1.0 / avgIat : 0;
        double simMu = avgSvc > 0 ? 1.0 / avgSvc : 0;

        AddComp(list, "Arrival Rate", "λ", $"{simLambda:F4} /{unit}", $"{theoLambda:F4} /{unit}", simLambda, theoLambda);
        AddComp(list, "Service Rate", "μ", $"{simMu:F4} /{unit}", $"{theoMu:F4} /{unit}", simMu, theoMu);

        if (theo != null)
        {
            AddComp(list, "Server Utilization", "ρ", $"{simRho * 100:F1}%", $"{theo.Rho * 100:F1}%", simRho, theo.Rho);
            AddComp(list, "Avg Waiting Time in Queue", "Wq", $"{simWq:F4} {unit}", $"{theo.Wq:F4} {unit}", simWq, theo.Wq);
            AddComp(list, "Avg Time in System", "W", $"{simW:F4} {unit}", $"{theo.W:F4} {unit}", simW, theo.W);
            AddComp(list, "Avg Queue Length", "Lq", $"{simLq:F4} veh", $"{theo.Lq:F4} veh", simLq, theo.Lq);
            AddComp(list, "Avg System Length", "L", $"{simL:F4} veh", $"{theo.L:F4} veh", simL, theo.L);
        }

        return list;
    }

    private static void AddComp(List<TheoreticalComparisonRow> list, string name, string sym, string simStr, string theoStr, double simVal, double theoVal)
    {
        double diff = theoVal != 0 ? Math.Abs(simVal - theoVal) / theoVal * 100.0 : 0;
        list.Add(new TheoreticalComparisonRow
        {
            MetricName = name,
            Symbol = sym,
            SimulatedValue = simStr,
            TheoreticalValue = theoStr,
            ErrorDelta = $"{diff:F2}%"
        });
    }

    private static List<(double WaitBin, int Count, double Pct)> GenerateWaitDistribution(List<CustomerEventRow> rows)
    {
        var list = new List<(double WaitBin, int Count, double Pct)>();
        if (rows.Count == 0) return list;

        double maxW = rows.Max(r => r.WaitingTime);
        int binCount = 6;
        double binWidth = maxW > 0 ? maxW / binCount : 1.0;

        for (int b = 0; b < binCount; b++)
        {
            double lo = b * binWidth;
            double hi = (b + 1) * binWidth;
            int count = rows.Count(r => r.WaitingTime >= lo && (b == binCount - 1 ? r.WaitingTime <= hi : r.WaitingTime < hi));
            double pct = (double)count / rows.Count * 100.0;
            list.Add((Math.Round(hi, 2), count, Math.Round(pct, 1)));
        }

        return list;
    }
}
