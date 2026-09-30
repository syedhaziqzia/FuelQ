using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FuelQ.Controls;

public enum ChartMode { Bar, Line }

/// <summary>
/// Native WPF chart via DrawingContext.OnRender — zero NuGet dependencies.
/// Mode A (Bar): P(n) distributions and frequency histograms.
/// Mode B (Line): Q(t) timelines and Lq-vs-ρ sensitivity curves.
/// </summary>
public class SimpleWpfChart : FrameworkElement
{
    // ── Dependency Properties ─────────────────────────────────────
    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(nameof(Mode), typeof(ChartMode), typeof(SimpleWpfChart),
        new FrameworkPropertyMetadata(ChartMode.Bar, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(double[]), typeof(SimpleWpfChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelsProperty =
        DependencyProperty.Register(nameof(Labels), typeof(string[]), typeof(SimpleWpfChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HighlightIndexProperty =
        DependencyProperty.Register(nameof(HighlightIndex), typeof(int), typeof(SimpleWpfChart),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty XAxisLabelProperty =
        DependencyProperty.Register(nameof(XAxisLabel), typeof(string), typeof(SimpleWpfChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty YAxisLabelProperty =
        DependencyProperty.Register(nameof(YAxisLabel), typeof(string), typeof(SimpleWpfChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    // Line chart series (list of (x,y) point arrays)
    private List<(double[] X, double[] Y, Color Color, string Label)> _series = [];

    // ── Public Properties ─────────────────────────────────────────
    public ChartMode Mode           { get => (ChartMode)GetValue(ModeProperty);      set => SetValue(ModeProperty, value); }
    public double[]? Values         { get => (double[]?)GetValue(ValuesProperty);    set => SetValue(ValuesProperty, value); }
    public string[]? Labels         { get => (string[]?)GetValue(LabelsProperty);    set => SetValue(LabelsProperty, value); }
    public int       HighlightIndex { get => (int)GetValue(HighlightIndexProperty);  set => SetValue(HighlightIndexProperty, value); }
    public string    XAxisLabel     { get => (string)GetValue(XAxisLabelProperty);   set => SetValue(XAxisLabelProperty, value); }
    public string    YAxisLabel     { get => (string)GetValue(YAxisLabelProperty);   set => SetValue(YAxisLabelProperty, value); }

    public void SetSeries(List<(double[] X, double[] Y, Color Color, string Label)> series)
    {
        _series = series;
        InvalidateVisual();
    }

    // ── Brushes & Pens ────────────────────────────────────────────
    private static readonly SolidColorBrush BgBrush      = new(Color.FromRgb(0x07, 0x0D, 0x1D));
    private static readonly SolidColorBrush GridBrush    = new(Color.FromRgb(0x0D, 0x15, 0x25));
    private static readonly SolidColorBrush DefaultBar   = new(Color.FromRgb(0x1E, 0x40, 0xAF));
    private static readonly SolidColorBrush CyanBar      = new(Color.FromRgb(0x38, 0xBD, 0xF8));
    private static readonly SolidColorBrush TextBrush    = new(Color.FromRgb(0x7A, 0x8B, 0xAA));
    private static readonly SolidColorBrush AxisBrush    = new(Color.FromRgb(0x14, 0x1F, 0x36));
    private static readonly Pen             GridPen      = new(GridBrush, 1) { DashStyle = DashStyles.Dot };
    private static readonly Pen             AxisPen      = new(AxisBrush, 1);
    private static readonly Pen             SteadyPen    = new(new SolidColorBrush(Color.FromArgb(160, 0xF8, 0x71, 0x71)), 1) { DashStyle = DashStyles.Dash };

    private static readonly Typeface SmallFont = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    // Cache DPI once at first render; avoids P/Invoke on every DrawText call.
    private double _dpi = 96.0 / 96.0; // default 1.0 until updated
    private bool   _dpiCached;

    static SimpleWpfChart()
    {
        BgBrush.Freeze(); GridBrush.Freeze(); DefaultBar.Freeze();
        CyanBar.Freeze(); TextBrush.Freeze(); AxisBrush.Freeze();
        GridPen.Freeze(); AxisPen.Freeze(); SteadyPen.Freeze();
    }

    // ── Render ─────────────────────────────────────────────────────
    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 10 || h < 10) return;

        // Cache DPI from the host window once per chart instance
        if (!_dpiCached)
        {
            var mainWin = Application.Current.MainWindow;
            if (mainWin != null)
            {
                _dpi = VisualTreeHelper.GetDpi(mainWin).PixelsPerDip;
                _dpiCached = true;
            }
        }

        dc.DrawRectangle(BgBrush, null, new Rect(0, 0, w, h));

        if (Mode == ChartMode.Bar)
            RenderBar(dc, w, h);
        else
            RenderLine(dc, w, h);
    }

    // ── Bar Chart ──────────────────────────────────────────────────
    private void RenderBar(DrawingContext dc, double w, double h)
    {
        double[]? vals   = Values;
        string[]? labels = Labels;
        if (vals == null || vals.Length == 0) { DrawEmpty(dc, w, h); return; }

        const double padL = 44, padB = 28, padT = 12, padR = 12;
        double plotW = w - padL - padR;
        double plotH = h - padT - padB;

        double maxVal = 0;
        foreach (double v in vals) if (v > maxVal) maxVal = v;
        if (maxVal == 0) maxVal = 1;

        // Grid lines
        for (int g = 0; g <= 4; g++)
        {
            double y = padT + plotH - (g / 4.0) * plotH;
            dc.DrawLine(GridPen, new Point(padL, y), new Point(padL + plotW, y));
            DrawText(dc, $"{maxVal * g / 4:F3}", padL - 4, y - 7, TextBrush, 9, TextAlignment.Right);
        }

        // Axis
        dc.DrawLine(AxisPen, new Point(padL, padT), new Point(padL, padT + plotH));
        dc.DrawLine(AxisPen, new Point(padL, padT + plotH), new Point(padL + plotW, padT + plotH));

        int n = vals.Length;
        double barW  = plotW / n;
        double gapW  = barW * 0.15;
        double rectW = barW - gapW * 2;

        for (int i = 0; i < n; i++)
        {
            double barH  = (vals[i] / maxVal) * plotH;
            double x     = padL + i * barW + gapW;
            double y     = padT + plotH - barH;
            bool   hilite= i == HighlightIndex;

            var brush = hilite ? CyanBar : DefaultBar;
            dc.DrawRectangle(brush, null, new Rect(x, y, rectW, barH));

            // Label
            string lbl = labels != null && i < labels.Length ? labels[i] : i.ToString();
            DrawText(dc, lbl, x + rectW / 2, padT + plotH + 4, TextBrush, 8.5, TextAlignment.Center);
        }

        // X/Y axis labels
        if (!string.IsNullOrEmpty(XAxisLabel))
            DrawText(dc, XAxisLabel, padL + plotW / 2, h - 10, TextBrush, 9, TextAlignment.Center);
        if (!string.IsNullOrEmpty(YAxisLabel))
            DrawTextVertical(dc, YAxisLabel, 8, padT + plotH / 2, TextBrush, 9);
    }

    // ── Line Chart ─────────────────────────────────────────────────
    private void RenderLine(DrawingContext dc, double w, double h)
    {
        if (_series.Count == 0 || _series[0].X.Length == 0) { DrawEmpty(dc, w, h); return; }

        const double padL = 48, padB = 28, padT = 12, padR = 16;
        double plotW = w - padL - padR;
        double plotH = h - padT - padB;

        // Find global X/Y range
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMax = double.MinValue;
        foreach (var (xs, ys, _, _) in _series)
        {
            foreach (double x in xs) { if (x < xMin) xMin = x; if (x > xMax) xMax = x; }
            foreach (double y in ys) { if (y > yMax) yMax = y; }
        }
        if (xMax <= xMin) xMax = xMin + 1;
        if (yMax <= 0) yMax = 1;
        yMax *= 1.1;

        // Grid
        for (int g = 0; g <= 4; g++)
        {
            double gy = padT + plotH - (g / 4.0) * plotH;
            dc.DrawLine(GridPen, new Point(padL, gy), new Point(padL + plotW, gy));
            DrawText(dc, $"{yMax * g / 4:F1}", padL - 4, gy - 7, TextBrush, 9, TextAlignment.Right);
        }
        for (int g = 0; g <= 4; g++)
        {
            double gx = padL + (g / 4.0) * plotW;
            dc.DrawLine(GridPen, new Point(gx, padT), new Point(gx, padT + plotH));
            double xLabel = xMin + (xMax - xMin) * g / 4;
            DrawText(dc, FormatXLabel(xLabel), gx, padT + plotH + 4, TextBrush, 8.5, TextAlignment.Center);
        }

        // Axis lines
        dc.DrawLine(AxisPen, new Point(padL, padT), new Point(padL, padT + plotH));
        dc.DrawLine(AxisPen, new Point(padL, padT + plotH), new Point(padL + plotW, padT + plotH));

        // Series
        foreach (var (xs, ys, color, _) in _series)
        {
            if (xs.Length == 0) continue;
            var pen = new Pen(new SolidColorBrush(color), 2);
            pen.Freeze();
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                double px = padL + ((xs[0] - xMin) / (xMax - xMin)) * plotW;
                double py = padT + plotH - (ys[0] / yMax) * plotH;
                ctx.BeginFigure(new Point(px, py), false, false);
                for (int i = 1; i < xs.Length; i++)
                {
                    px = padL + ((xs[i] - xMin) / (xMax - xMin)) * plotW;
                    py = padT + plotH - Math.Clamp(ys[i] / yMax, 0, 1) * plotH;
                    ctx.LineTo(new Point(px, py), true, false);
                }
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);
        }

        // Axis labels
        if (!string.IsNullOrEmpty(XAxisLabel))
            DrawText(dc, XAxisLabel, padL + plotW / 2, h - 10, TextBrush, 9, TextAlignment.Center);
        if (!string.IsNullOrEmpty(YAxisLabel))
            DrawTextVertical(dc, YAxisLabel, 8, padT + plotH / 2, TextBrush, 9);
    }

    private static string FormatXLabel(double x) => x >= 60 ? $"{x / 60:F0}m" : $"{x:F0}s";

    private void DrawEmpty(DrawingContext dc, double w, double h)
    {
        DrawText(dc, "No data", w / 2, h / 2 - 8, TextBrush, 12, TextAlignment.Center);
    }

    private void DrawText(DrawingContext dc, string text, double x, double y,
        Brush brush, double size, TextAlignment align)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            SmallFont, size, brush, _dpi)
        {
            TextAlignment = align
        };
        dc.DrawText(ft, new Point(x, y));
    }

    private void DrawTextVertical(DrawingContext dc, string text, double x, double y,
        Brush brush, double size)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            SmallFont, size, brush, _dpi);
        dc.PushTransform(new RotateTransform(-90, x, y));
        dc.DrawText(ft, new Point(x - ft.Width / 2, y - ft.Height / 2));
        dc.Pop();
    }
}
