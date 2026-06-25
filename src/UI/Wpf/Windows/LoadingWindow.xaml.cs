using System.Threading.Tasks;
using System.Windows;

namespace ApexTweaker.UI.Wpf.Windows;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(1500);
        DialogResult = true;
        Close();
    }
}
