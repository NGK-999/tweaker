using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ApexTweaker.UI.Wpf.Animations;

internal static class PageTransitionAnimator
{
    private static readonly TimeSpan OpacityDuration = TimeSpan.FromMilliseconds(200);
    private static readonly IEasingFunction EnterMotion = UiMotion.EaseOut;
    private static readonly IEasingFunction ExitMotion = UiMotion.EaseIn;

    // A cancelled transition's cleanup resumes via the WPF dispatcher, which can land
    // after a newer transition has already taken ownership of host.Content. This token
    // lets a stale cleanup detect that and skip touching host.Content instead of
    // clobbering whatever the newer transition already put there.
    private static object? activeTransition;

    public static async Task ShowAsync(
        ContentControl host,
        FrameworkElement incoming,
        CancellationToken cancellationToken,
        bool skipAnimation = false)
    {
        if (!host.Dispatcher.CheckAccess())
        {
            await host.Dispatcher.InvokeAsync(() => ShowAsync(host, incoming, cancellationToken, skipAnimation));
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var transitionToken = new object();
        activeTransition = transitionToken;

        if (host.Content is FrameworkElement current && ReferenceEquals(current, incoming))
        {
            PreparePage(incoming);
            return;
        }

        if (skipAnimation || host.Content is not FrameworkElement outgoing)
        {
            DetachFromParent(incoming);
            PreparePage(incoming);
            host.Content = incoming;
            return;
        }

        PreparePage(outgoing);
        PreparePage(incoming);

        host.Content = null;
        DetachFromParent(outgoing);
        DetachFromParent(incoming);

        outgoing.Opacity = 1D;
        incoming.Opacity = 0D;
        outgoing.RenderTransform = Transform.Identity;
        incoming.RenderTransform = Transform.Identity;

        var stage = new Grid { ClipToBounds = true };
        stage.Children.Add(outgoing);
        stage.Children.Add(incoming);
        host.Content = stage;

        try
        {
            await CrossfadeAsync(outgoing, incoming, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(activeTransition, transitionToken))
            {
                PreparePage(outgoing);
                host.Content = outgoing;
            }

            throw;
        }

        if (!ReferenceEquals(activeTransition, transitionToken))
        {
            return;
        }

        stage.Children.Remove(outgoing);
        stage.Children.Remove(incoming);
        host.Content = null;

        PreparePage(incoming);
        host.Content = incoming;
    }

    private static Task CrossfadeAsync(
        FrameworkElement outgoing,
        FrameworkElement incoming,
        CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var storyboard = new Storyboard
        {
            FillBehavior = FillBehavior.Stop
        };
        UiMotion.ConfigureStoryboard(storyboard);

        storyboard.Children.Add(UiMotion.CreateDoubleAnimation(
            outgoing,
            UIElement.OpacityProperty,
            0D,
            OpacityDuration * 0.85,
            ExitMotion,
            from: 1D));

        storyboard.Children.Add(UiMotion.CreateDoubleAnimation(
            incoming,
            UIElement.OpacityProperty,
            1D,
            OpacityDuration,
            EnterMotion,
            beginTime: TimeSpan.FromMilliseconds(16),
            from: 0D));

        void OnCompleted(object? sender, EventArgs args)
        {
            storyboard.Completed -= OnCompleted;
            outgoing.Opacity = 0D;
            incoming.Opacity = 1D;
            completion.TrySetResult(null);
        }

        storyboard.Completed += OnCompleted;

        using var registration = cancellationToken.Register(() =>
        {
            storyboard.Dispatcher.BeginInvoke(() =>
            {
                storyboard.Stop();
                storyboard.Completed -= OnCompleted;
                completion.TrySetCanceled(cancellationToken);
            });
        });

        storyboard.Begin();
        return completion.Task;
    }

    private static void DetachFromParent(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                return;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                return;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                return;
        }

        if (VisualTreeHelper.GetParent(element) is Panel visualPanel)
        {
            visualPanel.Children.Remove(element);
        }
    }

    private static void PreparePage(FrameworkElement page)
    {
        page.HorizontalAlignment = HorizontalAlignment.Stretch;
        page.VerticalAlignment = VerticalAlignment.Stretch;
        page.Margin = default;
        page.RenderTransform = Transform.Identity;
        page.Opacity = 1D;
        page.CacheMode = null;
        RenderOptions.SetCachingHint(page, CachingHint.Unspecified);
    }
}
