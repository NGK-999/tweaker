using System.Drawing;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class TransparentHostPanel : Panel
{
    private static readonly Color OpaqueTextSurface = Color.FromArgb(20, 20, 22);

    public TransparentHostPanel()
    {
        SetStyle(
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true);

        UpdateStyles();
        BackColor = Color.Transparent;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExTransparent = 0x20;
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExTransparent;
            return createParams;
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (BackColor.A == 255 && BackColor != Color.Transparent)
        {
            using var solidBrush = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(solidBrush, ClientRectangle);
            return;
        }

        if (ContainsTextSurface(this))
        {
            using var brush = new SolidBrush(Parent?.BackColor ?? OpaqueTextSurface);
            e.Graphics.FillRectangle(brush, ClientRectangle);
            return;
        }

        if (Parent is null)
        {
            base.OnPaintBackground(e);
            return;
        }

        var state = e.Graphics.Save();
        e.Graphics.TranslateTransform(-Left, -Top);

        using var parentEvent = new PaintEventArgs(
            e.Graphics,
            new Rectangle(Left, Top, Parent.Width, Parent.Height));

        InvokePaintBackground(Parent, parentEvent);
        InvokePaint(Parent, parentEvent);
        e.Graphics.Restore(state);
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is not null)
        {
            NormalizeTextSurfaces(e.Control);
        }
    }

    private static bool ContainsTextSurface(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is TextBoxBase)
            {
                return true;
            }

            if (child.HasChildren && ContainsTextSurface(child))
            {
                return true;
            }
        }

        return false;
    }

    private static void NormalizeTextSurfaces(Control root)
    {
        if (root is TextBoxBase textBox)
        {
            textBox.BackColor = OpaqueTextSurface;
            textBox.Invalidate();
            return;
        }

        root.ControlAdded -= OnNestedControlAdded;
        root.ControlAdded += OnNestedControlAdded;

        foreach (Control child in root.Controls)
        {
            NormalizeTextSurfaces(child);
        }
    }

    private static void OnNestedControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is not null)
        {
            NormalizeTextSurfaces(e.Control);
        }
    }
}
