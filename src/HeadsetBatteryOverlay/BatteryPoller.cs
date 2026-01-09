using System;
using System.Text;
using System.Threading.Tasks;

namespace HeadsetBatteryOverlay;

public sealed class BatteryPoller
{
    public Task<BatteryReadResult> TryReadBatteryAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                HidApi.EnsureLoadedOrThrow();

                var devInfo = HidApi.GetHeadsetDeviceInfoHighestUsage();
                if (devInfo is null)
                    return new BatteryReadResult(false, 0, "No headset device detected.", "");

                using var dev = HidApiDevice.Open(devInfo.Path);
                if (dev.IsInvalid)
                    return new BatteryReadResult(false, 0, "Could not connect to headset.", "");

                var manufacturer = dev.GetManufacturerString();
                var product = dev.GetProductString();
                var label = (manufacturer + " " + product).Trim();

                int pct = HidApiProtocol.GetBatteryLevel(dev.Handle, manufacturer, product);

                if (pct == 0)
                    return new BatteryReadResult(false, 0, "Headset found but not active.", label);

                if (pct < 0 || pct > 100)
                    return new BatteryReadResult(false, 0, "Battery N/A.", label);

                return new BatteryReadResult(true, pct, "", label);
            }
            catch (Exception ex)
            {
                // hidapi.dll missing or load failure will land here.
                return new BatteryReadResult(false, 0, ex.Message, "");
            }
        });
    }
}

public sealed record BatteryReadResult(bool Success, int BatteryPercent, string Message, string DeviceLabel);
