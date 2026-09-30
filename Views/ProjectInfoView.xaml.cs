using System.Windows;
using System.Windows.Controls;

namespace FuelQ.Views;

public partial class ProjectInfoView : UserControl
{
    public ProjectInfoView()
    {
        InitializeComponent();
    }

    private void NavCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int idx))
        {
            if (Window.GetWindow(this) is MainWindow mainWin)
            {
                mainWin.NavigateTo(idx);
            }
        }
    }
}
