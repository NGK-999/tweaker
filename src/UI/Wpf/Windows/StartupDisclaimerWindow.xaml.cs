using System.Windows;

namespace ApexTweaker.UI.Wpf.Windows;

public partial class StartupDisclaimerWindow : Window
{
    public StartupDisclaimerWindow()
    {
        InitializeComponent();
    }

    private void AcceptCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        ConfirmButton.IsEnabled = AcceptCheckBox.IsChecked == true;
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
