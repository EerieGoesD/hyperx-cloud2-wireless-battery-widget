# HeadsetBatteryOverlay

Always-on-top, draggable Windows overlay that shows the **battery percentage** of the **HyperX Cloud II Wireless** headset in real time.

Includes:
- Compact mode toggle: **“-”** to show only `50%`, **“+”** to restore full view
- Links:
  - Made by EERIE → https://linktr.ee/eeriegoesd
  - Buy Me a Coffee → https://buymeacoffee.com/eeriegoesd

## Run
- Launch the app.
- Drag the widget to reposition it.
- Use **“-”** / **“+”** to toggle compact mode.

## Build (developers)
Requirements:
- Windows 10/11
- Visual Studio 2022
- .NET 8 SDK

Steps:
1. Open `HeadsetBatteryOverlay_HidApi.sln`
2. Select **x64**
3. Build and run (F5) or build Release and run the produced EXE

## Third-party (HIDAPI)
This project uses HIDAPI for USB/HID communication.

- Notices: `THIRD_PARTY_NOTICES.txt`
- HIDAPI license: `src/HeadsetBatteryOverlay/third_party/hidapi/licenses/LICENSE-bsd.txt`

## License
See `LICENSE`.
