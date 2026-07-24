using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ApexTweaker.UI.Wpf.Animations;

internal static class UiMotion
{
    public static readonly TimeSpan Quick = TimeSpan.FromMilliseconds(160);
    public static readonly TimeSpan Standard = TimeSpan.FromMilliseconds(240);

    public static readonly IEasingFunction EaseOut = new PowerEase { Power = 4, EasingMode = EasingMode.EaseOut };
    public static readonly IEasingFunction EaseIn = new PowerEase { Power = 3, EasingMode = EasingMode.EaseIn };
    public static readonly IEasingFunction EaseInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };

    public static void ConfigureStoryboard(Storyboard storyboard, int desiredFrameRate = 120)
    {
        Timeline.SetDesiredFrameRate(storyboard, desiredFrameRate);
    }

    public static DoubleAnimation CreateDoubleAnimation(
        DependencyObject target,
        DependencyProperty property,
        double to,
        TimeSpan duration,
        IEasingFunction? easing = null,
        TimeSpan? beginTime = null,
        double? from = null)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing ?? EaseOut
        };

        if (from.HasValue)
        {
            animation.From = from.Value;
        }

        if (beginTime.HasValue)
        {
            animation.BeginTime = beginTime.Value;
        }

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(property));
        return animation;
    }

    public static void FadeIn(FrameworkElement element, TimeSpan? duration = null)
    {
        element.Opacity = 0D;
        var storyboard = new Storyboard();
        ConfigureStoryboard(storyboard);
        storyboard.Children.Add(CreateDoubleAnimation(
            element,
            UIElement.OpacityProperty,
            1D,
            duration ?? Standard,
            EaseOut));
        storyboard.Begin();
    }

    public static async Task AnimateHeaderAsync(TextBlock target, string newText, CancellationToken cancellationToken = default)
    {
        if (!target.Dispatcher.CheckAccess())
        {
            await target.Dispatcher.InvokeAsync(() => AnimateHeaderAsync(target, newText, cancellationToken));
            return;
        }

        if (string.Equals(target.Text, newText, StringComparison.Ordinal))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transform = new TranslateTransform(0D, 0D);
        target.RenderTransform = transform;
        target.RenderTransformOrigin = new Point(0D, 0.5D);

        var storyboard = new Storyboard { FillBehavior = FillBehavior.Stop };
        ConfigureStoryboard(storyboard);

        storyboard.Children.Add(CreateDoubleAnimation(target, UIElement.OpacityProperty, 0D, Quick, EaseIn));
        storyboard.Children.Add(CreateDoubleAnimation(transform, TranslateTransform.YProperty, -6D, Quick, EaseIn));

        void OnFirstCompleted(object? sender, EventArgs args)
        {
            storyboard.Completed -= OnFirstCompleted;
            target.Text = newText;
            transform.Y = 8D;
            target.Opacity = 0D;

            var enter = new Storyboard { FillBehavior = FillBehavior.Stop };
            ConfigureStoryboard(enter);
            enter.Children.Add(CreateDoubleAnimation(target, UIElement.OpacityProperty, 1D, Standard, EaseOut));
            enter.Children.Add(CreateDoubleAnimation(transform, TranslateTransform.YProperty, 0D, Standard, EaseOut));

            void OnSecondCompleted(object? sender2, EventArgs args2)
            {
                enter.Completed -= OnSecondCompleted;
                target.RenderTransform = Transform.Identity;
                target.Opacity = 1D;
                completion.TrySetResult(null);
            }

            enter.Completed += OnSecondCompleted;
            enter.Begin();
        }

        storyboard.Completed += OnFirstCompleted;

        // Runs synchronously as part of the caller's Cancel() call (CancellationToken.Register
        // callbacks execute on the cancelling thread) — deferring this via Dispatcher.BeginInvoke
        // let a superseding AnimateHeaderAsync call start first and then get its text/transform
        // stomped by this stale cleanup on the next dispatcher tick.
        using var registration = cancellationToken.Register(() =>
        {
            storyboard.Stop();
            storyboard.Completed -= OnFirstCompleted;
            target.Text = newText;
            target.RenderTransform = Transform.Identity;
            target.Opacity = 1D;
            completion.TrySetCanceled(cancellationToken);
        });

        storyboard.Begin();
        await completion.Task.ConfigureAwait(true);
    }
}
