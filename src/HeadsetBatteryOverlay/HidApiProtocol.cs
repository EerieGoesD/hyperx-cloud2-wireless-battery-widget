using System;

namespace HeadsetBatteryOverlay;

// Direct translation of the working C++ logic (buffer sizes, offsets, report priming).
public static class HidApiProtocol
{
    public static int GetBatteryLevel(IntPtr hidDevice, string manufacturer, string productName)
    {
        const int WRITE_BUFFER_SIZE = 52;
        const int DATA_BUFFER_SIZE = 20;

        var writeBuffer = new byte[WRITE_BUFFER_SIZE];
        int batteryIndex = 7;

        if (manufacturer.Contains("HP", StringComparison.OrdinalIgnoreCase))
        {
            if (productName.Contains("Cloud II Core", StringComparison.OrdinalIgnoreCase))
            {
                writeBuffer[0] = 0x66;
                writeBuffer[1] = 0x89;
                batteryIndex = 4;
            }
            else if (productName.Contains("Cloud II Wireless", StringComparison.OrdinalIgnoreCase) ||
                     productName.Contains("Cloud Stinger 2 Wireless", StringComparison.OrdinalIgnoreCase))
            {
                writeBuffer[0] = 0x06;
                writeBuffer[1] = 0xFF;
                writeBuffer[2] = 0xBB;
                writeBuffer[3] = 0x02;
                batteryIndex = 7;
            }
            else if (productName.Contains("Cloud Alpha Wireless", StringComparison.OrdinalIgnoreCase))
            {
                writeBuffer[0] = 0x21;
                writeBuffer[1] = 0xBB;
                writeBuffer[2] = 0x0B;
                batteryIndex = 3;
            }
        }
        else
        {
            // Kingston Cloud II: prime input report before writes.
            const int INPUT_BUFFER_SIZE = 160;
            var buf = new byte[INPUT_BUFFER_SIZE];
            buf[0] = 0x06; // report id
            HidApi.hid_get_input_report(hidDevice, buf, buf.Length);

            // Kingston Cloud II Wireless data
            writeBuffer[0]  = 0x06;
            writeBuffer[2]  = 0x02;
            writeBuffer[4]  = 0x9A;
            writeBuffer[7]  = 0x68;
            writeBuffer[8]  = 0x4A;
            writeBuffer[9]  = 0x8E;
            writeBuffer[10] = 0x0A;
            writeBuffer[14] = 0xBB;
            writeBuffer[15] = 0x02;
            batteryIndex = 7;
        }

        HidApi.hid_write(hidDevice, writeBuffer, writeBuffer.Length);

        var dataBuffer = new byte[DATA_BUFFER_SIZE];
        HidApi.hid_read_timeout(hidDevice, dataBuffer, dataBuffer.Length, 1000);

        if (batteryIndex < 0 || batteryIndex >= dataBuffer.Length) return -1;
        return dataBuffer[batteryIndex];
    }
}
