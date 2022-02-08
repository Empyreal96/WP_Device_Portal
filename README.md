# Windows Phone Device Portal Client
Small tool for easily using Windows Device Portal on Windows 10 Mobile (and Desktop)

*Sadly it's currently broken for some insider builds, the "first" release will be recommended if current builds crash*
## Requirements:
- Windows Device Portal enabled with Authentication Off
- Windows 10 Mobile (or Desktop) build 10240+

![](screenshot.jpg)

## Features:
Summary Info:
 - Network Adapter Info
 - WiFi IP Info

Applications:
- Install
- Uninstall
- Launch
- Information

Processes:
- Running Processes

Performance Info Graphs:

- CPU Load
- GPU
- Memory Pages
- Network

Devices (Basic):

- Hardware Driver Info

### Notes
- This is makes use of the [WindowsDevicePortalWrapper library and UWP sample](https://github.com/microsoft/WindowsDevicePortalWrapper).
- WDPWrapper I have slightly modified to report Platform as Windows for Unknown platforms
- Thanks to [BAstifan](https://github.com/basharast) for help with Processes Table
- The graphs were provided by [UWPQuickCharts](https://github.com/ailon/UWPQuickCharts)
