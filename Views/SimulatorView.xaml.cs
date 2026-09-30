using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FuelQ.Simulation;

namespace FuelQ.Views;

public partial class SimulatorView : UserControl
{
    private bool _loaded;

    public SimulatorView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (!_loaded)
            {
                _loaded = true;
                RunSimulation();
            }
        };
    }

    private void SimArrDist_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded) return;
        int idx = CbSimArrDist.SelectedIndex;
        PanelSimArrExp.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelSimArrUniform.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
        PanelSimArrNormal.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SimSvcDist_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded) return;
        int idx = CbSimSvcDist.SelectedIndex;
        PanelSimSvcExp.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelSimSvcNormal.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
        PanelSimSvcGamma.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
        PanelSimSvcUniform.Visibility = idx == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RunSim_Click(object sender, RoutedEventArgs e)
    {
        RunSimulation();
    }

    private void ResetSim_Click(object sender, RoutedEventArgs e)
    {
        TbSimServers.Text = "2";
        CbSimTimeUnit.SelectedIndex = 0; // Minutes
        TbSimCustomerLimit.Text = "25";
        TbSimDurationCap.Text = "480";
        TbSimSeed.Text = "42";

        CbSimArrDist.SelectedIndex = 0;
        TbSimArrExpMean.Text = "3.0";
        TbSimArrUniMin.Text = "2.0";
        TbSimArrUniMax.Text = "4.0";
        TbSimArrNormMean.Text = "3.0";
        TbSimArrNormStd.Text = "1.0";

        CbSimSvcDist.SelectedIndex = 0;
        TbSimSvcExpMean.Text = "2.0";
        TbSimSvcNormMean.Text = "2.0";
        TbSimSvcNormStd.Text = "0.8";
        TbSimSvcGammaShape.Text = "2.0";
        TbSimSvcGammaScale.Text = "1.0";
        TbSimSvcUniMin.Text = "1.0";
        TbSimSvcUniMax.Text = "3.0";

        // Reset arrival panel visibility (index 0 = Exponential)
        PanelSimArrExp.Visibility     = Visibility.Visible;
        PanelSimArrUniform.Visibility = Visibility.Collapsed;
        PanelSimArrNormal.Visibility  = Visibility.Collapsed;

        // Reset service panel visibility (index 0 = Exponential)
        PanelSimSvcExp.Visibility     = Visibility.Visible;
        PanelSimSvcNormal.Visibility  = Visibility.Collapsed;
        PanelSimSvcGamma.Visibility   = Visibility.Collapsed;
        PanelSimSvcUniform.Visibility = Visibility.Collapsed;

        RunSimulation();
    }

    private async void RunSimulation()
    {
        BtnRunSim.IsEnabled = false;
        BtnRunSim.Content = "⏳ Simulating...";

        try
        {
            var cfg = BuildConfig();
            var result = await Task.Run(() => GeneralDESimulator.Run(cfg));
            DisplaySimulationResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Simulation error: {ex.Message}", "DES Engine", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnRunSim.IsEnabled = true;
            BtnRunSim.Content = "▶  Run Discrete Event Simulation";
        }
    }

    private DESSimConfig BuildConfig()
    {
        int servers = int.TryParse(TbSimServers.Text, out int s) ? Math.Max(1, s) : 2;
        int limit = int.TryParse(TbSimCustomerLimit.Text, out int n) ? Math.Max(1, n) : 25;
        double duration = double.TryParse(TbSimDurationCap.Text, out double d) ? Math.Max(1.0, d) : 480.0;
        int seed = int.TryParse(TbSimSeed.Text, out int sd) ? sd : 42;

        var unit = CbSimTimeUnit.SelectedIndex switch
        {
            1 => TimeUnitType.Seconds,
            2 => TimeUnitType.Hours,
            _ => TimeUnitType.Minutes
        };

        // Build Arrival Distribution
        DistConfig arrConfig;
        switch (CbSimArrDist.SelectedIndex)
        {
            case 1: // Uniform
                double uMin = double.TryParse(TbSimArrUniMin.Text, out double a1) ? a1 : 2.0;
                double uMax = double.TryParse(TbSimArrUniMax.Text, out double a2) ? a2 : 4.0;
                arrConfig = new DistConfig(DistType.Uniform, uMin, uMax);
                break;
            case 2: // Normal
                double nMean = double.TryParse(TbSimArrNormMean.Text, out double nm) ? nm : 3.0;
                double nStd = double.TryParse(TbSimArrNormStd.Text, out double ns) ? ns : 1.0;
                arrConfig = new DistConfig(DistType.Normal, nMean, nStd);
                break;
            default: // Exponential
                double expMean = double.TryParse(TbSimArrExpMean.Text, out double em) ? em : 3.0;
                arrConfig = new DistConfig(DistType.Exponential, expMean);
                break;
        }

        // Build Service Distribution
        DistConfig svcConfig;
        switch (CbSimSvcDist.SelectedIndex)
        {
            case 1: // Normal
                double sMean = double.TryParse(TbSimSvcNormMean.Text, out double sm) ? sm : 2.0;
                double sStd = double.TryParse(TbSimSvcNormStd.Text, out double ss) ? ss : 0.8;
                svcConfig = new DistConfig(DistType.Normal, sMean, sStd);
                break;
            case 2: // Gamma
                double gShape = double.TryParse(TbSimSvcGammaShape.Text, out double gs) ? gs : 2.0;
                double gScale = double.TryParse(TbSimSvcGammaScale.Text, out double gsc) ? gsc : 1.0;
                svcConfig = new DistConfig(DistType.Gamma, gShape, gScale);
                break;
            case 3: // Uniform
                double sMin = double.TryParse(TbSimSvcUniMin.Text, out double su1) ? su1 : 1.0;
                double sMax = double.TryParse(TbSimSvcUniMax.Text, out double su2) ? su2 : 3.0;
                svcConfig = new DistConfig(DistType.Uniform, sMin, sMax);
                break;
            default: // Exponential
                double sExpMean = double.TryParse(TbSimSvcExpMean.Text, out double sem) ? sem : 2.0;
                svcConfig = new DistConfig(DistType.Exponential, sExpMean);
                break;
        }

        return new DESSimConfig(servers, unit, limit, duration, seed, arrConfig, svcConfig);
    }

    private void DisplaySimulationResult(DESSimulationResult res)
    {
        string u = res.TimeUnitStr;

        // KPI Badges
        SimMetricAvgWq.Text = res.AvgWaitingTime.ToString("F2");
        SimMetricAvgWqUnit.Text = $"{u} in queue";
        SimMetricMaxWq.Text = res.MaxWaitingTime.ToString("F2");
        SimMetricMaxWqUnit.Text = $"{u} peak delay";
        SimMetricAvgLq.Text = res.AvgQueueLength.ToString("F2");
        SimMetricMaxQLen.Text = res.MaxQueueLength.ToString();
        SimMetricRho.Text = $"{(res.ServerUtilization * 100):F1}%";
        SimMetricAvgW.Text = res.AvgSystemTime.ToString("F2");
        SimMetricAvgWUnit.Text = $"{u} total turnaround";
        SimMetricAvgL.Text = res.AvgSystemLength.ToString("F2");
        SimMetricThroughput.Text = res.Throughput.ToString("F3");
        SimMetricThroughputUnit.Text = $"vehicles / {u}";
        SimMetricTotalServed.Text = res.TotalCustomersServed.ToString();
        SimMetricTotalTime.Text = res.TotalSimTime.ToString("F1");
        SimMetricTotalTimeUnit.Text = $"{u} total elapsed";

        // Comparison Table
        GridComparison.ItemsSource = res.ComparisonRows;

        // Random Numbers Table
        GridRandomNumbers.ItemsSource = res.CustomerRows;

        // Event-by-Event Simulation Table
        GridEventTable.ItemsSource = res.CustomerRows;

        // Hourly Summary Table
        GridHourly.ItemsSource = res.HourlyRows;

        // Charts
        if (res.QueueTimeline.Count > 0)
        {
            double[] xs = [.. res.QueueTimeline.Select(t => t.Time)];
            double[] ys = [.. res.QueueTimeline.Select(t => (double)t.QLen)];
            ChartTimeline.XAxisLabel = $"Time ({u})";
            ChartTimeline.YAxisLabel = "Queue Length (veh)";
            ChartTimeline.SetSeries([(xs, ys, Color.FromRgb(0x38, 0xBD, 0xF8), "Q(t)")]);
        }

        if (res.WaitDistribution.Count > 0)
        {
            double[] vals = [.. res.WaitDistribution.Select(d => d.Pct)];
            string[] labels = [.. res.WaitDistribution.Select(d => $"≤{d.WaitBin:F1}{u}")];
            ChartWaitDist.Values = vals;
            ChartWaitDist.Labels = labels;
            ChartWaitDist.XAxisLabel = $"Wait Time Interval ({u})";
            ChartWaitDist.YAxisLabel = "% of Vehicles";
            ChartWaitDist.InvalidateVisual();
        }
    }
}
