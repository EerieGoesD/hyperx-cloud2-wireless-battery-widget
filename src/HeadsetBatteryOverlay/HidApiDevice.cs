using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HeadsetBatteryOverlay;

public sealed class HidApiDevice : IDisposable
{
    public IntPtr Handle { get; private set; }

    public bool IsInvalid => Handle == IntPtr.Zero;

    private HidApiDevice(IntPtr handle)
    {
        Handle = handle;
    }

    public static HidApiDevice Open(string path)
    {
        var h = HidApi.hid_open_path(path);
        return new HidApiDevice(h);
    }

    public string GetManufacturerString()
    {
        var sb = new StringBuilder(64);
        HidApi.hid_get_manufacturer_string(Handle, sb, sb.Capacity);
        return sb.ToString();
    }

    public string GetProductString()
    {
        var sb = new StringBuilder(128);
        HidApi.hid_get_product_string(Handle, sb, sb.Capacity);
        return sb.ToString();
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            HidApi.hid_close(Handle);
            Handle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
