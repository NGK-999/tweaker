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
    private static readonly TimeSpan TransformDuration = TimeSpan.FromMilliseconds(360);
    private static readonly TimeSpan OpacityDuration = TimeSpan.FromMilliseconds(300);
    private static readonly IEasingFunction EnterMotion = UiMotion.EaseOut;
    private static readonly IEasingFunction ExitMotion = UiMotion.EaseIn;

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
        SetTransform(incomingGroup, y: 5D, scale: 0.996D);
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
        UiMotion.ConfigureStoryboard(storyboard);

        AddTransformAnimations(
            storyboard,
            outgoingGroup,
            outgoing,
            fromY: 0D,
            toY: -4D,
            fromScale: 1D,
            toScale: 0.998D,
            fromOpacity: 1D,
            toOpacity: 0D,
            ExitMotion,
            transformDuration: TransformDuration,
            opacityDuration: OpacityDuration * 0.85);

        AddTransformAnimations(
            storyboard,
            incomingGroup,
            incoming,
            fromY: 5D,
            toY: 0D,
            fromScale: 0.996D,
            toScale: 1D,
            fromOpacity: 0D,
            toOpacity: 1D,
            EnterMotion,
            transformDuration: TransformDuration,
            opacityDuration: OpacityDuration,
            opacityBeginTime: TimeSpan.FromMilliseconds(20));

        void OnCompleted(object? sender, EventArgs args)
        {
            storyboard.Completed -= OnCompleted;
            SetTransform(outgoingGroup, y: -4D, scale: 0.998D);
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
        IEasingFunction easing,
        TimeSpan transformDuration,
        TimeSpan opacityDuration,
        TimeSpan? opacityBeginTime = null)
    {
        var translate = (TranslateTransform)group.Children[0];
        var scale = (ScaleTransform)group.Children[1];

        storyboard.Children.Add(CreateTransformAnimation(
            translate, TranslateTransform.YProperty, fromY, toY, transformDuration, easing));
        storyboard.Children.Add(CreateTransformAnimation(
            scale, ScaleTransform.ScaleXProperty, fromScale, toScale, transformDuration, easing));
        storyboard.Children.Add(CreateTransformAnimation(
            scale, ScaleTransform.ScaleYProperty, fromScale, toScale, transformDuration, easing));
        storyboard.Children.Add(CreateTransformAnimation(
            element, UIElement.OpacityProperty, fromOpacity, toOpacity, opacityDuration, easing, opacityBeginTime));
    }

    private static DoubleAnimation CreateTransformAnimation(
        DependencyObject target,
        DependencyProperty property,
        double from,
        double to,
        TimeSpan duration,
        IEasingFunction easing,
        TimeSpan? beginTime = null)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing
        };

        if (beginTime.HasValue)
        {
            animation.BeginTime = beginTime.Value;
        }

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(property));
        return animation;
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

    private static void EnableAnimationCache(FrameworkElement element)
    {
        RenderOptions.SetCachingHint(element, CachingHint.Cache);
        RenderOptions.SetCacheInvalidationThresholdMinimum(element, 0.5D);
        RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.HighQuality);
        element.CacheMode = new BitmapCache(1D);
    }

    private static void DisableAnimationCache(FrameworkElement element)
    {
        element.CacheMode = null;
        RenderOptions.SetCachingHint(element, CachingHint.Unspecified);
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
