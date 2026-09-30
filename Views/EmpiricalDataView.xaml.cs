using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using FuelQ.Statistics;

namespace FuelQ.Views;

public partial class EmpiricalDataView : UserControl
{
    private List<ForecourtObservationRecord> _records = [];

    public EmpiricalDataView() { InitializeComponent(); }

    private void GenerateBaseline_Click(object sender, RoutedEventArgs e)
    {
        _records = DatasetIngestionEngine.GenerateAndSave();
        RefreshGrid();
        StatusRecords.Text    = $"● {_records.Count} records generated & saved";
        StatusSession.Text    = "Oct 2026 Maskan Chowrangi Session";
        StatusOutliers.Visibility = Visibility.Collapsed;
        UpdateHistograms();
    }

    private void BrowseCsv_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title  = "Load Forecourt Observation CSV"
        };
        if (dlg.ShowDialog() != true) return;

        _records = DatasetIngestionEngine.LoadFromCsv(dlg.FileName);
        RefreshGrid();
        StatusRecords.Text = $"● {_records.Count} records loaded from {System.IO.Path.GetFileName(dlg.FileName)}";
        StatusOutliers.Visibility = Visibility.Collapsed;
        UpdateHistograms();
    }

    private void DetectOutliers_Click(object sender, RoutedEventArgs e)
    {
        if (_records.Count == 0) { StatusRecords.Text = "No data loaded. Generate or load a CSV first."; return; }
        int n = OutlierDetector.Detect(_records);
        RefreshGrid();
        StatusOutliers.Text = $"⚠ {n} outliers flagged ({n * 100.0 / _records.Count:F2}%)";
        StatusOutliers.Visibility = Visibility.Visible;
    }

    private void RunChiSquare_Click(object sender, RoutedEventArgs e)
    {
        if (_records.Count < 10) { StatusRecords.Text = "Need at least 10 records to run Chi-Square."; return; }
        var result = ChiSquareGoodnessOfFit.Run(_records);
        DisplayChiSquareResult(result);
    }

    private void RefreshGrid()
    {
        ObservationGrid.ItemsSource = null;
        ObservationGrid.ItemsSource = _records;
    }

    private void UpdateHistograms()
    {
        if (_records.Count == 0) return;

        const int bins = 12;
        // IAT histogram
        var iats = _records.Select(r => r.InterArrivalTime).ToArray();
        ComputeHistogram(iats, bins, out double[] iatCounts, out string[] iatLabels);
        IatHistogram.Values = iatCounts;
        IatHistogram.Labels = iatLabels;

        // Service duration histogram
        var svcs = _records.Select(r => r.ServiceDuration).ToArray();
        ComputeHistogram(svcs, bins, out double[] svcCounts, out string[] svcLabels);
        SvcHistogram.Values = svcCounts;
        SvcHistogram.Labels = svcLabels;
    }

    private static void ComputeHistogram(double[] data, int bins,
        out double[] counts, out string[] labels)
    {
        counts = new double[bins];
        labels = new string[bins];
        if (data.Length == 0) return;

        double min = data.Min();
        double max = data.Max();
        if (max <= min) max = min + 1;
        double width = (max - min) / bins;

        foreach (double v in data)
        {
            int b = (int)((v - min) / width);
            if (b >= bins) b = bins - 1;
            counts[b]++;
        }
        for (int i = 0; i < bins; i++)
            labels[i] = $"{min + i * width:F0}";
    }

    private void DisplayChiSquareResult(ChiSquareGoodnessOfFit.Result r)
    {
        ChiBins.Text    = r.Bins.ToString();
        ChiDf.Text      = r.DegreesOfFreedom.ToString();
        ChiStat.Text    = r.ChiSquareStat.ToString("F2");
        ChiCritical.Text= r.CriticalValue.ToString("F2");
        ChiPValue.Text  = r.PValue.ToString("F3");

        ChiStatHint.Text       = r.H0Accepted ? "χ² < Critical ✓" : "χ² ≥ Critical ✗";
        ChiHypothesisLabel.Text= $"Tested Null Hypothesis: Exponential (λ = {r.EstimatedLambda:F4} veh/s)";

        ChiPValue.Foreground = r.H0Accepted
            ? (Brush)FindResource("StatusGreenText")
            : (Brush)FindResource("StatusRedText");

        bool accept = r.H0Accepted;
        ChiVerdictBorder.Background  = accept ? (Brush)FindResource("StatusGreenBg")  : (Brush)FindResource("StatusRedBg");
        ChiVerdictBorder.BorderBrush = accept ? (Brush)FindResource("StatusGreenBorder") : (Brush)FindResource("StatusRedBorder");

        ChiVerdict.Foreground = accept ? (Brush)FindResource("StatusGreenText") : (Brush)FindResource("StatusRedText");
        ChiVerdict.Text = accept
            ? $"✓ H0 Accepted: Empirical Data Fits Exponential Distribution\n" +
              $"At α = 0.05, χ² = {r.ChiSquareStat:F2} < critical {r.CriticalValue:F2} (p = {r.PValue:F3}). " +
              $"Markovian Poisson arrival assumption is statistically justified."
            : $"✗ H0 Rejected: Data does not fit Exponential Distribution\n" +
              $"At α = 0.05, χ² = {r.ChiSquareStat:F2} ≥ critical {r.CriticalValue:F2} (p = {r.PValue:F3}).";
    }
}
