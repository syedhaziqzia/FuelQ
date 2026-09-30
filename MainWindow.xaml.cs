using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using FuelQ.Views;

namespace FuelQ;

public partial class MainWindow : Window
{
    private readonly Button[] _navButtons;
    private readonly UserControl[] _views;
    private int _activeIndex = -1;
    private bool _sidebarOpen = true;
    /// <summary>Must match the Width attribute on SidebarPanel in MainWindow.xaml.</summary>
    private const double SidebarWidth = 260;

    public MainWindow()
    {
        InitializeComponent();

        _navButtons = [NavBtn0, NavBtn1, NavBtn2, NavBtn3];
        _views =
        [
            new ProjectInfoView(),
            new QueuingModelsView(),
            new SimulatorView(),
            new PromptAuditView()
        ];

        NavigateTo(0);
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int idx))
            NavigateTo(idx);
    }

    public void NavigateTo(int index)
    {
        if (index < 0 || index >= _views.Length || index == _activeIndex) return;
        _activeIndex = index;

        // Update nav button styles
        for (int i = 0; i < _navButtons.Length; i++)
        {
            _navButtons[i].Style = i == index
                ? (Style)FindResource("NavButtonActive")
                : (Style)FindResource("NavButton");
        }

        MainContent.Content = _views[index];
    }

    // ── Modal Overlay ───────────────────────────────────────────────
    public void ShowModal(string title, UIElement content)
    {
        ModalTitle.Text = title;
        ModalContent.Content = content;
        ModalOverlay.Visibility = Visibility.Visible;
    }

    public void HideModal()
    {
        ModalOverlay.Visibility = Visibility.Collapsed;
        ModalContent.Content = null;
    }

    private void CloseModal_Click(object sender, RoutedEventArgs e) => HideModal();

    // ── Sidebar Toggle ───────────────────────────────────────────────
    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _sidebarOpen = !_sidebarOpen;

        // Width animation — cubic ease-out for a snappy, premium feel
        double targetWidth = _sidebarOpen ? SidebarWidth : 64;
        var widthAnim = new DoubleAnimation(targetWidth, new Duration(TimeSpan.FromMilliseconds(240)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        SidebarPanel.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);

        // Update toggle button
        BtnToggleSidebar.Content = _sidebarOpen ? "◀" : "▶";
        BtnToggleSidebar.ToolTip  = _sidebarOpen ? "Collapse sidebar" : "Expand sidebar";
        BtnToggleSidebar.HorizontalAlignment = _sidebarOpen ? HorizontalAlignment.Right : HorizontalAlignment.Center;

        if (_sidebarOpen)
        {
            // Expanding: show elements first, then fade them in
            BrandLogoPanel.Visibility    = Visibility.Visible;
            TbSidebarSubtitle.Visibility = Visibility.Visible;
            TbSidebarMeta.Visibility     = Visibility.Visible;
            TbNavLabel.Visibility        = Visibility.Visible;
            PanelSupervisor.Visibility   = Visibility.Visible;
            TbNavText0.Visibility = Visibility.Visible;
            TbNavText1.Visibility = Visibility.Visible;
            TbNavText2.Visibility = Visibility.Visible;
            TbNavText3.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BrandLogoPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            TbSidebarSubtitle.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            TbSidebarMeta.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            TbNavLabel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            PanelSupervisor.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            TbNavText0.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            TbNavText1.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            TbNavText2.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            TbNavText3.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
        else
        {
            // Collapsing: fade out first, then hide
            var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(120)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) =>
            {
                BrandLogoPanel.Visibility    = Visibility.Collapsed;
                TbSidebarSubtitle.Visibility = Visibility.Collapsed;
                TbSidebarMeta.Visibility     = Visibility.Collapsed;
                TbNavLabel.Visibility        = Visibility.Collapsed;
                PanelSupervisor.Visibility   = Visibility.Collapsed;
                TbNavText0.Visibility = Visibility.Collapsed;
                TbNavText1.Visibility = Visibility.Collapsed;
                TbNavText2.Visibility = Visibility.Collapsed;
                TbNavText3.Visibility = Visibility.Collapsed;
            };
            BrandLogoPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            TbSidebarSubtitle.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            TbSidebarMeta.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            TbNavLabel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            PanelSupervisor.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            TbNavText0.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            TbNavText1.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            TbNavText2.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            TbNavText3.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
