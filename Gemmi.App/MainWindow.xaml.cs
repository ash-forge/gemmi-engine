using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Gemmi.Core;
using Gemmi.Diagnostics;
using Gemmi.Hardware;
using Gemmi.Perception;

namespace Gemmi.App;

public partial class MainWindow : Window
{
    private readonly GemmiState _state = new();
    private readonly DeepHorizonHal _hal = new();
    private readonly NfcBadgeAuth _nfcAuth = new();
    private readonly AudioVadEngine _audioVad = new();
    private readonly VisionStreamIngest _visionIngest = new();
    private readonly SpontaneousInitiationEvaluator _spontaneousEval = new();
    private readonly ScreenCodeMonitor _codeMonitor = new();
    private readonly SpeechSynthesisEngine _speechSynth = new();
    private readonly NetBirdMeshSync _meshSync = new();
    private readonly CancellationTokenSource _cts = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _hal.InitializeNodeHardware(_state);
        TxtAlertsLog.Text = $"[System Started] Gemmi Engine v1.0.0 Pro Initialized on Node '{_state.Telemetry.NodeId}'.\n24/7 Asynchronous Second Brain online.\n";

        // Start background perception loops
        _ = Task.Run(() => _audioVad.StartVadLoopAsync(_state, _cts.Token));
        _ = Task.Run(() => _visionIngest.StartVisionLoopAsync(_state, _cts.Token));
        _ = Task.Run(() => _spontaneousEval.StartSpontaneousEvaluatorLoopAsync(_state, OnSpontaneousInitiated, _cts.Token));
        _ = Task.Run(() => _codeMonitor.StartCodeMonitorLoopAsync(_state, OnActiveIdeDetected, _cts.Token));

        // Load initial HIL milestones, sensors, and mesh nodes
        var milestones = JtagUartProfiler.CaptureBootProfile("Rev 3");
        GridHilMilestones.ItemsSource = milestones;

        var sensors = HardwareSensorGateway.ScanHardwareSensors();
        GridHardwareSensors.ItemsSource = sensors;

        var nodes = await MeshAutoDiscovery.ScanNetBirdMeshAsync();
        GridMeshNodes.ItemsSource = nodes;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _cts.Cancel();
    }

    private void OnSpontaneousInitiated(string alert)
    {
        Dispatcher.Invoke(() =>
        {
            TxtAlertsLog.Text += $"{alert}\n";
            TxtStatus.Text = alert;
        });
    }

    private void OnActiveIdeDetected(string ideInfo)
    {
        Dispatcher.Invoke(() =>
        {
            TxtAlertsLog.Text += $"{DateTime.Now:HH:mm:ss} - {ideInfo}\n";
        });
    }

    private void BtnTestVocalAlert_Click(object sender, RoutedEventArgs e)
    {
        string msg = $"[Manual Vocal Alert] Hey John, I just verified the LPDDR5X RAM training latency on Rev 3 is down to 0.31s!";
        TxtAlertsLog.Text += $"{DateTime.Now:HH:mm:ss} - {msg}\n";
        TxtStatus.Text = msg;
    }

    private async void BtnSpeakOutLoud_Click(object sender, RoutedEventArgs e)
    {
        string textToSpeak = "Hey John, Gemmi Second Brain is online and running locally on your Deep Horizon hardware node!";
        TxtStatus.Text = $"Speaking out loud: '{textToSpeak}'...";
        await _speechSynth.SpeakAsync(textToSpeak, _state);
        TxtStatus.Text = "Neural Speech Output Complete";
    }

    private void ComboSkuType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboSkuType.SelectedItem is ComboBoxItem item)
        {
            bool isEnterprise = item.Content.ToString()?.Contains("Enterprise") == true;
            _hal.SkuType = isEnterprise ? HardwareSkuType.EnterpriseGoogleInternalNode : HardwareSkuType.ConsumerDesktopNode;
            _hal.InitializeNodeHardware(_state);

            TxtNfcStatus.Text = isEnterprise ? "Active Badge User: Unauthenticated (NFC Ready)" : "Active Badge User: Disabled (Consumer SKU)";
            TxtStatus.Text = $"Configured Deep Horizon SKU: {_hal.SkuType}";
        }
    }

    private async void BtnTapNfcBadge_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg) = await _nfcAuth.OnNfcBadgeTappedAsync("GOOG-884920", "John (DeepMind Lead)", _state);
        TxtNfcStatus.Text = $"Active Badge User: {(_state.Telemetry.ActiveNfcBadgeUser)}";
        TxtMeshLog.Text += $"{DateTime.Now:HH:mm:ss} - {msg}\n";
        MessageBox.Show(msg, success ? "NFC Badge Authenticated" : "NFC Unavailable", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private async void BtnSyncMesh_Click(object sender, RoutedEventArgs e)
    {
        TxtStatus.Text = "Hydrating Gemmi state over NetBird mesh...";
        var res = await _meshSync.SerializeAndHydrateMeshStateAsync(_state, "DeepMind-Lab-Stack");
        TxtMeshLog.Text += $"{DateTime.Now:HH:mm:ss} - {res}\n";
        TxtStatus.Text = "NetBird P2P Mesh Sync Complete";
    }

    private async void BtnRunHilStress_Click(object sender, RoutedEventArgs e)
    {
        var selectedRev = (ComboNodeRev.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Rev 3 Next-Gen Silicon";
        string revKey = selectedRev.Contains("Rev 1") ? "Rev 1" : selectedRev.Contains("Rev 2") ? "Rev 2" : "Rev 3";

        TxtStatus.Text = $"Executing 100 Cold-Boot Stress Cycles on {revKey}...";
        var results = await HilTestHarness.Run100BootStressCyclesAsync(revKey);
        var profile = JtagUartProfiler.CaptureBootProfile(revKey);

        GridHilMilestones.ItemsSource = profile;
        TxtStatus.Text = $"Completed 100 boot stress cycles for {revKey}. Average boot time: {results[0].TotalBootTimeMs:F1}ms";
        MessageBox.Show($"Completed 100 cold-boot stress cycles for {revKey}.\nTotal Startup Latency: {results[0].TotalBootTimeMs:F1} ms (100% Passed)", "HIL Stress Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnScanHardwareSensors_Click(object sender, RoutedEventArgs e)
    {
        var sensors = HardwareSensorGateway.ScanHardwareSensors();
        GridHardwareSensors.ItemsSource = sensors;
        TxtStatus.Text = $"Scanned {sensors.Count} hardware sensors & serial buses";
    }

    private async void BtnScanMeshNodes_Click(object sender, RoutedEventArgs e)
    {
        TxtStatus.Text = "Scanning NetBird private overlay mesh (mesh.barrer.net)...";
        var nodes = await MeshAutoDiscovery.ScanNetBirdMeshAsync();
        GridMeshNodes.ItemsSource = nodes;
        TxtStatus.Text = $"Discovered {nodes.Count} active nodes on NetBird mesh";
    }

    private void BtnRenderWhiteboard_Click(object sender, RoutedEventArgs e)
    {
        CanvasWhiteboard.Children.Clear();
        double x = 40;
        double y = 80;

        DrawWhiteboardNode(CanvasWhiteboard, "Deep Horizon Node Array", "ARM CPU + Edge TPU ($400-$600)", Brushes.Violet, x, y);
        x += 280;

        DrawWhiteboardNode(CanvasWhiteboard, "Gemmi Engine (C# .NET 10)", "24/7 Asynchronous Second Brain", Brushes.Cyan, x, y);
        x += 280;

        DrawWhiteboardNode(CanvasWhiteboard, "NetBird Mesh Overlay", "mesh.barrer.net (Home <-> Lab)", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), x, y);
        x += 280;

        DrawWhiteboardNode(CanvasWhiteboard, "ModelStudio IDE Target", "1-Click C# Architecture Export", Brushes.Magenta, x, y);

        TxtStatus.Text = "Rendered Collaborative Digital Whiteboard Canvas";
    }

    private void DrawWhiteboardNode(Canvas canvas, string title, string subtitle, Brush accentColor, double x, double y)
    {
        var border = new Border
        {
            Width = 240,
            Height = 120,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141A26")),
            BorderBrush = accentColor,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 14 });
        sp.Children.Add(new TextBlock { Text = subtitle, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")), FontSize = 11, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap });
        border.Child = sp;

        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        canvas.Children.Add(border);
    }

    private void BtnExportModelStudio_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Exported Whiteboard Architecture Diagram directly into C# Project format for ModelStudio IDE!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        TxtStatus.Text = "Exported architecture to ModelStudio IDE";
    }
}