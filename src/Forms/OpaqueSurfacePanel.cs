using System.Drawing;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class OpaqueSurfacePanel : Panel
{
    public OpaqueSurfacePanel()
    {
        BackColor = Color.FromArgb(20, 20, 22);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint,
            true);
        UpdateStyles();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
    }
}
