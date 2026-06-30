using System.Threading.Tasks;
using System.Windows;
using ApexTweaker.UI.Wpf;

namespace ApexTweaker.UI.Wpf.Windows;

public partial class LoadingWindow : Window
{
    private const int MinimumDisplayMs = 420;

    public LoadingWindow()
    {
        InitializeComponent();
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        var warmupTask = Task.Run(ApplicationWarmup.Run);
        var minimumDisplayTask = Task.Delay(MinimumDisplayMs);
        await Task.WhenAll(warmupTask, minimumDisplayTask).ConfigureAwait(true);

        DialogResult = true;
        Close();
    }
}
