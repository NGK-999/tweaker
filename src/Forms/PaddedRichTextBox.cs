using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class PaddedRichTextBox : RichTextBox
{
    public PaddedRichTextBox()
    {
        BorderStyle = BorderStyle.None;
        Margin = new Padding(10);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int InnerPadding { get; set; } = 10;

    protected override void OnHandleCreated(System.EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyPadding();
    }

    protected override void OnSizeChanged(System.EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyPadding();
    }

    private void ApplyPadding()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        var rectangle = new Rectangle(
            InnerPadding,
            InnerPadding,
            Math.Max(InnerPadding, Width - (InnerPadding * 2)),
            Math.Max(InnerPadding, Height - (InnerPadding * 2)));
        NativeMethods.SendMessage(
            Handle,
            NativeMethods.EmSetRect,
            0,
            ref rectangle);
    }

    private static class NativeMethods
    {
        public const int EmSetRect = 0xB3;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern nint SendMessage(nint hWnd, int msg, int wParam, ref Rectangle lParam);
    }
}
