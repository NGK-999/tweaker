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
    private static readonly IEasingFunction EnterEasing = new CubicEase { EasingMode = EasingMode.EaseOut };
    private static readonly IEasingFunction ExitEasing = new QuadraticEase { EasingMode = EasingMode.EaseIn };

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

        var outgoingGroup = CreateTransformGroup();
        var incomingGroup = CreateTransformGroup();
        outgoing.RenderTransform = outgoingGroup;
        incoming.RenderTransform = incomingGroup;
        outgoing.RenderTransformOrigin = new Point(0.5, 0.5);
        incoming.RenderTransformOrigin = new Point(0.5, 0.5);
        outgoing.Opacity = 1D;

        SetTransform(outgoingGroup, y: 0D, scale: 1D);
        SetTransform(incomingGroup, y: 10D, scale: 0.988D);
        incoming.Opacity = 0D;

        var stage = new Grid { ClipToBounds = true };
        stage.Children.Add(outgoing);
        stage.Children.Add(incoming);
        host.Content = stage;

        EnableAnimationCache(outgoing);
        EnableAnimationCache(incoming);

        try
        {
            await CrossfadeAsync(outgoing, outgoingGroup, incoming, incomingGroup, cancellationToken)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            DisableAnimationCache(outgoing);
            DisableAnimationCache(incoming);
            PreparePage(outgoing);
            PreparePage(incoming);
            host.Content = outgoing;
            throw;
        }

        DisableAnimationCache(outgoing);
        DisableAnimationCache(incoming);

        stage.Children.Remove(outgoing);
        stage.Children.Remove(incoming);
        host.Content = null;

        PreparePage(incoming);
        host.Content = incoming;
    }

    private static Task CrossfadeAsync(
        FrameworkElement outgoing,
        TransformGroup outgoingGroup,
        FrameworkElement incoming,
        TransformGroup incomingGroup,
        CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var storyboard = new Storyboard
        {
            FillBehavior = FillBehavior.Stop
        };

        AddTransformAnimations(
            storyboard,
            outgoingGroup,
            outgoing,
            fromY: 0D,
            toY: -8D,
            fromScale: 1D,
            toScale: 0.992D,
            fromOpacity: 1D,
            toOpacity: 0D,
            ExitEasing);

        AddTransformAnimations(
            storyboard,
            incomingGroup,
            incoming,
            fromY: 10D,
            toY: 0D,
            fromScale: 0.988D,
            toScale: 1D,
            fromOpacity: 0D,
            toOpacity: 1D,
            EnterEasing);

        void OnCompleted(object? sender, EventArgs args)
        {
            storyboard.Completed -= OnCompleted;
            SetTransform(outgoingGroup, y: -8D, scale: 0.992D);
            SetTransform(incomingGroup, y: 0D, scale: 1D);
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

    private static void AddTransformAnimations(
        Storyboard storyboard,
        TransformGroup group,
        UIElement element,
        double fromY,
        double toY,
        double fromScale,
        double toScale,
        double fromOpacity,
        double toOpacity,
        IEasingFunction easing)
    {
        var translate = (TranslateTransform)group.Children[0];
        var scale = (ScaleTransform)group.Children[1];

        storyboard.Children.Add(CreateAnimation(translate, TranslateTransform.YProperty, fromY, toY, easing));
        storyboard.Children.Add(CreateAnimation(scale, ScaleTransform.ScaleXProperty, fromScale, toScale, easing));
        storyboard.Children.Add(CreateAnimation(scale, ScaleTransform.ScaleYProperty, fromScale, toScale, easing));
        storyboard.Children.Add(CreateAnimation(element, UIElement.OpacityProperty, fromOpacity, toOpacity, easing));
    }

    private static TransformGroup CreateTransformGroup()
    {
        return new TransformGroup
        {
            Children =
            [
                new TranslateTransform(),
                new ScaleTransform(1D, 1D)
            ]
        };
    }

    private static void SetTransform(TransformGroup group, double y, double scale)
    {
        ((TranslateTransform)group.Children[0]).Y = y;
        ((ScaleTransform)group.Children[1]).ScaleX = scale;
        ((ScaleTransform)group.Children[1]).ScaleY = scale;
    }

    private static DoubleAnimation CreateAnimation(
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

    private static void EnableAnimationCache(FrameworkElement element)
    {
        element.CacheMode = new BitmapCache(1D);
        RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.LowQuality);
    }

    private static void DisableAnimationCache(FrameworkElement element)
    {
        element.CacheMode = null;
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
        page.CacheMode = null;
    }
}
