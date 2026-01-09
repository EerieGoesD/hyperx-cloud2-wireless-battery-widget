using System.Windows;

namespace HeadsetBatteryOverlay;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new OverlayWindow();
        window.Show();
    }
}
