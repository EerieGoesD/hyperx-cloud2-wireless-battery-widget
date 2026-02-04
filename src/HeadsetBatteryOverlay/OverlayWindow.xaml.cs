using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using WinForms = System.Windows.Forms;

namespace HeadsetBatteryOverlay;

public partial class OverlayWindow : Window
{
    private const double ExpandedWidth = 220;
    private const double ExpandedHeight = 104;
    private const double CompactWidth = 120;
    private const double CompactHeight = 60;
    private bool _isCompact;

    private readonly BatteryPoller _poller;

    private readonly string _statePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HeadsetBatteryOverlay",
        "ui_state.json"
    );

    public OverlayWindow()
    {
        InitializeComponent();

        _poller = new BatteryPoller();

        Loaded += (_, _) =>
        {
            RestoreWindowPositionOrDefault();
            SnapToVisibleArea(); // <- clamp depois de restaurar coords
            InstallContextMenu();
            StartPolling();
        };

        MouseLeftButtonDown += (_, _) =>
        {
            try { DragMove(); SaveWindowPosition(); } catch { }
        };
    }

    private void SnapToVisibleArea()
    {
        // Ensure layout is measured so ActualWidth/Height are valid
        if (ActualWidth <= 0 || ActualHeight <= 0)
            UpdateLayout();

        double wDip = (ActualWidth > 0) ? ActualWidth : Width;
        double hDip = (ActualHeight > 0) ? ActualHeight : Height;

        // Get the monitor that currently contains the window (or nearest)
        var hwnd = new WindowInteropHelper(this).Handle;
        var screen = WinForms.Screen.FromHandle(hwnd);
        var waPx = screen.WorkingArea; // pixels

        // Convert monitor working area from pixels -> WPF DIPs for this window
        var dpi = VisualTreeHelper.GetDpi(this);
        double scaleX = dpi.DpiScaleX;
        double scaleY = dpi.DpiScaleY;

        double leftBoundDip = waPx.Left / scaleX;
        double topBoundDip = waPx.Top / scaleY;
        double rightBoundDip = waPx.Right / scaleX;
        double bottomBoundDip = waPx.Bottom / scaleY;

        const double marginDip = 12;

        // If Left/Top are NaN or outside bounds, snap to top-right of that monitor
        bool invalid = double.IsNaN(Left) || double.IsNaN(Top);

        bool offScreen =
            invalid ||
            (Left + wDip) < leftBoundDip ||
            Left > rightBoundDip ||
            (Top + hDip) < topBoundDip ||
            Top > bottomBoundDip;

        if (offScreen)
        {
            Left = rightBoundDip - wDip - marginDip;
            Top = topBoundDip + marginDip;
        }

        // Always clamp (covers edge cases and DPI/layout changes)
        Left = Math.Max(leftBoundDip, Math.Min(Left, rightBoundDip - wDip));
        Top = Math.Max(topBoundDip, Math.Min(Top, bottomBoundDip - hDip));
    }

    private void InstallContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var refresh = new System.Windows.Controls.MenuItem { Header = "Refresh now" };
        refresh.Click += async (_, _) => await RefreshAsync();

        var diagnostics = new System.Windows.Controls.MenuItem { Header = "Diagnostics" };
        diagnostics.Click += (_, _) => new DiagnosticsWindow { Owner = this }.Show();

        var exit = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exit.Click += (_, _) => System.Windows.Application.Current.Shutdown();

        menu.Items.Add(refresh);
        menu.Items.Add(diagnostics);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exit);

        ContextMenu = menu;
    }

    private void StartPolling()
    {
        _ = RefreshAsync(); // one-time refresh on launch
    }

    // Exposed for tray icon "Refresh now"
    public System.Threading.Tasks.Task RefreshFromTrayAsync() => RefreshAsync();

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var result = await _poller.TryReadBatteryAsync();

        if (!result.Success)
        {
            PercentText.Text = "--%";
            StatusText.Text = result.Message;
            SetFill(0, isUnknown: true);
            return;
        }

        var pct = Math.Clamp(result.BatteryPercent, 0, 100);
        PercentText.Text = $"{pct}%";
        StatusText.Text = result.DeviceLabel;

        SetFill(pct, isUnknown: false);
    }

    private void SetFill(int percent, bool isUnknown)
    {
        if (isUnknown)
        {
            BatteryFill.Opacity = 0.15;
            BatteryFill.Margin = new Thickness(3, 5, 21, 5);
            BatteryFill.Fill = System.Windows.Media.Brushes.Gray;
            return;
        }

        BatteryFill.Opacity = 1.0;

        double usable = 15.0;
        double filled = usable * (percent / 100.0);
        double right = 6 + (usable - filled);

        BatteryFill.Margin = new Thickness(3, 5, right, 5);

        if (percent >= 50) BatteryFill.Fill = System.Windows.Media.Brushes.LimeGreen;
        else if (percent >= 20) BatteryFill.Fill = System.Windows.Media.Brushes.Gold;
        else BatteryFill.Fill = System.Windows.Media.Brushes.OrangeRed;
    }

    private void SetCompactMode(bool compact)
    {
        _isCompact = compact;

        LeftPanel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        StatusText.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

        BtnCompact.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        BtnExpand.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;

        PercentRow.HorizontalAlignment = compact ? System.Windows.HorizontalAlignment.Center : System.Windows.HorizontalAlignment.Left;
        RightPanel.HorizontalAlignment = compact ? System.Windows.HorizontalAlignment.Center : System.Windows.HorizontalAlignment.Left;

        Width = compact ? CompactWidth : ExpandedWidth;
        Height = compact ? CompactHeight : ExpandedHeight;

        SnapToVisibleArea(); // <- clamp após resize
    }

    private void BtnCompact_Click(object sender, RoutedEventArgs e)
    {
        SetCompactMode(true);
        SaveWindowPosition();
    }

    private void BtnExpand_Click(object sender, RoutedEventArgs e)
    {
        SetCompactMode(false);
        SaveWindowPosition();
    }

    private void RestoreWindowPositionOrDefault()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);

            if (File.Exists(_statePath))
            {
                var json = File.ReadAllText(_statePath);
                var state = JsonSerializer.Deserialize<WindowStateModel>(json);
                if (state is not null)
                {
                    Left = state.Left;
                    Top = state.Top;
                    return;
                }
            }
        }
        catch { }

        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 20;
        Top = wa.Top + 20;
    }

    private void SaveWindowPosition()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);

            var state = new WindowStateModel { Left = Left, Top = Top };
            File.WriteAllText(_statePath, JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { WriteIndented = true }
            ));
        }
        catch { }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { }

        e.Handled = true;
    }

    private sealed class WindowStateModel
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }
}