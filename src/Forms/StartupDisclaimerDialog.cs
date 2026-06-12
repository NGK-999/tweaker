using System;
using System.Drawing;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class StartupDisclaimerDialog : Form
{
    private static readonly Color Bg = Color.FromArgb(10, 15, 24);
    private static readonly Color Panel = Color.FromArgb(17, 24, 39);
    private static readonly Color Border = Color.FromArgb(45, 58, 80);
    private static readonly Color TextMain = Color.FromArgb(243, 244, 246);
    private static readonly Color TextMuted = Color.FromArgb(156, 163, 175);
    private static readonly Color Primary = Color.FromArgb(37, 99, 235);

    public StartupDisclaimerDialog()
    {
        Text = AppInfo.Name;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(600, 380);
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
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

        var acknowledgementCheckBox = CreateAcknowledgementCheckBox();

        layout.Controls.Add(CreateTitle(), 0, 0);
        layout.Controls.Add(CreateMessage(), 0, 1);
        layout.Controls.Add(acknowledgementCheckBox, 0, 2);
        layout.Controls.Add(CreateFooter(acknowledgementCheckBox), 0, 3);

        Controls.Add(layout);
    }

    private static Label CreateTitle()
    {
        return new Label
        {
            Text = "Aviso de responsabilidade",
            Dock = DockStyle.Fill,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Label CreateMessage()
    {
        return new Label
        {
            AutoSize = false,
            Text =
                "O ApexTweaker altera configuracoes avancadas do Windows, energia, Registro, rede, GPU e scheduler.\n\n" +
                "Use por sua conta e risco. O autor nao se responsabiliza por instabilidade, perda de desempenho, falhas, superaquecimento, incompatibilidades, perda de dados ou qualquer problema causado no PC.\n\n" +
                "Crie backup/ponto de restauracao antes de aplicar presets. Clique OK somente se voce entende e aceita esses riscos.",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 8, 0, 8)
        };
    }

    private static CheckBox CreateAcknowledgementCheckBox()
    {
        return new CheckBox
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Eu compreendo os riscos e assumo total responsabilidade.",
            ForeColor = TextMain,
            BackColor = Bg,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 8, 0, 0),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
    }

    private Control CreateFooter(CheckBox acknowledgementCheckBox)
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Bg,
            Padding = new Padding(0, 10, 0, 0)
        };

        var okButton = new RoundedButton
        {
            Text = "OK",
            Width = 110,
            Height = 36,
            Enabled = false,
            BackColor = Color.FromArgb(26, 32, 53),
            ForeColor = TextMuted,
            BorderColor = Border,
            HoverBackColor = Color.FromArgb(26, 32, 53),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        acknowledgementCheckBox.CheckedChanged += (_, _) =>
        {
            okButton.Enabled = acknowledgementCheckBox.Checked;
            okButton.BackColor = acknowledgementCheckBox.Checked ? Primary : Color.FromArgb(26, 32, 53);
            okButton.ForeColor = acknowledgementCheckBox.Checked ? Color.White : TextMuted;
            okButton.HoverBackColor = acknowledgementCheckBox.Checked
                ? Color.FromArgb(59, 130, 246)
                : Color.FromArgb(26, 32, 53);
            okButton.Invalidate();
        };

        okButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        footer.Controls.Add(okButton);
        return footer;
    }
}
