using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal static class UiAnimator
{
    private const int DurationMs = 380;
    private const int EnterOffsetPx = 34;
    private const int FrameDelayMs = 4;

    private static readonly PropertyInfo? DoubleBufferedProperty = typeof(Control).GetProperty(
        "DoubleBuffered",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static async Task AnimatePageTransitionAsync(Control host, Control incoming, CancellationToken cancellationToken = default)
    {
        if (host.IsDisposed || incoming.IsDisposed || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        EnableDoubleBuffering(host);
        EnableDoubleBuffering(incoming);

        var hostBounds = host.ClientRectangle;
        if (hostBounds.Width <= 0 || hostBounds.Height <= 0)
        {
            incoming.Dock = DockStyle.Fill;
            if (incoming.Parent is not null && !ReferenceEquals(incoming.Parent, host))
            {
                incoming.Parent.Controls.Remove(incoming);
            }

            if (!host.Controls.Contains(incoming))
            {
                host.Controls.Add(incoming);
            }

            incoming.BringToFront();
            return;
        }

        if (host.Controls.Count == 0)
        {
            incoming.Dock = DockStyle.Fill;
            if (incoming.Parent is not null && !ReferenceEquals(incoming.Parent, host))
            {
                incoming.Parent.Controls.Remove(incoming);
            }

            if (!host.Controls.Contains(incoming))
            {
                host.Controls.Add(incoming);
            }

            incoming.BringToFront();
            return;
        }

        var outgoing = host.Controls[0];
        if (ReferenceEquals(outgoing, incoming))
        {
            incoming.Dock = DockStyle.Fill;
            incoming.Bounds = hostBounds;
            incoming.BringToFront();
            return;
        }

        var startX = hostBounds.Left + EnterOffsetPx;
        var width = hostBounds.Width;
        var height = hostBounds.Height;
        var previousBounds = Rectangle.FromLTRB(startX, hostBounds.Top, startX + width, hostBounds.Top + height);
        var layoutSuspended = false;

        try
        {
            host.SuspendLayout();
            layoutSuspended = true;

            incoming.Dock = DockStyle.None;
            incoming.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            incoming.Visible = false;
            incoming.Bounds = previousBounds;

            if (incoming.Parent is not null && !ReferenceEquals(incoming.Parent, host))
            {
                incoming.Parent.Controls.Remove(incoming);
            }

            if (!host.Controls.Contains(incoming))
            {
                host.Controls.Add(incoming);
            }

            incoming.Visible = true;
            incoming.BringToFront();

            host.ResumeLayout(false);
            layoutSuspended = false;

            var durationTicks = DurationMs * Stopwatch.Frequency / 1000D;
            var animationStartTicks = Stopwatch.GetTimestamp();

            while (true)
            {
                if (host.IsDisposed || incoming.IsDisposed || outgoing.IsDisposed || cancellationToken.IsCancellationRequested)
                {
                    CleanupCanceledTransition(host, incoming, outgoing, hostBounds);
                    return;
                }

                var elapsedTicks = Stopwatch.GetTimestamp() - animationStartTicks;
                var progress = Math.Clamp(elapsedTicks / durationTicks, 0D, 1D);
                var eased = EaseOutQuart(progress);
                var incomingX = startX - (EnterOffsetPx * eased);
                var currentBounds = Rectangle.FromLTRB(
                    (int)Math.Round(incomingX),
                    hostBounds.Top,
                    (int)Math.Round(incomingX) + width,
                    hostBounds.Top + height);

                var dirtyRegion = Rectangle.Union(previousBounds, currentBounds);
                dirtyRegion.Inflate(2, 2);

                incoming.Bounds = currentBounds;
                host.Invalidate(dirtyRegion);
                host.Update();
                previousBounds = currentBounds;

                if (progress >= 1D)
                {
                    break;
                }

                await Task.Delay(FrameDelayMs, cancellationToken).ConfigureAwait(true);
            }

            if (host.IsDisposed || incoming.IsDisposed || cancellationToken.IsCancellationRequested)
            {
                CleanupCanceledTransition(host, incoming, outgoing, hostBounds);
                return;
            }

            host.SuspendLayout();
            layoutSuspended = true;
            incoming.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            incoming.Bounds = hostBounds;
            incoming.Dock = DockStyle.Fill;

            if (!outgoing.IsDisposed && host.Controls.Contains(outgoing))
            {
                host.Controls.Remove(outgoing);
            }

            incoming.BringToFront();
        }
        catch (OperationCanceledException)
        {
            CleanupCanceledTransition(host, incoming, outgoing, hostBounds);
            return;
        }
        catch (ObjectDisposedException)
        {
            CleanupCanceledTransition(host, incoming, outgoing, hostBounds);
            return;
        }
        finally
        {
            if (layoutSuspended && !host.IsDisposed)
            {
                try
                {
                    host.ResumeLayout(true);
                }
                catch
                {
                    // Layout teardown is best-effort only.
                }
            }
        }
    }

    private static void CleanupCanceledTransition(Control host, Control incoming, Control outgoing, Rectangle hostBounds)
    {
        try
        {
            if (!host.IsDisposed && !incoming.IsDisposed && host.Controls.Contains(incoming))
            {
                host.Controls.Remove(incoming);
            }

            if (!outgoing.IsDisposed)
            {
                outgoing.Dock = DockStyle.Fill;
                outgoing.Bounds = hostBounds;
                outgoing.BringToFront();
            }

            host.Invalidate(hostBounds);
            host.Update();
        }
        catch
        {
            // Cancellation cleanup must never surface UI exceptions.
        }
    }

    private static double EaseOutQuart(double progress)
    {
        return 1D - Math.Pow(1D - progress, 4D);
    }

    private static void EnableDoubleBuffering(Control control)
    {
        TrySetDoubleBuffered(control);
        foreach (Control child in control.Controls)
        {
            EnableDoubleBuffering(child);
        }
    }

    private static void TrySetDoubleBuffered(Control control)
    {
        if (DoubleBufferedProperty is null)
        {
            return;
        }

        try
        {
            DoubleBufferedProperty.SetValue(control, true);
        }
        catch
        {
            // Visual optimization only.
        }
    }
}
