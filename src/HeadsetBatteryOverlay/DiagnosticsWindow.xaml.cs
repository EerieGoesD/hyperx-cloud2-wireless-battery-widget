using System;
using System.Linq;
using System.Windows;

namespace HeadsetBatteryOverlay;

public partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindow()
    {
        InitializeComponent();
        BtnRefresh.Click += (_, _) => LoadDevices();
        BtnCopy.Click += (_, _) => CopySelected();
        Loaded += (_, _) => LoadDevices();
    }

    private void LoadDevices()
    {
        var list = HidApi.EnumerateSupportedDevices();

        GridDevices.ItemsSource = list
            .OrderByDescending(d => d.Usage)
            .ThenByDescending(d => d.UsagePage)
            .ThenBy(d => d.Manufacturer)
            .ToList();
    }

    private void CopySelected()
    {
        if (GridDevices.SelectedItem is not HidApiDeviceInfoRow row) return;
        System.Windows.Clipboard.SetText(row.Path);
    }
}