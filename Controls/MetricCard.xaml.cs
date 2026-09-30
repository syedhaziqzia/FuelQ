using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FuelQ.Controls;

public partial class MetricCard : UserControl
{
    public static readonly DependencyProperty SymbolProperty =
        DependencyProperty.Register(nameof(Symbol), typeof(string), typeof(MetricCard),
        new PropertyMetadata("ρ"));

    public static readonly DependencyProperty MetricValueProperty =
        DependencyProperty.Register(nameof(MetricValue), typeof(string), typeof(MetricCard),
        new PropertyMetadata("—"));

    public static readonly DependencyProperty MetricLabelProperty =
        DependencyProperty.Register(nameof(MetricLabel), typeof(string), typeof(MetricCard),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty FormulaTextProperty =
        DependencyProperty.Register(nameof(FormulaText), typeof(string), typeof(MetricCard),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(nameof(AccentColor), typeof(Brush), typeof(MetricCard),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8))));

    public string Symbol      { get => (string)GetValue(SymbolProperty);      set => SetValue(SymbolProperty, value); }
    public string MetricValue { get => (string)GetValue(MetricValueProperty); set => SetValue(MetricValueProperty, value); }
    public string MetricLabel { get => (string)GetValue(MetricLabelProperty); set => SetValue(MetricLabelProperty, value); }
    public string FormulaText { get => (string)GetValue(FormulaTextProperty); set => SetValue(FormulaTextProperty, value); }
    public Brush  AccentColor { get => (Brush)GetValue(AccentColorProperty);  set => SetValue(AccentColorProperty, value); }

    public MetricCard() { InitializeComponent(); }
}
