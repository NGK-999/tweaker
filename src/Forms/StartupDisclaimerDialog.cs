using System;
using System.Drawing;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class StartupDisclaimerDialog : Form
{
    private static readonly Color Bg = Color.FromArgb(17, 22, 37);
    private static readonly Color Panel = Color.FromArgb(26, 31, 44);
    private static readonly Color Border = Color.FromArgb(42, 50, 66);
    private static readonly Color TextMain = Color.FromArgb(255, 255, 255);
    private static readonly Color TextMuted = Color.FromArgb(139, 148, 158);
    private static readonly Color Accent = Color.FromArgb(0, 180, 216);

    public StartupDisclaimerDialog()
    {
        Text = AppInfo.Name;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(700, 430);
        BackColor = Bg;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

        var exePath = Environment.ProcessPath;
        if (exePath is not null && System.IO.File.Exists(exePath))
        {
            Icon = Icon.ExtractAssociatedIcon(exePath);
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Bg,
            Padding = new Padding(22)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));

        var acknowledgementCheckBox = CreateAcknowledgementCheckBox();

        layout.Controls.Add(CreateTitle(), 0, 0);
        layout.Controls.Add(CreateMessageCard(), 0, 1);
        layout.Controls.Add(CreateAcknowledgementPanel(acknowledgementCheckBox), 0, 2);
        layout.Controls.Add(CreateFooter(acknowledgementCheckBox), 0, 3);

        Controls.Add(layout);
    }

    private static Label CreateTitle()
    {
        return new Label
        {
            Text = "Aviso de Responsabilidade",
            Dock = DockStyle.Fill,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Control CreateMessageCard()
    {
        var frame = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            Padding = new Padding(15),
            Margin = new Padding(0, 12, 0, 12)
        };

        frame.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawRectangle(pen, 0, 0, frame.Width - 1, frame.Height - 1);
        };

        var messageBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.None,
            DetectUrls = false,
            TabStop = false,
            BackColor = Panel,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Text =
                "O ApexTweaker altera configura\u00E7\u00F5es avan\u00E7adas do Windows, energia, Registro, rede, GPU e scheduler." + Environment.NewLine + Environment.NewLine +
                "Use por sua conta e risco. O autor n\u00E3o se responsabiliza por instabilidade, perda de desempenho, falhas, superaquecimento, incompatibilidades, perda de dados ou qualquer problema causado no PC." + Environment.NewLine + Environment.NewLine +
                "Crie um backup ou ponto de restaura\u00E7\u00E3o antes de aplicar otimiza\u00E7\u00F5es. Clique em OK somente se voc\u00EA entende e aceita esses riscos."
        };

        frame.Controls.Add(messageBox);
        return frame;
    }

    private static CheckBox CreateAcknowledgementCheckBox()
    {
        return new CheckBox
        {
            AutoSize = false,
            Size = new Size(430, 34),
            MinimumSize = new Size(430, 34),
            Text = "Eu compreendo os riscos e assumo total responsabilidade.",
            ForeColor = TextMain,
            BackColor = Bg,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 4, 0, 0),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
    }

    private static Control CreateAcknowledgementPanel(CheckBox acknowledgementCheckBox)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            Padding = new Padding(15, 8, 15, 8),
            Margin = new Padding(0)
        };

        panel.Controls.Add(acknowledgementCheckBox);
        panel.Resize += (_, _) =>
        {
            acknowledgementCheckBox.Location = new Point(
                Math.Max(0, (panel.ClientSize.Width - acknowledgementCheckBox.Width) / 2),
                Math.Max(0, (panel.ClientSize.Height - acknowledgementCheckBox.Height) / 2));
        };

        return panel;
    }

    private Control CreateFooter(CheckBox acknowledgementCheckBox)
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            Padding = new Padding(15, 10, 15, 15),
            Margin = new Padding(0)
        };

        var okButton = new RoundedButton
        {
            Text = "OK",
            Width = 132,
            Height = 38,
            Enabled = false,
            BackColor = Panel,
            ForeColor = TextMuted,
            BorderColor = Border,
            HoverBackColor = Panel,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        acknowledgementCheckBox.CheckedChanged += (_, _) =>
        {
            okButton.Enabled = acknowledgementCheckBox.Checked;
            okButton.BackColor = acknowledgementCheckBox.Checked ? Accent : Panel;
            okButton.ForeColor = acknowledgementCheckBox.Checked ? Color.White : TextMuted;
            okButton.HoverBackColor = acknowledgementCheckBox.Checked
                ? Color.FromArgb(24, 196, 224)
                : Panel;
            okButton.Invalidate();
        };

        okButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        footer.Controls.Add(okButton);
        footer.Resize += (_, _) =>
        {
            okButton.Location = new Point(
                Math.Max(0, (footer.ClientSize.Width - okButton.Width) / 2),
                Math.Max(0, (footer.ClientSize.Height - okButton.Height) / 2));
        };

        return footer;
    }
}
