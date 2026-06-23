using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ApexTweaker.NativeInterop;
using LibreHardwareMonitor.Hardware;
using Renomeador.Models;
using Renomeador.Forms.Components;
using Renomeador.Services;

namespace Renomeador.Forms;

internal sealed class ValorantTweakerForm : Form
{
    private const string AppTitle = "ApexTweaker";
    private const string RiotSupportUrl = "https://support-valorant.riotgames.com/";
    private const string DashboardPageKey = "Dashboard";
    private const string ModulesPageKey = "Modules";
    private const string TelemetryPageKey = "Telemetry";
    private const string UtilitiesPageKey = "Utilities";
    private const int SidebarButtonWidth = 180;
    private const int SidebarButtonHeight = 36;
    private static readonly string RuntimeLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApexTweaker",
        "Logs",
        "latest_runtime.log");

    private enum TelemetryVisualState
    {
        Stopped,
        Waiting,
        Active
    }

    private enum LogType
    {
        Info,
        Success,
        Warning,
        Bottleneck
    }

    private readonly record struct NativeHardwareSnapshot(
        float? CpuLoadPercent,
        float? CpuTemperatureC,
        float? GpuLoadPercent,
        float? GpuTemperatureC,
        float? RamLoadPercent,
        float? RamUsedGb);

    private const int SidebarDividerThickness = 1;
    private static readonly Color ConsoleSurface = Color.FromArgb(20, 20, 22);
    private static readonly Color Bg = ColorTranslator.FromHtml("#1E1E1E");
    private static readonly Color SidebarBg = ColorTranslator.FromHtml("#252525");
    private static readonly Color Panel = ColorTranslator.FromHtml("#2A2A2A");
    private static readonly Color PanelSoft = ColorTranslator.FromHtml("#2A2A2A");
    private static readonly Color Border = ColorTranslator.FromHtml("#3A3A3C");
    private static readonly Color GlassCardFill = ColorTranslator.FromHtml("#2A2A2A");
    private static readonly Color GlassCardBorder = ColorTranslator.FromHtml("#3A3A3C");
    private static readonly Color NeonBlue = Color.FromArgb(0, 180, 216);
    private static readonly Color TextMain = Color.FromArgb(255, 255, 255);
    private static readonly Color TextMuted = Color.FromArgb(139, 148, 158);
    private static readonly Color Accent = Color.FromArgb(0, 180, 216);
    private static readonly Color Primary = Color.FromArgb(0, 180, 216);
    private static readonly Color Danger = ColorTranslator.FromHtml("#FF453A");
    private static readonly Color Success = Color.FromArgb(0, 180, 216);
    private static readonly Color OptimizedGreen = Color.FromArgb(0, 132, 160);
    private static readonly Color Warning = Color.FromArgb(250, 204, 21);
    private static readonly Color Error = Color.FromArgb(248, 113, 113);
    private static readonly Color TerminalInfo = Color.FromArgb(160, 170, 178);
    private static readonly Color TerminalSuccess = Color.FromArgb(0, 255, 102);
    private static readonly Color TerminalWarning = Color.FromArgb(255, 204, 0);
    private static readonly Color TerminalBottleneck = Color.FromArgb(255, 0, 85);
    private static readonly PropertyInfo? DoubleBufferedProperty = typeof(Control).GetProperty(
        "DoubleBuffered",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly SystemDiagnosticsService diagnosticsService = new();
    private readonly TweakService tweakService = new();
    private readonly ValorantLocator valorantLocator = new();
    private readonly BackupService backupService = new();
    private readonly MasterRollbackService masterRollbackService = new();
    private readonly GpuOptimizationService gpuOptimizationService = new();
    private readonly OptimizationEngine optimizationEngine = new();
    private readonly HardwareTelemetryService hardwareTelemetryService = new();
    private readonly EtwFrameTracker etwFrameTracker;
    private readonly PerformanceGamerChart performanceChart = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer telemetryWatcherTimer = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer nativeHardwareTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer terminalFlushTimer = new() { Interval = 90 };

    private readonly ConsoleControl consoleView;
    private readonly Control telemetryLogFrame;
    private readonly Label statusLabel;
    private readonly Label creditsLabel;
    private readonly TableLayoutPanel rootLayout;
    private readonly Panel sidebarContainer = new();
    private Control? sidebarHeader;
    private readonly Panel contentHost = new() { Dock = DockStyle.Fill, BackColor = Bg };
    private readonly TableLayoutPanel titleBar;
    private readonly Label titleBarTitleLabel;
    private readonly Label titleBarSubtitleLabel;
    private readonly Button minimizeWindowButton;
    private readonly Button maximizeWindowButton;
    private readonly Button closeWindowButton;
    private Button? activeTabButton;
    private CancellationTokenSource? _ctsTransition;
    private readonly Dictionary<string, (Control? Instance, Func<Control> Factory)> _pageCache = new(StringComparer.OrdinalIgnoreCase);
    private string? _activePageKey;
    private string? _lastTerminalLine;
    private long _lastTerminalLineTimestamp;

    private readonly Button diagnoseButton;
    private readonly Button btnAutoOptimize;
    private readonly Button restorePointButton;
    private readonly Button gpuProfileButton;
    private readonly Button gpuRegistryButton;
    private readonly Button btnABTest;
    private readonly Button powerButton;
    private readonly Button extremeLatencyButton;
    private readonly Button cpuSchedulerButton;
    private readonly Button gpuDisplayButton;
    private readonly Button inputButton;
    private readonly Button networkButton;
    private readonly Button policyServicesButton;
    private readonly Button backgroundButton;
    private readonly Button revertButton;
    private readonly Button uninstallButton;
    private readonly Button aboutButton;
    private readonly Button openRiotSupportButton;
    private readonly Button dashboardTabButton;
    private readonly Button modulesTabButton;
    private readonly Button telemetryTabButton;
    private readonly Button utilitiesTabButton;
    private readonly System.Windows.Forms.Timer telemetryPulseTimer = new() { Interval = 450 };
    private readonly Label nativeCpuLoadLabel;
    private readonly Label nativeCpuTempLabel;
    private readonly Label nativeGpuLoadLabel;
    private readonly Label nativeGpuTempLabel;
    private readonly Label nativeRamLoadLabel;
    private readonly Label nativeRamUsedLabel;
    private readonly Label nativeDpcLatencyLabel;
    private readonly Label nativeBoostDropLabel;
    private readonly Label nativeHardwareStatusLabel;
    private Computer? nativeHardwareComputer;
    private bool nativeHardwareMonitorStarted;
    private bool nativeHardwareTickInProgress;
    private TelemetryVisualState telemetryVisualState = TelemetryVisualState.Stopped;
    private bool telemetryPulseOn;
    private bool telemetrySawTarget;
    private bool telemetryWatcherTickInProgress;
    private bool telemetryStopInProgress;
    private volatile bool telemetryUiSuspended;
    private BenchmarkState activeBenchmarkCaptureState = BenchmarkState.None;
    private bool _isTweaking;
    private volatile bool _isUiSuspended;
    private bool initialDiagnosticsRequested;
    private readonly object pendingTerminalSync = new();
    private readonly object runtimeLogSync = new();
    private readonly Queue<(string Text, Color Color)> pendingTerminalLines = [];
    private readonly List<(string Text, Color Color)> renderedTerminalLines = [];
    private bool pendingTerminalClear;
    private bool terminalFlushScheduled;
    private int renderedTerminalCharCount;
    private StreamWriter? runtimeLogWriter;

    public ValorantTweakerForm()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true);
        UpdateStyles();

        Text = AppTitle;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(1040, 680);
        ClientSize = new Size(1220, 760);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Bg;

        var exePath = Environment.ProcessPath;
        if (exePath is not null && System.IO.File.Exists(exePath))
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }

        statusLabel = CreateStatusLabel();
        creditsLabel = CreateCreditsLabel();
        consoleView = new ConsoleControl();
        telemetryLogFrame = BuildLogFrame();
        etwFrameTracker = new EtwFrameTracker(hardwareTelemetryService);
        hardwareTelemetryService.TelemetryPointRecorded += OnTelemetryPointRecorded;
        hardwareTelemetryService.MetricsSnapshotUpdated += OnTelemetryMetricsSnapshotUpdated;
        hardwareTelemetryService.DiagnosticEventRecorded += OnTelemetryDiagnosticEventRecorded;
        etwFrameTracker.Error += OnEtwFrameTrackerError;

        diagnoseButton = CreateModuleButton("Diagnosticar");
        btnAutoOptimize = CreateAutoOptimizeButton();
        restorePointButton = CreateModuleButton("Restore point");
        gpuProfileButton = CreateModuleButton("GPU Windows");
        gpuRegistryButton = CreateModuleButton("GPU regedit");
        btnABTest = CreateSecondaryButton("Iniciar Teste (Antes da Otimização)");
        EnsureABTestButtonLayout("Iniciar Teste (Antes da Otimização)");
        powerButton = CreateModuleButton("Energia");
        extremeLatencyButton = CreateModuleButton("Latência extrema");
        cpuSchedulerButton = CreateModuleButton("CPU/Scheduler");
        gpuDisplayButton = CreateModuleButton("GPU/Display");
        inputButton = CreateModuleButton("Input/USB");
        networkButton = CreateModuleButton("Rede");
        policyServicesButton = CreateModuleButton("Políticas/Serviços");
        backgroundButton = CreateModuleButton("Background");
        revertButton = CreateUtilityDangerButton("Reverter");
        uninstallButton = CreateDangerTextButton("Desinstalar e Sair");
        aboutButton = CreateSecondaryButton("Sobre");
        openRiotSupportButton = CreateSecondaryButton("Suporte Riot");
        dashboardTabButton = CreateTabButton("Dashboard", "\uE80F");
        modulesTabButton = CreateTabButton("Módulos", "\uEA86");
        telemetryTabButton = CreateTabButton("Telemetria", "\uE9D2");
        utilitiesTabButton = CreateTabButton("Utilidades", "\uE713");

        nativeCpuLoadLabel = CreateMetricValueLabel();
        nativeCpuTempLabel = CreateMetricValueLabel();
        nativeGpuLoadLabel = CreateMetricValueLabel();
        nativeGpuTempLabel = CreateMetricValueLabel();
        nativeRamLoadLabel = CreateMetricValueLabel();
        nativeRamUsedLabel = CreateMetricValueLabel();
        nativeDpcLatencyLabel = CreateMetricValueLabel("0 \u00B5s");
        nativeBoostDropLabel = CreateMetricValueLabel("0 MHz");
        nativeHardwareStatusLabel = CreateMetricValueLabel("Telemetria parcial - aguardando monitoramento");
        titleBarTitleLabel = CreateHeaderLabel(AppTitle, 11.5F, FontStyle.Bold, TextMain);
        titleBarSubtitleLabel = CreateHeaderLabel("Windows 11 Native UI | Mica | Telemetria assíncrona", 8.75F, FontStyle.Regular, TextMuted);
        minimizeWindowButton = CreateWindowCommandButton("\uE921");
        maximizeWindowButton = CreateWindowCommandButton("\uE922");
        closeWindowButton = CreateWindowCommandButton("\uE8BB", Danger, Color.FromArgb(192, 52, 64));
        titleBar = CreateTitleBar();
        InitializePageCache();

        rootLayout = CreateLayout();
        InitializeRuntimeLog();
        WireEvents();

        Controls.Add(rootLayout);
        EnsureTransparentContentHost();
        ForceDoubleBuffering(rootLayout);
        TrySetDoubleBuffered(performanceChart);
        AcceptButton = btnAutoOptimize;
        ShowFreshDashboardPage();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ctsTransition?.Cancel();
            _ctsTransition?.Dispose();
            foreach (var entry in _pageCache.Values)
            {
                if (entry.Instance is not null &&
                    !entry.Instance.IsDisposed &&
                    entry.Instance.Parent is null)
                {
                    entry.Instance.Dispose();
                }
            }

            CloseNativeHardwareMonitor();
            telemetryWatcherTimer.Stop();
            telemetryWatcherTimer.Dispose();
            nativeHardwareTimer.Stop();
            nativeHardwareTimer.Dispose();
            terminalFlushTimer.Stop();
            terminalFlushTimer.Dispose();
            telemetryPulseTimer.Dispose();
            etwFrameTracker.Dispose();
            hardwareTelemetryService.Dispose();
            DisposeRuntimeLog();
            rootLayout.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
        _ = NativeUiMethods.ApplyWindowCorners(Handle);
        _ = NativeUiMethods.TryApplyModernWindowFrame(Handle);
        EnsureTransparentContentHost();
    }

    private void WireEvents()
    {
        diagnoseButton.Click += async (_, _) => await RunDiagnosticsAsync();
        btnAutoOptimize.Click += async (_, _) => await RunAutoOptimizeAsync();
        restorePointButton.Click += async (_, _) => await CreateRestorePointAsync();
        gpuProfileButton.Click += async (_, _) => await RunTweakAsync("GPU Windows", () => tweakService.ApplyGpuWindowsProfile());
        gpuRegistryButton.Click += async (_, _) => await RunTweakAsync("GPU regedit", () => tweakService.ApplyGpuDriverRegistryProfile());
        btnABTest.Click += async (_, _) => await ToggleHardwareTelemetryAsync();
        telemetryPulseTimer.Tick += (_, _) => PulseTelemetryButton();
        telemetryWatcherTimer.Tick += async (_, _) => await TickTelemetryWatcherAsync();
        nativeHardwareTimer.Tick += async (_, _) => await TickNativeHardwareMonitorAsync();
        terminalFlushTimer.Tick += (_, _) => FlushPendingTerminalLines();
        powerButton.Click += async (_, _) => await RunTweakAsync("Energia", () => tweakService.ApplyPowerTweaks());
        extremeLatencyButton.Click += async (_, _) => await ApplyExtremeLatencyTweaksAsync();
        cpuSchedulerButton.Click += async (_, _) => await RunTweakAsync("CPU/Scheduler", () => tweakService.ApplyCpuSchedulerTweaks());
        gpuDisplayButton.Click += async (_, _) => await RunTweakAsync("GPU/Display", () => tweakService.ApplyGpuDisplayTweaks(valorantLocator.FindExecutable()));
        inputButton.Click += async (_, _) => await RunTweakAsync("Input/USB", () => tweakService.ApplyInputTweaks());
        networkButton.Click += async (_, _) => await RunNetworkTweaksWithLatencyCheckAsync();
        policyServicesButton.Click += async (_, _) => await RunTweakAsync("Políticas/Serviços", () => tweakService.ApplyPolicyAndServiceTweaks());
        backgroundButton.Click += async (_, _) => await RunTweakAsync("Background", () => tweakService.ApplyBackgroundTweaks());
        revertButton.Click += btnReverter_Click;
        uninstallButton.Click += async (_, _) => await UninstallAndExitAsync();
        aboutButton.Click += (_, _) => ShowAbout();
        openRiotSupportButton.Click += (_, _) => OpenUrl(RiotSupportUrl);
        dashboardTabButton.Click += (_, _) => ShowFreshDashboardPage();
        modulesTabButton.Click += async (_, _) => await ShowPageAsync(ModulesPageKey, modulesTabButton);
        telemetryTabButton.Click += async (_, _) => await ShowPageAsync(TelemetryPageKey, telemetryTabButton);
        utilitiesTabButton.Click += async (_, _) => await ShowUtilitiesPageAsync();
        minimizeWindowButton.Click += (_, _) => WindowState = FormWindowState.Minimized;
        maximizeWindowButton.Click += (_, _) => ToggleWindowState();
        closeWindowButton.Click += (_, _) => Close();
        titleBar.DoubleClick += (_, _) => ToggleWindowState();
        Load -= ValorantTweakerForm_Load;
        Load += ValorantTweakerForm_Load;
        Resize += ValorantTweakerForm_Resize;
        FormClosing += ValorantTweakerForm_FormClosing;
    }

    private void ValorantTweakerForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            etwFrameTracker.Dispose();
            hardwareTelemetryService.Dispose();
            WriteLine("[SHUTDOWN] Sessão ETW encerrada com sucesso no Kernel NT.");
        }
        catch
        {
            // Shutdown must never be blocked by ETW/kernel teardown failures.
        }

        try
        {
            CloseNativeHardwareMonitor();
        }
        catch
        {
            // Best effort: the form must close even when hardware drivers misbehave.
        }

        try
        {
            FlushRuntimeLog();
        }
        catch
        {
            // Logging is non-critical during process shutdown.
        }
    }

    private void ValorantTweakerForm_Resize(object? sender, EventArgs e)
    {
        maximizeWindowButton.Text = WindowState == FormWindowState.Maximized ? "\uE923" : "\uE922";

        var shouldSuspend = WindowState == FormWindowState.Minimized;
        if (_isUiSuspended == shouldSuspend)
        {
            return;
        }

        _isUiSuspended = shouldSuspend;
        performanceChart.SuppressRendering = shouldSuspend;

        if (shouldSuspend)
        {
            telemetryPulseTimer.Stop();
            return;
        }

        FlushPendingTerminalLines();
        ForceDoubleBuffering(rootLayout);
        if (telemetryVisualState == TelemetryVisualState.Waiting)
        {
            telemetryPulseTimer.Start();
        }

        performanceChart.Invalidate();
    }

    private async void ValorantTweakerForm_Load(object? sender, EventArgs e)
    {
        if (initialDiagnosticsRequested)
        {
            return;
        }

        initialDiagnosticsRequested = true;

        try
        {
            RefreshAutoOptimizeButtonState();
            await HardwareTelemetryService.CleanupOldTelemetrySessionsAsync();
            await HardwareTelemetryService.InitializeBenchmarkSessionsAsync();
            SetTelemetryButtonState(TelemetryVisualState.Stopped);
            await RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            WriteLine($"[AVISO] Inicialização parcial: {ex.Message}");
        }
    }

    private TableLayoutPanel CreateLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 214F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateSidebar(), 0, 0);
        layout.Controls.Add(CreateContentShell(), 1, 0);

        return layout;
    }

    private Control CreateSidebar()
    {
        sidebarContainer.SuspendLayout();
        sidebarContainer.Controls.Clear();

        sidebarContainer.Dock = DockStyle.Fill;
        sidebarContainer.BackColor = SidebarBg;
        sidebarContainer.Padding = new Padding(0);
        sidebarContainer.Paint -= SidebarContainer_Paint;
        sidebarContainer.Paint += SidebarContainer_Paint;

        sidebarHeader = CreateSidebarHeader();
        sidebarHeader.Dock = DockStyle.None;
        sidebarHeader.Size = new Size(SidebarButtonWidth, 70);

        ConfigureSidebarButton(dashboardTabButton);
        ConfigureSidebarButton(modulesTabButton);
        ConfigureSidebarButton(telemetryTabButton);
        ConfigureSidebarButton(utilitiesTabButton);

        creditsLabel.Dock = DockStyle.None;
        creditsLabel.AutoSize = false;
        creditsLabel.TextAlign = ContentAlignment.MiddleLeft;
        creditsLabel.Size = new Size(SidebarButtonWidth, 32);
        creditsLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

        sidebarContainer.Controls.Add(sidebarHeader);
        sidebarContainer.Controls.Add(dashboardTabButton);
        sidebarContainer.Controls.Add(modulesTabButton);
        sidebarContainer.Controls.Add(telemetryTabButton);
        sidebarContainer.Controls.Add(utilitiesTabButton);
        sidebarContainer.Controls.Add(creditsLabel);

        LayoutSidebarChrome(sidebarContainer);
        sidebarContainer.Resize -= SidebarContainer_Resize;
        sidebarContainer.Resize += SidebarContainer_Resize;
        sidebarContainer.ResumeLayout(false);

        return sidebarContainer;
    }

    private void SidebarContainer_Resize(object? sender, EventArgs e)
    {
        LayoutSidebarChrome(sidebarContainer);
    }

    private static void SidebarContainer_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Control sidebar)
        {
            return;
        }

        using var dividerPen = new Pen(Color.FromArgb(45, 45, 45), SidebarDividerThickness);
        var x = Math.Max(0, sidebar.ClientSize.Width - SidebarDividerThickness);
        e.Graphics.DrawLine(dividerPen, x, 0, x, sidebar.ClientSize.Height);
    }

    private void LayoutSidebarChrome(Control sidebar)
    {
        var centeredX = Math.Max(12, ((sidebar.ClientSize.Width - SidebarDividerThickness) - SidebarButtonWidth) / 2);

        if (sidebarHeader is not null)
        {
            sidebarHeader.Location = new Point(centeredX, 18);
            sidebarHeader.Size = new Size(SidebarButtonWidth, 70);
        }

        dashboardTabButton.Location = new Point(centeredX, 104);
        modulesTabButton.Location = new Point(centeredX, 148);
        telemetryTabButton.Location = new Point(centeredX, 192);
        utilitiesTabButton.Location = new Point(centeredX, 236);
        creditsLabel.Location = new Point(centeredX, Math.Max(12, sidebar.ClientSize.Height - creditsLabel.Height - 18));
    }

    private static void ConfigureSidebarButton(Button button)
    {
        button.Size = new Size(SidebarButtonWidth, SidebarButtonHeight);
        button.MinimumSize = new Size(SidebarButtonWidth, SidebarButtonHeight);
        button.MaximumSize = new Size(SidebarButtonWidth, SidebarButtonHeight);
        button.AutoSize = false;
        button.Dock = DockStyle.None;
        button.BringToFront();

        ApplyRoundedRegion(button, 10);
    }

    private Control CreateSidebarHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SidebarBg,
            ColumnCount = 1,
            RowCount = 2
        };

        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        header.Controls.Add(CreateHeaderLabel(AppTitle, 18F, FontStyle.Bold, TextMain), 0, 0);
        header.Controls.Add(CreateHeaderLabel($"v{AppInfo.Version}", 9.5F, FontStyle.Regular, TextMuted), 0, 1);
        return header;
    }

    private Control CreateContentShell()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14, 12, 14, 12)
        };

        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        shell.Controls.Add(titleBar, 0, 0);
        shell.Controls.Add(contentHost, 0, 1);
        shell.Controls.Add(statusLabel, 0, 2);
        return shell;
    }

    private TableLayoutPanel CreateTitleBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(12, 6, 8, 6)
        };

        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        titleStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
        titleBarTitleLabel.Dock = DockStyle.Fill;
        titleBarSubtitleLabel.Dock = DockStyle.Fill;
        titleStack.Controls.Add(titleBarTitleLabel, 0, 0);
        titleStack.Controls.Add(titleBarSubtitleLabel, 0, 1);

        var windowButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 1, 0, 0),
            Padding = new Padding(0)
        };

        windowButtons.Controls.Add(minimizeWindowButton);
        windowButtons.Controls.Add(maximizeWindowButton);
        windowButtons.Controls.Add(closeWindowButton);

        bar.Controls.Add(titleStack, 0, 0);
        bar.Controls.Add(windowButtons, 1, 0);
        ForceDoubleBuffering(bar);
        return bar;
    }

    private Control CreateMainArea()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.Controls.Add(CreateActionArea(), 0, 0);
        content.Controls.Add(CreateLogPanel(), 1, 0);
        return content;
    }

    private Control CreateLogPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            Margin = new Padding(8, 0, 0, 0)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.Controls.Add(CreateHeaderLabel("Console", 10F, FontStyle.Bold, Accent), 0, 0);
        panel.Controls.Add(CreateLogFrame(), 0, 1);
        return panel;
    }

    private Control CreateLogFrame()
    {
        return telemetryLogFrame;
    }

    private Control BuildLogFrame()
    {
        var frame = new OpaqueSurfacePanel
        {
            Dock = DockStyle.Fill,
            BackColor = ConsoleSurface,
            Padding = new Padding(1),
            Margin = new Padding(0)
        };

        consoleView.Dock = DockStyle.Fill;
        consoleView.BackColor = ConsoleSurface;
        frame.Controls.Add(consoleView);
        return frame;
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2
        };

        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        header.Controls.Add(CreateHeaderLabel(AppTitle, 20F, FontStyle.Bold, TextMain), 0, 0);
        header.Controls.Add(CreateHeaderLabel("Performance extrema | Frametime consistente | Backups reversíveis", 9.5F, FontStyle.Regular, TextMuted), 0, 1);
        return header;
    }

    private void InitializePageCache()
    {
        _pageCache[DashboardPageKey] = (null, CreateDashboardPage);
        _pageCache[ModulesPageKey] = (null, CreateModulesPage);
        _pageCache[TelemetryPageKey] = (null, CreateTelemetryPage);
        _pageCache[UtilitiesPageKey] = (null, CreateUtilitiesPage);
    }

    private Control GetOrCreatePage(string pageKey)
    {
        if (!_pageCache.TryGetValue(pageKey, out var entry))
        {
            throw new ArgumentOutOfRangeException(nameof(pageKey), pageKey, "Página não registrada no cache.");
        }

        var page = entry.Instance;
        if (page is null || page.IsDisposed)
        {
            page = entry.Factory();
            ForceDoubleBuffering(page);
            _pageCache[pageKey] = (page, entry.Factory);
        }

        try
        {
            _ = page.Handle;
        }
        catch (ObjectDisposedException)
        {
            page = entry.Factory();
            ForceDoubleBuffering(page);
            _pageCache[pageKey] = (page, entry.Factory);
            _ = page.Handle;
        }

        return page;
    }

    private void ShowPage(Control page, Button tabButton, string pageKey)
    {
        EnsureTransparentContentHost();

        if (!ReferenceEquals(page.Parent, contentHost))
        {
            RemoveHostedPages();
        }

        if (page.Parent is not null && !ReferenceEquals(page.Parent, contentHost))
        {
            page.Parent.Controls.Remove(page);
        }

        page.BackColor = Color.Transparent;
        page.Dock = DockStyle.Fill;
        if (!contentHost.Controls.Contains(page))
        {
            contentHost.Controls.Add(page);
        }

        _activePageKey = pageKey;
        RestorePageVisualState(pageKey, page);
        page.BringToFront();
        SetActiveTab(tabButton);
        contentHost.Refresh();
    }

    private async System.Threading.Tasks.Task ShowPageAsync(string pageKey, Button tabButton)
    {
        if (string.Equals(pageKey, DashboardPageKey, StringComparison.Ordinal))
        {
            ShowFreshDashboardPage();
            return;
        }

        if (string.Equals(_activePageKey, pageKey, StringComparison.Ordinal))
        {
            return;
        }

        var page = GetOrCreatePage(pageKey);
        page.Dock = DockStyle.Fill;
        ForceDoubleBuffering(page);
        SetActiveTab(tabButton);

        if (RequiresStaticPageSwap(pageKey))
        {
            AttachPageStatic(page, tabButton, pageKey);
            return;
        }

        _ctsTransition?.Cancel();
        _ctsTransition?.Dispose();
        _ctsTransition = new CancellationTokenSource();

        try
        {
            await UiAnimator.AnimatePageTransitionAsync(contentHost, page, _ctsTransition.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException)
        {
            AttachPageStatic(page, tabButton, pageKey);
            return;
        }
        catch (Exception)
        {
            AttachPageStatic(page, tabButton, pageKey);
            return;
        }

        RestorePageVisualState(pageKey, page);
        _activePageKey = pageKey;
        contentHost.Refresh();
    }

    private bool RequiresStaticPageSwap(string pageKey)
    {
        return string.Equals(pageKey, TelemetryPageKey, StringComparison.Ordinal) ||
               string.Equals(pageKey, DashboardPageKey, StringComparison.Ordinal) ||
               string.Equals(_activePageKey, TelemetryPageKey, StringComparison.Ordinal) ||
               string.Equals(_activePageKey, DashboardPageKey, StringComparison.Ordinal);
    }

    private void ShowFreshDashboardPage()
    {
        EnsureTransparentContentHost();

        _ctsTransition?.Cancel();
        _ctsTransition?.Dispose();
        _ctsTransition = null;

        DashboardPage? previousDashboard = null;
        if (_pageCache.TryGetValue(DashboardPageKey, out var cachedEntry) &&
            cachedEntry.Instance is DashboardPage cachedDashboard &&
            !cachedDashboard.IsDisposed)
        {
            previousDashboard = cachedDashboard;
        }

        var dashboard = CreateDashboardPage();
        dashboard.Dock = DockStyle.Fill;
        dashboard.Visible = true;
        ForceDoubleBuffering(dashboard);
        _pageCache[DashboardPageKey] = (dashboard, CreateDashboardPage);

        contentHost.SuspendLayout();
        try
        {
            contentHost.Controls.Clear();
            contentHost.Controls.Add(dashboard);
            dashboard.BringToFront();
            SetActiveTab(dashboardTabButton);
            _activePageKey = DashboardPageKey;
        }
        finally
        {
            contentHost.ResumeLayout(true);
        }

        RestorePageVisualState(DashboardPageKey, dashboard);

        if (previousDashboard is not null &&
            !ReferenceEquals(previousDashboard, dashboard) &&
            !previousDashboard.IsDisposed)
        {
            previousDashboard.Dispose();
        }

        contentHost.Invalidate();
        contentHost.Update();
        contentHost.Refresh();
    }

    private async System.Threading.Tasks.Task ShowUtilitiesPageAsync()
    {
        await ShowPageAsync(UtilitiesPageKey, utilitiesTabButton);
        _ = EnsureNativeHardwareMonitorStartedAsync();
    }

    private void AttachPageStatic(Control page, Button tabButton, string pageKey)
    {
        EnsureTransparentContentHost();
        RemoveHostedPages();
        if (page.Parent is not null && !ReferenceEquals(page.Parent, contentHost))
        {
            page.Parent.Controls.Remove(page);
        }

        page.BackColor = Color.Transparent;
        page.Dock = DockStyle.Fill;
        _activePageKey = pageKey;
        contentHost.Controls.Add(page);
        RestorePageVisualState(pageKey, page);
        page.BringToFront();
        SetActiveTab(tabButton);
        contentHost.Refresh();
    }

    private void RemoveHostedPages()
    {
        for (var index = contentHost.Controls.Count - 1; index >= 0; index--)
        {
            contentHost.Controls.RemoveAt(index);
        }
    }

    private void EnsureTransparentContentHost()
    {
        contentHost.BackColor = Bg;

        if (contentHost.Parent is not null)
        {
            contentHost.Parent.BackColor = Bg;
        }

        contentHost.Invalidate();
    }

    private void RestorePageVisualState(string pageKey, Control page)
    {
        if (string.Equals(pageKey, DashboardPageKey, StringComparison.Ordinal) &&
            page is DashboardPage dashboardPage)
        {
            dashboardPage.RestoreVisualState();
            contentHost.Invalidate();
            contentHost.Update();
        }

        if (string.Equals(pageKey, TelemetryPageKey, StringComparison.Ordinal))
        {
            FlushPendingTerminalLines();
            RestoreConsoleBuffer();
            RefreshConsoleSurface();
            performanceChart.Invalidate();
            contentHost.Invalidate();
            contentHost.Update();
        }
    }

    private void RestoreConsoleBuffer()
    {
        consoleView.CreateControl();

        if (!consoleView.IsSurfaceReady || renderedTerminalLines.Count == 0)
        {
            return;
        }

        consoleView.SetEntries(renderedTerminalLines);
    }

    private async System.Threading.Tasks.Task EnsureNativeHardwareMonitorStartedAsync()
    {
        try
        {
            await StartNativeHardwareMonitorAsync();
        }
        catch (Exception ex)
        {
            WriteLine($"[AVISO] Monitor nativo de hardware não iniciou: {ex.Message}");
        }
    }

    private void SetActiveTab(Button selectedButton)
    {
        var inactiveTextColor = Color.FromArgb(200, 200, 200);
        var sidebarButtons = new[]
        {
            dashboardTabButton,
            modulesTabButton,
            telemetryTabButton,
            utilitiesTabButton
        };

        foreach (var button in sidebarButtons)
        {
            button.BackColor = Color.Transparent;
            button.ForeColor = inactiveTextColor;

            if (button is SidebarNavButton navButton)
            {
                navButton.IsSelected = false;
            }

            button.Invalidate();
        }

        activeTabButton = selectedButton;
        activeTabButton.ForeColor = Color.White;

        if (activeTabButton is SidebarNavButton activeNavButton)
        {
            activeNavButton.IsSelected = true;
        }

        activeTabButton.Invalidate();
        sidebarContainer.Invalidate();
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private void PostToUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeUiMethods.WM_NCHITTEST)
        {
            base.WndProc(ref m);
            if ((int)m.Result == NativeUiMethods.HTCLIENT)
            {
                var hitTest = ResolveChromeHitTest(m.LParam);
                if (hitTest != NativeUiMethods.HTCLIENT)
                {
                    m.Result = (nint)hitTest;
                    return;
                }
            }

            return;
        }

        base.WndProc(ref m);
    }

    private int ResolveChromeHitTest(nint lParam)
    {
        if (WindowState == FormWindowState.Maximized)
        {
            return IsPointInTitleBar(GetScreenPointFromLParam(lParam))
                ? NativeUiMethods.HTCAPTION
                : NativeUiMethods.HTCLIENT;
        }

        const int resizeBorder = 8;
        var screenPoint = GetScreenPointFromLParam(lParam);
        var clientPoint = PointToClient(screenPoint);

        var onLeft = clientPoint.X <= resizeBorder;
        var onRight = clientPoint.X >= ClientSize.Width - resizeBorder;
        var onTop = clientPoint.Y <= resizeBorder;
        var onBottom = clientPoint.Y >= ClientSize.Height - resizeBorder;

        if (onLeft && onTop)
        {
            return NativeUiMethods.HTTOPLEFT;
        }

        if (onRight && onTop)
        {
            return NativeUiMethods.HTTOPRIGHT;
        }

        if (onLeft && onBottom)
        {
            return NativeUiMethods.HTBOTTOMLEFT;
        }

        if (onRight && onBottom)
        {
            return NativeUiMethods.HTBOTTOMRIGHT;
        }

        if (onLeft)
        {
            return NativeUiMethods.HTLEFT;
        }

        if (onRight)
        {
            return NativeUiMethods.HTRIGHT;
        }

        if (onTop)
        {
            return NativeUiMethods.HTTOP;
        }

        if (onBottom)
        {
            return NativeUiMethods.HTBOTTOM;
        }

        return IsPointInTitleBar(screenPoint)
            ? NativeUiMethods.HTCAPTION
            : NativeUiMethods.HTCLIENT;
    }

    private bool IsPointInTitleBar(Point screenPoint)
    {
        if (!titleBar.Visible || !titleBar.IsHandleCreated)
        {
            return false;
        }

        var titleBounds = titleBar.RectangleToScreen(titleBar.ClientRectangle);
        if (!titleBounds.Contains(screenPoint))
        {
            return false;
        }

        return !IsPointOverInteractiveControl(screenPoint, minimizeWindowButton) &&
               !IsPointOverInteractiveControl(screenPoint, maximizeWindowButton) &&
               !IsPointOverInteractiveControl(screenPoint, closeWindowButton);
    }

    private static bool IsPointOverInteractiveControl(Point screenPoint, Control control)
    {
        return control.Visible &&
               control.IsHandleCreated &&
               control.RectangleToScreen(control.ClientRectangle).Contains(screenPoint);
    }

    private static Point GetScreenPointFromLParam(nint lParam)
    {
        var value = lParam.ToInt64();
        return new Point(
            unchecked((short)(value & 0xFFFF)),
            unchecked((short)((value >> 16) & 0xFFFF)));
    }

    private static void ForceDoubleBuffering(Control control)
    {
        if (control is System.Windows.Forms.Panel or TableLayoutPanel or FlowLayoutPanel)
        {
            TrySetDoubleBuffered(control);
        }

        foreach (Control child in control.Controls)
        {
            ForceDoubleBuffering(child);
        }
    }

    private static void TrySetDoubleBuffered(Control control)
    {
        try
        {
            DoubleBufferedProperty?.SetValue(control, true);
        }
        catch
        {
            // Reflection aqui é apenas otimização visual; falha não deve afetar a UI.
        }
    }

    private Control CreateDashboardPage()
    {
        return new DashboardPage(btnAutoOptimize, restorePointButton)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
    }

    private Control CreateModulesPage()
    {
        var page = CreatePageGrid(1);
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        page.Controls.Add(CreateCard("Módulos do Sistema", CreateGroupedModulesPanel()), 0, 0);
        return page;
    }

    private Control CreateGroupedModulesPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 6,
            Margin = new Padding(0),
            Padding = new Padding(0, 6, 0, 0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateGroupedModuleSection(
            "Otimizações Core",
            4,
            cpuSchedulerButton,
            gpuDisplayButton,
            powerButton,
            extremeLatencyButton), 0, 0);
        layout.Controls.Add(CreateSectionDivider(), 0, 1);
        layout.Controls.Add(CreateGroupedModuleSection(
            "Rede e Periféricos",
            3,
            inputButton,
            networkButton,
            policyServicesButton), 0, 2);
        layout.Controls.Add(CreateSectionDivider(), 0, 3);
        layout.Controls.Add(CreateGroupedModuleSection(
            "GPU e Background",
            3,
            gpuProfileButton,
            gpuRegistryButton,
            backgroundButton), 0, 4);
        layout.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        }, 0, 5);

        return layout;
    }

    private static Control CreateGroupedModuleSection(string title, int columns, params Control[] buttons)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 8)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(CreateHeaderLabel(title, 10F, FontStyle.Bold, TextMain), 0, 0);
        layout.Controls.Add(CreateModuleButtonRow(columns, buttons), 0, 1);
        return layout;
    }

    private static Control CreateModuleButtonRow(int columns, params Control[] buttons)
    {
        var normalizedColumns = Math.Max(1, columns);
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = normalizedColumns,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0, 6, 0, 0)
        };

        for (var column = 0; column < normalizedColumns; column++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / normalizedColumns));
        }

        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        for (var index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            ConfigureModuleActionButton(button);

            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Panel,
                Margin = index < buttons.Length - 1 ? new Padding(0, 0, 12, 0) : new Padding(0),
                Padding = new Padding(0)
            };

            host.Controls.Add(button);
            row.Controls.Add(host, index, 0);
        }

        return row;
    }

    private static Control CreateSectionDivider()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Height = 1,
            Margin = new Padding(0),
            BackColor = Color.FromArgb(45, 45, 45)
        };
    }

    private Control CreateTelemetryPage()
    {
        var page = CreatePageGrid(3);
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

        var abTestCard = CreateCard("Teste A/B de Estabilidade", CreateABTestPanel());
        abTestCard.Height = 112;
        abTestCard.MinimumSize = new Size(0, 96);

        page.Controls.Add(abTestCard, 0, 0);
        page.Controls.Add(CreateCard("Gráfico em tempo real", performanceChart, Border), 0, 1);
        page.Controls.Add(CreateCard("Console", CreateLogFrame(), Border), 0, 2);
        return page;
    }

    private Control CreateABTestPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0),
            Height = 52,
            MinimumSize = new Size(0, 52)
        };

        EnsureABTestButtonLayout(GetTelemetryStoppedButtonText());
        panel.Controls.Add(btnABTest);
        btnABTest.BringToFront();
        return panel;
    }

    private void EnsureABTestButtonLayout(string? text = null)
    {
        btnABTest.Visible = true;
        btnABTest.Height = 40;
        btnABTest.MinimumSize = new Size(240, 40);
        btnABTest.Dock = DockStyle.Top;
        btnABTest.Margin = new Padding(0);
        btnABTest.Text = string.IsNullOrWhiteSpace(text)
            ? "Iniciar Teste (Antes da Otimização)"
            : text;
    }

    private Control CreateUtilitiesPage()
    {
        var page = CreatePageGrid(2);
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 124F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(CreateCard("Utilidades e Suporte", CreateUtilitiesSupportPanel()), 0, 0);
        page.Controls.Add(CreateHardwareHub(), 0, 1);
        return page;
    }

    private Control CreateUtilitiesSupportPanel()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 8),
            MinimumSize = new Size(0, 48)
        };

        host.SuspendLayout();
        host.Controls.Clear();

        ConfigureUtilityActionButton(revertButton, Danger, Color.White, new Point(12, 10));
        ConfigureUtilityActionButton(uninstallButton, Color.FromArgb(38, 38, 40), TextMain, new Point(178, 10));
        ConfigureUtilityActionButton(aboutButton, Color.FromArgb(38, 38, 40), TextMain, new Point(344, 10));
        ConfigureUtilityActionButton(openRiotSupportButton, Color.FromArgb(38, 38, 40), TextMain, new Point(510, 10));

        host.Controls.Add(revertButton);
        host.Controls.Add(uninstallButton);
        host.Controls.Add(aboutButton);
        host.Controls.Add(openRiotSupportButton);
        host.ResumeLayout(false);
        return host;
    }

    private Control CreateHardwareHub()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        grid.Controls.Add(CreateTelemetryCard(
            "CPU",
            CreateMetricPanel(
                ("Uso total", nativeCpuLoadLabel),
                ("Temperatura", nativeCpuTempLabel),
                ("P-Core boost", nativeBoostDropLabel))), 0, 0);

        grid.Controls.Add(CreateTelemetryCard(
            "GPU",
            CreateMetricPanel(
                ("Uso 3D", nativeGpuLoadLabel),
                ("Temperatura", nativeGpuTempLabel))), 1, 0);

        grid.Controls.Add(CreateTelemetryCard(
            "Memória",
            CreateMetricPanel(
                ("Uso físico", nativeRamLoadLabel),
                ("RAM usada", nativeRamUsedLabel))), 0, 1);

        grid.Controls.Add(CreateTelemetryCard(
            "Kernel / ETW",
            CreateKernelTelemetryPanel()), 1, 1);
        return grid;
    }

    private static Control CreateTelemetryCard(string title, Control content)
    {
        var card = (GamerCard)CreateCard(title, content, Border);
        card.FillColor = Panel;
        card.BackColor = Color.Transparent;
        return card;
    }

    private Control CreateKernelTelemetryPanel()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8, 6, 8, 6)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var dpcLabel = CreateHeaderLabel("Pico DPC", 10F, FontStyle.Regular, TextMuted);
        dpcLabel.TextAlign = ContentAlignment.MiddleCenter;

        nativeDpcLatencyLabel.Dock = DockStyle.Fill;
        nativeDpcLatencyLabel.TextAlign = ContentAlignment.MiddleCenter;

        var statusLabelTitle = CreateHeaderLabel("Status da telemetria", 10F, FontStyle.Regular, TextMuted);
        statusLabelTitle.TextAlign = ContentAlignment.MiddleCenter;

        nativeHardwareStatusLabel.Dock = DockStyle.Fill;
        nativeHardwareStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
        nativeHardwareStatusLabel.AutoEllipsis = true;
        nativeHardwareStatusLabel.Padding = new Padding(4, 0, 4, 0);

        grid.Controls.Add(dpcLabel, 0, 0);
        grid.Controls.Add(nativeDpcLatencyLabel, 0, 1);
        grid.Controls.Add(statusLabelTitle, 0, 2);
        grid.Controls.Add(nativeHardwareStatusLabel, 0, 3);
        return grid;
    }

    private static Control CreateSingleMetricPanel(string name, Label valueLabel)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 6, 8, 6)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var nameLabel = new Label
        {
            Text = name,
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        valueLabel.AutoEllipsis = true;
        valueLabel.Padding = new Padding(4, 0, 4, 0);

        grid.Controls.Add(nameLabel, 0, 0);
        grid.Controls.Add(valueLabel, 0, 1);
        return grid;
    }

    private static Control CreateMetricPanel(params (string Name, Label ValueLabel)[] metrics)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 3,
            RowCount = Math.Max(1, metrics.Length),
            Padding = new Padding(10, 10, 10, 10),
            Margin = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (var row = 0; row < metrics.Length; row++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / metrics.Length));

            var nameLabel = new Label
            {
                Text = metrics[row].Name,
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            };

            metrics[row].ValueLabel.Dock = DockStyle.Fill;
            metrics[row].ValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            metrics[row].ValueLabel.AutoEllipsis = true;
            metrics[row].ValueLabel.Margin = new Padding(0);
            metrics[row].ValueLabel.Padding = new Padding(0);

            grid.Controls.Add(nameLabel, 0, row);
            grid.Controls.Add(metrics[row].ValueLabel, 2, row);
        }

        return grid;
    }

    private static TableLayoutPanel CreatePageGrid(int rows)
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = rows,
            Padding = new Padding(0)
        };

        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return page;
    }

    private static Control CreateCard(string title, Control content, Color? borderColor = null)
    {
        var card = new GamerCard
        {
            Dock = DockStyle.Fill,
            FillColor = GlassCardFill,
            BorderColor = borderColor ?? GlassCardBorder
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(CreateHeaderLabel(title, 10F, FontStyle.Bold, Accent), 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CreateButtonGridFilled(int rows, int columns, params Control[] buttons)
    {
        var grid = CreateButtonGrid(rows, columns);
        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].Dock = DockStyle.Fill;
            grid.Controls.Add(buttons[i], i % columns, i / columns);
        }

        return grid;
    }

    private static Control CreateSummaryText()
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.TopLeft,
            Text = "Use o Auto-Tuning para aplicar a melhor configuração automaticamente. Use Módulos apenas para ajustes específicos. Use Telemetria para investigar gargalos e micro-stuttering com sensores em tempo real."
        };
    }

    private Control CreateActionArea()
    {
        var area = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        area.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        area.Controls.Add(CreateControlPanel(), 0, 0);
        return area;
    }

    private Control CreateControlPanel()
    {
        var panel = CreatePanel();
        panel.RowCount = 6;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

        panel.Controls.Add(CreateHeaderLabel("Controle de otimização", 13F, FontStyle.Bold, TextMain), 0, 0);
        panel.Controls.Add(CreateHeaderLabel("Analise o hardware e aplique automaticamente o perfil mais agressivo suportado.", 9.2F, FontStyle.Regular, TextMuted), 0, 1);
        panel.Controls.Add(CreateGlobalCommandPanel(), 0, 2);
        panel.Controls.Add(CreateHeaderLabel("Módulos por categoria", 10F, FontStyle.Bold, Accent), 0, 3);
        panel.Controls.Add(CreateCategoryGrid(), 0, 4);
        panel.Controls.Add(CreateUtilitiesSupportPanel(), 0, 5);
        return panel;
    }

    private Control CreateGlobalCommandPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(8, 5, 8, 5),
            Margin = new Padding(0, 2, 0, 6)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        btnAutoOptimize.Dock = DockStyle.Fill;
        panel.Controls.Add(btnAutoOptimize, 0, 0);
        return panel;
    }

    private Control CreateCategoryGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

        grid.Controls.Add(CreateCategoryPanel("Segurança do Sistema", 1, restorePointButton), 0, 0);
        grid.Controls.Add(CreateCategoryPanel("Otimizações Core", 4, cpuSchedulerButton, gpuDisplayButton, powerButton, extremeLatencyButton), 0, 1);
        grid.Controls.Add(CreateCategoryPanel("Rede e Periféricos", 3, inputButton, networkButton, policyServicesButton), 0, 2);
        grid.Controls.Add(CreateCategoryPanel("Avançado / Específicos", 3, gpuProfileButton, gpuRegistryButton, backgroundButton, diagnoseButton), 0, 3);

        return grid;
    }

    private static Control CreateCategoryPanel(string title, int columns, params Control[] buttons)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelSoft,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 6)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.Controls.Add(CreateHeaderLabel(title, 8.8F, FontStyle.Bold, Accent), 0, 0);

        var buttonFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelSoft,
            AutoScroll = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4, 2, 4, 2),
            Margin = new Padding(0)
        };

        foreach (var button in buttons)
        {
            button.Dock = DockStyle.None;
            buttonFlow.Controls.Add(button);
        }

        buttonFlow.Resize += (_, _) => ResizeCategoryButtons(buttonFlow, columns);
        ResizeCategoryButtons(buttonFlow, columns);

        panel.Controls.Add(buttonFlow, 0, 1);
        return panel;
    }

    private static void ResizeCategoryButtons(FlowLayoutPanel flow, int columns)
    {
        var availableWidth = Math.Max(1, flow.ClientSize.Width - 8);
        var buttonWidth = Math.Max(110, (availableWidth / columns) - 10);

        foreach (Control control in flow.Controls)
        {
            control.Dock = DockStyle.None;
            control.Width = buttonWidth;
            control.Height = 30;
            control.Margin = new Padding(5, 3, 5, 3);
        }
    }

    private static void ConfigureUtilityActionButton(Button button, Color backColor, Color foreColor, Point location)
    {
        button.SuspendLayout();
        button.Dock = DockStyle.None;
        button.AutoSize = false;
        button.Size = new Size(148, 30);
        button.MinimumSize = new Size(148, 30);
        button.MaximumSize = new Size(148, 30);
        button.Location = location;
        button.Margin = new Padding(0);
        button.Padding = new Padding(0);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.BackColor = backColor;
        button.ForeColor = foreColor;

        if (button is RoundedButton rounded)
        {
            rounded.BorderRadius = 9;
            rounded.BorderColor = ColorTranslator.FromHtml("#333333");
            rounded.NormalBorderColor = ColorTranslator.FromHtml("#333333");
            rounded.HoverBorderColor = Color.FromArgb(72, 72, 74);
            rounded.HoverBackColor = Color.FromArgb(
                Math.Min(backColor.R + 10, 255),
                Math.Min(backColor.G + 10, 255),
                Math.Min(backColor.B + 10, 255));
        }

        button.ResumeLayout(false);
    }

    private static void ConfigureModuleActionButton(Control button)
    {
        button.SuspendLayout();
        button.Size = new Size(160, 45);
        button.MinimumSize = new Size(0, 45);
        button.MaximumSize = Size.Empty;
        button.AutoSize = false;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0);
        button.BackColor = Color.FromArgb(38, 38, 40);
        button.ForeColor = Color.White;

        if (button is Button standardButton)
        {
            standardButton.TextAlign = ContentAlignment.MiddleCenter;
            standardButton.UseVisualStyleBackColor = false;
        }

        if (button is RoundedButton rounded)
        {
            rounded.BorderRadius = 10;
            rounded.BorderColor = Color.FromArgb(50, 50, 52);
            rounded.NormalBorderColor = Color.FromArgb(50, 50, 52);
            rounded.HoverBorderColor = Color.FromArgb(72, 72, 74);
            rounded.HoverBackColor = Color.FromArgb(48, 48, 50);
        }

        button.ResumeLayout(false);
    }

    private static TableLayoutPanel CreateButtonGrid(int rows, int columns)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = columns,
            RowCount = rows,
            Margin = new Padding(0),
            Padding = new Padding(2),
            MinimumSize = new Size(0, rows * 32)
        };

        for (var column = 0; column < columns; column++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        }

        for (var row = 0; row < rows; row++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
        }

        return grid;
    }

    private static TableLayoutPanel CreatePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(14),
            BackColor = Color.Transparent,
            ColumnCount = 1
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return panel;
    }

    private static Label CreateHeaderLabel(string text, float size, FontStyle style, Color color)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", size, style, GraphicsUnit.Point),
            ForeColor = color
        };
    }

    private static Label CreateStatusLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Accent,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Color.Transparent
        };
    }

    private static Label CreateMetricValueLabel(string text = "--")
    {
        return new Label
        {
            AutoSize = false,
            Text = text,
            ForeColor = TextMain,
            Font = new Font("Consolas", 12F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
    }

    private static Label CreateCreditsLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = TextMuted,
            Text = $"{AppInfo.Credits}  |  v{AppInfo.Version}"
        };
    }

    private static Button CreatePrimaryButton(string text)
    {
        return CreateButton(text, Primary, Color.White);
    }

    private static Button CreateAutoOptimizeButton()
    {
        var button = CreateButton("\u26A1 OTIMIZAR SISTEMA AO MÁXIMO", Primary, Color.White);
        button.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
        button.MinimumSize = new Size(260, 46);
        button.Height = 52;
        if (button is RoundedButton rounded)
        {
            rounded.BorderColor = Accent;
            rounded.HoverBackColor = Color.FromArgb(24, 196, 224);
        }

        return button;
    }

    private void RefreshAutoOptimizeButtonState()
    {
        if (optimizationEngine.CheckIfAlreadyOptimized())
        {
            SetAutoOptimizeOptimizedState();
            return;
        }

        SetAutoOptimizeDefaultState();
    }

    private void SetAutoOptimizeDefaultState()
    {
        SetAutoOptimizeVisualState(
            "\u26A1 OTIMIZAR SISTEMA AO MÁXIMO",
            Primary,
            Accent,
            Color.FromArgb(24, 196, 224));
    }

    private void SetAutoOptimizeApplyingState()
    {
        SetAutoOptimizeVisualState(
            "\u2699\uFE0F Aplicando Otimizações...",
            Color.FromArgb(0, 132, 160),
            Warning,
            Color.FromArgb(24, 196, 224));
    }

    private void SetAutoOptimizeOptimizedState()
    {
        SetAutoOptimizeVisualState(
            "\u2713 SISTEMA JÁ OTIMIZADO AO MÁXIMO",
            OptimizedGreen,
            Accent,
            Color.FromArgb(24, 196, 224));
    }

    private void SetAutoOptimizeVisualState(string text, Color backColor, Color borderColor, Color hoverBackColor)
    {
        btnAutoOptimize.Text = text;
        btnAutoOptimize.BackColor = backColor;
        btnAutoOptimize.ForeColor = Color.White;

        if (btnAutoOptimize is RoundedButton rounded)
        {
            rounded.BorderColor = borderColor;
            rounded.NormalBorderColor = borderColor;
            rounded.HoverBorderColor = Accent;
            rounded.HoverBackColor = hoverBackColor;
        }

        btnAutoOptimize.Invalidate();
    }

    private static Button CreateDangerButton(string text)
    {
        return CreateButton(text, Danger, Color.White);
    }

    private static Button CreateUtilityDangerButton(string text)
    {
        var button = CreateButton(text, Danger, Color.White);
        button.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
        if (button is RoundedButton rounded)
        {
            rounded.BorderColor = Danger;
            rounded.NormalBorderColor = Danger;
            rounded.HoverBorderColor = Danger;
        }

        return button;
    }

    private static Button CreateDangerTextButton(string text)
    {
        var button = CreateButton(text, Panel, Danger);
        button.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
        if (button is RoundedButton rounded)
        {
            rounded.BorderColor = Color.FromArgb(50, 50, 52);
            rounded.NormalBorderColor = Color.FromArgb(50, 50, 52);
            rounded.HoverBorderColor = Color.FromArgb(50, 50, 52);
            rounded.HoverBackColor = Color.FromArgb(34, 39, 54);
        }

        button.MouseEnter += (_, _) => button.ForeColor = Danger;
        button.MouseLeave += (_, _) => button.ForeColor = Danger;
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        var button = CreateButton(text, Panel, TextMain);
        if (button is RoundedButton rounded)
        {
            rounded.BorderColor = Color.FromArgb(50, 50, 52);
            rounded.NormalBorderColor = Color.FromArgb(50, 50, 52);
            rounded.HoverBorderColor = Color.FromArgb(50, 50, 52);
            rounded.HoverBackColor = Color.FromArgb(44, 44, 46);
        }

        return button;
    }

    private static Button CreateModuleButton(string text)
    {
        var button = CreateButton(text, Color.FromArgb(38, 38, 40), Color.White);
        button.Height = 45;
        button.Width = 160;
        button.MinimumSize = new Size(160, 45);
        button.MaximumSize = new Size(160, 45);
        button.Margin = new Padding(0, 0, 12, 0);
        button.AutoSize = false;
        button.Dock = DockStyle.None;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        button.ForeColor = Color.White;
        button.BackColor = Color.FromArgb(38, 38, 40);

        if (button is RoundedButton rounded)
        {
            rounded.BorderRadius = 10;
            rounded.BorderColor = Color.FromArgb(50, 50, 52);
            rounded.NormalBorderColor = Color.FromArgb(50, 50, 52);
            rounded.HoverBorderColor = Color.FromArgb(72, 72, 74);
            rounded.HoverBackColor = Color.FromArgb(48, 48, 50);
        }

        return button;
    }

    private static Button CreateWindowCommandButton(string text, Color? foreColor = null, Color? hoverBackColor = null)
    {
        var button = new RoundedButton
        {
            Text = text,
            Width = 40,
            Height = 32,
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = PanelSoft,
            ForeColor = foreColor ?? TextMuted,
            Font = CreateWindowCommandFont(),
            Cursor = Cursors.Hand,
            TabStop = false,
            UseVisualStyleBackColor = false,
            BorderRadius = 8,
            BorderColor = Border,
            NormalBorderColor = Border,
            HoverBorderColor = Color.FromArgb(80, 80, 84),
            Padding = new Padding(0, 1, 0, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var normalBackColor = button.BackColor;
        var hotBackColor = hoverBackColor ?? Color.FromArgb(55, 55, 57);

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = hotBackColor;
        button.FlatAppearance.MouseOverBackColor = hotBackColor;
        button.MouseEnter += (_, _) =>
        {
            button.BackColor = hotBackColor;
            button.ForeColor = Color.White;
        };
        button.MouseLeave += (_, _) =>
        {
            button.BackColor = normalBackColor;
            button.ForeColor = foreColor ?? TextMuted;
        };

        return button;
    }

    private static Font CreateWindowCommandFont()
    {
        try
        {
            return new Font("Segoe Fluent Icons", 9F, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font("Segoe MDL2 Assets", 9F, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    private Button CreateTabButton(string text, string iconGlyph)
    {
        var inactiveTextColor = Color.FromArgb(200, 200, 200);
        var button = new SidebarNavButton
        {
            Text = text,
            Dock = DockStyle.None,
            AutoSize = false,
            Anchor = AnchorStyles.None,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent,
            ForeColor = inactiveTextColor,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            Size = new Size(SidebarButtonWidth, SidebarButtonHeight),
            MinimumSize = new Size(SidebarButtonWidth, SidebarButtonHeight),
            MaximumSize = new Size(SidebarButtonWidth, SidebarButtonHeight),
            IconGlyph = iconGlyph,
            IconColor = inactiveTextColor,
            SelectedFillColor = ColorTranslator.FromHtml("#383838"),
            HoverFillColor = Color.FromArgb(48, 48, 48),
            Radius = 10
        };

        button.MouseEnter += (_, _) =>
        {
            if (!ReferenceEquals(activeTabButton, button))
            {
                button.ForeColor = Color.White;
                button.Invalidate();
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (!ReferenceEquals(activeTabButton, button))
            {
                button.BackColor = Color.Transparent;
                button.ForeColor = inactiveTextColor;
                button.Invalidate();
            }
        };

        return button;
    }

    private static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
        {
            return;
        }

        using var path = new GraphicsPath();
        var diameter = radius * 2;
        var rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        control.Region?.Dispose();
        control.Region = new Region(path);
    }

    private static Button CreateButton(string text, Color backColor, Color foreColor)
    {
        var button = new RoundedButton
        {
            Text = text,
            Height = 42,
            Width = 170,
            MinimumSize = new Size(110, 32),
            Margin = new Padding(3),
            BackColor = backColor,
            ForeColor = foreColor,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            TabStop = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BorderColor = Color.FromArgb(50, 50, 52),
            NormalBorderColor = Color.FromArgb(50, 50, 52),
            HoverBorderColor = Color.FromArgb(50, 50, 52),
            HoverBackColor = Color.FromArgb(
                Math.Min(backColor.R + 18, 255),
                Math.Min(backColor.G + 18, 255),
                Math.Min(backColor.B + 18, 255))
        };

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.MouseEnter += (_, _) =>
        {
            if (button.Tag is TelemetryVisualState state && state != TelemetryVisualState.Stopped)
            {
                return;
            }

            button.ForeColor = Color.White;
            button.Invalidate();
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Tag is TelemetryVisualState state && state != TelemetryVisualState.Stopped)
            {
                return;
            }

            button.ForeColor = foreColor;
            button.Invalidate();
        };
        return button;
    }

    private async System.Threading.Tasks.Task RunDiagnosticsAsync()
    {
        ClearTerminal();
        WriteLine("Diagnóstico geral iniciado.");

        var lines = await System.Threading.Tasks.Task.Run(() =>
        {
            var report = new List<string>();
            report.AddRange(diagnosticsService.BuildDiagnosticReport());
            report.AddRange(gpuOptimizationService.BuildRecommendations());

            var valorantExe = valorantLocator.FindExecutable();
            if (valorantExe is null)
            {
                report.Add("Submódulo opcional de jogo Riot: executável não encontrado nos caminhos padrão.");
            }
            else
            {
                report.Add($"Submódulo opcional de jogo Riot: executável encontrado em {valorantExe}");
                report.Add($"Submódulo opcional de jogo Riot: otimização de tela cheia desativada = {(tweakService.HasFullscreenOptimizationDisabled(valorantExe) ? "sim" : "não")}");
            }

            return report;
        });

        foreach (var line in lines)
        {
            WriteLine(line);
        }

        statusLabel.Text = "Pronto: revise CPU, GPU, RAM e latências. Use competitivo como padrão e reinicie.";
    }

    private async System.Threading.Tasks.Task CreateRestorePointAsync()
    {
        await RunTweakAsync("Criando restore point", () => tweakService.CreateRestorePoint(), createAutomaticBackup: false);
    }

    private async System.Threading.Tasks.Task RunAutoOptimizeAsync()
    {
        if (!TryBeginTweaking("Auto-Tuning"))
        {
            return;
        }

        SetAutoOptimizeApplyingState();
        WriteSection("Auto-Tuning inteligente");
        WriteLine("Analisando hardware...");
        statusLabel.Text = "Auto-Tuning: analisando hardware e aplicando perfil ideal...";

        try
        {
            if (optimizationEngine.CheckIfAlreadyOptimized())
            {
                WriteLine("[INFO] Sistema ja esta otimizado pelo ApexTweaker. Comandos redundantes foram ignorados.");
                statusLabel.Text = "Auto-Tuning: sistema ja otimizado.";
                return;
            }

            await CreateAutomaticBackupAsync("Auto-Tuning");
            var lines = await System.Threading.Tasks.Task.Run(() => tweakService.ApplyAutonomousOptimization(valorantLocator.FindExecutable()));
            foreach (var line in lines)
            {
                WriteLine(line);
            }

            statusLabel.Text = "Auto-Tuning aplicado. Reinicie o PC antes de medir.";
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se já estiver como admin, o driver protegeu essa chave e ela foi ignorada por segurança.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Auto-Tuning: acesso negado. Veja o log.";
        }
        catch (SecurityException ex)
        {
            WriteLine("A política de segurança do Windows bloqueou a alteração.");
            WriteLine("Nenhuma alteracao adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Auto-Tuning: bloqueado pela política de segurança.";
        }
        catch (Exception ex)
        {
            WriteLine("A operação não pode ser concluída.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Auto-Tuning: falhou. Veja o log.";
        }
        finally
        {
            EndTweaking();
            RefreshAutoOptimizeButtonState();
        }
    }

    private async System.Threading.Tasks.Task ApplyExtremeLatencyTweaksAsync()
    {
        if (_isTweaking)
        {
            WriteLine("[AVISO] Aguarde a rotina atual terminar antes de iniciar Latência extrema.");
            return;
        }

        var hardware = diagnosticsService.GetHardwareInfo();
        if (!optimizationEngine.CanApplyExtremeLatency(hardware))
        {
            var recommendation = optimizationEngine.Analyze(hardware);
            WriteSection("Latência extrema bloqueada");
            WriteLine($"{recommendation.Title}: {recommendation.Reason}");
            WriteLine("Use Preset seguro ou Preset competitivo conforme recomendado no diagnostico.");
            statusLabel.Text = "Latência extrema bloqueada para proteger temperatura/estabilidade.";
            return;
        }

        await RunTweakAsync("Latência extrema", () => tweakService.ApplyExtremeLatencyTweaks(hardware));
    }

    private void RunTweak(string section, IReadOnlyList<string> lines, string? completionStatus = null)
    {
        WriteSection(section);

        foreach (var line in lines)
        {
            WriteLine(line);
        }

        statusLabel.Text = completionStatus ?? $"{section}: concluido. Veja o log.";
    }

    private bool TryBeginTweaking(string operationName)
    {
        if (_isTweaking)
        {
            WriteLine($"[AVISO] Aguarde a rotina atual terminar antes de iniciar {operationName}.");
            return false;
        }

        _isTweaking = true;
        SetTweakingControlsEnabled(false);
        return true;
    }

    private void EndTweaking()
    {
        _isTweaking = false;
        SetTweakingControlsEnabled(true);
    }

    private void SetTweakingControlsEnabled(bool enabled)
    {
        foreach (var button in EnumerateActionButtons())
        {
            button.Enabled = enabled;
        }
    }

    private IEnumerable<Control> EnumerateActionButtons()
    {
        yield return diagnoseButton;
        yield return btnAutoOptimize;
        yield return restorePointButton;
        yield return gpuProfileButton;
        yield return gpuRegistryButton;
        yield return btnABTest;
        yield return powerButton;
        yield return extremeLatencyButton;
        yield return cpuSchedulerButton;
        yield return gpuDisplayButton;
        yield return inputButton;
        yield return networkButton;
        yield return policyServicesButton;
        yield return backgroundButton;
        yield return revertButton;
        yield return uninstallButton;
        yield return aboutButton;
        yield return openRiotSupportButton;
    }

    private async System.Threading.Tasks.Task RunTweakAsync(
        string section,
        Func<IReadOnlyList<string>> action,
        string? completionStatus = null,
        bool createAutomaticBackup = true)
    {
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection(section);
        statusLabel.Text = $"{section}: em andamento...";

        try
        {
            if (createAutomaticBackup)
            {
                await CreateAutomaticBackupAsync(section);
            }

            var lines = await System.Threading.Tasks.Task.Run(action);
            foreach (var line in lines)
            {
                WriteLine(line);
            }

            statusLabel.Text = completionStatus ?? $"{section}: concluido. Veja o log.";
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se já estiver como admin, o driver protegeu essa chave e ela foi ignorada por segurança.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: acesso negado. Veja o log.";
        }
        catch (SecurityException ex)
        {
            WriteLine("A política de segurança do Windows bloqueou a alteração.");
            WriteLine("Nenhuma alteracao adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: bloqueado pela política de segurança.";
        }
        catch (Exception ex)
        {
            WriteLine("A operação não pode ser concluída.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: falhou. Veja o log.";
        }
        finally
        {
            EndTweaking();
        }
    }

    private async System.Threading.Tasks.Task CreateAutomaticBackupAsync(string section)
    {
        WriteLine("[INFO] Criando backup granular automaticamente antes da otimização...");
        statusLabel.Text = $"{section}: criando backup preventivo...";

        var backupLines = await System.Threading.Tasks.Task.Run(() => backupService.CreateBackup());
        foreach (var line in backupLines)
        {
            WriteLine(line);
        }
    }

    private async System.Threading.Tasks.Task RunNetworkTweaksWithLatencyCheckAsync()
    {
        const string section = "Rede";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        try
        {
            WriteSection(section);
            WriteLine("Validando latencia de rede com 1.1.1.1...");
            await CreateAutomaticBackupAsync(section);

            var before = await MeasureNetworkLatencyAsync();
            WriteLine(before.HasValue
                ? $"Ping 1.1.1.1 antes dos ajustes: {before.Value} ms."
                : "Ping 1.1.1.1 antes dos ajustes: sem resposta.");

            var networkLines = await System.Threading.Tasks.Task.Run(() => tweakService.ApplyNetworkTweaks());
            foreach (var line in networkLines)
            {
                WriteLine(line);
            }

            var after = await MeasureNetworkLatencyAsync();
            WriteLine(after.HasValue
                ? $"Ping 1.1.1.1 após ajustes: {after.Value} ms."
                : "Ping 1.1.1.1 após ajustes: sem resposta.");

            statusLabel.Text = $"{section}: concluido. Veja o log.";
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteSection(section);
            WriteLine("Acesso negado pelo Windows ao Registro.");
            WriteLine("Execute o app como Administrador. Se já estiver como admin, o driver protegeu essa chave e ela foi ignorada por segurança.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: acesso negado. Veja o log.";
        }
        catch (SecurityException ex)
        {
            WriteSection(section);
            WriteLine("A política de segurança do Windows bloqueou a alteração.");
            WriteLine("Nenhuma alteracao adicional foi aplicada nessa etapa.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: bloqueado pela política de segurança.";
        }
        catch (Exception ex)
        {
            WriteSection(section);
            WriteLine("A operação não pode ser concluída.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = $"{section}: falhou. Veja o log.";
        }
        finally
        {
            EndTweaking();
        }
    }

    private static async System.Threading.Tasks.Task<long?> MeasureNetworkLatencyAsync()
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("1.1.1.1", 900);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
        }
        catch (PingException)
        {
            return null;
        }
    }

    private async System.Threading.Tasks.Task RevertTweaksAsync()
    {
        const string section = "Master rollback";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        WriteSection("Master rollback");
        statusLabel.Text = "Restaurando snapshots transacionais em ordem reversa...";
        UseWaitCursor = true;

        try
        {
            var progress = new Progress<string>(WriteLine);
            var lines = await masterRollbackService.ExecuteAsync(progress);

            if (lines.Count > 0 &&
                lines[^1].Contains("Nenhum snapshot pendente", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.Text = "Nenhum rollback pendente encontrado.";
                return;
            }

            statusLabel.Text = "Rollback concluído. Reinicie o PC se houver alterações de BCD ou energia.";
        }
        catch (OperationCanceledException)
        {
            WriteLine("[AVISO] Master rollback cancelado antes da conclusão.");
            statusLabel.Text = "Rollback cancelado.";
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteLine("A operação não pode ser concluída.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Rollback bloqueado por permissão.";
        }
        catch (Exception ex)
        {
            WriteLine("A operação não pode ser concluída.");
            WriteLine($"Detalhe: {ex.Message}");
            statusLabel.Text = "Rollback falhou. Veja o log.";
        }
        finally
        {
            UseWaitCursor = false;
            EndTweaking();
        }
    }

    private async void btnReverter_Click(object? sender, EventArgs e)
    {
        await RevertTweaksAsync();
    }

    private async System.Threading.Tasks.Task UninstallAndExitAsync()
    {
        const string section = "Desinstalar e Sair";
        if (!TryBeginTweaking(section))
        {
            return;
        }

        statusLabel.Text = "Restaurando ultimo backup e limpando dados locais...";

        try
        {
            await ShutdownBackgroundServicesAsync();

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    _ = tweakService.RevertLastAppliedState();
                }
                catch
                {
                    // Desinstalador de emergencia: segue limpando dados mesmo se nao houver backup valido.
                }

                try
                {
                    var appDataRoot = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "ApexTweaker");

                    if (Directory.Exists(appDataRoot))
                    {
                        Directory.Delete(appDataRoot, recursive: true);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Permissao negada em ProgramData nao deve impedir o encerramento solicitado.
                }
                catch (IOException)
                {
                    // Arquivos bloqueados pelo Windows podem ser removidos manualmente depois.
                }
                catch
                {
                    // Mantem o fluxo de saida sem expor erro destrutivo ao cliente.
                }
            });
        }
        finally
        {
            EndTweaking();
            BeginInvoke(new Action(Close));
        }
    }

    private async System.Threading.Tasks.Task ShutdownBackgroundServicesAsync()
    {
        telemetryWatcherTimer.Stop();
        telemetryPulseTimer.Stop();

        try
        {
            await etwFrameTracker.StopAsync();
        }
        catch
        {
            // ETW teardown is best-effort during emergency shutdown.
        }

        try
        {
            await hardwareTelemetryService.StopMonitoringAsync();
        }
        catch
        {
            // Telemetry shutdown must not block exit.
        }

        try
        {
            CloseNativeHardwareMonitor();
        }
        catch
        {
            // Sensor driver cleanup remains best-effort.
        }
    }

    private void RevertTweaks()
    {
        WriteSection("Revertendo tweaks");

        foreach (var line in tweakService.RevertAdvancedTweaks(valorantLocator.FindExecutable()))
        {
            WriteLine(line);
        }

        statusLabel.Text = "Reversao solicitada. Reinicie o PC para fechar a reversao.";
    }

    private async System.Threading.Tasks.Task ToggleHardwareTelemetryAsync()
    {
        EnsureABTestButtonLayout(btnABTest.Text);

        if (hardwareTelemetryService.IsMonitoring)
        {
            await StopHardwareTelemetryAsync();
            return;
        }

        activeBenchmarkCaptureState = GetNextBenchmarkCaptureState();
        WriteSection(GetBenchmarkSectionTitle(activeBenchmarkCaptureState));
        performanceChart.Clear();
        performanceChart.SetStatusText("\u23F3 Inicializando drivers de telemetria...");
        performanceChart.Refresh();
        hardwareTelemetryService.StartMonitoringGame();
        etwFrameTracker.Start();
        if (!hardwareTelemetryService.HasMonitoredProcess)
        {
            WriteLine("[AVISO] Aguardando a inicialização de um jogo em tela cheia para iniciar a coleta ETW...");
        }

        performanceChart.SetStatusText("Aguardando início do jogo...");
        telemetrySawTarget = false;
        telemetryStopInProgress = false;
        telemetryWatcherTimer.Start();
        SetTelemetryButtonState(TelemetryVisualState.Waiting);
        statusLabel.Text = "Telemetria ativa: foque o jogo/app que deseja monitorar.";
        WriteLine(GetBenchmarkStartMessage(activeBenchmarkCaptureState));
        WriteLine("Telemetria de Hardware iniciada. O relatório de causa raiz será gerado assim que o jogo em execução for fechado ou o monitoramento for interrompido.");

        WriteLine("ETW DxgKrnl ativo: capturando eventos Present/PresentMPO para frametime real.");
    }

    private async System.Threading.Tasks.Task StopHardwareTelemetryAsync()
    {
        telemetryWatcherTimer.Stop();
        telemetryStopInProgress = true;

        await etwFrameTracker.StopAsync();
        await hardwareTelemetryService.StopMonitoringAsync();
        telemetryUiSuspended = false;
        var session = await HardwareTelemetryService.LoadSessionDataAsync();
        if (session is not null)
        {
            await HardwareTelemetryService.SaveBenchmarkSessionAsync(activeBenchmarkCaptureState, session);
            ClearTerminal();
            WriteSessionSummaryTable(session);
            ShowSessionSummaryNotification(session);
            performanceChart.SetPoints(session.Points);
        }

        SetTelemetryButtonState(TelemetryVisualState.Stopped);
        telemetrySawTarget = false;
        telemetryStopInProgress = false;
        statusLabel.Text = "Telemetria parada. Relatório gerado no console.";
        WriteLine($"Sessao JSON salva em: {HardwareTelemetryService.CurrentSessionFilePath}");
        WriteBenchmarkCaptureResult(activeBenchmarkCaptureState);
        WriteTelemetryReport(hardwareTelemetryService.GenerateBottleneckReport());
        if (HardwareTelemetryService.BenchmarkState == BenchmarkState.Finished)
        {
            WriteBenchmarkComparisonReport();
        }

        activeBenchmarkCaptureState = BenchmarkState.None;
    }

    private void OnTelemetryPointRecorded(object? sender, HardwareTelemetryService.TelemetryPointEventArgs e)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (_isUiSuspended || telemetryUiSuspended)
        {
            return;
        }

        PostToUi(() => performanceChart.AddPoint(e.Point));
    }

    private void OnTelemetryMetricsSnapshotUpdated(object? sender, HardwareTelemetryService.TelemetryMetricsUpdatedEventArgs e)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (_isUiSuspended || telemetryUiSuspended)
        {
            return;
        }

        PostToUi(() =>
        {
            nativeDpcLatencyLabel.Text = FormatLatencyMicros(e.Snapshot.PeakDpcLatencyMicros);
            nativeBoostDropLabel.Text = FormatMegahertz(e.Snapshot.BoostDropMhz);
            nativeHardwareStatusLabel.Text = e.Snapshot.TelemetryStatusMessage;
        });
    }

    private static BenchmarkState GetNextBenchmarkCaptureState()
    {
        return HardwareTelemetryService.BenchmarkState switch
        {
            BenchmarkState.OptimizedPending => BenchmarkState.OptimizedPending,
            _ => BenchmarkState.BaselinePending
        };
    }

    private static string GetBenchmarkSectionTitle(BenchmarkState state)
    {
        return state == BenchmarkState.OptimizedPending
            ? "Teste A/B - Depois da Otimização"
            : "Teste A/B - Antes da Otimização";
    }

    private static string GetBenchmarkStartMessage(BenchmarkState state)
    {
        return state == BenchmarkState.OptimizedPending
            ? "Passo 3: capturando sessão após otimização. Feche o jogo ou pare o monitoramento para gerar o comparativo."
            : "Passo 1: capturando baseline antes da otimização. Jogue a mesma cena/mapa por alguns minutos para ter uma base limpa.";
    }

    private void WriteBenchmarkCaptureResult(BenchmarkState capturedState)
    {
        if (capturedState == BenchmarkState.OptimizedPending)
        {
            WriteLine($"Sessao Depois salva em: {HardwareTelemetryService.CurrentOptimizedSessionFilePath}");
            return;
        }

        WriteLine($"Sessao Antes salva em: {HardwareTelemetryService.CurrentBaselineSessionFilePath}");
        WriteLine("Passo 2: clique em \"\u26A1 OTIMIZAR SISTEMA AO MÁXIMO\", reinicie o PC e rode o teste após a otimização.");
    }

    private void WriteBenchmarkComparisonReport()
    {
        WriteSection("Comparativo A/B de Estabilidade");
        var baseline = HardwareTelemetryService.BaselineSession;
        var optimized = HardwareTelemetryService.OptimizedSession;

        baseline.RecalculateFrameStats();
        optimized.RecalculateFrameStats();

        WriteBenchmarkMetricLine("FPS médio", baseline.AverageFps, optimized.AverageFps);
        WriteBenchmarkMetricLine("1% Low", baseline.OnePercentLowFps, optimized.OnePercentLowFps);
        WriteBenchmarkMetricLine("0.1% Low", baseline.ZeroPointOnePercentLowFps, optimized.ZeroPointOnePercentLowFps);
        WriteBenchmarkMetricLine("Stutters severos", baseline.SevereStutterCount, optimized.SevereStutterCount, invertImprovement: true);
    }

    private void WriteBenchmarkMetricLine(string metric, double before, double after, bool invertImprovement = false)
    {
        var deltaPercent = before <= 0D
            ? (after > 0D ? 100D : 0D)
            : ((after - before) / before) * 100D;

        var isImprovement = invertImprovement ? deltaPercent <= 0D : deltaPercent >= 0D;
        var sign = deltaPercent >= 0D ? "+" : string.Empty;
        var type = isImprovement ? LogType.Success : LogType.Bottleneck;
        var beforeText = metric.Contains("Stutters", StringComparison.OrdinalIgnoreCase)
            ? before.ToString("0")
            : $"{before:0.0}";
        var afterText = metric.Contains("Stutters", StringComparison.OrdinalIgnoreCase)
            ? after.ToString("0")
            : $"{after:0.0}";

        LogToTerminal($"{metric}: {beforeText} -> {afterText} ({sign}{deltaPercent:0.0}%)", type);
    }

    private static LogType ResolveBenchmarkComparisonType(string line)
    {
        var normalized = line.ToUpperInvariant();
        if (normalized.Contains("STUTTERS", StringComparison.Ordinal))
        {
            return normalized.Contains("(-", StringComparison.Ordinal) || normalized.Contains("(0.0%", StringComparison.Ordinal)
                ? LogType.Success
                : LogType.Bottleneck;
        }

        if (normalized.Contains("1% LOW", StringComparison.Ordinal) ||
            normalized.Contains("0.1% LOW", StringComparison.Ordinal) ||
            normalized.Contains("FPS MEDIO", StringComparison.Ordinal) ||
            normalized.Contains("FPS MÉDIO", StringComparison.Ordinal))
        {
            return normalized.Contains("(+", StringComparison.Ordinal) || normalized.Contains("(0.0%", StringComparison.Ordinal)
                ? LogType.Success
                : LogType.Bottleneck;
        }

        return LogType.Info;
    }

    private void OnEtwFrameTrackerError(string message)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (!_isUiSuspended && !telemetryUiSuspended)
        {
            PostToUi(() => WriteLine($"ETW DxgKrnl indisponivel: {message}"));
        }
    }

    private void OnTelemetryDiagnosticEventRecorded(object? sender, HardwareTelemetryService.TelemetryDiagnosticEventArgs e)
    {
        if (IsDisposed || !IsHandleCreated || _isUiSuspended || telemetryUiSuspended)
        {
            return;
        }

        PostToUi(() => WriteLine(e.Message));
    }

    private void ShowSessionSummaryNotification(TelemetrySessionData session)
    {
        var score = session.CalculateStabilityScore();
        var title = "ApexTweaker - sessão encerrada";
        var text = $"Estabilidade {score}/100 | FPS {session.AverageFps:0} | 1% Low {session.OnePercentLowFps:0} | 0.1% Low {session.ZeroPointOnePercentLowFps:0}";

        try
        {
            var notification = new NotifyIcon
            {
                Icon = Icon ?? SystemIcons.Application,
                Visible = true,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = title,
                BalloonTipText = text
            };
            notification.ShowBalloonTip(4500);
            _ = System.Threading.Tasks.Task.Delay(6000).ContinueWith(_ =>
            {
                notification.Visible = false;
                notification.Dispose();
            });
        }
        catch
        {
            // Toast/balloon is a convenience layer; never block report generation.
        }

    }

    private void WriteSessionSummaryTable(TelemetrySessionData session)
    {
        session.RecalculateFrameStats();
        var score = session.CalculateStabilityScore();
        var (verdict, verdictType) = ResolveSessionVerdict(score);
        var endedAt = session.EndedAtUtc ?? DateTime.UtcNow;
        var duration = endedAt > session.StartedAtUtc
            ? endedAt - session.StartedAtUtc
            : TimeSpan.Zero;

        LogToTerminal("Resumo da sessão encerrada", LogType.Info);
        LogToTerminal($"Processo: {(string.IsNullOrWhiteSpace(session.TargetProcess) ? "Jogo detectado dinamicamente" : session.TargetProcess)}", LogType.Info);
        LogToTerminal($"Duração: {duration:hh\\:mm\\:ss}", LogType.Info);
        LogToTerminal($"Amostras: {session.Points.Count} pontos / {session.FrameTimesMs.Count} frames", LogType.Info);
        LogToTerminal($"FPS médio: {session.AverageFps:0.0} FPS", LogType.Success);
        LogToTerminal($"1% Low: {session.OnePercentLowFps:0.0} FPS", ResolveLowFpsType(session.AverageFps, session.OnePercentLowFps));
        LogToTerminal($"0.1% Low: {session.ZeroPointOnePercentLowFps:0.0} FPS", ResolveLowFpsType(session.AverageFps, session.ZeroPointOnePercentLowFps));
        LogToTerminal($"Stutters severos: {session.SevereStutterCount}", session.SevereStutterCount == 0 ? LogType.Success : LogType.Warning);
        LogToTerminal($"Score: {score}/100", verdictType);
        LogToTerminal($"Veredito: {verdict}", verdictType);
    }

    private static LogType ResolveLowFpsType(double averageFps, double lowFps)
    {
        if (averageFps <= 0 || lowFps <= 0)
        {
            return LogType.Warning;
        }

        var ratio = lowFps / averageFps;
        if (ratio >= 0.72)
        {
            return LogType.Success;
        }

        return ratio >= 0.52 ? LogType.Warning : LogType.Bottleneck;
    }

    private static (string Verdict, LogType Type) ResolveSessionVerdict(int score)
    {
        return score switch
        {
            >= 85 => ("ESTAVEL - frametime consistente", LogType.Success),
            >= 65 => ("ATENCAO - revisar gargalos", LogType.Warning),
            _ => ("GARGALO - stutter relevante", LogType.Bottleneck)
        };
    }

    private async System.Threading.Tasks.Task TickTelemetryWatcherAsync()
    {
        if (_isUiSuspended || telemetryStopInProgress || telemetryWatcherTickInProgress)
        {
            return;
        }

        telemetryWatcherTickInProgress = true;

        try
        {
            if (!hardwareTelemetryService.IsMonitoring)
            {
                telemetryWatcherTimer.Stop();
                return;
            }

            var (hasTarget, running, description) = await System.Threading.Tasks.Task.Run(() =>
            {
                return (
                    hardwareTelemetryService.HasMonitoredProcess,
                    hardwareTelemetryService.IsMonitoredProcessRunning,
                    hardwareTelemetryService.MonitoredProcessDescription);
            });

            telemetrySawTarget |= hasTarget && running;

            if (hasTarget && running)
            {
                telemetryUiSuspended = IsForegroundFullscreenWindow();
                SetTelemetryButtonState(TelemetryVisualState.Active);
                statusLabel.Text = $"Telemetria ativa: {description}";
                return;
            }

            if (telemetrySawTarget && !running)
            {
                telemetryUiSuspended = false;
                telemetryWatcherTimer.Stop();
                await StopHardwareTelemetryAsync();
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Telemetria: falha no watcher dinamico: {ex.Message}");
        }
        finally
        {
            telemetryWatcherTickInProgress = false;
        }
    }

    private static bool IsForegroundFullscreenWindow()
    {
        var foreground = NativeUiMethods.GetForegroundWindow();
        if (foreground == nint.Zero)
        {
            return false;
        }

        _ = NativeUiMethods.GetWindowThreadProcessId(foreground, out var processId);
        if (processId == Environment.ProcessId)
        {
            return false;
        }

        if (!NativeUiMethods.GetWindowRect(foreground, out var rect))
        {
            return false;
        }

        var screen = Screen.FromHandle(foreground).Bounds;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        return width >= screen.Width && height >= screen.Height &&
               rect.Left <= screen.Left && rect.Top <= screen.Top;
    }

    private void SetTelemetryButtonState(TelemetryVisualState state)
    {
        EnsureABTestButtonLayout(state == TelemetryVisualState.Stopped ? GetTelemetryStoppedButtonText() : btnABTest.Text);

        if (telemetryVisualState == state && state != TelemetryVisualState.Waiting)
        {
            return;
        }

        telemetryVisualState = state;
        telemetryPulseOn = true;
        btnABTest.Tag = state;
        if (_isUiSuspended)
        {
            telemetryPulseTimer.Stop();
            return;
        }

        if (btnABTest is not RoundedButton roundedButton)
        {
            return;
        }

        switch (state)
        {
            case TelemetryVisualState.Stopped:
                telemetryPulseTimer.Stop();
                btnABTest.Text = GetTelemetryStoppedButtonText();
                btnABTest.BackColor = Panel;
                btnABTest.ForeColor = TextMain;
                roundedButton.BorderColor = Color.Transparent;
                roundedButton.HoverBackColor = Color.FromArgb(34, 39, 54);
                break;
            case TelemetryVisualState.Waiting:
                btnABTest.Text = "\u23F3 Aguardando Jogo...";
                btnABTest.BackColor = Panel;
                btnABTest.ForeColor = TextMain;
                roundedButton.BorderColor = Warning;
                roundedButton.HoverBackColor = Color.FromArgb(34, 39, 54);
                telemetryPulseTimer.Start();
                break;
            case TelemetryVisualState.Active:
                telemetryPulseTimer.Stop();
                btnABTest.Text = "\uD83D\uDFE2 Monitorando Ativamente";
                btnABTest.BackColor = Color.FromArgb(0, 132, 160);
                btnABTest.ForeColor = Color.White;
                roundedButton.BorderColor = Accent;
                roundedButton.HoverBackColor = Color.FromArgb(24, 196, 224);
                break;
        }

        btnABTest.Invalidate();
    }

    private static string GetTelemetryStoppedButtonText()
    {
        return HardwareTelemetryService.BenchmarkState switch
        {
            BenchmarkState.OptimizedPending => "Iniciar Teste (Após Otimização)",
            BenchmarkState.Finished => "Refazer Teste (Antes da Otimização)",
            _ => "Iniciar Teste (Antes da Otimização)"
        };
    }

    private void PulseTelemetryButton()
    {
        if (_isUiSuspended ||
            telemetryVisualState != TelemetryVisualState.Waiting ||
            btnABTest is not RoundedButton roundedButton)
        {
            telemetryPulseTimer.Stop();
            return;
        }

        telemetryPulseOn = !telemetryPulseOn;
        roundedButton.BorderColor = telemetryPulseOn ? Warning : Color.FromArgb(110, 90, 34);
        btnABTest.Invalidate();
    }

    private void WriteTelemetryReport(string report)
    {
        WriteSection("Relatório de telemetria");

        foreach (var line in report.Split([Environment.NewLine], StringSplitOptions.None))
        {
            LogToTerminal(line, ResolveTelemetryReportType(line));
        }
    }

    private static LogType ResolveTelemetryReportType(string line)
    {
        var normalized = line.ToUpperInvariant();

        if (normalized.Contains("ANALISE CONCLUSIVA") ||
            normalized.Contains("ANÁLISE CONCLUSIVA") ||
            normalized.Contains("MICRO-TRAVADA") ||
            (normalized.Contains("CPU") && normalized.Contains("ATINGIU")) ||
            (normalized.Contains("GPU") && normalized.Contains("HOTSPOT")) ||
            normalized.Contains("GARGALO"))
        {
            return LogType.Bottleneck;
        }

        if (normalized.Contains("SUGESTAO") ||
            normalized.Contains("SUGESTÃO") ||
            normalized.Contains("APLICAR PRESET") ||
            normalized.Contains("MODULO") ||
            normalized.Contains("M\u00D3DULO") ||
            normalized.Contains("BACKGROUND") ||
            normalized.Contains("POLITICAS"))
        {
            return LogType.Success;
        }

        if (normalized.Contains("VEREDITO") ||
            normalized.Contains("PICOS") ||
            normalized.Contains("RELATORIO"))
        {
            return LogType.Info;
        }

        return ResolveLogType(line);
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void WriteSection(string text)
    {
        LogToTerminal(string.Empty, LogType.Info);
        LogToTerminal($">>> {text.ToUpperInvariant()}", LogType.Info);
    }

    private void WriteLine(string text)
    {
        var message = NormalizeTerminalMessage(text);

        if (ShouldSuppressTerminalDuplicate(message))
        {
            return;
        }

        LogToTerminal(message, ResolveLogType(message));
    }

    private void LogToTerminal(string message, LogType type)
    {
        var normalizedMessage = NormalizeConsolePayload(message);
        var text = normalizedMessage.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? normalizedMessage
            : normalizedMessage + Environment.NewLine;

        AppendRuntimeLog(text);
        AppendLog(text, ResolveTerminalColor(type));
    }

    private void InitializeRuntimeLog()
    {
        try
        {
            var directory = Path.GetDirectoryName(RuntimeLogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (runtimeLogSync)
            {
                runtimeLogWriter?.Dispose();
                runtimeLogWriter = new StreamWriter(RuntimeLogPath, append: false, Encoding.UTF8, 16 * 1024)
                {
                    AutoFlush = false
                };
            }
        }
        catch
        {
            // Runtime log is diagnostic only; UI must keep opening if ProgramData is blocked.
        }
    }

    private void AppendRuntimeLog(string text)
    {
        try
        {
            lock (runtimeLogSync)
            {
                runtimeLogWriter?.Write(text);
            }
        }
        catch
        {
            // Logging must never break tweak execution or UI rendering.
        }
    }

    private void FlushRuntimeLog()
    {
        lock (runtimeLogSync)
        {
            runtimeLogWriter?.Flush();
        }
    }

    private void DisposeRuntimeLog()
    {
        lock (runtimeLogSync)
        {
            runtimeLogWriter?.Dispose();
            runtimeLogWriter = null;
        }
    }

    private void AppendLog(string text, Color color)
    {
        text = NormalizeConsolePayload(text);

        if (IsDisposed)
        {
            return;
        }

        QueueTerminalLine(text, color);
        ScheduleTerminalFlush();
    }

    private void QueueTerminalLine(string text, Color color)
    {
        lock (pendingTerminalSync)
        {
            pendingTerminalLines.Enqueue((text, color));
        }
    }

    private void FlushPendingTerminalLines()
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(FlushPendingTerminalLines));
            }

            return;
        }

        terminalFlushTimer.Stop();
        terminalFlushScheduled = false;

        List<(string Text, Color Color)> lines = [];
        var shouldClear = false;
        lock (pendingTerminalSync)
        {
            shouldClear = pendingTerminalClear;
            pendingTerminalClear = false;

            while (pendingTerminalLines.Count > 0)
            {
                lines.Add(pendingTerminalLines.Dequeue());
            }
        }

        if (shouldClear && lines.Count == 0)
        {
            ClearConsoleCore();
            return;
        }

        if (!shouldClear && lines.Count == 0)
        {
            return;
        }

        ApplyConsoleBatch(shouldClear, lines);
        FlushRuntimeLog();
    }

    private void ClearTerminal()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_isUiSuspended || !IsTelemetryConsoleVisible())
        {
            lock (pendingTerminalSync)
            {
                pendingTerminalLines.Clear();
                pendingTerminalClear = true;
            }

            return;
        }

        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(ClearTerminal));
            }

            return;
        }

        _lastTerminalLine = null;
        _lastTerminalLineTimestamp = 0;
        ClearConsoleCore();
    }

    private void RefreshConsoleSurface()
    {
        consoleView.RefreshSurface();
    }

    private void ScheduleTerminalFlush()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_isUiSuspended || !IsTelemetryConsoleVisible())
        {
            return;
        }

        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(ScheduleTerminalFlush));
            }

            return;
        }

        if (terminalFlushScheduled)
        {
            return;
        }

        terminalFlushScheduled = true;
        terminalFlushTimer.Start();
    }

    private void ApplyConsoleBatch(bool clearBeforeAppend, List<(string Text, Color Color)> lines)
    {
        const int maxConsoleChars = 12000;

        if (clearBeforeAppend)
        {
            renderedTerminalLines.Clear();
            renderedTerminalCharCount = 0;
        }

        foreach (var line in lines)
        {
            renderedTerminalLines.Add(line);
            renderedTerminalCharCount += line.Text.Length;
        }

        while (renderedTerminalCharCount > maxConsoleChars && renderedTerminalLines.Count > 0)
        {
            renderedTerminalCharCount -= renderedTerminalLines[0].Text.Length;
            renderedTerminalLines.RemoveAt(0);
        }

        consoleView.SetEntries(renderedTerminalLines);
    }

    private void ClearConsoleCore()
    {
        renderedTerminalLines.Clear();
        renderedTerminalCharCount = 0;
        consoleView.ClearEntries();
    }

    private bool IsTelemetryConsoleVisible()
    {
        return string.Equals(_activePageKey, TelemetryPageKey, StringComparison.Ordinal) &&
               !Disposing &&
               !IsDisposed &&
               consoleView.IsSurfaceReady;
    }

    private static Color ResolveTerminalColor(LogType type)
    {
        return type switch
        {
            LogType.Success => TerminalSuccess,
            LogType.Warning => TerminalWarning,
            LogType.Bottleneck => TerminalBottleneck,
            _ => TerminalInfo
        };
    }

    private static LogType ResolveLogType(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return LogType.Info;
        }

        var normalized = text.ToUpperInvariant();
        if (normalized.Contains("ESTABILIDADE"))
        {
            return LogType.Warning;
        }

        if (normalized.Contains("ERRO") ||
            normalized.Contains("FALHA") ||
            normalized.Contains("GARGALO") ||
            normalized.Contains("THERMAL") ||
            normalized.Contains("STUTTER") ||
            normalized.Contains("NEGADO") ||
            normalized.Contains("BLOQUEAD") ||
            normalized.Contains("NAO ENCONTR") ||
            normalized.Contains("NÃO ENCONTR"))
        {
            return LogType.Bottleneck;
        }

        if (normalized.Contains("AVISO") ||
            normalized.Contains("ATENCAO") ||
            normalized.Contains("ATENÇÃO") ||
            normalized.Contains("REINICIE") ||
            normalized.Contains("TEMPERATURA") ||
            normalized.Contains("IGNORAD"))
        {
            return LogType.Warning;
        }

        if (normalized.Contains("TRUE") ||
            normalized.Contains("SIM") ||
            normalized.Contains("SUCESSO") ||
            normalized.Contains("CONCLUID") ||
            normalized.Contains("APLICAD") ||
            normalized.Contains("CRIADO") ||
            normalized.Contains("ATIVAD") ||
            normalized.Contains("[OTIMIZADO]") ||
            normalized.Contains("RESTAURAD") ||
            normalized.Contains("ROLLBACK") ||
            normalized.Contains("REMOVID"))
        {
            return LogType.Success;
        }

        return LogType.Info;
    }

    private static string NormalizeTerminalMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var normalized = text.ToUpperInvariant();

        if (normalized.Contains("NETWORKTHROTTLINGINDEX") ||
            normalized.Contains("NETWORK THROTTLING DESLIGADO"))
        {
            return "[OTIMIZADO] Estrangulamento de rede removido do Kernel do Windows.";
        }

        if (normalized.Contains("GAMEDVR_FSEBEHAVIOR"))
        {
            return "[OTIMIZADO] Pipeline de tela cheia exclusiva reforcado para reduzir interferencia do DWM.";
        }

        if (normalized.Contains("GAME MODE ESTRITO"))
        {
            return "[OTIMIZADO] Game Mode estrito habilitado para orientar Thread Director/CCD.";
        }

        if (normalized.Contains("OVERLAY/GAMEDVR DESATIVADO"))
        {
            return "[OTIMIZADO] Overlay GameDVR desativado sem desligar o agendador do Game Mode.";
        }

        if (normalized.Contains("CORE PARKING MINIMO") ||
            normalized.Contains("CPMINCORES"))
        {
            return "[OTIMIZADO] Core Parking minimo travado para reduzir latencia de acordada de thread.";
        }

        if (normalized.Contains("HETEROPOLICY") ||
            normalized.Contains("HETEROTHREAD") ||
            normalized.Contains("SCHEDPOLICY") ||
            normalized.Contains("THREAD DIRECTOR"))
        {
            return "[OTIMIZADO] Politica heterogenea preservada para direcionar threads aos P-Cores sem forcar Core Parking.";
        }

        if (normalized.Contains("WIN32PRIORITYSEPARATION"))
        {
            return "[OTIMIZADO] Quantum do scheduler focado no processo ativo em primeiro plano.";
        }

        if (normalized.Contains("IOPAGELIMIT"))
        {
            return "[OTIMIZADO] Buffer de I/O do kernel elevado para o pipeline disco/memoria.";
        }

        if (normalized.Contains("DISABLEPAGINGEXECUTIVE"))
        {
            return "[OTIMIZADO] Kernel e drivers fixados em RAM fisica.";
        }

        if (normalized.Contains("LARGESYSTEMCACHE"))
        {
            return "[OTIMIZADO] Cache de sistema largo habilitado para aproveitar RAM abundante.";
        }

        if (normalized.Contains("DISABLEDYNAMICPSTATE") ||
            normalized.Contains("POWERMIZER"))
        {
            return "[OTIMIZADO] Perfil PowerMizer NVIDIA travado para desempenho maximo.";
        }

        if (normalized.Contains("ENABLEULPS") ||
            normalized.Contains("ULPS AMD"))
        {
            return "[OTIMIZADO] ULPS AMD/Radeon desativado para reduzir troca agressiva de estado de energia.";
        }

        if (normalized.Contains("USEPLATFORMCLOCK=FALSE") ||
            normalized.Contains("USEPLATFORMCLOCK FALSE"))
        {
            return "[OTIMIZADO] Clock de plataforma liberado para o Windows priorizar TSC invariante.";
        }

        if (normalized.Contains("DISABLEDYNAMICTICK=YES") ||
            normalized.Contains("DISABLEDYNAMICTICK YES"))
        {
            return "[OTIMIZADO] Dynamic tick desativado para reduzir variacao de timer.";
        }

        if (normalized.Contains("SYSTEMRESPONSIVENESS=0"))
        {
            return "[OTIMIZADO] Scheduler multimidia priorizado para jogos.";
        }

        if (normalized.Contains("GAMES GPU PRIORITY=8"))
        {
            return "[OTIMIZADO] Prioridade de GPU para jogos elevada.";
        }

        if (normalized.Contains("GAMES PRIORITY=6"))
        {
            return "[OTIMIZADO] Prioridade de tarefa de jogos elevada.";
        }

        if (normalized.Contains("GAMES SCHEDULING CATEGORY=HIGH"))
        {
            return "[OTIMIZADO] Categoria de agendamento definida para alta prioridade.";
        }

        if (normalized.Contains("GAMES SFIO PRIORITY=HIGH"))
        {
            return "[OTIMIZADO] Prioridade de I/O de jogos elevada.";
        }

        if (normalized.Contains("DISABLEPAGINGEXECUTIVE=1"))
        {
            return "[OTIMIZADO] Kernel e drivers mantidos em RAM fisica.";
        }

        if (normalized.Contains("LARGESYSTEMCACHE=1"))
        {
            return "[OTIMIZADO] Cache de sistema largo habilitado para benchmark.";
        }

        if (normalized.Contains("DODOWNLOADMODE=0"))
        {
            return "[OTIMIZADO] Delivery Optimization P2P bloqueado para reduzir IOPS e ruido de rede.";
        }

        if (normalized.Contains("REALTIMEGAMINGRESOLUTION"))
        {
            return "[OTIMIZADO] DWM priorizado para workload 3D em foco.";
        }

        if (normalized.Contains("COMPOSITIONPOLICY"))
        {
            return "[OTIMIZADO] Politica de composicao do DWM ajustada para baixa latencia.";
        }

        if (normalized.Contains("OVERLAYTESTMODE") ||
            normalized.Contains("MPO/DWM FIX"))
        {
            return "[OTIMIZADO] MPO/DWM estabilizado para reduzir micro-stutter visual.";
        }

        if (normalized.Contains("HWSCHMODE") ||
            normalized.Contains("HARDWARE ACCELERATED GPU SCHEDULING"))
        {
            return "[OTIMIZADO] Agendamento de GPU por hardware solicitado ao Windows.";
        }

        if (normalized.Contains("MSISUPPORTED=1") &&
            normalized.Contains("DEVICEPRIORITY"))
        {
            return "[OTIMIZADO] Interrupcoes MSI e prioridade de GPU elevadas no driver.";
        }

        return text;
    }

    private static string NormalizeConsolePayload(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        var builder = new StringBuilder(normalized.Length + 8);
        foreach (var ch in normalized)
        {
            if (ch == '\n')
            {
                builder.Append(Environment.NewLine);
                continue;
            }

            if (ch == '\t')
            {
                builder.Append("    ");
                continue;
            }

            if (!char.IsControl(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private bool ShouldSuppressTerminalDuplicate(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var now = Environment.TickCount64;
        if (string.Equals(_lastTerminalLine, message, StringComparison.Ordinal) &&
            now - _lastTerminalLineTimestamp < 1200)
        {
            return true;
        }

        _lastTerminalLine = message;
        _lastTerminalLineTimestamp = now;
        return false;
    }

    private async System.Threading.Tasks.Task StartNativeHardwareMonitorAsync()
    {
        if (nativeHardwareMonitorStarted)
        {
            if (!nativeHardwareTimer.Enabled)
            {
                nativeHardwareTimer.Start();
            }

            return;
        }

        nativeHardwareComputer = await System.Threading.Tasks.Task.Run(() =>
        {
            var computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true
            };
            computer.Open();
            return computer;
        });

        nativeHardwareMonitorStarted = true;
        nativeHardwareTimer.Start();
        await TickNativeHardwareMonitorAsync();
    }

    private async System.Threading.Tasks.Task TickNativeHardwareMonitorAsync()
    {
        if (_isUiSuspended || nativeHardwareTickInProgress || nativeHardwareComputer is null)
        {
            return;
        }

        nativeHardwareTickInProgress = true;
        try
        {
            var snapshot = await System.Threading.Tasks.Task.Run(ReadNativeHardwareSnapshot);
            UpdateNativeHardwareLabels(snapshot);
        }
        catch (Exception ex)
        {
            if (!_isUiSuspended)
            {
                WriteLine($"[AVISO] Falha na leitura local de sensores: {ex.Message}");
            }
        }
        finally
        {
            nativeHardwareTickInProgress = false;
        }
    }

    private NativeHardwareSnapshot ReadNativeHardwareSnapshot()
    {
        var snapshot = new MutableNativeHardwareSnapshot();
        var computer = nativeHardwareComputer;
        if (computer is null)
        {
            return snapshot.ToImmutable();
        }

        foreach (var hardware in computer.Hardware)
        {
            UpdateAndReadNativeHardware(hardware, snapshot);
        }

        return snapshot.ToImmutable();
    }

    private static void UpdateAndReadNativeHardware(IHardware hardware, MutableNativeHardwareSnapshot snapshot)
    {
        try
        {
            hardware.Update();
        }
        catch
        {
            return;
        }

        ReadNativeHardwareSensors(hardware, snapshot);

        foreach (var subHardware in hardware.SubHardware)
        {
            UpdateAndReadNativeHardware(subHardware, snapshot);
        }
    }

    private static void ReadNativeHardwareSensors(IHardware hardware, MutableNativeHardwareSnapshot snapshot)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not { } value)
            {
                continue;
            }

            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    if (sensor.SensorType == SensorType.Load &&
                        sensor.Name.Contains("CPU Total", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.CpuLoadPercent = value;
                    }
                    else if (sensor.SensorType == SensorType.Temperature)
                    {
                        snapshot.CpuTemperatureC = MaxNullable(snapshot.CpuTemperatureC, value);
                    }
                    break;

                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                case HardwareType.GpuNvidia:
                    if (sensor.SensorType == SensorType.Load &&
                        sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.GpuLoadPercent = value;
                    }
                    else if (sensor.SensorType == SensorType.Temperature &&
                             sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.GpuTemperatureC = value;
                    }
                    break;

                case HardwareType.Memory:
                    if (sensor.SensorType == SensorType.Load)
                    {
                        snapshot.RamLoadPercent = value;
                    }
                    else if (sensor.SensorType == SensorType.Data &&
                             sensor.Name.Contains("Used", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.RamUsedGb = value;
                    }
                    break;
            }
        }
    }

    private void UpdateNativeHardwareLabels(NativeHardwareSnapshot snapshot)
    {
        if (IsDisposed || _isUiSuspended)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateNativeHardwareLabels(snapshot)));
            return;
        }

        nativeCpuLoadLabel.Text = FormatPercent(snapshot.CpuLoadPercent);
        nativeCpuTempLabel.Text = FormatTemperature(snapshot.CpuTemperatureC);
        nativeGpuLoadLabel.Text = FormatPercent(snapshot.GpuLoadPercent);
        nativeGpuTempLabel.Text = FormatTemperature(snapshot.GpuTemperatureC);
        nativeRamLoadLabel.Text = FormatPercent(snapshot.RamLoadPercent);
        nativeRamUsedLabel.Text = snapshot.RamUsedGb.HasValue ? $"{snapshot.RamUsedGb.Value:0.0} GB" : "--";
    }

    private void CloseNativeHardwareMonitor()
    {
        nativeHardwareTimer.Stop();

        try
        {
            nativeHardwareComputer?.Close();
        }
        catch
        {
            // Driver/sensor cleanup is best-effort during shutdown.
        }
        finally
        {
            nativeHardwareComputer = null;
            nativeHardwareMonitorStarted = false;
            nativeHardwareTickInProgress = false;
        }
    }

    private static float? MaxNullable(float? current, float next)
    {
        return current.HasValue ? Math.Max(current.Value, next) : next;
    }

    private static string FormatPercent(float? value)
    {
        return value.HasValue ? $"{value.Value:0}%" : "--";
    }

    private static string FormatTemperature(float? value)
    {
        return value.HasValue ? $"{value.Value:0} °C" : "--";
    }

    private static string FormatLatencyMicros(double value)
    {
        return value > 0 ? $"{value:0} \u00B5s" : "0 \u00B5s";
    }

    private static string FormatMegahertz(double value)
    {
        return value > 0 ? $"{value:0} MHz" : "0 MHz";
    }

    private sealed class MutableNativeHardwareSnapshot
    {
        public float? CpuLoadPercent { get; set; }

        public float? CpuTemperatureC { get; set; }

        public float? GpuLoadPercent { get; set; }

        public float? GpuTemperatureC { get; set; }

        public float? RamLoadPercent { get; set; }

        public float? RamUsedGb { get; set; }

        public NativeHardwareSnapshot ToImmutable()
        {
            return new NativeHardwareSnapshot(
                CpuLoadPercent,
                CpuTemperatureC,
                GpuLoadPercent,
                GpuTemperatureC,
                RamLoadPercent,
                RamUsedGb);
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            $"{AppInfo.Name} v{AppInfo.Version}{Environment.NewLine}{AppInfo.Credits}{Environment.NewLine}{Environment.NewLine}Backups: {backupService.BackupDirectory}{Environment.NewLine}Distribuição: executável Win64 com suporte nativo ao ApexTweaker.Native.dll.",
            "Sobre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}

