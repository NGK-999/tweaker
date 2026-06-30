using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ApexTweaker.UI.Wpf;
using ApexTweaker.UI.Wpf.Animations;

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
        PlayEntranceAnimation();

        var warmupTask = Task.Run(ApplicationWarmup.Run);
        var minimumDisplayTask = Task.Delay(MinimumDisplayMs);
        await Task.WhenAll(warmupTask, minimumDisplayTask).ConfigureAwait(true);

        DialogResult = true;
        Close();
    }

    private void PlayEntranceAnimation()
    {
        var storyboard = new Storyboard();
        UiMotion.ConfigureStoryboard(storyboard);

        storyboard.Children.Add(UiMotion.CreateDoubleAnimation(
            RootCard,
            UIElement.OpacityProperty,
            1D,
            UiMotion.Standard,
            UiMotion.EaseOut));

        if (RootCard.RenderTransform is ScaleTransform scale)
        {
            storyboard.Children.Add(UiMotion.CreateDoubleAnimation(
                scale,
                ScaleTransform.ScaleXProperty,
                1D,
                UiMotion.Standard,
                UiMotion.EaseOut,
                from: 0.98D));
            storyboard.Children.Add(UiMotion.CreateDoubleAnimation(
                scale,
                ScaleTransform.ScaleYProperty,
                1D,
                UiMotion.Standard,
                UiMotion.EaseOut,
                from: 0.98D));
        }

        storyboard.Begin();
    }
}
