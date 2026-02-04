// App.xaml.cs
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace HeadsetBatteryOverlay;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _tray;
    private OverlayWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _window = new OverlayWindow();
        _window.Closed += (_, _) => { _window = null; };
        _window.Show();

        CreateTrayIcon();
    }

    private void CreateTrayIcon()
    {
        _tray = new NotifyIcon
        {
            Text = "Headset Battery Overlay",
            Visible = true,
            Icon = new Icon("tray.ico")
        };

        var menu = new ContextMenuStrip();

        var showHide = new ToolStripMenuItem("Show/Hide");
        showHide.Click += (_, _) => ToggleWindowVisibility();

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
        _window.Closed += (_, _) => { _window = null; };
        _window.Show();
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
}