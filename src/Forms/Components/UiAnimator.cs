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
    private const int DurationMs = 240;
    private const int EnterOffsetPx = 36;
    private const int ExitOffsetPx = 16;
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

        if (host.Controls.Count == 0)
        {
            incoming.Dock = DockStyle.Fill;
            if (!host.IsDisposed && !incoming.IsDisposed && !cancellationToken.IsCancellationRequested)
            {
                if (incoming.Parent is not null && !ReferenceEquals(incoming.Parent, host))
                {
                    incoming.Parent.Controls.Remove(incoming);
                }

                host.Controls.Add(incoming);
            }

            return;
        }

        var outgoing = host.Controls[0];
        if (ReferenceEquals(outgoing, incoming))
        {
            incoming.Dock = DockStyle.Fill;
            incoming.Bounds = host.ClientRectangle;
            return;
        }

        var hostBounds = host.ClientRectangle;
        var startX = hostBounds.Left + EnterOffsetPx;
        var startY = hostBounds.Top;
        var width = hostBounds.Width;
        var height = hostBounds.Height;
        var layoutSuspended = false;

        try
        {
            host.SuspendLayout();
            layoutSuspended = true;

            outgoing.Dock = DockStyle.None;
            outgoing.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            outgoing.Bounds = hostBounds;

            incoming.Visible = false;
            incoming.Dock = DockStyle.None;
            incoming.Bounds = new Rectangle(startX, startY, width, height);
            incoming.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            if (incoming.Parent is not null && !ReferenceEquals(incoming.Parent, host))
            {
                incoming.Parent.Controls.Remove(incoming);
            }

            host.Controls.Add(incoming);
            incoming.BringToFront();
            incoming.Visible = true;

            var durationTicks = DurationMs * Stopwatch.Frequency / 1000D;
            var animationStartTicks = Stopwatch.GetTimestamp();
            var previousIncomingBounds = RectangleF.FromLTRB(startX, startY, startX + width, startY + height);
            var previousOutgoingBounds = RectangleF.FromLTRB(hostBounds.Left, hostBounds.Top, hostBounds.Right, hostBounds.Bottom);

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
                var outgoingX = hostBounds.Left - (ExitOffsetPx * eased);

                var nextIncomingBounds = RectangleF.FromLTRB(
                    (float)incomingX,
                    startY,
                    (float)(incomingX + width),
                    startY + height);
                var nextOutgoingBounds = RectangleF.FromLTRB(
                    (float)outgoingX,
                    hostBounds.Top,
                    (float)(outgoingX + width),
                    hostBounds.Top + height);
                var dirtyRegion = Rectangle.Ceiling(RectangleF.Union(
                    RectangleF.Union(previousIncomingBounds, previousOutgoingBounds),
                    RectangleF.Union(nextIncomingBounds, nextOutgoingBounds)));
                dirtyRegion.Inflate(2, 2);

                incoming.SetBounds((int)Math.Round(incomingX), startY, width, height);
                outgoing.SetBounds((int)Math.Round(outgoingX), hostBounds.Top, width, height);

                host.Invalidate(dirtyRegion);
                host.Update();

                previousIncomingBounds = nextIncomingBounds;
                previousOutgoingBounds = nextOutgoingBounds;

                if (progress >= 1D)
                {
                    break;
                }

                await Task.Delay(FrameDelayMs, cancellationToken).ConfigureAwait(true);
            }

            if (host.IsDisposed || incoming.IsDisposed || outgoing.IsDisposed || cancellationToken.IsCancellationRequested)
            {
                CleanupCanceledTransition(host, incoming, outgoing, hostBounds);
                return;
            }

            host.SuspendLayout();
            incoming.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            incoming.Bounds = hostBounds;
            incoming.Dock = DockStyle.Fill;
            if (!host.IsDisposed && host.Controls.Contains(outgoing))
            {
                host.Controls.Remove(outgoing);
            }
            host.ResumeLayout(true);
            layoutSuspended = false;
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
                    host.ResumeLayout(false);
                }
                catch
                {
                    // Layout teardown is best-effort only.
                }
            }
        }
    }

    private static void CleanupCanceledTransition(Control host, Control incoming, Control outgoing, System.Drawing.Rectangle hostBounds)
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
            }
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
