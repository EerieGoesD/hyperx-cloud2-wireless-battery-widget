// App.xaml.cs
using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;

namespace HeadsetBatteryOverlay;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _tray;
    private OverlayWindow? _window;

    private bool _hideTaskbar;

    private readonly string _appStatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HeadsetBatteryOverlay",
        "app_state.json"
    );

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        LoadAppState();

        _window = new OverlayWindow();
        _window.ShowInTaskbar = !_hideTaskbar;
        _window.Closed += (_, _) => { _window = null; };
        _window.Show();

        CreateTrayIcon();
    }

    private void CreateTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "tray.ico");

        _tray = new NotifyIcon
        {
            Text = "Headset Battery Overlay",
            Visible = true,
            Icon = new Icon(iconPath)
        };

        var menu = new ContextMenuStrip();

        var showHide = new ToolStripMenuItem("Show/Hide");
        showHide.Click += (_, _) => ToggleWindowVisibility();

        var hideTaskbar = new ToolStripMenuItem("Hide Taskbar")
        {
            CheckOnClick = true,
            Checked = _hideTaskbar
        };
        hideTaskbar.CheckedChanged += (_, _) => SetHideTaskbar(hideTaskbar.Checked);

        var refresh = new ToolStripMenuItem("Refresh now");
        refresh.Click += async (_, _) =>
        {
            EnsureWindow();
            await _window!.Dispatcher.InvokeAsync(async () => await _window.RefreshFromTrayAsync());
        };

        var diagnostics = new ToolStripMenuItem("Diagnostics");
        diagnostics.Click += (_, _) =>
        {
            EnsureWindow();
            _window!.Dispatcher.Invoke(() => new DiagnosticsWindow { Owner = _window }.Show());
        };

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => Shutdown();

        menu.Items.Add(showHide);
        menu.Items.Add(hideTaskbar);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(refresh);
        menu.Items.Add(diagnostics);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        _tray.ContextMenuStrip = menu;

        _tray.MouseClick += (_, ev) =>
        {
            if (ev.Button == MouseButtons.Left)
                ToggleWindowVisibility();
        };
    }

    private void SetHideTaskbar(bool hide)
    {
        _hideTaskbar = hide;
        SaveAppState();

        if (_window == null) return;

        _window.Dispatcher.Invoke(() =>
        {
            _window.ShowInTaskbar = !hide;
        });
    }

    private void ToggleWindowVisibility()
    {
        EnsureWindow();

        _window!.Dispatcher.Invoke(() =>
        {
            if (_window.Visibility == Visibility.Visible)
            {
                _window.Hide();
            }
            else
            {
                _window.Show();
                _window.Activate();
            }
        });
    }

    private void EnsureWindow()
    {
        if (_window != null) return;

        _window = new OverlayWindow();
        _window.ShowInTaskbar = !_hideTaskbar;
        _window.Closed += (_, _) => { _window = null; };
        _window.Show();
    }

    private void LoadAppState()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_appStatePath)!);

            if (!File.Exists(_appStatePath))
            {
                _hideTaskbar = false;
                return;
            }

            var json = File.ReadAllText(_appStatePath);
            var state = JsonSerializer.Deserialize<AppStateModel>(json);
            _hideTaskbar = state?.HideTaskbar ?? false;
        }
        catch
        {
            _hideTaskbar = false;
        }
    }

    private void SaveAppState()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_appStatePath)!);

            var state = new AppStateModel { HideTaskbar = _hideTaskbar };
            File.WriteAllText(
                _appStatePath,
                JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }

        base.OnExit(e);
    }

    private sealed class AppStateModel
    {
        public bool HideTaskbar { get; set; }
    }
}