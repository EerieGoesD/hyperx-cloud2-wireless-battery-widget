using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HeadsetBatteryOverlay;

public sealed class HidApiDeviceInfoRow
{
    public string Vid { get; init; } = "";
    public string Pid { get; init; } = "";
    public int Usage { get; init; }
    public int UsagePage { get; init; }
    public string Manufacturer { get; init; } = "";
    public string Product { get; init; } = "";
    public string Path { get; init; } = "";
    public string PathShort => Path.Length <= 80 ? Path : Path[..77] + "...";
}

public sealed class HidApiDeviceInfo
{
    public string Path { get; init; } = "";
    public ushort VendorId { get; init; }
    public ushort ProductId { get; init; }
    public int Usage { get; init; }
    public int UsagePage { get; init; }
}

public static class HidApi
{
    // From the original repo
    private const ushort VID_KINGSTON = 2385; // 0x0951
    private const ushort VID_HP = 1008;       // 0x03F0

    private const ushort PID_KINGSTON_CLOUD_II = 5912;  // 0x1718
    private const ushort PID_HP_CLOUD_II = 395;         // 0x018B
    private const ushort PID_HP_CLOUD_II_B = 1686;      // 0x0696
    private const ushort PID_HP_CLOUD_II_CORE = 2453;   // 0x0995
    private const ushort PID_HP_CLOUD_ALPHA = 2445;     // 0x098D
    private const ushort PID_HP_CLOUD_STINGER_II = 3475;// 0x0D93

    private static readonly ushort[] VENDORS = { VID_KINGSTON, VID_HP, VID_HP, VID_HP, VID_HP, VID_HP };
    private static readonly ushort[] PRODUCTS = { PID_KINGSTON_CLOUD_II, PID_HP_CLOUD_II, PID_HP_CLOUD_II_CORE, PID_HP_CLOUD_ALPHA, PID_HP_CLOUD_II_B, PID_HP_CLOUD_STINGER_II };

    public static void EnsureLoadedOrThrow()
    {
        // Force a call so missing DLL surfaces immediately with a clear message.
        try
        {
            hid_init();
        }
        catch (DllNotFoundException)
        {
            throw new Exception("hidapi.dll not found. Place the x64 hidapi.dll at src\\HeadsetBatteryOverlay\\third_party\\hidapi\\x64\\hidapi.dll and rebuild.");
        }
    }

    public static HidApiDeviceInfo? GetHeadsetDeviceInfoHighestUsage()
    {
        IntPtr devices = IntPtr.Zero;

        // Loop supported vendor/product pairs until we get any devices.
        for (int i = 0; i < VENDORS.Length && devices == IntPtr.Zero; i++)
        {
            devices = hid_enumerate(VENDORS[i], PRODUCTS[i]);
        }

        if (devices == IntPtr.Zero) return null;

        try
        {
            int highestUsage = 0;
            int highestUsagePage = 0;
            HidApiDeviceInfo? best = null;

            IntPtr cur = devices;
            while (cur != IntPtr.Zero)
            {
                var info = Marshal.PtrToStructure<hid_device_info>(cur);

                int usage = unchecked((int)info.usage);
                int usagePage = unchecked((int)info.usage_page);

                if (usage > highestUsage || (usage == highestUsage && usagePage >= highestUsagePage))
                {
                    highestUsage = usage;
                    highestUsagePage = usagePage;

                    best = new HidApiDeviceInfo
                    {
                        Path = Marshal.PtrToStringAnsi(info.path) ?? "",
                        VendorId = info.vendor_id,
                        ProductId = info.product_id,
                        Usage = usage,
                        UsagePage = usagePage
                    };
                }

                cur = info.next;
            }

            return best;
        }
        finally
        {
            hid_free_enumeration(devices);
        }
    }

    public static List<HidApiDeviceInfoRow> EnumerateSupportedDevices()
    {
        var list = new List<HidApiDeviceInfoRow>();

        IntPtr devices = IntPtr.Zero;
        for (int i = 0; i < VENDORS.Length && devices == IntPtr.Zero; i++)
        {
            devices = hid_enumerate(VENDORS[i], PRODUCTS[i]);
        }

        if (devices == IntPtr.Zero) return list;

        try
        {
            IntPtr cur = devices;
            while (cur != IntPtr.Zero)
            {
                var info = Marshal.PtrToStructure<hid_device_info>(cur);

                string path = Marshal.PtrToStringAnsi(info.path) ?? "";
                string manuf = PtrToWideString(info.manufacturer_string);
                string prod = PtrToWideString(info.product_string);

                list.Add(new HidApiDeviceInfoRow
                {
                    Vid = $"0x{info.vendor_id:X4}",
                    Pid = $"0x{info.product_id:X4}",
                    Usage = unchecked((int)info.usage),
                    UsagePage = unchecked((int)info.usage_page),
                    Manufacturer = manuf,
                    Product = prod,
                    Path = path
                });

                cur = info.next;
            }
        }
        finally
        {
            hid_free_enumeration(devices);
        }

        return list;
    }

    private static string PtrToWideString(IntPtr p)
    {
        if (p == IntPtr.Zero) return "";
        return Marshal.PtrToStringUni(p) ?? "";
    }

    // ===== hidapi P/Invoke =====
    [StructLayout(LayoutKind.Sequential)]
    public struct hid_device_info
    {
        public IntPtr path; // char*
        public ushort vendor_id;
        public ushort product_id;
        public IntPtr serial_number; // wchar_t*
        public ushort release_number;
        public IntPtr manufacturer_string; // wchar_t*
        public IntPtr product_string; // wchar_t*
        public ushort usage_page;
        public ushort usage;
        public int interface_number;
        public IntPtr next; // hid_device_info*
    }

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int hid_init();

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void hid_exit();

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr hid_enumerate(ushort vendor_id, ushort product_id);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void hid_free_enumeration(IntPtr devs);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr hid_open_path([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void hid_close(IntPtr device);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern int hid_get_manufacturer_string(IntPtr device, StringBuilder @string, int maxlen);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern int hid_get_product_string(IntPtr device, StringBuilder @string, int maxlen);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int hid_write(IntPtr device, byte[] data, int length);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int hid_read_timeout(IntPtr device, byte[] data, int length, int milliseconds);

    [DllImport("hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int hid_get_input_report(IntPtr device, byte[] data, int length);
}
