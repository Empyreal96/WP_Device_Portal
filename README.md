# WP_Device_Portal
Small tool for easily using Windows Device Portal on Windows 10 Mobile (and Desktop)

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

Performance Info (Not real-time yet):

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
- Minimum build 10240 required
