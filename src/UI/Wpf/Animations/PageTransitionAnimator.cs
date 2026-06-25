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
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(280);

    public static async Task ShowAsync(
        ContentControl host,
        FrameworkElement incoming,
        CancellationToken cancellationToken,
        bool skipAnimation = false)
    {
        if (!host.Dispatcher.CheckAccess())
        {
            await await host.Dispatcher.InvokeAsync(() => ShowAsync(host, incoming, cancellationToken, skipAnimation));
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

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

        var stage = new Grid
        {
            ClipToBounds = true
        };

        var outgoingTransform = new TranslateTransform();
        var incomingTransform = new TranslateTransform(34D, 0D);

        outgoing.RenderTransform = outgoingTransform;
        outgoing.Opacity = 1D;

        incoming.RenderTransform = incomingTransform;
        incoming.Opacity = 0D;

        stage.Children.Add(outgoing);
        stage.Children.Add(incoming);
        host.Content = stage;

        try
        {
            await RunStoryboardAsync(outgoing, outgoingTransform, incoming, incomingTransform, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            DetachFromParent(incoming);
            DetachFromParent(outgoing);
            PreparePage(outgoing);
            if (host.Content is Grid)
            {
                host.Content = outgoing;
            }

            throw;
        }

        DetachFromParent(incoming);
        DetachFromParent(outgoing);
        PreparePage(incoming);
        host.Content = incoming;
    }

    private static Task RunStoryboardAsync(
        FrameworkElement outgoing,
        TranslateTransform outgoingTransform,
        FrameworkElement incoming,
        TranslateTransform incomingTransform,
        CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var storyboard = new Storyboard
        {
            FillBehavior = FillBehavior.HoldEnd
        };

        var easing = new CubicEase
        {
            EasingMode = EasingMode.EaseOut
        };

        storyboard.Children.Add(CreateAnimation(outgoingTransform, TranslateTransform.XProperty, 0D, -18D, easing));
        storyboard.Children.Add(CreateAnimation(incomingTransform, TranslateTransform.XProperty, 34D, 0D, easing));
        storyboard.Children.Add(CreateAnimation(outgoing, UIElement.OpacityProperty, 1D, 0D, easing));
        storyboard.Children.Add(CreateAnimation(incoming, UIElement.OpacityProperty, 0D, 1D, easing));

        void OnCompleted(object? sender, EventArgs args)
        {
            storyboard.Completed -= OnCompleted;
            completion.TrySetResult(null);
        }

        storyboard.Completed += OnCompleted;

        using var registration = cancellationToken.Register(() =>
        {
            storyboard.Dispatcher.BeginInvoke(new Action(() =>
            {
                storyboard.Stop();
                storyboard.Completed -= OnCompleted;
                completion.TrySetCanceled(cancellationToken);
            }));
        });

        storyboard.Begin();
        return completion.Task;
    }

    private static Timeline CreateAnimation(
        DependencyObject target,
        DependencyProperty property,
        double from,
        double to,
        IEasingFunction easing)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(Duration),
            EasingFunction = easing
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(property));
        return animation;
    }

    private static void DetachFromParent(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case System.Windows.Controls.Panel panel:
                panel.Children.Remove(element);
                return;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                return;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                return;
        }

        if (VisualTreeHelper.GetParent(element) is System.Windows.Controls.Panel visualPanel)
        {
            visualPanel.Children.Remove(element);
        }
    }

    private static void PreparePage(FrameworkElement page)
    {
        page.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        page.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
        page.Margin = default;
        page.RenderTransform = Transform.Identity;
        page.Opacity = 1D;
    }
}