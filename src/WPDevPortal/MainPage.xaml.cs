using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Tools.WindowsDevicePortal;
using static Microsoft.Tools.WindowsDevicePortal.DevicePortal;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.Security.ExchangeActiveSyncProvisioning;
using System.Linq;
using System.Threading;
using Windows.Networking.BackgroundTransfer;
using System.Net.NetworkInformation;
using Octokit;
using System.IO;
using LightBuzz.Archiver;
using Windows.Management.Deployment;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.System.Profile;
using System.Collections.Specialized;
using Windows.Security.Credentials;
using Windows.Devices.WiFi;
using ExceptionHelper;
using Windows.Data.Xml.Dom;

using System.IO.Compression;
using System.Xml.Linq;
using System.Xml;
using Windows.Security.Cryptography;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.Devices.Radios;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using System.ComponentModel;

namespace WPDevPortal
{
    /// <summary>
    /// The main page of the application.
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        /// <summary>
        /// The device portal to which we are connecting.
        /// </summary>
        bool FinishedLoadingData;
        private DevicePortal portal;
        private Certificate certificate;
        private string WDPAddress { get; set; }
        private string WDPUser { get; set; }
        private string WDPPass { get; set; }
        public string pkgResults { get; set; }
        public Task<AppPackages> packages { get; set; }
        public Task<List<Device>> hardwareList { get; set; }
        public StringBuilder sb { get; set; }
        public StringBuilder sb1 { get; set; }
        public StringBuilder sb2 { get; set; }
        private string pkgFullName { get; set; }
        private string pkgAppID { get; set; }
        private string pkgOrigin { get; set; }
        private string pkgPublisher { get; set; }
        private string pkgVersion { get; set; }
        private bool IsMatching { get; set; }
        private DispatcherTimer _timer = new DispatcherTimer();
        private bool isConnected { get; set; }
        private SystemPerformanceInformation perfResult { get; set; }
        private AppCrashDumpSettings dumpResult { get; set; }
        private List<AppPackage> app { get; set; }
        private EtwProviders ETWResult { get; set; }
        private bool IsETWStarted { get; set; }
        private WebSocketMessageReceivedEventArgs<EtwEvents> ETWEventList { get; set; }
        int etwTimer = 0;
        string etwMessage;
        private string[] ETWLogger { get; set; }
        private List<string> ETW_LOGGER { get; set; }
        string selectedKnownFolder { get; set; }
        string itemId = null;
        bool IsCrashDumpEnabled;
        PasswordVault PasswordStore = new PasswordVault();
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        string WindowsEdition { get; set; }
        /// <summary>
        /// The main page constructor.
        /// </summary>
        public MainPage()
        {
            try
            {

                this.InitializeComponent();
                this.EnableDeviceControls(false);
                rootPage = this;
                //
                // Hide Crash Dumps for now
                EnablePivotPages(false);
                // MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPage"));
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
                {
                    CloseBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    CloseBtn.Visibility = Visibility.Collapsed;
                }



                IsETWStarted = false;
                AppNamesCombo.IsEnabled = false;
                ProgRingFiles.IsEnabled = false;
                ProgressText.Visibility = Visibility.Collapsed;
                AppNamesCombo.Visibility = Visibility.Collapsed;
                DLButton.Visibility = Visibility.Collapsed;
                ProgressBarDownload.Visibility = Visibility.Collapsed;
                Acknowledgements.Text =
                    $"This software uses Open Source libraries.\n" +
                    $"• WindowsDevicePortalWrapper and Sample by Microsoft.\n" +
                    $"• UWPQuickCharts by 'ailon'\n" +
                    $"• Octokit by Github\n" +
                    $"• ArchiverPlus Class by Lightbuzz(?)\n" +
                    $"• SharpCompress archiver for Appx reading\n" +
                    $"• Various StackExchange pages\n\n" +
                    $"Thanks to BAstifan for contributions to development";
                WindowsEdition = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
                string lastSavedAddress = LoadLastAddress();
                if (lastSavedAddress == "NoAddress")
                {
                    if (WindowsEdition == "Windows.Desktop")
                    {
                        address.Text = @"http://127.0.0.1:50080";
                        WDPAddress = address.Text;
                        WDPUser = "Administrator";
                        ConnectionUser.Text = "Administrator";
                        WDPPass = "IJustNeedSomeTextHere";
                        connectToDevice.IsEnabled = true;
                    }
                    else if (WindowsEdition == "Windows.Mobile")
                    {
                        address.Text = @"https://127.0.0.1";
                        WDPAddress = address.Text;
                        WDPUser = "Administrator";
                        ConnectionUser.Text = "Administrator";
                        WDPPass = "IJustNeedSomeTextHere";
                        connectToDevice.IsEnabled = true;
                    }
                    else
                    {
                        address.Text = @"https://127.0.0.1";
                        WDPAddress = address.Text;
                        WDPUser = "Administrator";
                        ConnectionUser.Text = "Administrator";
                        WDPPass = "IJustNeedSomeTextHere";
                        connectToDevice.IsEnabled = true;
                    }
                }
                else
                {
                    address.Text = lastSavedAddress;
                    WDPAddress = address.Text;
                    WDPUser = "Administrator";
                    ConnectionUser.Text = "Administrator";
                    WDPPass = "IJustNeedSomeTextHere";
                    connectToDevice.IsEnabled = true;
                }


            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }

        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            this.DataContext = this;

            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += UpdateRealTimeData;
            _timer.Start();



        }


        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GC.Collect();
            PivotItem pivot = null;
            pivot = (PivotItem)(sender as Pivot).SelectedItem;
            if (pivot.Header.ToString() != "Processes")
            {
                processesListView.IsEnabled = false;
            }
            else
            {
                processesListView.IsEnabled = true;
            }

            if (pivot.Header.ToString() != "Performance")
            {
                _timer.Stop();
                CPUGraph.IsEnabled = false;
                GPUGraph.IsEnabled = false;
                IOData.IsEnabled = false;
                NetData.IsEnabled = false;
            }
            else
            {
                _timer.Start();
                CPUGraph.IsEnabled = true;
                GPUGraph.IsEnabled = true;
                IOData.IsEnabled = true;
                NetData.IsEnabled = true;
            }
        }

        /// <summary>
        /// TextChanged handler for the address text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Address_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /// <summary>
        /// If specified in the UI, clears the test output display, otherwise does nothing.
        /// </summary>


        bool autoreboot;
        int maxDumpCount;
        bool overwriteDump;
        DumpFileSettings.DumpTypes dumpType;
        int i;
        /// <summary>
        /// Click handler for the connectToDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void ConnectToDevice_Click(object sender, RoutedEventArgs e)
        {


            try
            {
                FinishedLoadingData = false;
                ProgBar.Visibility = Visibility.Visible;
                ProgBar.IsEnabled = true;
                ProgBar.IsIndeterminate = true;

                this.EnableConnectionControls(false);
                this.EnableDeviceControls(false);
                ConnectionNoticeVisible = Visibility.Visible;


                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   commandOutput.Text = "Connecting, Please wait..";
                               });
                bool allowUntrusted = true;
                WDPAddress = address.Text;
                if (ConnectionUser.Text == "")
                {

                }
                else
                {
                    WDPUser = ConnectionUser.Text;
                }
                if (ConnectionPassword.Password == "")
                {

                }
                else
                {
                    WDPPass = ConnectionPassword.Password;
                }
                portal = new DevicePortal(
                    new DefaultDevicePortalConnection(
                        WDPAddress,
                        WDPUser,
                        WDPPass));


                EasClientDeviceInformation eas = new EasClientDeviceInformation();
                string DeviceManufacturer = eas.SystemManufacturer;
                string DeviceModel = eas.SystemProductName;


                sb = new StringBuilder();
                sb1 = new StringBuilder();
                sb2 = new StringBuilder();
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   commandOutput.Text = "";
                               });
                
                portal.ConnectionStatus += async (portal, connectArgs) =>
                {

                    if (connectArgs.Status == DeviceConnectionStatus.Connected)
                    {
                        RadioAccessStatus result = await Radio.RequestAccessAsync();

                        // var token = processesAppsContainerScroll.RegisterPropertyChangedCallback(ScrollViewer.HorizontalOffsetProperty, OnScrollChangedChange);

                        sb.Append("Connected to: ");
                        sb.AppendLine(portal.Address);
                        sb.Append("OS version: ");
                        sb.AppendLine($"10.0.{portal.OperatingSystemVersion}");
                        sb.Append("Device family: ");
                        sb.AppendLine(portal.DeviceFamily.Replace(".", " "));
                        sb.Append("Platform: ");
                        sb.AppendLine(String.Format("{0} ({1})",
                            portal.PlatformName,
                            portal.Platform.ToString()));


                        if (WDPAddress.Contains("127.0.0.1"))
                        {
                            sb.Append("Manufacture: ");
                            sb.AppendLine($"{DeviceManufacturer}");
                            sb.Append("Model: ");
                            sb.AppendLine($"{DeviceModel}");
                        }
                        Task<BatteryState> batteryStat = portal.GetBatteryStateAsync();
                        BatteryState batteryResult = batteryStat.Result;
                        sb.Append("Battery Level: ");
                        sb.AppendLine($"{batteryResult.Level.ToString()}%");
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   sb.Append(this.commandOutput.Text);
                                   sb.AppendLine("");


                                   this.commandOutput.Text = sb.ToString();

                               });

                        List<string> crashDumpList = new List<string>();
                        if (crashDumpList.Count > 0)
                        {
                            crashDumpList.Clear();
                        }
                        List<string> appNamesList = new List<string>();
                        if (appNamesList.Count > 0)
                        {
                            appNamesList.Clear();
                        }

                        List<string> appNamesListFull = new List<string>();
                        if (appNamesListFull.Count > 0)
                        {
                            appNamesListFull.Clear();
                        }

                        await Task.Run(async () =>
                        {

                            packages = portal.GetInstalledAppPackagesAsync();
                            foreach (var pkg in packages.Result.Packages)
                            {
                                // Updating te textBox required this to prevent halting the main thread
                                appNamesListFull.Add(pkg.FullName);
                                //appsComboBox.Items.Add(pkg.FullName);
                                if (!pkg.FullName.Contains("8wekyb3d8bbwe"))
                                {
                                    if (!pkg.FullName.Contains("cw5n1h2txyewy"))
                                    {
                                        appNamesList.Add(pkg.FullName);
                                        //AppNamesCombo.Items.Add(pkg.FullName);
                                        crashDumpList.Add(pkg.FullName);
                                        // CrashDumpAppCombo.Items.Add(pkg.FullName);
                                    }
                                }


                            }

                            appNamesList.Sort();
                            appNamesListFull.Sort();
                            crashDumpList.Sort();
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                        () =>
                                        {
                                            appsComboBox.ItemsSource = appNamesListFull;
                                            AppNamesCombo.ItemsSource = appNamesList;
                                            CrashDumpAppCombo.ItemsSource = crashDumpList;
                                        });




                            portal.RealtimeEventsMessageReceived += Portal_RealtimeEventsMessageReceived;
                            // ETWLogger.Add($"[Timestamp]\n[ID] Provider\nValues\n\n");
                          

                        });


                        isConnected = true;
                        // await Task.Run(() =>
                        //{
                        if (WDPAddress.Contains("127.0.0.1"))
                        {
                            FetchProcessInfo();
                            FillInitialRealTimeData();
                            FetchHWInfo();
                            FetchKnownFolders();
                            ETWSetup();
                            FetchWifiInformation();
                            GetRadioInfo();
                            if (WDPAddress.Contains("127.0.0.1"))
                            {
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                        () =>
                                        {
                                            CPUHeader.Text = $"Processor: {ProcessorName}";
                                        });
                            }
                            // DisablePagesForXbox(true);
                        }
                        else
                        {
                            if (portal.DeviceFamily == "Windows.Xbox")
                            {
                                DisablePagesForXbox(true);
                                FillInitialRealTimeData();
                                FetchKnownFolders();
                            }
                            else
                            {
                                FetchProcessInfo();
                                FillInitialRealTimeData();
                                FetchHWInfo();
                                FetchKnownFolders();
                                ETWSetup();
                                FetchWifiInformation();
                            }
                        }
                        //   });


                        /// Crash dumps for Windows 10 Mobile are limited to Insider builds.... I NEED to have a nicer way to check this...
                        /// Crash Dumps cause a crash on insider builds also, not sure why :(

                        if (portal.DeviceFamily == "Windows.Mobile" || portal.DeviceFamily == "Windows.Xbox")
                        {

                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                           () =>
                           {
                               MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPivot"));
                               MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPage"));

                           });

                        }
                        else
                        {

                            DumpFileSettings test = await portal.GetDumpFileSettingsAsync();
                            dumpType = test.DumpType;
                            autoreboot = test.AutoReboot;
                            maxDumpCount = test.MaxDumpCount;
                            overwriteDump = test.Overwrite;
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                   () =>
                                   {
                                       switch (dumpType)
                                       {
                                           case DumpFileSettings.DumpTypes.Disabled:
                                               IsCrashDumpEnabled = false;
                                               AutoRebootTog.IsEnabled = false;
                                               OverwriteDumpTog.IsEnabled = false;
                                               MaxDumpSlider.IsEnabled = false;
                                               DumpDisabledItem.IsSelected = true;
                                               DumpTypeCombo.SelectedIndex = 0;
                                               break;
                                           case DumpFileSettings.DumpTypes.CompleteMemoryDump:
                                               IsCrashDumpEnabled = true;
                                               DumpCompleteItem.IsSelected = true;
                                               DumpTypeCombo.SelectedIndex = 1;
                                               MaxDumpSlider.Value = maxDumpCount;
                                               AutoRebootTog.IsOn = autoreboot;
                                               OverwriteDumpTog.IsOn = overwriteDump;
                                               AutoRebootTog.IsEnabled = true;
                                               OverwriteDumpTog.IsEnabled = true;
                                               MaxDumpSlider.IsEnabled = true;
                                               break;
                                           case DumpFileSettings.DumpTypes.KernelDump:
                                               IsCrashDumpEnabled = true;
                                               DumpKernelItem.IsSelected = true;
                                               DumpTypeCombo.SelectedIndex = 2;
                                               MaxDumpSlider.Value = maxDumpCount;
                                               AutoRebootTog.IsOn = autoreboot;
                                               OverwriteDumpTog.IsOn = overwriteDump;
                                               AutoRebootTog.IsEnabled = true;
                                               OverwriteDumpTog.IsEnabled = true;
                                               MaxDumpSlider.IsEnabled = true;

                                               break;
                                           case DumpFileSettings.DumpTypes.Minidump:
                                               IsCrashDumpEnabled = true;
                                               DumpMiniItem.IsSelected = true;
                                               DumpTypeCombo.SelectedIndex = 3;
                                               MaxDumpSlider.Value = maxDumpCount;
                                               AutoRebootTog.IsOn = autoreboot;
                                               OverwriteDumpTog.IsOn = overwriteDump;
                                               AutoRebootTog.IsEnabled = true;
                                               OverwriteDumpTog.IsEnabled = true;
                                               MaxDumpSlider.IsEnabled = true;

                                               break;
                                           default:
                                               IsCrashDumpEnabled = false;
                                               AutoRebootTog.IsEnabled = false;
                                               OverwriteDumpTog.IsEnabled = false;
                                               MaxDumpSlider.IsEnabled = false;
                                               DumpDisabledItem.IsSelected = true;
                                               DumpTypeCombo.SelectedIndex = 0;

                                               break;
                                       }




                                   });
                        }


                        await Task.Delay(3000);
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                              () =>
                              {
                                 
                                  ProgBar.Visibility = Visibility.Collapsed;
                                  ProgBar.IsEnabled = false;
                                  ProgBar.IsIndeterminate = false;
                                  address.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Green);
                                  connectedbox.Text = "connected";
                                  connectedbox.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                                  connectedbox.Visibility = Visibility.Visible;
                                  connectToDevice.IsEnabled = false;
                                  EnablePivotPages(true);
                                  if (WDPAddress.Contains("127.0.0.1"))
                                  {
                                      LocalAppsPanel.Visibility = Visibility.Visible;
                                  }
                                  else
                                  {
                                      LocalAppsPanel.Visibility = Visibility.Collapsed;
                                  }
                                  BTToggle.Toggled += BTToggle_Toggled;
                              });
                        SaveLastAddress(WDPAddress);
                        FinishedLoadingData = true;
                        ConnectionNoticeVisible = Visibility.Collapsed;
 

                        Debug.WriteLine("Finished loading data");



                    }
                    else if (connectArgs.Status == DeviceConnectionStatus.Failed)
                    {
                        sb.AppendLine("Failed to connect to the device.");
                        sb.AppendLine($"Message: {connectArgs.Message}\n\nPhase: {connectArgs.Phase}");
                        connectToDevice.IsEnabled = true;
                        ConnectionNoticeVisible = Visibility.Collapsed;

                    }
                };
               
                try
                {
                    // If the user wants to allow untrusted connections, make a call to GetRootDeviceCertificate
                    // with acceptUntrustedCerts set to true. This will enable untrusted connections for the
                    // remainder of this session.
                    if (allowUntrusted)
                    {
                        this.certificate = await portal.GetRootDeviceCertificateAsync(true);
                    }
                    await portal.ConnectAsync(manualCertificate: this.certificate);
                    EnableDeviceControls(true);

                }
                catch (Exception exception)
                {
                    EnableDeviceControls(false);
                    EnableConnectionControls(true);
                    ProgBar.Visibility = Visibility.Collapsed;
                    ProgBar.IsEnabled = false;
                    ProgBar.IsIndeterminate = false;
                    address.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                    connectedbox.Text = "failed";
                    connectedbox.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                    connectedbox.Visibility = Visibility.Visible;
                    ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(exception);
                    //sb.AppendLine(exception.Message);
                    connectToDevice.IsEnabled = true;
                    ConnectionNoticeVisible = Visibility.Collapsed;

                }

                this.commandOutput.Text = sb.ToString();

                EnableConnectionControls(true);
                // connectToDevice.IsEnabled = false;
            }
            catch (Exception ex)
            {
                EnableDeviceControls(false);
                EnableConnectionControls(true);
                ProgBar.Visibility = Visibility.Collapsed;
                ProgBar.IsEnabled = false;
                ProgBar.IsIndeterminate = false;
                address.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                connectedbox.Text = "disconnected";
                connectedbox.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                connectedbox.Visibility = Visibility.Visible;
                commandOutput.Text = ex.Message;
                connectToDevice.IsEnabled = true;
                ConnectionNoticeVisible = Visibility.Collapsed;

                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }


        private async void ETWSetup()
        {
            var ETWStart = portal.GetEtwProvidersAsync();
            ETWResult = ETWStart.Result;
            List<string> etwprovlist = new List<string>();
            foreach (var etw in ETWResult.Providers)
            {
                etwprovlist.Add(etw.Name);

            }
            etwprovlist.Sort();
            foreach (var prov in etwprovlist)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
               () =>
               {
                   CrashAppList.Items.Add(prov);
               });
            }
        }
        private async void DisablePagesForXbox(bool IsDisabled)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                              () =>
                              {
                                  if (IsDisabled == true)
                                  {
                                      // MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "ApplicationPivot"));
                                      // MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "FilesPivot"));
                                      MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "ProcessesPivot"));
                                      // MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "PerfPivot"));
                                      MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "DevicePivot"));
                                      MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "WifiPivotItem"));
                                      MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPage"));
                                      MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "BluetoothPivotPage"));
                                      MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPivot"));
                                  }
                                  else
                                  {
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "ApplicationPivot"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "FilesPivot"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "ProcessesPivot"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "PerfPivot"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "DevicePivot"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "WifiPivotItem"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPivot"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPage"));
                                      MainPivot.Items.Add(MainPivot.Items.Single(p => ((PivotItem)p).Name == "BluetoothPivotPage"));
                                  }
                              });
        }

        private async void Portal_RealtimeEventsMessageReceived(DevicePortal sender, WebSocketMessageReceivedEventArgs<EtwEvents> args)
        {
            if (IsETWStarted == true)
            {
                if (args.Message == null)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () =>
                                    {
                                        ETWLogOutput.Text = "Null";
                                    });
                }
                else
                {

                    ETWEventList = args;
                    List<EtwEventInfo> fetchedevents = ETWEventList.Message.Events;
                    ETW_LOGGER = new List<string>();
                    i = 0;
                    foreach (var elist in fetchedevents)
                    {
                        
                        
                        var etwTimestamp = DateTime.UtcNow;
                        string etwprovider = elist.TaskName;
                        uint etwlevellog = elist.Level;
                        uint etwID = elist.PID;
                        string[] etwValues = elist.Values.ToArray();
                        Dictionary<string, string>.ValueCollection etwvalues = elist.Values;
                        ulong etwKeyword = elist.Keyword;
                        Dictionary<string, string>.KeyCollection etwKeys = elist.Keys;
                        List<string> keysList = new List<string>();
                        List<string> valuesList = new List<string>();
                        foreach (var key in etwKeys)
                        {
                            keysList.Add(key);
                        }

                        foreach (string val in etwvalues)
                        {
                            
                            valuesList.Add(val);
                        }

                        List<string> finalList = new List<string>();

                        if (keysList.Count == valuesList.Count)
                        {
                            for (int i = 0; i < keysList.Count; i++)
                            {
                                finalList.Add($"{keysList[i]}: {valuesList[i]}");
                            }
                        }

                        

                        ETW_LOGGER.Add($"[{etwTimestamp.ToString()}]\n" +
                            $"[PID: {etwID.ToString()}] {etwprovider.ToString()}:\n{string.Join("\n", finalList)}\n\n");
                        i++;
                        /* await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () =>
                                    {
                                        ETWLogOutput.Text += $"[{etwTimestamp.ToString()}]\n[{etwID.ToString()}] {etwprovider.ToString()}\n{etwMessage}\n\n";
                                    });
                       */
                    }
                }
            }
        }



        private void SaveLastAddress(string address)
        {
            localSettings.Values["LastIPAddress"] = address;
            Debug.WriteLine($"LastIpAddress {address} saved");
        }

        private string LoadLastAddress()
        {
            string address = localSettings.Values["LastIPAddress"] as string;
            if (address == null)
            {
                address = "NoAddress";
            }
            return address;
        }

        public void timer_Tick(object sender, EventArgs e)
        {
            FetchPerfInfo();
            
        }

        /// <summary>
        /// Enables or disables the Connect button based on the current state of the
        /// Address, User name and Password fields.
        /// </summary>
        private void EnableConnectButton()
        {

            this.connectToDevice.IsEnabled = true;
        }



        /// <summary>
        /// Sets the IsEnabled property appropriately for the connection controls.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void EnableConnectionControls(bool enable)
        {
            this.address.IsEnabled = enable;
            this.connectToDevice.IsEnabled = enable;

        }



        /// <summary>
        /// Sets the IsEnabled property appropriately for the device command controls.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void EnableDeviceControls(bool enable)
        {
            this.rebootDevice.IsEnabled = enable;
            this.shutdownDevice.IsEnabled = enable;

        }

        private void EnablePivotPages(bool enable)
        {
            if (enable == false)
            {
                /* MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "ApplicationPivot"));
                 MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "FilesPivot"));
                 MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "ProcessesPivot"));
                 MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "PerfPivot"));
                 MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "DevicePivot"));
                 MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "WifiPivotItem"));
                 MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPivot"));
                 MainPivot.Items.Remove(MainPivot.Items.Single(p => ((PivotItem)p).Name == "CrashDumpsPivot")); */

                ApplicationPivot.Visibility = Visibility.Collapsed;
                FilesPivot.Visibility = Visibility.Collapsed;
                ProcessesPivot.Visibility = Visibility.Collapsed;
                PerfPivot.Visibility = Visibility.Collapsed;
                DevicePivot.Visibility = Visibility.Collapsed;
                WifiPivotItem.Visibility = Visibility.Collapsed;
                CrashDumpsPivot.Visibility = Visibility.Collapsed;
                CrashDumpsPage.Visibility = Visibility.Collapsed;

                CrashDumpsPage.IsEnabled = false;
                ApplicationPivot.IsEnabled = false;
                FilesPivot.IsEnabled = false;
                ProcessesPivot.IsEnabled = false;
                PerfPivot.IsEnabled = false;
                DevicePivot.IsEnabled = false;
                WifiPivotItem.IsEnabled = false;
                CrashDumpsPivot.IsEnabled = false;


            }
            else
            {
                ApplicationPivot.Visibility = Visibility.Visible;
                FilesPivot.Visibility = Visibility.Visible;
                ProcessesPivot.Visibility = Visibility.Visible;
                PerfPivot.Visibility = Visibility.Visible;
                DevicePivot.Visibility = Visibility.Visible;
                WifiPivotItem.Visibility = Visibility.Visible;
                CrashDumpsPivot.Visibility = Visibility.Visible;
                CrashDumpsPage.Visibility = Visibility.Visible;

                CrashDumpsPage.IsEnabled = true;
                ApplicationPivot.IsEnabled = true;
                FilesPivot.IsEnabled = true;
                ProcessesPivot.IsEnabled = true;
                PerfPivot.IsEnabled = true;
                DevicePivot.IsEnabled = true;
                WifiPivotItem.IsEnabled = true;
                CrashDumpsPivot.IsEnabled = true;
            }
        }





        /// <summary>
        /// PasswordChanged handler for the password text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            EnableConnectButton();
        }

        /// <summary>
        /// Click handler for the rebootDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void RebootDevice_Click(object sender, RoutedEventArgs e)
        {
            bool reenableDeviceControls = false;


            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();

            sb.Append(commandOutput.Text);
            sb.AppendLine("Rebooting the device");
            commandOutput.Text = sb.ToString();

            try
            {
                await portal.RebootAsync();
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to reboot the device.");
                sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
                reenableDeviceControls = true;
            }

            commandOutput.Text = sb.ToString();
            EnableDeviceControls(reenableDeviceControls);
            EnableConnectionControls(true);
        }

        /// <summary>
        /// Click handler for the shutdownDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void ShutdownDevice_Click(object sender, RoutedEventArgs e)
        {
            bool reenableDeviceControls = false;


            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();
            sb.Append(commandOutput.Text);
            sb.AppendLine("Shutting down the device");
            commandOutput.Text = sb.ToString();
            try
            {
                await portal.ShutdownAsync();
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to shut down the device.");
                sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
                reenableDeviceControls = true;
            }

            commandOutput.Text = sb.ToString();
            EnableDeviceControls(reenableDeviceControls);
            EnableConnectionControls(true);
        }

        /// <summary>
        /// TextChanged handler for the username text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Username_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableConnectButton();
        }



        /// <summary>
        /// Loads a certificates asynchronously (runs on the UI thread).
        /// </summary>
        /// <returns></returns>
        private async Task LoadCertificate()
        {
            try
            {
                Exceptions.CustomException("TBA: Downloading current WDp Certificate");
            }
            catch (Exception exception)
            {
                this.commandOutput.Text = "Failed to get cert file: " + exception.Message;
            }
        }


        #region ApplicationPage

        private void appsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                applicationList.Text = "";
                sb1.Clear();
                string selected = (sender as ComboBox).SelectedItem.ToString();
                FetchAppInfo(selected);
                launchApp.IsEnabled = true;
                uninstallApp.IsEnabled = true;

            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }
        public string stuff { get; set; }




        string deplist;
        /// <summary>
        /// Fetch Package information
        /// </summary>
        /// <param name="selectedItem"></param>
        private async void FetchAppInfo(string selectedItem)
        {
            try
            {
                PackageManager pacman = new PackageManager();

                sb1.Append(applicationList.Text);
                sb1.AppendLine("Package Info:\n");
                foreach (var pkg in packages.Result.Packages)
                {
                    if (pkg.FullName == selectedItem)
                    {

                        IsMatching = true;
                        pkgFullName = pkg.FullName;
                        pkgAppID = pkg.AppId;
                        pkgOrigin = PkgOrigin(pkg.PackageOrigin);
                        pkgPublisher = pkg.Publisher;
                        pkgVersion = pkg.Version.Version.ToString(); ;
                    }
                }
                if (IsMatching == true)
                {

                    var test = pacman.FindPackageForUser("", pkgFullName);
                    deplist = string.Empty;

                    StorageFolder temp = ApplicationData.Current.TemporaryFolder;
                    var appEntry = await test.GetAppListEntriesAsync();
                    var packageID = test.Id.FullName;
                    var PackageLogo = "";
                    var fileName = $"{packageID}.png";
                    var logoSize = new Size
                    {
                        Height = 150,
                        Width = 150
                    };
                    if (appEntry != null && appEntry.Count > 0)
                    {
                        var Logo = await appEntry?.FirstOrDefault()?.DisplayInfo?.GetLogo(logoSize)?.OpenReadAsync();
                        if (Logo != null)
                        {
                            var storageFile = await temp.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                            using (var targetStream = await storageFile.OpenAsync(FileAccessMode.ReadWrite))
                            {
                                var output = targetStream.AsStreamForWrite();
                                Logo.AsStream().CopyTo(output);
                                output.Dispose();
                                PackageLogo = storageFile.Path;

                                try
                                {
                                    Logo.Dispose();
                                    storageFile = null;
                                }
                                catch (Exception e)
                                {

                                }
                            }
                        }
                    }

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () =>
                                {

                                    AppxImageIcon.Source = new BitmapImage(new Uri(PackageLogo));

                                    applicationList.Text = $"ID: {pkgAppID}\n";
                                    AppxDetails.Text =
                                        $"Full Name: \n{pkgFullName}\n\n" +
                                        $"Publisher: {pkgPublisher}\n\n" +
                                        $"Version: {pkgVersion}\n\n" +
                                    $"Origin: {pkgOrigin}\n\n" +
                                        $"Install Date: \n{test.InstalledDate}\n\n" +
                                        $"Location: \n{test.InstalledLocation.Path}\n\n";

                                });


                    try
                    {
                        if (test.Dependencies != null)
                        {
                            foreach (var dep in test.Dependencies)
                            {

                                deplist += $"{dep.Description}\n";
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    if (deplist != null)
                    {
                        AppxDetails.Text += $"Dependencies: \n{deplist}";
                    }
                }



            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }


        public string result;
        /// <summary>
        /// Determine what origin the app is
        /// </summary>
        /// <param name="pkgOrigin"></param>
        /// <returns></returns>
        private string PkgOrigin(int pkgOrigin)
        {
            try
            {

                switch (pkgOrigin)
                {
                    case 0:
                        result = "Unkown Origin";
                        break;
                    case 1:
                        result = "Unsigned";
                        break;
                    case 2:
                        result = "Inbox App";
                        break;
                    case 3:
                        result = "Store App";
                        break;
                    case 4:
                        result = "Developer Unsigned";
                        break;
                    case 5:
                        result = "Developer Signed";

                        break;
                    case 6:
                        result = "LineOfBusiness(OEM?)";
                        break;
                }
                return result;
            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
                return "Error finding value";
            }
        }
        private List<StorageFile> depList = new List<StorageFile>();
        private IReadOnlyList<StorageFile> depFiles = new List<StorageFile>();
        private StorageFile pkgFile { get; set; }

        /// <summary>
        /// Uninstall selected App
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void uninstallApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppProgbar.Visibility = Visibility.Visible;
                AppProgbar.IsIndeterminate = true;
                AppProgbar.IsEnabled = true;
                applicationList.Text = $"Please Wait, Attempting to uninstall {pkgFullName}\n";
                await portal.UninstallApplicationAsync(pkgFullName);
                applicationList.Text = "Uninstall Complete!";
                AppProgbar.Visibility = Visibility.Collapsed;
                AppProgbar.IsIndeterminate = false;
                AppProgbar.IsEnabled = false;
            }
            catch (Exception ex)
            {
                applicationList.Text = $"ERROR:\n" +
                    $"{ex.Message}\n\n" +
                    $"{ex.StackTrace}";
            }
        }

        /// <summary>
        /// Launch selected app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void launchApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                await portal.LaunchApplicationAsync(pkgAppID, pkgFullName);
            }
            catch (Exception ex)
            {
                applicationList.Text = $"ERROR: {ex.Message}\n" +
                    $"{ex.StackTrace}\n{ex.Source}";
            }
        }




        private StorageFile tempStorage;
        /// <summary>
        /// Load file to install
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void applicationLoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadApplication();




        }

        StorageFile FoundStoreLogo;
        Stream innerStream;
        private async void LoadApplication()
        {
            try
            {
                //AppxImageIcon.Source = null;


                tempStorage = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("logo.png", CreationCollisionOption.ReplaceExisting);

                FileOpenPicker picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".appx");
                picker.FileTypeFilter.Add(".appxbundle");
                pkgFile = await picker.PickSingleFileAsync();

                if (pkgFile == null)
                {
                    applicationList.Text = "No File Selected";

                }
                else
                {
                    applicationList.Text = $"Loaded {pkgFile.Name}, Reading Manifest.\n";

                    Stream fileStream = await pkgFile.OpenStreamForReadAsync();
                    var tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(Path.GetFileName(pkgFile.Path), CreationCollisionOption.ReplaceExisting);

                    switch (Path.GetExtension(pkgFile.Path).ToLower())
                    {
                        case ".appxbundle":
                            updateFileData(pkgFile, null);
                            break;
                        case ".appx":
                            updateFileData(pkgFile, null);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Exceptions.ThrownExceptionErrorExtended(ex);
            }

        }

        string FileIcon;
        StorageFile tempFile;
        public async void updateFileData(StorageFile storageFile = null, Stream BundleStream = null)
        {
            try
            {
                AppxDetails.Text = "";
                List<string> permissionsList = new List<string>();
                if (storageFile != null)
                {

                    var fileExt = Path.GetExtension(storageFile.Name);
                    Stream packageStream = null;
                    var stream = (await storageFile.OpenReadAsync()).AsStream();
                    bool isAppxBundle = false;

                    //Main Data
                    var packageName = "";
                    var packagePublisher = "";
                    var packageLogo = "";

                    switch (fileExt.ToLower())
                    {
                        case ".appxbundle":
                        case ".msixbundle":
                            ZipArchive zip = new ZipArchive(stream);
                            foreach (var item in zip.Entries)
                            {
                                if (item.Name.Contains(".appx"))
                                {
                                    if (!item.Name.Contains("scale"))
                                    {
                                        if (item.Name.Contains("ARM") || item.Name.Contains("arm"))
                                        {
                                            packageStream = item.Open();
                                            isAppxBundle = true;
                                            break;
                                        }
                                        else
                                        {
                                            packageStream = item.Open();
                                            isAppxBundle = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            break;

                        case ".appx":
                        case ".msix":
                            packageStream = stream;
                            break;
                    }
                    if (packageStream != null)
                    {
                        var package = System.IO.Packaging.Package.Open(packageStream);
                        var part = package.GetPart(new Uri("/AppxManifest.xml", UriKind.Relative));
                        var manifest = XElement.Load(part.GetStream());


                        var properties = manifest.Element(manifest.Name.Namespace + "Properties");
                        packageName = properties.Element(manifest.Name.Namespace + "DisplayName").Value;
                        packagePublisher = properties.Element(manifest.Name.Namespace + "PublisherDisplayName").Value;
                        packageLogo = properties.Element(manifest.Name.Namespace + "Logo").Value;
                        XmlReader reader = XmlReader.Create(part.GetStream());


                        while (reader.Read())
                        {
                            if (reader.Name == "Capabilities")
                            {
                                XmlReader inner = reader.ReadSubtree();
                                permissionsList = new List<string>();

                                while (inner.Read())
                                {
                                    if (reader.Name.Contains("Capability"))
                                    {
                                        permissionsList.Add(reader.GetAttribute("Name"));
                                    }

                                }

                            }
                        }

                        try
                        {

                            var packageLogoResolved = packageLogo.Replace("\\", "/");
                            var logoExt = Path.GetExtension(packageLogoResolved);

                            //We have to remove the extension and test with 'StartsWith'
                            //Logo name will contains at the end .scale-100 ..etc
                            var logoName = packageLogoResolved.Replace(logoExt, "");
                            var packageParts = package.GetParts();
                            foreach (var packagePart in packageParts)
                            {
                                if (packagePart.Uri.ToString().StartsWith($"/{logoName}"))
                                {
                                    var logoPartStream = packagePart.GetStream();
                                    var tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Path.GetFileName(packageLogo), CreationCollisionOption.GenerateUniqueName);
                                    var readStream = logoPartStream.AsInputStream().AsStreamForRead();
                                    byte[] buffer = new byte[readStream.Length];
                                    await readStream.ReadAsync(buffer, 0, buffer.Length);
                                    await FileIO.WriteBufferAsync(tempFile, CryptographicBuffer.CreateFromByteArray(buffer));
                                    FileIcon = tempFile.Path;
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                        }


                        package.Close();

                    }

                    applicationList.Text =
                           $"Name: {packageName}\n" +
                           $"Publisher: {packagePublisher}";

                    foreach (string cap in permissionsList)
                    {
                        AppxDetails.Text += $"{RetrieveCapability(cap)}\n";
                    }
                    AppxImageIcon.Source = new BitmapImage(new Uri(FileIcon));
                    permissionsList.Clear();
                    // package.Close();
                    installPkg.IsEnabled = true;
                    if (packageStream != null)
                    {
                        packageStream.Dispose();
                    }
                    if (stream != null)
                    {
                        stream.Dispose();
                    }


                }
            }
            catch (Exception ex)
            {
            }
        }





        /// <summary>
        /// Load dependencies for the App being Installed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void loadDependencies_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".appx");
            picker.FileTypeFilter.Add(".appxbundle");
            depFiles = await picker.PickMultipleFilesAsync();
            if (depFiles == null)
            {
                applicationList.Text = "No Dependencies Selected";
            }
            AppxDetails.Text += $"\nDependencies:\n";
            foreach (var file in depFiles)
            {
                AppxDetails.Text += $"{file.Name}\n";
                depList.Add(file);
            }
        }



        /// <summary>
        /// Install Selected Application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void installPkg_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                AppProgbar.Visibility = Visibility.Visible;
                AppProgbar.IsIndeterminate = true;
                AppProgbar.IsEnabled = true;
                applicationList.Text = $"Installing {pkgFile.Name}";

                portal.AppInstallStatus += Portal_AppInstallStatus;
                await portal.InstallApplicationAsync(pkgFile.Name, pkgFile, depList);




                //Debug.WriteLine(status.ToString());
                // applicationList.Text = status.ToString();
                AppProgbar.Visibility = Visibility.Collapsed;
                AppProgbar.IsIndeterminate = false;
                AppProgbar.IsEnabled = false;
            }
            catch (Exception ex)
            {
                AppxDetails.Text = $"ERROR:\n" +
                    $"{ex.Message}\n\n" +
                    $"{ex.StackTrace}\n\n" +
                    $"{ex.Source}";
            }
        }

        private async void Portal_AppInstallStatus(DevicePortal sender, ApplicationInstallStatusEventArgs args)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () =>
                                {
                                    ApplicationInstallStatus status = args.Status;
                                    var message = args.Message;
                                    var phase = args.Phase;
                                    applicationList.Text = message;
                                    if (status == ApplicationInstallStatus.Failed)
                                    {
                                        AppxDetails.Text = message;
                                    }
                                });
        }

        public void AppInstallStatusEvent()
        {

        }

        #endregion



        #region FetchProcessInfo

        public class ProcessInformation
        {
            public string ProcessName { get; set; }
            public uint PID { get; set; }
            public uint SessionID { get; set; }
            public string UserName { get; set; }
            public string PackageName { get; set; }
            public ulong WorkingSet { get; set; }
            public ulong PageFile { get; set; }
            public ulong TotalCommits { get; set; }
            public bool IsXAP { get; set; }
            public string FullProcDetails { get; set; }
        }

        List<ProcessInformation> ProcessInfoList;
        Visibility ConnectionNoticeVisible = Visibility.Visible;
        private async void FetchProcessInfo()
        {
            int runningAmount = 0;
            ProcessInfoList = new List<ProcessInformation>();
            RunningProcesses proclists = await portal.GetRunningProcessesAsync();
            List<DeviceProcessInfo> processes = proclists.Processes;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () =>
                                {
                                    foreach (var processItem in processes.OrderBy(item => item.Name))
                                    {
                                        ProcessInfoList.Add(new ProcessInformation
                                        {
                                            ProcessName = processItem.Name,
                                            PID = processItem.ProcessId,
                                            SessionID = processItem.SessionId,
                                            UserName = processItem.UserName,
                                            PackageName = processItem.PackageFullName,
                                            WorkingSet = processItem.WorkingSet,
                                            PageFile = processItem.PageFile,
                                            TotalCommits = processItem.TotalCommit,
                                            IsXAP = processItem.IsXAP,
                                            FullProcDetails =
                                            $"Package: {processItem.PackageFullName}\n" +
                                            $"Publisher: {processItem.Publisher}\n" +
                                            $"PID: {processItem.ProcessId}\n" +
                                            $"Session ID: {processItem.SessionId}\n" +
                                            $"User: {processItem.UserName}\n" +
                                            $"Working Set: {((long)processItem.WorkingSet).ToFileSize()}\n" +
                                            $"Total Commits: {((long)processItem.TotalCommit).ToFileSize()}\n" +
                                            $"Page File: {((long)processItem.PageFile).ToFileSize()}\n" +
                                            $"Is XAP: {processItem.IsXAP}\n" +
                                            $"Is Running: {processItem.IsRunning}\n" +
                                            $"CPU Usage: {processItem.CpuUsage}%\n\n"
                                        });

                                        if (processItem.IsRunning)
                                        {
                                            runningAmount++;
                                        }


                                    }

                                    //Set Items Source
                                    ProcessesHeader.Text = $"Total: {ProcessInfoList.Count} | Active: {runningAmount}";
                                    processesListView.ItemsSource = ProcessInfoList;
                                });
        }

        private void ProcRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessInfoList.Count > 0)
            {
                ProcessInfoList.Clear();
                FetchProcessInfo();
            }

        }








        #endregion




        private string PerformanceResults;
        private string engines { get; set; }
        private void FetchPerfInfo()
        {
            Task<SystemPerformanceInformation> perf = portal.GetSystemPerfAsync();
            perfResult = perf.Result;


            //Paging/Memory
            uint pagedPoolPages = perfResult.PagedPoolPages;
            uint nonpagedPoolPages = perfResult.NonPagedPoolPages;
            uint pageSize = perfResult.PageSize;
            ulong pagesAvailable = perfResult.AvailablePages;
            uint pagesCommited = perfResult.CommittedPages;
            uint commitLimit = perfResult.CommitLimit;
            uint pagesTotal = perfResult.TotalPages;
            ulong totalInstalledPages = perfResult.TotalInstalledKb;

            PerformanceResults +=
                $"Paging/Memory:\n" +
                $"Page Size: {pageSize}\n" +
                $"Pages Available: {pagesAvailable}\n" +
                $"Pages Commited: {pagesCommited}\n" +
                $"Paged Pool: {pagedPoolPages}\n" +
                $"Non-Paged Pool: {nonpagedPoolPages}\n" +
                $"Pages Total: {pagesTotal}\n" +
                $"Commit Limit: {commitLimit}\n" +
                $"Total Installed Pages: {totalInstalledPages}\n\n";


        }




        #region ETWPage

        int etwnumber = -1;
        string selectedetw;
        private void CrashAppList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            etwToggle.IsEnabled = true;
            selectedetw = (sender as ComboBox).SelectedItem.ToString();

        }


        private async void etwToggle_Toggled(object sender, RoutedEventArgs e)
        {

            if (etwToggle.IsOn == true)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   ETWLogOutput.Text = "Started listening for Events";
                               });
                try
                {

                    await portal.StartListeningForEtwEventsAsync();
                    StartETW();
                }
                catch (Exception ex)
                {
                    ETWLogOutput.Text = $"{ex.Message}\n\n{ex.StackTrace}";
                    StopETW();
                    etwToggle.IsOn = false;
                }

            }
            else
            {
                StopETW();
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   ETWLogOutput.Text = "Stopped Listening for Events";
                               });
            }

        }


        private async void StartETW()
        {
            etwTimer = 0;
            etwTimerBox.Text = string.Empty;
            IsETWStarted = true;
            if (etwnumber == -1)
            {
                etwnumber = 5;
            }
            await Task.Run(async () =>
            {


                foreach (var provider in ETWResult.Providers)
                {
                    if (provider.Name == selectedetw)
                    {

                        Task providerState = portal.ToggleEtwProviderAsync(provider, true, etwnumber);

                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                 () =>
                                 {
                                     ETWLogOutput.Text = $"Started tracing {selectedetw}\n";
                                 });
                    }
                }


            });
        }



        private async void StopETW()
        {
            etwTimer = 0;
            //etwToggle.OnContent = "On";
            IsETWStarted = false;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   SaveETWLog.IsEnabled = true;
                                   ViewETWLog.IsEnabled = true;
                                   ClearLog.IsEnabled = true;
                               });
            await Task.Run(() =>
           {
               foreach (var provider in ETWResult.Providers)
               {
                   if (provider.Name == selectedetw)
                   {
                       Task providerState = portal.ToggleEtwProviderAsync(provider, false, etwnumber);
                       //await portal.StopListeningForEtwEventsAsync();
                   }
               }


           });
        }

        private void etwLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selected = (sender as ComboBox).SelectedIndex;
            switch (selected)
            {
                case 0:
                    etwnumber = 1;
                    return;
                case 1:
                    etwnumber = 2;
                    return;
                case 2:
                    etwnumber = 3;
                    return;
                case 3:
                    etwnumber = 4;
                    return;
                case 4:
                    etwnumber = 5;
                    return;



            }

        }


        private void ViewETWLog_Click(object sender, RoutedEventArgs e)
        {
            if (ETW_LOGGER == null)
            {
                ETWLogOutput.Text = "No Events Logged.";
            }
            else
            {
                ETWLogOutput.Text = string.Join("\n", ETW_LOGGER);
            }
        }

        private async void SaveETWLog_Click(object sender, RoutedEventArgs e)
        {

            FolderPicker picker = new FolderPicker();
            picker.FileTypeFilter.Add(".txt");
            StorageFolder storage = await picker.PickSingleFolderAsync();
            if (storage == null)
            {
                return;
            }
            StorageFile SavedETWLog = await storage.CreateFileAsync($"{selectedetw}-{DateTime.Now.ToString("yyyyMMdd_hhmmss")}");
            await FileIO.WriteTextAsync(SavedETWLog, string.Join("\n", ETW_LOGGER));

        }

        private async void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            SaveETWLog.IsEnabled = false;
            ViewETWLog.IsEnabled = false;
            ClearLog.IsEnabled = true;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   //Array.Clear(ETWLogger, 0, ETWLogger.Length);
                                   ETW_LOGGER.Clear();
                                   ETWLogOutput.Text = string.Empty;
                               });
        }

        private void ETWLogOutput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (etwToggle.IsOn == true)
            {
                if (IsETWStarted == true)
                {
                    etwTimer++;
                    etwTimerBox.Text = etwTimer.ToString();
                }
            }

        }
        #endregion








        #region RealtimeData
        // OLD PAGING GRAPH XAML - NOT USED
        //  <Border x:Name="PagingBorder" RelativePanel.Below="NetData" RelativePanel.AlignLeftWithPanel="True" RelativePanel.AlignRightWithPanel="True" Height="3" Background="#FF1C1C1C" BorderBrush="#FF1C1C1C"/>
        //                <TextBlock x:Name="PagingHeader" Text="Paging:" RelativePanel.Below="PagingBorder"    RelativePanel.AlignLeftWithPanel="True" RelativePanel.AlignRightWithPanel="True" Margin="5,8,5,5"/>
        //                <Border x:Name="PagingGraphBorder" RelativePanel.Below="PagingHeader" RelativePanel.AlignLeftWithPanel="True" RelativePanel.AlignRightWithPanel="True" Height="3" Background="#FF1C1C1C" BorderBrush="#FF1C1C1C"/>
        //                <qc:SerialChart x:Name="PagingData" RelativePanel.AlignLeftWithPanel="True" RelativePanel.AlignRightWithPanel="True" RelativePanel.Below="PagingGraphBorder" DataSource="{Binding RealTimeData}" Height="200" CategoryValueMemberPath="PagedPages" PlotAreaBackground="#FF1E1E1E" AxisForeground="White" Background="#FF1E1E1E" GridStroke="{x:Null}">
        //                    <qc:SerialChart.Graphs >
        //                        <qc:LineGraph Title = "Paged Pool" ValueMemberPath="PagedPages"/>
        //                        <qc:LineGraph Title = "Non-Paged Pool" ValueMemberPath="NonPagedPages"/>
        //
        //
        //
        //                    </qc:SerialChart.Graphs>
        //                </qc:SerialChart>



        public ObservableCollection<RealTimeDataItem> RealTimeData { get { return _realTimeData; } }
        private ObservableCollection<RealTimeDataItem> _realTimeData = new ObservableCollection<RealTimeDataItem>();
        long gpuSharedTotal;
        long gpuSharedUsed;
        long gpuSelfTotal;
        long gpuSelfUsed;
        string gpuname;
        /// <summary>
        /// 
        /// </summary>
        private async void FillInitialRealTimeData()
        {
            SystemPerformanceInformation perf = await portal.GetSystemPerfAsync();

            var startDate = DateTime.Now.AddSeconds(-15);
            var cpuload = perf.CpuLoad;
            var gpu = perf.GpuData.Adapters;
            var network = perf.NetworkData;
            double gpuresult = 0;
            uint pagedPoolPages = perf.PagedPoolPages;
            uint nonpagedPoolPages = perf.NonPagedPoolPages;
            uint pageSize = perf.PageSize;
            ulong pagesAvailable = perf.AvailablePages;
            uint pagesCommited = perf.CommittedPages;
            uint commitLimit = perf.CommitLimit;
            uint pagesTotal = perf.TotalPages;
            ulong totalInstalledPages = perf.TotalInstalledKb;
            long KB = 1024;
            long MB = 1024 * 1024;
            long GB = MB * 1024;
            double convertedFileSize;

            foreach (var adapter in gpu)
            {

                var maingpu = adapter.EnginesUtilization;
                foreach (var engine in maingpu)
                {
                    gpuname = adapter.Description;
                    gpuresult += engine;
                }
            }
            foreach (var gpumem in gpu)
            {
                convertedFileSize = (long)gpumem.SystemMemory / MB;
                gpuSharedTotal = (long)convertedFileSize;
                convertedFileSize = (long)gpumem.SystemMemoryUsed / MB;
                gpuSharedUsed = (long)convertedFileSize;
                convertedFileSize = (long)gpumem.DedicatedMemory / MB;
                gpuSelfTotal = (long)convertedFileSize;
                convertedFileSize = (long)gpumem.DedicatedMemoryUsed / MB;
                gpuSelfUsed = (long)convertedFileSize;
            }
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                              () =>
                              {
                                 
                                  GPUheader.Text += $" {gpuname}";
                              });

            double ioreaddata = (long)perf.IoReadSpeed / MB;
            double iowritedata = (long)perf.IoWriteSpeed / MB;
            double iootherdata = (long)perf.IoOtherSpeed / MB;
            double netread = ((long)network.BytesIn) / KB;
            double netwrite = ((long)network.BytesOut) / KB;
            double totalpages = ((long)pagesTotal);
            double pagescommited = ((long)pagesCommited);
            double pagesavailable = ((long)pagesAvailable);
            double pagesize = ((long)pageSize) / MB;
            double nonpaged = ((long)nonpagedPoolPages);
            double pagedpages = (long)pagedPoolPages;
            var rnd = new Random();

            for (var date = startDate; date < DateTime.Now; date = date.AddSeconds(1))
            {
                RealTimeData.Add(new RealTimeDataItem()
                {
                    Seconds = date.Second,
                    CPULoad = (int)cpuload,
                    GPULoad = (int)gpuresult,
                    GPUMemSharedTotal = gpuSharedTotal,
                    GPUMemShared = gpuSharedUsed,
                    GPUMemSelf = gpuSelfUsed,
                    GPUMemSelfTotal = gpuSelfTotal,
                    GPUUsedTotal = gpuSelfUsed + gpuSharedUsed,
                    IORead = ioreaddata,
                    IOWrite = iowritedata,
                    IOOther = iootherdata,
                    NetRead = netread,
                    NetWrite = netwrite,
                    PagedPages = pagedpages,
                    NonPagedPages = nonpaged,
                    PagesAvailable = pagesavailable,
                    PagesCommited = pagescommited,
                    PageSize = pageSize,
                    TotalPages = totalpages
                });

                cpuload = cpuload < 0 ? 0 : cpuload;
                gpuresult = gpuresult < 0 ? 0 : gpuresult;
                gpuSharedUsed = gpuSharedUsed < 0 ? 0 : gpuSharedUsed;
                gpuSelfUsed = gpuSelfUsed < 0 ? 0 : gpuSelfUsed;
                ioreaddata = ioreaddata < 0 ? 0 : ioreaddata;
                iowritedata = iowritedata < 0 ? 0 : iowritedata;
                iootherdata = iootherdata < 0 ? 0 : iootherdata;
                netread = netread < 0 ? 0 : netread;
                netwrite = netwrite < 0 ? 0 : netwrite;
                totalpages = totalpages < 0 ? 0 : totalpages;
                pagescommited = pagescommited < 0 ? 0 : pagescommited;
                pagesavailable = pagesavailable < 0 ? 0 : pagesavailable;
                pagesize = pagesize < 0 ? 0 : pagesize;
                nonpaged = nonpaged < 0 ? 0 : nonpaged;
                pagedpages = pagedpages < 0 ? 0 : pagedpages;
            }
        }

        public static string CPULoadStat { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateRealTimeData(object sender, object e)
        {
            CPULoadStat = "CPU Load:";
            if (isConnected == true)
            {


                try
                {
                    SystemPerformanceInformation perf = await portal.GetSystemPerfAsync();
                    if (RealTimeData.Count != -1)
                    {
                        RealTimeData.RemoveAt(0);
                    }
                
                var gpu = perf.GpuData.Adapters;
                var network = perf.NetworkData;
                uint pagedPoolPages = perf.PagedPoolPages;
                uint nonpagedPoolPages = perf.NonPagedPoolPages;
                uint pageSize = perf.PageSize;
                ulong pagesAvailable = perf.AvailablePages;
                uint pagesCommited = perf.CommittedPages;
                uint commitLimit = perf.CommitLimit;
                uint pagesTotal = perf.TotalPages;
                ulong totalInstalledPages = perf.TotalInstalledKb;

                double gpuresult = 0;
                long KB = 1024;
                long MB = 1024 * 1024;
                long GB = MB * 1024;
                double convertedFileSize;

                foreach (var adapter in gpu)
                {
                    var maingpu = adapter.EnginesUtilization;
                    foreach (var engine in maingpu)
                    {
                        gpuresult += engine;
                    }
                }
                foreach (var gpumem in gpu)
                {
                    convertedFileSize = (long)gpumem.SystemMemory / MB;
                    gpuSharedTotal = (long)convertedFileSize;
                    convertedFileSize = (long)gpumem.SystemMemoryUsed / MB;
                    gpuSharedUsed = (long)convertedFileSize;
                    convertedFileSize = (long)gpumem.DedicatedMemory / MB;
                    gpuSelfTotal = (long)convertedFileSize;
                    convertedFileSize = (long)gpumem.DedicatedMemoryUsed / MB;
                    gpuSelfUsed = (long)convertedFileSize;
                }
                double ioreaddata = (long)perf.IoReadSpeed / MB;
                double iowritedata = (long)perf.IoWriteSpeed / MB;
                double iootherdata = (long)perf.IoOtherSpeed / MB;
                double netread = ((long)network.BytesIn) / KB;
                double netwrite = ((long)network.BytesOut) / KB;
                double totalpages = ((long)pagesTotal);
                double pagescommited = ((long)pagesCommited);
                double pagesavailable = ((long)pagesAvailable);
                double pagesize = ((long)pageSize) / MB;
                double nonpaged = ((long)nonpagedPoolPages);
                double pagedpages = (long)pagedPoolPages;
                var rnd = new Random();
                var cpuload = perf.CpuLoad;
                cpuload = cpuload < 0 ? 0 : cpuload;
                gpuresult = gpuresult < 0 ? 0 : gpuresult;
                gpuSharedUsed = gpuSharedUsed < 0 ? 0 : gpuSharedUsed;
                gpuSelfUsed = gpuSelfUsed < 0 ? 0 : gpuSelfUsed;
                ioreaddata = ioreaddata < 0 ? 0 : ioreaddata;
                iowritedata = iowritedata < 0 ? 0 : iowritedata;
                iootherdata = iootherdata < 0 ? 0 : iootherdata;
                netread = netread < 0 ? 0 : netread;
                netwrite = netwrite < 0 ? 0 : netwrite;
                totalpages = totalpages < 0 ? 0 : totalpages;
                pagescommited = pagescommited < 0 ? 0 : pagescommited;
                pagesavailable = pagesavailable < 0 ? 0 : pagesavailable;
                pagesize = pagesize < 0 ? 0 : pagesize;
                nonpaged = nonpaged < 0 ? 0 : nonpaged;
                pagedpages = pagedpages < 0 ? 0 : pagedpages;

               

                RealTimeData.Add(new RealTimeDataItem()
                {
                    Seconds = DateTime.Now.Second,
                    CPULoad = (int)cpuload,
                    GPULoad = (int)gpuresult,
                    GPUMemSharedTotal = gpuSharedTotal,
                    GPUMemShared = gpuSharedUsed,
                    GPUMemSelf = gpuSelfUsed,
                    GPUMemSelfTotal = gpuSelfTotal,
                    GPUUsedTotal = gpuSelfUsed + gpuSharedUsed,
                    IORead = ioreaddata,
                    IOWrite = iowritedata,
                    IOOther = iootherdata,
                    NetRead = netread,
                    NetWrite = netwrite,
                    PagedPages = pagedpages,
                    NonPagedPages = nonpaged,
                    PagesAvailable = pagesavailable,
                    PagesCommited = pagescommited,
                    PageSize = pageSize,
                    TotalPages = totalpages

                });
                CPULoadStat = $"CPU Load: {cpuload}%";
                }
                catch (Exception ex)
                {
                    //Exceptions.ThrownExceptionErrorExtended(ex);
                    
                }
                /// GPUUsageText.Text = $"Shared: {GPUMemShared.ToFileSize()}, Dedicated: {gpuSelfUsed}";
            }
        }


        public class RealTimeDataItem
        {
            public int Seconds { get; set; }
            public int CPULoad { get; set; }
            public int GPULoad { get; set; }
            public double GPUMemShared { get; set; }
            public double GPUMemSelf { get; set; }
            public double GPUMemSharedTotal { get; set; }
            public double GPUMemSelfTotal { get; set; }
            public double GPUUsedTotal { get; set; }
            public double IORead { get; set; }
            public double IOWrite { get; set; }
            public double IOOther { get; set; }
            public double NetRead { get; set; }
            public double NetWrite { get; set; }
            public double PagesAvailable { get; set; }
            public double PagesCommited { get; set; }
            public double PageSize { get; set; }
            public double PagedPages { get; set; }
            public double NonPagedPages { get; set; }
            public double TotalPages { get; set; }
        }

        #endregion



        #region HWInfoPage

        public class HardwareInformation
        {
            public string HWDeviceName { get; set; }
            public string Manufacturer { get; set; }
            public string ID { get; set; }
            public string ParentID { get; set; }
            public string Description { get; set; }
            public string Class { get; set; }
            public int StatusCode { get; set; }
            public int ErrorCode { get; set; }
            public string FullDetails { get; set; }
        }


        string newName;
        string newDescription;
        public string ProcessorName;
        private Task<List<Device>> hardwareResult { get; set; }
        private async void FetchHWInfo()
        {
            List<HardwareInformation> DeviceInfo = new List<HardwareInformation>();
            List<string> ClassList = new List<string>();


            hardwareResult = portal.GetDeviceListAsync();

            foreach (Device device in hardwareResult.Result)
            {
                if (device.Class == "Processor")
                {
                    ProcessorName = device.FriendlyName;
                }

                ClassList.Add(device.Class);


                if (device.FriendlyName == "" | device.Description == "")
                {
                    if (device.FriendlyName == "")
                    {
                        newName = "Unknown";
                    }
                    if (device.Description == "")
                    {
                        newDescription = "Unknown";
                    }

                    DeviceInfo.Add(new HardwareInformation
                    {
                        HWDeviceName = device.FriendlyName,
                        Manufacturer = device.Manufacturer,
                        ID = device.ID,
                        ParentID = device.ParentID,
                        Description = newDescription,
                        Class = device.Class,
                        StatusCode = device.StatusCode,
                        ErrorCode = device.ProblemCode,
                        FullDetails =
                          $"Properties:\n" +
                          $"Friendly Name: {newName}\n\n" +
                          $"ID: {device.ID}\n\n" +
                          $"ParentID: {device.ParentID}\n\n" +
                          $"Class: {device.Class}\n\n" +
                          $"Manufacturer: {device.Manufacturer}\n\n\n"
                    });

                }
                else
                {
                    DeviceInfo.Add(new HardwareInformation
                    {
                        HWDeviceName = device.FriendlyName,
                        Manufacturer = device.Manufacturer,
                        ID = device.ID,
                        ParentID = device.ParentID,
                        Description = device.Description,
                        Class = device.Class,
                        StatusCode = device.StatusCode,
                        ErrorCode = device.ProblemCode,
                        FullDetails =
                        $"Properties:\n" +
                        $"Friendly Name: {device.FriendlyName}\n\n" +
                        $"ID: {device.ID}\n\n" +
                        $"ParentID: {device.ParentID}\n\n" +
                        $"Class: {device.Class}\n\n" +
                        $"Manufacturer: {device.Manufacturer}\n\n\n"
                    });
                }


            }


            //DeviceInfo.Sort();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                 () =>
                                 {
                                     var groups = from c in DeviceInfo
                                                  group c by c.Class;
                                     this.cvs.Source = groups;



                                     //  DevicesListView.ItemsSource = DeviceInfo;
                                 });

        }


        private void DriverComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected = (sender as ComboBox).SelectedItem.ToString();
            FetchHWInfoResults(selected);
        }
        string name;
        string ID;
        string parentID;
        string hwclass;
        string description;
        string manufacture;
        int statusCode;
        int ErrorCode;
        private void FetchHWInfoResults(string selected)
        {


            /*  sb2.Clear();

              foreach (var device in hardwareResult.Result)
              {
                  if (device.ID == selected)
                  {

                      if (device.FriendlyName == string.Empty)
                      {
                          name = "N/A";
                      }
                      else
                      {
                          name = device.FriendlyName;
                      }
                      ID = device.ID;
                      parentID = device.ParentID;
                      if (device.Class == string.Empty)
                      {
                          hwclass = "N/A";
                      }
                      else
                      {
                          hwclass = device.Class;
                      }
                      if (device.Description == string.Empty)
                      {
                          description = "N/A";

                      }
                      else
                      {
                          description = device.Description;
                      }
                      if (device.Manufacturer == string.Empty)
                      {
                          manufacture = "Unknown";
                      }
                      else
                      {
                          manufacture = device.Manufacturer;
                      }
                      statusCode = device.StatusCode;
                      ErrorCode = device.ProblemCode;
                      sb2.Append(DevicesText.Text);
                      sb2.AppendLine("");
                      sb2.Append("Name: ");
                      sb2.AppendLine(name);
                      sb2.Append("Description: ");
                      sb2.AppendLine(description);
                      sb2.Append("Class: ");
                      sb2.AppendLine(hwclass);
                      sb2.Append("ID: ");
                      sb2.AppendLine(ID);
                      sb2.Append("Parent ID: ");
                      sb2.AppendLine(parentID);
                      DevicesText.Text = sb2.ToString();
                  } 


              } */
        }

        #endregion




        #region Updater
        /// <summary>
        /// MUST CHANGE THESE BEFORE EACH PUBLIC GITHUB RELEASE
        /// 
        /// MUST CHANGE THESE BEFORE EACH PUBLIC GITHUB RELEASE
        /// 
        /// MUST CHANGE THESE BEFORE EACH PUBLIC GITHUB RELEASE
        /// </summary>
        public static string CurrentBuildVersion = "1.0.18.0";
        public static string PreviousBuildVersion = "1.0.17.0";
        public static string NextBuildVersion = "1.0.19.0";
        public static string UploadedFileName = "WPDevPortal_1.0.18.0_Test.zip";
        public static string AppxUpdateName = "WPDevPortal_1.0.18.0_x86_x64_arm.appxbundle";

        public StorageFolder folder { get; set; }
        public StorageFile file { get; set; }
        public bool isLatestBuild { get; set; }
        public static string UpdateURL { get; set; }
        DownloadOperation downloadOperation;
        CancellationTokenSource cancellationToken;

        BackgroundDownloader backgroundDownloader = new BackgroundDownloader();

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isNetworkConnected = NetworkInterface.GetIsNetworkAvailable();
            if (isNetworkConnected == true)
            {
                UpdateBtn.Visibility = Visibility.Collapsed;
                CheckForUpdate();

            }
            else
            {
                UpdateBtn.Visibility = Visibility.Collapsed;
                UpdateDetailsBox.Text = "Connect to the Internet to check for updates";
                return;
            }

        }
        private async void CheckForUpdate()
        {
            await Windows.Storage.ApplicationData.Current.ClearAsync(ApplicationDataLocality.LocalCache);
            GitHubClient client = new GitHubClient(new ProductHeaderValue("WP_Device_Portal"));
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("Empyreal96", "WP_Device_Portal");
            var latestRelease = releases[0];

            if (latestRelease.Assets != null && latestRelease.Assets.Count > 0)
            {
                if (latestRelease.TagName == CurrentBuildVersion)
                {
                    isLatestBuild = true;
                    UpdateBtn.Visibility = Visibility.Collapsed;

                }
                //Test Function
                if (latestRelease.TagName == PreviousBuildVersion)
                {
                    UpdateDetailsBox.Text = "You are on an unreleased build";
                    DLButton.Visibility = Visibility.Collapsed;
                    UpdateBtn.Visibility = Visibility.Collapsed;
                }

                else
                {
                    var updateURL = latestRelease.Assets[0].BrowserDownloadUrl;
                    UpdateURL = $"https://github.com/Empyreal96/WP_Device_Portal/releases/download/{latestRelease.TagName}/{UploadedFileName}";

                    UpdateDetailsBox.Visibility = Visibility.Visible;


                    UpdateDetailsBox.Text = $"Latest Build: {latestRelease.TagName}\n";
                    UpdateDetailsBox.Text += $"Current Build: {CurrentBuildVersion}\n";
                    UpdateDetailsBox.Text += $"Date Update Published: {latestRelease.PublishedAt}\n\n";


                    if (latestRelease.TagName == CurrentBuildVersion || latestRelease.TagName == PreviousBuildVersion)
                    {
                        UpdateDetailsBox.Text = "No Updated Found";
                        DLButton.Visibility = Visibility.Collapsed;
                        UpdateBtn.Visibility = Visibility.Collapsed;
                    }
                    if (latestRelease.TagName == NextBuildVersion)
                    {

                        UpdateDetailsBox.Text += $"Download URL: {UpdateURL}\n";
                        DLButton.Visibility = Visibility.Visible;
                        UpdateBtn.Visibility = Visibility.Collapsed;
                    }

                }
            }
        }

        private async void DLButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateDetailsBox.Text = "Checking for updates";
                ProgressBarDownload.Visibility = Visibility.Visible;
                DLButton.Visibility = Visibility.Collapsed;
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
                folderPicker.ViewMode = PickerViewMode.Thumbnail;
                folderPicker.FileTypeFilter.Add("*");
                folder = await folderPicker.PickSingleFolderAsync();
                if (folder == null)
                {
                    return;
                }
                file = await folder.CreateFileAsync($"{UploadedFileName}", CreationCollisionOption.ReplaceExisting);

                downloadOperation = backgroundDownloader.CreateDownload(new Uri(UpdateURL), file);

                Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progressChanged);
                cancellationToken = new CancellationTokenSource();
                await downloadOperation.StartAsync().AsTask(cancellationToken.Token, progress);
                ProgressBarDownload.Visibility = Visibility.Collapsed;
                InstallUpdate();

            }
            catch (Exception ex)
            {

            }
        }
        private async void InstallUpdate()
        {
            DLButton.Visibility = Visibility.Collapsed;
            UpdateDetailsBox.Text = "Extracting Update to App Cache";
            try
            {

                await ArchiverPlus.Decompress(file, Windows.Storage.ApplicationData.Current.LocalCacheFolder);

                var options = new Windows.System.LauncherOptions();
                options.PreferredApplicationPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";
                options.PreferredApplicationDisplayName = "App Installer";
                UpdateDetailsBox.Text = "Attempting to Install Update Package, Please Wait";
                await Windows.System.Launcher.LaunchFileAsync(await Windows.Storage.ApplicationData.Current.LocalCacheFolder.GetFileAsync(AppxUpdateName), options);


            }
            catch (Exception ex)
            {
                UpdateDetailsBox.Text = $"An error occured while trying to install: \n{ex.Message}";
            }
        }

        /// <summary>
        /// Progress for Download
        /// </summary>
        /// <param name="downloadOperation"></param>
        private void progressChanged(DownloadOperation downloadOperation)
        {
            int progress = (int)(100 * ((double)downloadOperation.Progress.BytesReceived / (double)downloadOperation.Progress.TotalBytesToReceive));
            //TextBlockProgress.Text = String.Format("{0} of {1} kb. downloaded - {2}% complete.", downloadOperation.Progress.BytesReceived / 1024, downloadOperation.Progress.TotalBytesToReceive / 1024, progress);
            ProgressBarDownload.Value = progress;
            switch (downloadOperation.Progress.Status)
            {
                case BackgroundTransferStatus.Running:
                    {
                        UpdateDetailsBox.Text = $"Downloading from {UpdateURL}";
                        //ButtonPauseResume.Content = "Pause";
                        break;
                    }
                case BackgroundTransferStatus.PausedByApplication:
                    {
                        UpdateDetailsBox.Text = "Download paused.";
                        //ButtonPauseResume.Content = "Resume";
                        break;
                    }
                case BackgroundTransferStatus.PausedCostedNetwork:
                    {
                        UpdateDetailsBox.Text = "Download paused because of metered connection.";
                        //ButtonPauseResume.Content = "Resume";
                        break;
                    }
                case BackgroundTransferStatus.PausedNoNetwork:
                    {
                        UpdateDetailsBox.Text = "No network detected. Please check your internet connection.";
                        break;
                    }
                case BackgroundTransferStatus.Error:
                    {
                        UpdateDetailsBox.Text = "An error occured while downloading.";
                        break;
                    }
            }
            if (progress >= 100)
            {
                UpdateDetailsBox.Text = $"Download complete. Update downloaded to {folder.Path}\\{UploadedFileName}";
                // ButtonCancel.IsEnabled = false;
                //ButtonPauseResume.IsEnabled = false;
                //ButtonDownload.IsEnabled = true;
                downloadOperation = null;
            }

        }




        #endregion


        #region File Manager
        public class FolderInfo
        {
            public string ListViewFileName { get; set; }
            public string ListViewFileSizeType { get; set; }
            public BitmapImage ListViewFileIcon { get; set; }
        }
        FolderContents folderContents { get; set; }
        public async void FetchKnownFolders()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () =>
                                {
                                    KnownFoldersCombo.Items.Clear();
                                });
            var knownFolders = portal.GetKnownFoldersAsync().Result;
            List<string> folderlist = knownFolders.Folders;

            foreach (var folder in folderlist)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                 () =>
                                 {

                                     KnownFoldersCombo.Items.Add(folder);

                                 });
            }
        }


        public static bool UploadComplete;
        private async void UploadFilebtn_Click(object sender, RoutedEventArgs e)
        {
            UploadComplete = false;
            ProgRingFiles.IsEnabled = true;
            ProgRingFiles.IsActive = true;
            ProgBorder.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;
            FileListView.IsHitTestVisible = false;
            FileListView.IsEnabled = false;
            ProgressText.Text = "Processing..";
            FileRPanel.IsHitTestVisible = false;
            try
            {
                FileOpenPicker uploadfiles = new FileOpenPicker();
                uploadfiles.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                uploadfiles.FileTypeFilter.Add("*");
                var selectedFiles = await uploadfiles.PickSingleFileAsync();
                if (selectedFiles == null)
                {
                    ProgRingFiles.IsEnabled = false;
                    ProgRingFiles.IsActive = false;
                    ProgBorder.Visibility = Visibility.Collapsed;
                    ProgressText.Visibility = Visibility.Collapsed;
                    FileRPanel.IsHitTestVisible = true;
                    FileListView.IsHitTestVisible = true;
                    FileListView.IsEnabled = true;
                }
                else
                {
                    ProgressText.Text = $"Uploading {selectedFiles.Name}..";
                    Task TransferTask = new Task(async () =>
                    {
                        try
                        {
                            Debug.WriteLine($"Copying {selectedFiles.Path} to {ApplicationData.Current.LocalCacheFolder.Path}");
                            var tempfile = await selectedFiles.CopyAsync(ApplicationData.Current.LocalCacheFolder, selectedFiles.Name, NameCollisionOption.GenerateUniqueName);
                            Debug.WriteLine($"TRANSFER: {selectedKnownFolder}, {tempfile.Path}, {tempfile.Name}");

                            await portal.UploadFileAsync(selectedKnownFolder, tempfile.Path);
                            Debug.WriteLine("Task reported completed");
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                     async () =>
                                     {
                                         ExceptionHelper.Exceptions.WDPUploadSuccess(selectedKnownFolder, portal.Address);
                                         ProgRingFiles.IsEnabled = false;
                                         ProgRingFiles.IsActive = false;
                                         ProgressText.Visibility = Visibility.Collapsed;
                                         ProgBorder.Visibility = Visibility.Collapsed;
                                         FileListView.IsHitTestVisible = true;
                                         FileListView.IsEnabled = true;
                                         await tempfile.DeleteAsync();
                                         selectedFiles = null;
                                         uploadfiles = null;
                                         tempfile = null;
                                     });
                        }
                        catch (Exception ex)
                        {
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                     async () =>
                                     {
                                         Exceptions.ThrownExceptionErrorExtended(ex);
                                     });
                        }
                    });
                    Debug.WriteLine("Starting File Upload Task");
                    TransferTask.Start();


                    FileRPanel.IsHitTestVisible = true;
                    UploadComplete = false;

                }

            }
            catch (Exception ex)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                     async () =>
                                     {
                                         Exceptions.ThrownExceptionErrorExtended(ex);
                                         Debug.WriteLine(ex.Message);
                                         ProgRingFiles.IsEnabled = false;
                                         ProgRingFiles.IsActive = false;
                                         FileRPanel.IsHitTestVisible = true;
                                         ProgressText.Visibility = Visibility.Collapsed;
                                         ProgBorder.Visibility = Visibility.Collapsed;
                                         FileListView.IsHitTestVisible = true;
                                         FileListView.IsEnabled = true;
                                         UploadComplete = false;
                                     });
            }

        }



        List<string> path = new List<string>();
        string paths { get; set; }
        bool WasFolderSelected = false;
        Stream testfile = null;
        FolderContents testfolder;
        string itemType;
        string saveFileName;
        Task<FolderContents> WDPFolder;

        private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ProgRingFiles.IsEnabled = true;
            ProgRingFiles.IsActive = true;
            ProgressText.Visibility = Visibility.Visible;
            ProgBorder.Visibility = Visibility.Visible;
            FileListView.IsHitTestVisible = false;
            FileListView.IsEnabled = false;
            ProgressText.Text = "Processing..";
            FileRPanel.IsHitTestVisible = false;
            if (itemId != null)
            {
                if (WasFolderSelected == true)
                {

                }
                else
                {

                    Debug.WriteLine("Fixing path");
                    string oldpaths = paths;
                    paths = oldpaths.Replace($"\\{itemId}", "");

                }
            }
            if (testfile != null)
            {
                await testfile.FlushAsync();
                testfile = null;
            }



            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                 () =>
                                 {
                                     var value = FileListView.SelectedIndex;
                                     Debug.WriteLine((FileListView.SelectedItem as FolderInfo).ListViewFileSizeType);
                                     itemType = (FileListView.SelectedItem as FolderInfo).ListViewFileSizeType;
                                     itemId = (e.ClickedItem as FolderInfo).ListViewFileName;
                                     path.Add(itemId);
                                     FilesHeader.Text += $"\\{itemId}";
                                     paths += $"\\{itemId}";
                                     Debug.WriteLine($"{paths}, {itemId}");
                                 });
            // await Task.Run(async () =>
            //{
            Debug.WriteLine("Testing if folder or file\n");
            if (selectedKnownFolder == "LocalAppData")
            {

                if (itemType.Contains("Folder"))
                {
                    testfolder = await portal.GetFolderContentsAsync(selectedKnownFolder, paths, SelectedPkgFullName);
                }
                else
                {

                    WasFolderSelected = false;
                    Debug.WriteLine("Fixing path");
                    string oldpaths = paths;
                    paths = oldpaths.Replace($"\\{itemId}", "");
                    Debug.WriteLine($"{selectedKnownFolder}, {itemId}, {paths}, {SelectedPkgFullName}");
                    ProgressText.Text = $"Downloading {itemId}";
                    testfile = await portal.GetFileAsync(selectedKnownFolder, itemId, paths, SelectedPkgFullName);
                }
            }
            else
            {
                if (itemType.Contains("Folder"))
                {
                    testfolder = await portal.GetFolderContentsAsync(selectedKnownFolder, paths);
                }
                else
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        ProgressText.Text = $"Downloading {itemId}";
                        Debug.WriteLine("It's a File!\n");

                        StackPanel panel = new StackPanel();

                        TextBlock nameHeader = new TextBlock();
                        nameHeader.Margin = new Thickness(5, 5, 5, 5);
                        nameHeader.Text = "Enter file name";
                        nameHeader.HorizontalAlignment = HorizontalAlignment.Stretch;
                        TextBox fileNameUser = new TextBox();
                        fileNameUser.PlaceholderText = "Enter a File Name";
                        fileNameUser.Margin = new Thickness(5, 5, 5, 5);
                        fileNameUser.Text = itemId;
                        fileNameUser.Height = 40;
                        fileNameUser.VerticalAlignment = VerticalAlignment.Center;
                        panel.Children.Add(nameHeader);
                        panel.Children.Add(fileNameUser);
                        ContentDialog dialog = new ContentDialog();
                        dialog.Content = panel;
                        dialog.Height = 200;
                        dialog.VerticalContentAlignment = VerticalAlignment.Center;
                        dialog.IsSecondaryButtonEnabled = true;
                        dialog.PrimaryButtonText = "Confirm";
                        dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick;
                        dialog.SecondaryButtonText = "Cancel";
                        dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick;


                        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            if (fileNameUser.Text == "")
                            {
                                fileNameUser.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                            }
                            else
                            {
                                fileNameUser.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Green);
                                saveFileName = fileNameUser.Text;
                            }

                        }
                    });

                }
            }


            if (!itemType.Contains("Folder"))
            {

            }
            else
            {
                Debug.WriteLine("It's a Folder\n");
                // await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                //                () =>
                //              {
                FileListView.ItemsSource = null;
                //            });
                Debug.WriteLine("Clearing List View");
                List<FolderInfo> folderInfo = new List<FolderInfo>();
                if (selectedKnownFolder == "LocalAppData")
                {
                    WDPFolder = portal.GetFolderContentsAsync(selectedKnownFolder, paths, SelectedPkgFullName);
                }
                else
                {
                    Debug.WriteLine("Fetching folder contents");
                    await Task.Run(() =>
                    {
                        WDPFolder = portal.GetFolderContentsAsync(selectedKnownFolder, paths);
                    });
                }
                var result = WDPFolder.Result;
                Debug.WriteLine("Cheching for folder contents");
                foreach (var item in result.Contents)
                {
                    Debug.WriteLine(item.Name + "\n" + item.Type);

                    folderInfo.Add(new FolderInfo
                    {
                        ListViewFileName = item.Name,
                        ListViewFileSizeType = $"{item.SizeInBytes.ToFileSize().ToString()}  {FindFileFolderAttribute(item.Type)}",
                        ListViewFileIcon = CommonFileIcons.IconFromExtention(item.Name)
                    });

                }

                //  await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                //                  () =>
                //                 {
                FileListView.ItemsSource = folderInfo;
                //               });
                WasFolderSelected = true;

            }
            // });
            ProgRingFiles.IsEnabled = false;
            ProgRingFiles.IsActive = false;
            ProgressText.Visibility = Visibility.Collapsed;
            ProgBorder.Visibility = Visibility.Collapsed;

            FileRPanel.IsHitTestVisible = true;
            FileListView.IsHitTestVisible = true;
            FileListView.IsEnabled = true;
        }

        private void Dialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FilesHeader.Text = "";
            FileListView.IsHitTestVisible = true;
            FileListView.IsEnabled = true;
        }

        private async void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

            ProgRingFiles.IsEnabled = true;
            ProgRingFiles.IsActive = true;
            ProgressText.Visibility = Visibility.Visible;
            ProgBorder.Visibility = Visibility.Visible;

            ProgressText.Text = "Processing..";
            FolderPicker saveFolder = new FolderPicker();
            saveFolder.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            saveFolder.FileTypeFilter.Add("*");
            StorageFolder SelectedFolder = await saveFolder.PickSingleFolderAsync();
            if (SelectedFolder == null)
            {
                ProgRingFiles.IsEnabled = false;
                ProgRingFiles.IsActive = false;
                ProgressText.Visibility = Visibility.Collapsed;
                ProgBorder.Visibility = Visibility.Collapsed;
                FileListView.IsHitTestVisible = true;
                FileListView.IsEnabled = true;
                return;

            }

            try
            {
                ProgressText.Text = $"Downloading {saveFileName}";
                Debug.WriteLine("Getting remote file");
                testfile = await portal.GetFileAsync(selectedKnownFolder, paths);
                Debug.WriteLine("Getting local storage file");
                StorageFile SavedStorageFile = await SelectedFolder.CreateFileAsync(saveFileName, CreationCollisionOption.GenerateUniqueName);
                var output = await SavedStorageFile.OpenStreamForWriteAsync();
                Debug.WriteLine($"Copying data");
                await testfile.CopyToAsync(output);
                output.Dispose();
                FilesHeader.Text = "";
                ExceptionHelper.Exceptions.WDPSuccessDownload(saveFileName, SelectedFolder.Path);
                Debug.WriteLine("Copying file complete");
                FileRPanel.IsHitTestVisible = true;
                // SavedStorageFile = null;
            }
            catch (Exception ex)
            {
                FileRPanel.IsHitTestVisible = true;
                FilesHeader.Text = "";
                ExceptionHelper.Exceptions.WDPDownloadFail(saveFileName, SelectedFolder.Path, ex);
                Debug.WriteLine("Copying file failed");
            }
            ProgRingFiles.IsEnabled = false;
            ProgRingFiles.IsActive = false;
            ProgressText.Visibility = Visibility.Collapsed;
            ProgBorder.Visibility = Visibility.Collapsed;
            FileListView.IsHitTestVisible = true;
            FileListView.IsEnabled = true;
        }

        private class FileOrFolder
        {
            public string PackageName { get; set; }
            public bool Folder { get; set; }
        }

        private async void KnownFoldersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProgRingFiles.IsEnabled = true;
            ProgRingFiles.IsActive = true;
            ProgressText.Visibility = Visibility.Visible;
            ProgressText.Text = "Processing..";
            FilesHeader.Text = "";
            selectedKnownFolder = (sender as ComboBox).SelectedItem.ToString();


            if (selectedKnownFolder != null)
            {
                List<FolderInfo> folderInfo = new List<FolderInfo>();
                if (selectedKnownFolder == "LocalAppData")
                {
                    AppNamesCombo.Visibility = Visibility.Visible;
                    AppNamesCombo.IsEnabled = true;
                }
                else
                {
                    AppNamesCombo.IsEnabled = false;
                    AppNamesCombo.Visibility = Visibility.Collapsed;
                    folderContents = await portal.GetFolderContentsAsync(selectedKnownFolder);


                    foreach (var item in folderContents.Contents)
                    {
                        if (FindFileFolderAttribute(item.Type).Contains("Folder"))
                        {



                            folderInfo.Add(new FolderInfo
                            {
                                ListViewFileName = item.Name,
                                ListViewFileSizeType = $"{FindFileFolderAttribute(item.Type)}",

                                ListViewFileIcon = new BitmapImage(new Uri("ms-appx:///mimetypes/folder.png"))
                            });
                        }
                        else
                        {

                            folderInfo.Add(new FolderInfo
                            {
                                ListViewFileName = item.Name,
                                ListViewFileSizeType = $"{item.SizeInBytes.ToFileSize().ToString()}  {FindFileFolderAttribute(item.Type)}",
                                ListViewFileIcon = CommonFileIcons.IconFromExtention(item.Name)
                            });
                        }

                    }

                }

                FileListView.ItemsSource = folderInfo;

            }

            ProgRingFiles.IsEnabled = false;
            ProgRingFiles.IsActive = false;
            ProgressText.Visibility = Visibility.Collapsed;

            UploadFilebtn.IsEnabled = true;
            UpFolderFilebtn.IsEnabled = true;

        }

        string SelectedPkgFullName;
        private async void AppNamesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProgRingFiles.IsEnabled = true;
            ProgRingFiles.IsActive = true;
            ProgressText.Visibility = Visibility.Visible;
            ProgressText.Text = "Processing..";
            SelectedPkgFullName = (sender as ComboBox).SelectedItem.ToString();

            try
            {
                folderContents = await portal.GetFolderContentsAsync("LocalAppData", "", SelectedPkgFullName);
                List<FolderInfo> folderInfo = new List<FolderInfo>();
                foreach (var item in folderContents.Contents)
                {

                    folderInfo.Add(new FolderInfo
                    {
                        ListViewFileName = item.Name,
                        ListViewFileSizeType = $"{item.SizeInBytes.ToFileSize().ToString()}  {FindFileFolderAttribute(item.Type)}"
                    });

                }

                FileListView.ItemsSource = folderInfo;

            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.WDPLocalAppDataError(SelectedPkgFullName);
            }
            ProgRingFiles.IsEnabled = false;
            ProgRingFiles.IsActive = false;
            ProgressText.Visibility = Visibility.Collapsed;

        }



        private async void UpFolderFilebtn_Click(object sender, RoutedEventArgs e)
        {
            ProgRingFiles.IsEnabled = true;
            ProgRingFiles.IsActive = true;
            ProgressText.Visibility = Visibility.Visible;
            ProgressText.Text = "Processing..";
            FilesHeader.Text = "";
            if (selectedKnownFolder == "LocalAppData")
            {
                folderContents = await portal.GetFolderContentsAsync("LocalAppData", "", SelectedPkgFullName);
            }
            else
            {
                folderContents = await portal.GetFolderContentsAsync(selectedKnownFolder);
            }
            List<FolderInfo> folderInfo = new List<FolderInfo>();
            foreach (var item in folderContents.Contents)
            {
                if (FindFileFolderAttribute(item.Type).Contains("Folder"))
                {



                    folderInfo.Add(new FolderInfo
                    {
                        ListViewFileName = item.Name,
                        ListViewFileSizeType = $"{FindFileFolderAttribute(item.Type)}",

                        ListViewFileIcon = new BitmapImage(new Uri("ms-appx:///mimetypes/folder.png"))
                    });
                }
                else
                {

                    folderInfo.Add(new FolderInfo
                    {
                        ListViewFileName = item.Name,
                        ListViewFileSizeType = $"{item.SizeInBytes.ToFileSize().ToString()}  {FindFileFolderAttribute(item.Type)}",
                        ListViewFileIcon = CommonFileIcons.IconFromExtention(item.Name)
                    });
                }
            }

            FileListView.ItemsSource = folderInfo;
            WasFolderSelected = false;
            paths = "";
            ProgRingFiles.IsEnabled = false;
            ProgRingFiles.IsActive = false;
            ProgressText.Visibility = Visibility.Collapsed;
        }

        public string FindFileFolderAttribute(int type)
        {
            switch (type)
            {
                case 32:
                    return "File";
                case 2048:
                    return "File";
                case 64:
                    return "Reserved";
                case 16:
                    return "Folder";
                case 2:
                    return "Hidden File";
                case 32768:
                    return "Not Supported";
                case 128:
                    return "File";
                case 8192:
                    return "Non-Indexed File";
                case 131072:
                    return "Not Supported";
                case 4096:
                    return "File (Offline)";
                case 1:
                    return "File";
                case 1024:
                    return "Shortcut";
                case 4:
                    return "System File";
                default:
                    return "File";
            }
        }

        private void ProcessesPivot_Loaded(object sender, RoutedEventArgs e)
        {
            processesListView.IsEnabled = true;
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }

        #endregion


        private void ProcessesPivot_Unloaded(object sender, RoutedEventArgs e)
        {
            processesListView.IsEnabled = false;
        }




        #region CrashDumps

        private async void GetCrashDumpConfig(int i)
        {
            DumpFileSettings test = await portal.GetDumpFileSettingsAsync();
            dumpType = test.DumpType;
            autoreboot = test.AutoReboot;
            maxDumpCount = test.MaxDumpCount;
            overwriteDump = test.Overwrite;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                   () =>
                   {
                       switch (dumpType)
                       {
                           case DumpFileSettings.DumpTypes.Disabled:
                               IsCrashDumpEnabled = false;
                               AutoRebootTog.IsEnabled = false;
                               OverwriteDumpTog.IsEnabled = false;
                               MaxDumpSlider.IsEnabled = false;
                               DumpDisabledItem.IsSelected = true;
                               DumpTypeCombo.SelectedIndex = 0;
                               break;
                           case DumpFileSettings.DumpTypes.CompleteMemoryDump:
                               IsCrashDumpEnabled = true;
                               DumpCompleteItem.IsSelected = true;
                               DumpTypeCombo.SelectedIndex = 1;
                               MaxDumpSlider.Value = maxDumpCount;
                               AutoRebootTog.IsOn = autoreboot;
                               OverwriteDumpTog.IsOn = overwriteDump;
                               AutoRebootTog.IsEnabled = true;
                               OverwriteDumpTog.IsEnabled = true;
                               MaxDumpSlider.IsEnabled = true;
                               break;
                           case DumpFileSettings.DumpTypes.KernelDump:
                               IsCrashDumpEnabled = true;
                               DumpKernelItem.IsSelected = true;
                               DumpTypeCombo.SelectedIndex = 2;
                               MaxDumpSlider.Value = maxDumpCount;
                               AutoRebootTog.IsOn = autoreboot;
                               OverwriteDumpTog.IsOn = overwriteDump;
                               AutoRebootTog.IsEnabled = true;
                               OverwriteDumpTog.IsEnabled = true;
                               MaxDumpSlider.IsEnabled = true;
                               break;
                           case DumpFileSettings.DumpTypes.Minidump:
                               IsCrashDumpEnabled = true;
                               DumpMiniItem.IsSelected = true;
                               DumpTypeCombo.SelectedIndex = 3;
                               MaxDumpSlider.Value = maxDumpCount;
                               AutoRebootTog.IsOn = autoreboot;
                               OverwriteDumpTog.IsOn = overwriteDump;
                               AutoRebootTog.IsEnabled = true;
                               OverwriteDumpTog.IsEnabled = true;
                               MaxDumpSlider.IsEnabled = true;
                               break;
                           default:
                               IsCrashDumpEnabled = false;
                               AutoRebootTog.IsEnabled = false;
                               OverwriteDumpTog.IsEnabled = false;
                               MaxDumpSlider.IsEnabled = false;
                               DumpDisabledItem.IsSelected = true;
                               DumpTypeCombo.SelectedIndex = 0;

                               break;
                       }




                   });


        }

        private async void MaxDumpSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (FinishedLoadingData == false)
            {

            }
            else
            {
                DumpFileSettings fileSettings = new DumpFileSettings();
                fileSettings.MaxDumpCount = (int)MaxDumpSlider.Value;
                fileSettings.Overwrite = overwriteDump;
                fileSettings.AutoReboot = autoreboot;
                fileSettings.DumpType = dumpType;
                await portal.SetDumpFileSettingsAsync(fileSettings);
            }
        }

        private async void OverwriteDumpTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (FinishedLoadingData == false)
            {

            }
            else
            {
                DumpFileSettings fileSettings = new DumpFileSettings();
                if (OverwriteDumpTog.IsOn)
                {
                    fileSettings.Overwrite = true;
                    fileSettings.AutoReboot = autoreboot;
                    fileSettings.MaxDumpCount = maxDumpCount;
                    fileSettings.DumpType = dumpType;
                    await portal.SetDumpFileSettingsAsync(fileSettings);
                }
                else
                {
                    fileSettings.Overwrite = false;
                    fileSettings.AutoReboot = autoreboot;
                    fileSettings.MaxDumpCount = maxDumpCount;
                    fileSettings.DumpType = dumpType;
                    await portal.SetDumpFileSettingsAsync(fileSettings);
                }
            }

        }

        private async void AutoRebootTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (FinishedLoadingData == false)
            {

            }
            else
            {
                DumpFileSettings fileSettings = new DumpFileSettings();
                if (AutoRebootTog.IsOn)
                {
                    fileSettings.AutoReboot = true;
                    fileSettings.MaxDumpCount = maxDumpCount;
                    fileSettings.DumpType = dumpType;
                    fileSettings.Overwrite = overwriteDump;
                    await portal.SetDumpFileSettingsAsync(fileSettings);
                }
                else
                {
                    fileSettings.AutoReboot = true;
                    fileSettings.MaxDumpCount = maxDumpCount;
                    fileSettings.DumpType = dumpType;
                    fileSettings.Overwrite = overwriteDump;
                    await portal.SetDumpFileSettingsAsync(fileSettings);

                }
            }
        }

        int driveType;
        private async void DumpTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FinishedLoadingData == false)
            {

            }
            else
            {
                DumpFileSettings fileSettings = new DumpFileSettings();
                switch (DumpTypeCombo.SelectedIndex)
                {
                    case 0:
                        driveType = 0;
                        fileSettings.Overwrite = overwriteDump;
                        fileSettings.AutoReboot = autoreboot;
                        fileSettings.MaxDumpCount = maxDumpCount;
                        fileSettings.DumpType = DumpFileSettings.DumpTypes.Disabled;
                        await portal.SetDumpFileSettingsAsync(fileSettings);
                        break;

                    case 1:
                        driveType = 1;
                        fileSettings.Overwrite = overwriteDump;
                        fileSettings.AutoReboot = autoreboot;
                        fileSettings.MaxDumpCount = maxDumpCount;
                        fileSettings.DumpType = DumpFileSettings.DumpTypes.CompleteMemoryDump;
                        await portal.SetDumpFileSettingsAsync(fileSettings);
                        break;

                    case 2:
                        driveType = 2;
                        fileSettings.Overwrite = overwriteDump;
                        fileSettings.AutoReboot = autoreboot;
                        fileSettings.MaxDumpCount = maxDumpCount;
                        fileSettings.DumpType = DumpFileSettings.DumpTypes.KernelDump;
                        await portal.SetDumpFileSettingsAsync(fileSettings);
                        break;

                    case 3:
                        driveType = 3;
                        fileSettings.Overwrite = overwriteDump;
                        fileSettings.AutoReboot = autoreboot;
                        fileSettings.MaxDumpCount = maxDumpCount;
                        fileSettings.DumpType = DumpFileSettings.DumpTypes.Minidump;
                        await portal.SetDumpFileSettingsAsync(fileSettings);
                        break;
                }
                GetCrashDumpConfig(driveType);
            }
        }
        bool CheckingDumpStatus = false;
        string SelectedDumpPkg;
        private async void CrashDumpAppCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckingDumpStatus = true;
            CrashDumpAppCheck.IsChecked = false;
            DumpsListView.ItemsSource = null;
            Debug.WriteLine((sender as ComboBox).SelectedItem.ToString());
            SelectedDumpPkg = (sender as ComboBox).SelectedItem.ToString();

            var appSettings = await portal.GetAppCrashDumpSettingsAsync(SelectedDumpPkg);
            var isenabled = appSettings.CrashDumpEnabled;
            CrashDumpAppCheck.IsChecked = isenabled;
            CheckingDumpStatus = false;
        }

        private async void CrashDumpAppCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckingDumpStatus == false)
                await portal.SetAppCrashDumpSettingsAsync(SelectedDumpPkg, true);
        }

        private async void CrashDumpAppCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckingDumpStatus == false)
                await portal.SetAppCrashDumpSettingsAsync(SelectedDumpPkg, false);
        }

        private async void FetchCrashDumpFiles_Click(object sender, RoutedEventArgs e)
        {

            RefreshDumpListView();
        }


        public async void RefreshDumpListView()
        {

            List<AppCrashDumpSettings> dumpSettings = new List<AppCrashDumpSettings>();
            var dmpFiles = await portal.GetAppCrashDumpListAsync();
            foreach (var item in dmpFiles)
            {

                if (item.PackageFullName == SelectedDumpPkg)
                {

                    dumpSettings.Add(new AppCrashDumpSettings
                    {
                        FileName = item.Filename,
                        //FileDate = item.FileDateAsString,
                        FileSizeAndDate = $"{((long)item.FileSizeInBytes).ToFileSize()} | {item.FileDateAsString}",
                        PackageFullName = item.PackageFullName,
                        CrashDump = item

                    });
                }
                else
                {

                }
                Debug.WriteLine($"Dump App: {item.PackageFullName}");

                DumpsListView.ItemsSource = dumpSettings;
            }
        }

        public class AppCrashDumpSettings
        {
            //public string FileDate { get; set; }
            public string FileName { get; set; }
            public string FileSizeAndDate { get; set; }
            public string PackageFullName { get; set; }
            public AppCrashDump CrashDump { get; set; }
        }
        int ClickedIndexItem;
        AppCrashDump ClickedIndexSettings;
        private async void ListBtn_Click(object sender, RoutedEventArgs e)
        {
            ClickedIndexItem = 0;
            ClickedIndexSettings = null;

            var item = (sender as FrameworkElement).DataContext;
            ClickedIndexItem = DumpsListView.Items.IndexOf(item);
            DumpsListView.SelectedIndex = ClickedIndexItem;
            ClickedIndexSettings = (DumpsListView.SelectedItem as MainPage.AppCrashDumpSettings).CrashDump;

            StackPanel Panel = new StackPanel();

            TextBlock nameHeader = new TextBlock();
            nameHeader.Margin = new Thickness(5, 5, 5, 5);
            nameHeader.Text = $"\nDelete crash dump?\n\n{ (DumpsListView.SelectedItem as MainPage.AppCrashDumpSettings).FileName}";
            nameHeader.HorizontalAlignment = HorizontalAlignment.Stretch;


            Panel.Children.Add(nameHeader);

            ContentDialog dialog = new ContentDialog();
            dialog.Content = Panel;
            dialog.VerticalContentAlignment = VerticalAlignment.Center;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Confirm";
            dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick1;
            dialog.SecondaryButtonText = "Cancel";
            dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick1;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {


            }






        }

        private void Dialog_SecondaryButtonClick1(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            return;
        }

        private async void Dialog_PrimaryButtonClick1(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await portal.DeleteAppCrashDumpAsync(ClickedIndexSettings);
            RefreshDumpListView();

        }

        string ClickedListViewItem;
        private async void DumpsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var test = (AppCrashDumpSettings)e.ClickedItem;
            //var item = (sender as AppCrashDumpSettings).FileName;
            // string name = (DumpsListView.SelectedItem as MainPage.AppCrashDumpSettings).FileName;
            ClickedListViewItem = test.FileName;
            ClickedIndexSettings = test.CrashDump;
            StackPanel Panel = new StackPanel();

            TextBlock nameHeader = new TextBlock();
            nameHeader.Margin = new Thickness(5, 5, 5, 5);
            nameHeader.Text = $"\nDownload Crash Dump?\n\n{test.FileName}";
            nameHeader.HorizontalAlignment = HorizontalAlignment.Stretch;


            Panel.Children.Add(nameHeader);

            ContentDialog dialog = new ContentDialog();
            dialog.Content = Panel;
            dialog.VerticalContentAlignment = VerticalAlignment.Center;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Confirm";
            dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick2;
            dialog.SecondaryButtonText = "Cancel";
            dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick2;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {


            }

        }

        private void Dialog_SecondaryButtonClick2(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            return;
        }

        private async void Dialog_PrimaryButtonClick2(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {

                var dmp = await portal.GetAppCrashDumpAsync(ClickedIndexSettings);


                FolderPicker picker = new FolderPicker();
                picker.FileTypeFilter.Add(".dmp");
                StorageFolder folder = await picker.PickSingleFolderAsync();
                StorageFile file = await folder.CreateFileAsync(ClickedListViewItem, CreationCollisionOption.ReplaceExisting);
                Stream outstream = await file.OpenStreamForWriteAsync();
                Debug.WriteLine("Downloading data " + dmp.Length);

                await dmp.CopyToAsync(outstream);

                await outstream.FlushAsync();
                outstream.Dispose();
                dmp.Dispose();
                var success = new MessageDialog("Successfully saved to " + file.Path);
                success.Commands.Add(new UICommand("Close"));
                await success.ShowAsync();




            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }



        #endregion


        #region Wifi

        WifiInterfaces WifiInterfaces { get; set; }
        private async void FetchWifiInformation()
        {
            var access = await Windows.Devices.WiFi.WiFiAdapter.RequestAccessAsync();

            if (access == Windows.Devices.WiFi.WiFiAccessStatus.Allowed)
            {
                var uwpAdapters = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.WiFi.WiFiAdapter.GetDeviceSelector());
                Debug.WriteLine("Allowed Wifi Control");
                foreach (var adapter in uwpAdapters)
                {
                    Debug.WriteLine($"{adapter.Name} | {adapter.IsDefault} | {adapter.IsEnabled}");
                }
            }
            WifiInterfaces = await portal.GetWifiInterfacesAsync();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                   () =>
                   {
                       foreach (var device in WifiInterfaces.Interfaces)
                       {

                           WifiAdaptersCombo.Items.Add(device.Description);

                       }
                       if (WifiAdaptersCombo.Items.Count == 1)
                       {
                           WifiAdaptersCombo.SelectedIndex = 0;
                       }
                   });


        }




        private async void GetInterfaceInfo(WifiInterface device)
        {


            foreach (var item in device.Profiles)
            {
                AdapterInfoText.Text +=
                    $"Profile: {item.Name} | {item.IsPerUserProfile} | {item.IsGroupPolicyProfile}";
            }
        }
        Guid deviceGUID { get; set; }
        string ConnectedNetwork { get; set; }
        private async void WifiAdaptersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            RefreshNetworkList();
        }



        private async void RefreshNetworkList()
        {
            try
            {
                NetworkProgress.IsIndeterminate = true;
                List<WifiNetworlInfo> availableNetworks = new List<WifiNetworlInfo>();
                List<WifiNetworlInfo> connectedNetworks = new List<WifiNetworlInfo>();

                foreach (var device in WifiInterfaces.Interfaces)
                {
                    var deviceDescription = device.Description;
                    deviceGUID = device.Guid;
                    var deviceProfiles = device.Profiles;

                    // var netType = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();

                    // var uwpNetGUID = netType.NetworkAdapter.NetworkAdapterId;

                    if (WifiAdaptersCombo.SelectedItem.ToString() == deviceDescription)
                    {

                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                       () =>
                       {
                           AdapterInfoText.Text =
                            $"GUID: {deviceGUID}";


                       });

                        var networks = await portal.GetWifiNetworksAsync(deviceGUID);
                        foreach (var item in networks.AvailableNetworks)
                        {

                            if (item.IsConnected == true)
                            {
                                connectedNetworks.Add(new WifiNetworlInfo
                                {
                                    SSID = item.Ssid,
                                    AuthenticationType = item.AuthenticationAlgorithm,
                                    ProfileName = item.ProfileName,
                                    IsConnectable = item.IsConnectable,
                                    IsConnected = item.IsConnected,
                                    Channel = item.Channel,
                                    CombinedInfo =
                                $"Auth: {item.AuthenticationAlgorithm}\n" +
                                $"Channel: {item.Channel}\n" +
                                $"Strength: {item.SignalQuality}\n",
                                    SignalImage = CheckSignalStrength(item.SignalQuality)
                                });
                            }
                            else
                            {


                                availableNetworks.Add(new WifiNetworlInfo
                                {
                                    SSID = item.Ssid,
                                    AuthenticationType = item.AuthenticationAlgorithm,
                                    ProfileName = item.ProfileName,
                                    IsConnectable = item.IsConnectable,
                                    IsConnected = item.IsConnected,
                                    Channel = item.Channel,
                                    CombinedInfo =
                                    $"Profile Name: {item.ProfileName}\n" +
                                    $"Auth: {item.AuthenticationAlgorithm}\n" +
                                    $"Channel: {item.Channel}\n" +
                                    $"Strength: {item.SignalQuality}\n",
                                    SignalImage = CheckSignalStrength(item.SignalQuality)

                                });
                            }


                        }

                        WifiNetworksListView.ItemsSource = availableNetworks;
                        CurrentlyConnectedInfo.ItemsSource = connectedNetworks;
                        if (CurrentlyConnectedInfo.Items.Count != 0)
                        {
                            CurrentlyConnectedInfo.SelectedIndex = 0;
                        }
                        NetworkProgress.IsIndeterminate = false;

                    }
                }
            }
            catch (Exception ex)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                   () =>
                   {
                       NetworkProgress.IsIndeterminate = false;
                       //Exceptions.CustomException($"Please make sure WiFi is enabled in Device Settings.\n\n{ex.Message}\n{ex.StackTrace}");
                       //Exceptions.ThrownExceptionErrorExtended(ex);
                       AdapterInfoText.Text = "Wifi is turned off or inaccessable";
                   });
            }
        }


        private class WifiNetworlInfo
        {
            public string SSID { get; set; }
            public string AuthenticationType { get; set; }
            public string ProfileName { get; set; }
            public bool IsConnectable { get; set; }
            public bool IsConnected { get; set; }
            public int Channel { get; set; }
            public string CombinedInfo { get; set; }
            public BitmapImage SignalImage { get; set; }
        }




        string ClickedNetwork { get; set; }
        string TempPassword { get; set; }
        private async void WifiNetworksListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            NetworkProgress.IsIndeterminate = true;
            var clickedItem = (e.ClickedItem as WifiNetworlInfo);
            Debug.WriteLine($"Item Clicked: {clickedItem.SSID}");
            ClickedNetwork = clickedItem.SSID;

            List<string> retrievedDetails = CheckForCredentials(ClickedNetwork);
            if (retrievedDetails != null)
            {
                await portal.ConnectToWifiNetworkAsync(deviceGUID, ClickedNetwork, retrievedDetails[1]);
                RefreshNetworkList();
                retrievedDetails.Clear();
                retrievedDetails = null;
                NetworkProgress.IsIndeterminate = false;
            }
            else
            {



                StackPanel Panel = new StackPanel();

                TextBlock nameHeader = new TextBlock();
                nameHeader.Margin = new Thickness(5, 5, 5, 5);
                nameHeader.Text = $"Connect to {clickedItem.SSID}?";
                nameHeader.HorizontalAlignment = HorizontalAlignment.Stretch;
                PasswordBox password = new PasswordBox();
                password.Margin = new Thickness(5, 5, 5, 5);
                password.PlaceholderText = "Enter password";
                password.HorizontalAlignment = HorizontalAlignment.Stretch;
                Panel.Children.Add(nameHeader);
                Panel.Children.Add(password);
                ContentDialog dialog = new ContentDialog();
                dialog.Content = Panel;
                dialog.VerticalContentAlignment = VerticalAlignment.Center;
                dialog.IsSecondaryButtonEnabled = true;
                dialog.PrimaryButtonText = "Confirm";
                dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick3;
                dialog.SecondaryButtonText = "Cancel";
                dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick3;

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {

                    if (password.Password != "")
                    {
                        SaveNetworkPassword(ClickedNetwork, password.Password);
                        await portal.ConnectToWifiNetworkAsync(deviceGUID, ClickedNetwork, password.Password);
                        RefreshNetworkList();
                        NetworkProgress.IsIndeterminate = false;
                    }
                }
            }

        }

        private async void Dialog_SecondaryButtonClick3(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void Dialog_PrimaryButtonClick3(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            return;
        }

        public async void DisconnectNetwork()
        {
            try
            {
                Guid non = new Guid();
                await portal.ConnectToWifiNetworkAsync(non, "", "");
            }
            catch (Exception ex)
            {
                Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }

        private void SaveNetworkPassword(string SSID, string password)
        {
            PasswordStore.Add(new PasswordCredential("WiFiNetworks", SSID, password));
            Debug.WriteLine("Network Saved");
        }

        List<string> credentials { get; set; }
        private List<string> CheckForCredentials(string SSID)
        {
            // PasswordStore.Retrieve("WiFiNetworks", SSID);
            try
            {
                if (PasswordStore.FindAllByUserName(SSID) != null)
                {
                    var storedCredentials = PasswordStore.FindAllByUserName(SSID);
                    credentials = new List<string>();
                    foreach (var item in storedCredentials)
                    {
                        item.RetrievePassword();
                        credentials.Add(item.UserName);
                        credentials.Add(item.Password);
                    }

                    return credentials;

                }
                else
                {
                    credentials = null;
                    return credentials;
                }

            }
            catch (Exception ex)
            {
                credentials = null;
                return credentials;
            }
        }

        private void DisconnectNetworkBtn_Click(object sender, RoutedEventArgs e)
        {
            DisconnectNetwork();
        }


        private BitmapImage CheckSignalStrength(int signal)
        {
            int oneBar = 35;
            int twoBar = 70;
            int threeBar = 100;

            if (signal <= oneBar)
            {
                BitmapImage img = new BitmapImage(new Uri("ms-appx:///Assets/wifi-low.png"));
                return img;
            }
            if (signal < twoBar && signal > oneBar)
            {
                BitmapImage img = new BitmapImage(new Uri("ms-appx:///Assets/wifi-mid.png"));
                return img;
            }
            if (signal >= twoBar)
            {
                BitmapImage img = new BitmapImage(new Uri("ms-appx:///Assets/wifi-full.png"));
                return img;
            }
            return null;

        }

        string ipAddr;
        string ipSubnet;
        string ipAddrGate;
        string ipSubnetGate;
        public async void ViewCurrentConnection()
        {
            IpConfiguration ipconfig = await portal.GetIpConfigAsync();

            foreach (NetworkAdapterInfo adapterInfo in ipconfig.Adapters)
            {
                if (adapterInfo.Id == deviceGUID)
                {

                    foreach (IpAddressInfo address in adapterInfo.IpAddresses)
                    {
                        ipAddr = address.Address;
                        ipSubnet = address.SubnetMask;
                    }
                    foreach (var gateway in adapterInfo.Gateways)
                    {
                        ipSubnetGate = gateway.SubnetMask;
                        ipAddrGate = gateway.Address;
                    }


                    CurrentConnectionLabel.Text = adapterInfo.Description;
                    ConnectionDetails.Text =
                        $"IP Address: {ipAddr}\n" +
                        $"Subnet Mask: {ipSubnet}\n" +
                        $"Default Gateway IP: {ipAddrGate}\n" +
                        $"Default Gateway Subnet Mask: {ipSubnetGate}\n\n" +
                        $"MAC Address: {adapterInfo.MacAddress}\n" +
                        $"DHCP Address: {adapterInfo.Dhcp.Address.Address}\n" +
                        $"Adapter Type: {adapterInfo.AdapterType}";
                }
            }
            ConnectionInfoWindow.Visibility = Visibility.Visible;


        }

        #endregion

        private void ConnectionWindowClose_Click(object sender, RoutedEventArgs e)
        {
            ConnectionInfoWindow.Visibility = Visibility.Collapsed;
            CurrentConnectionLabel.Text = "";
            ConnectionDetails.Text = "";

        }

        #region Bluetooth

        private class BTNetworkInfo
        {
            public string SSID { get; set; }
            public string AuthenticationType { get; set; }
            public string ProfileName { get; set; }
            public bool IsConnectable { get; set; }
            public bool IsConnected { get; set; }
            public int Channel { get; set; }
            public string CombinedInfo { get; set; }
            public BitmapImage SignalImage { get; set; }
        }

        private MainPage rootPage;
        private DeviceWatcher BTDeviceWatcher = null;
        private DeviceWatcher deviceWatcher = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformation> handlerAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerRemoved = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerEnumCompleted = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerStopped = null;

        public ObservableCollection<DeviceInformationDisplay> ResultCollection
        {
            get;
            private set;
        }

        public async void GetRadioInfo()
        {
            try
            {
                // RadioAccessStatus result = await Radio.RequestAccessAsync();
                IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
                Radio BluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                //RadioAccessStatus btresult;
                foreach (Radio module in radios)
                {
                    if (module.Kind == RadioKind.Bluetooth)
                    {


                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                      () =>
                      {

                          if (module.State == RadioState.Off)
                          {

                              BTToggle.IsOn = false;

                          }
                          else
                          {
                              BTToggle.IsOn = true;
                          }
                      });
                    }
                }
                ResultCollection = new ObservableCollection<DeviceInformationDisplay>();

            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionError(ex);
            }

        }

        public bool IsWatcherStarted = false;
        private void StartListeningForDevices()
        {
            try
            {
                ResultCollection.Clear();
                Debug.WriteLine("Setting Device Info");
                BTNetworksListView.IsEnabled = false;
                DeviceSelectorInfo deviceSelectorInfo = Bluetooth;
                Debug.WriteLine("Creating Watcher");

                if (null == deviceSelectorInfo.Selector)
                {
                    // If the a pre-canned device class selector was chosen, call the DeviceClass overload
                    BTDeviceWatcher = DeviceInformation.CreateWatcher(deviceSelectorInfo.DeviceClassSelector);
                }
                else if (deviceSelectorInfo.Kind == DeviceInformationKind.Unknown)
                {
                    // Use AQS string selector from dynamic call to a device api's GetDeviceSelector call
                    // Kind will be determined by the selector
                    BTDeviceWatcher = DeviceInformation.CreateWatcher(
                        deviceSelectorInfo.Selector,
                        null // don't request additional properties for this sample
                        );
                }
                else
                {
                    // Kind is specified in the selector info
                    BTDeviceWatcher = DeviceInformation.CreateWatcher(
                        deviceSelectorInfo.Selector,
                        null, // don't request additional properties for this sample
                        deviceSelectorInfo.Kind);
                }


                /// BTDeviceWatcher = DeviceInformation.CreateWatcher(deviceSelectorInfo.Selector, null, deviceSelectorInfo.Kind);

                // Hook up handlers for the watcher events before starting the watcher

                handlerAdded = new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            Debug.WriteLine("Adding info to Collection");

                            
                            ResultCollection.Add(new DeviceInformationDisplay(deviceInfo));

                            rootPage.NotifyUser(
                                String.Format("{0} devices found.", ResultCollection.Count),
                                NotifyType.StatusMessage);
                        });
                });
                BTDeviceWatcher.Added += handlerAdded;
                Debug.WriteLine("Watcher Handler Added");

                IsWatcherStarted = true;
                handlerUpdated = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            // Find the corresponding updated DeviceInformation in the collection and pass the update object
                            // to the Update method of the existing DeviceInformation. This automatically updates the object
                            // for us.
                            foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                            {
                                if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                                {
                                   
                                    deviceInfoDisp.Update(deviceInfoUpdate);
                                    break;
                                }
                            }
                        });
                });
                BTDeviceWatcher.Updated += handlerUpdated;

                handlerRemoved = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            // Find the corresponding DeviceInformation in the collection and remove it
                            foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                            {
                                if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                                {
                                    ResultCollection.Remove(deviceInfoDisp);
                                    break;
                                }
                            }

                            rootPage.NotifyUser(
                                String.Format("{0} devices found.", ResultCollection.Count),
                                NotifyType.StatusMessage);
                        });
                });
                BTDeviceWatcher.Removed += handlerRemoved;

                handlerEnumCompleted = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
                {
                    await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        rootPage.NotifyUser(
                            String.Format("{0} devices found. Enumeration completed. Watching for updates...", ResultCollection.Count),
                            NotifyType.StatusMessage);
                    });
                });
                BTDeviceWatcher.EnumerationCompleted += handlerEnumCompleted;

                handlerStopped = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
                {
                    await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        rootPage.NotifyUser(
                            String.Format("{0} devices found. Searching {1}.",
                                ResultCollection.Count,
                                DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped"),
                            NotifyType.StatusMessage);
                    });
                });
                BTDeviceWatcher.Stopped += handlerStopped;

                rootPage.NotifyUser("Starting Watcher...", NotifyType.StatusMessage);
                BTDeviceWatcher.Start();

               
                // stopWatcherButton.IsEnabled = true;

            }
            catch (Exception ex)
            {

                Exceptions.ThrownExceptionError(ex);
            }
        }

        private async void StopListeningForDevices()
        {
            if (null != BTDeviceWatcher)
            {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                BTDeviceWatcher.Added -= handlerAdded;
                BTDeviceWatcher.Updated -= handlerUpdated;
                BTDeviceWatcher.Removed -= handlerRemoved;
                BTDeviceWatcher.EnumerationCompleted -= handlerEnumCompleted;

                if (DeviceWatcherStatus.Started == BTDeviceWatcher.Status ||
                    DeviceWatcherStatus.EnumerationCompleted == BTDeviceWatcher.Status)
                {
                    BTDeviceWatcher.Stop();
                    IsWatcherStarted = false;
                    BTNetworksListView.IsEnabled = true;


                }
            }
        }

        private void BTRescanNetworks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Loading StartListeningForDevices()");
                StartListeningForDevices();
                BTRescanNetworks.Visibility = Visibility.Collapsed;
                BTStopScanNetworks.Visibility = Visibility.Visible;


            }
            catch (Exception ex)
            {
                Exceptions.ThrownExceptionError(ex);
            }
        }
        private void BTStopScanNetworks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopListeningForDevices();
                BTRescanNetworks.Visibility = Visibility.Visible;
                BTStopScanNetworks.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Exceptions.ThrownExceptionError(ex);
            }

        }

        private void BluetoothPivotPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void BTAdaptersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BTWindowClose_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void BTToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (BTToggle.IsOn == true)
            {
                IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
                Radio BluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                //RadioAccessStatus btresult;
                foreach (Radio module in radios)
                {
                    if (module.Kind == RadioKind.Bluetooth)
                    {
                        await module.SetStateAsync(RadioState.On);

                    }
                }
            }
            if (BTToggle.IsOn == false)
            {
                IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
                Radio BluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                //RadioAccessStatus btresult;
                foreach (Radio module in radios)
                {
                    if (module.Kind == RadioKind.Bluetooth)
                    {
                        await module.SetStateAsync(RadioState.Off);

                    }
                }
            }
        }

        public async void ViewBTCurrentConnection()
        {

        }


        private async void PairDeviceBtn_Click(object sender, RoutedEventArgs e)
        {
            rootPage.NotifyUser("Pairing started. Please wait...", NotifyType.StatusMessage);

            DeviceInformationDisplay deviceInfoDisp = BTNetworksListView.SelectedItem as DeviceInformationDisplay;

            DevicePairingResult dpr = await deviceInfoDisp.DeviceInformation.Pairing.PairAsync();

            rootPage.NotifyUser(
                "Pairing result = " + dpr.Status.ToString(),
                dpr.Status == DevicePairingResultStatus.Paired ? NotifyType.StatusMessage : NotifyType.ErrorMessage);


            PairDeviceBtn.Visibility = Visibility.Collapsed;
            UnpairDeviceBtn.Visibility = Visibility.Visible;


            //UpdatePairingButtons();
        }

        private async void UnpairDeviceBtn_Click(object sender, RoutedEventArgs e)
        {
            rootPage.NotifyUser("Unpairing started. Please wait...", NotifyType.StatusMessage);

            DeviceInformationDisplay deviceInfoDisp = BTNetworksListView.SelectedItem as DeviceInformationDisplay;

            DeviceUnpairingResult dupr = await deviceInfoDisp.DeviceInformation.Pairing.UnpairAsync();

            rootPage.NotifyUser(
                "Unpairing result = " + dupr.Status.ToString(),
                dupr.Status == DeviceUnpairingResultStatus.Unpaired ? NotifyType.StatusMessage : NotifyType.ErrorMessage);




            //ResultCollection.Clear();
            BTNetworksListView.SelectedItem = null;
            PairDeviceBtn.Visibility = Visibility.Collapsed;
            UnpairDeviceBtn.Visibility = Visibility.Collapsed;

            //resultsListView.IsEnabled = true;
        }

        private void BTNetworksListView_ItemClick(object sender, SelectionChangedEventArgs e)
        {
            if (BTNetworksListView.SelectedItem == null)
            {
            }
            else
            {

                UpdatePairingButtons();
            }
        }

        private void UpdatePairingButtons()
        {
            DeviceInformationDisplay deviceInfoDisp = (DeviceInformationDisplay)BTNetworksListView.SelectedItem;
            Debug.WriteLine(deviceInfoDisp.Name);
            if (null != deviceInfoDisp &&
                deviceInfoDisp.DeviceInformation.Pairing.CanPair &&
                !deviceInfoDisp.DeviceInformation.Pairing.IsPaired)
            {
                PairDeviceBtn.Visibility = Visibility.Visible;
                UnpairDeviceBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                PairDeviceBtn.Visibility = Visibility.Collapsed;
                UnpairDeviceBtn.Visibility = Visibility.Visible;

            }

            if (null != deviceInfoDisp &&
                deviceInfoDisp.DeviceInformation.Pairing.IsPaired)
            {
                PairDeviceBtn.Visibility = Visibility.Collapsed;
                UnpairDeviceBtn.Visibility = Visibility.Visible;
            }
            else
            {
                PairDeviceBtn.Visibility = Visibility.Visible;
                UnpairDeviceBtn.Visibility = Visibility.Collapsed;
            }
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };
        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    // StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    //StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            BTAdapterInfoText.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            //StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (BTAdapterInfoText.Text != String.Empty)
            {
                // StatusBorder.Visibility = Visibility.Visible;
                // StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                // StatusBorder.Visibility = Visibility.Collapsed;
                // StatusPanel.Visibility = Visibility.Collapsed;
            }
        }


        public class DeviceInformationDisplay : INotifyPropertyChanged
        {
            private DeviceInformation deviceInfo;

            public DeviceInformationDisplay(DeviceInformation deviceInfoIn)
            {
                deviceInfo = deviceInfoIn;
                UpdateGlyphBitmapImage();
            }

            public DeviceInformationKind Kind
            {
                get
                {
                    return deviceInfo.Kind;
                }
            }

            public string Id
            {
                get
                {
                    return deviceInfo.Id;
                }
            }

            public string Name
            {
                get
                {
                    return deviceInfo.Name;
                }
                set
                {

                }
            }

            public BitmapImage GlyphBitmapImage
            {
                get;
                private set;
            }

            public bool CanPair
            {
                get
                {
                    return deviceInfo.Pairing.CanPair;
                }
            }

            public bool IsPaired
            {
                get
                {
                    return deviceInfo.Pairing.IsPaired;
                }
            }

            public IReadOnlyDictionary<string, object> Properties
            {
                get
                {
                    return deviceInfo.Properties;
                }
            }

            public DeviceInformation DeviceInformation
            {
                get
                {
                    return deviceInfo;
                }

                private set
                {
                    deviceInfo = value;
                }
            }

            public void Update(DeviceInformationUpdate deviceInfoUpdate)
            {
                deviceInfo.Update(deviceInfoUpdate);

                OnPropertyChanged("Kind");
                OnPropertyChanged("Id");
                OnPropertyChanged("Name");
                OnPropertyChanged("DeviceInformation");
                OnPropertyChanged("CanPair");
                OnPropertyChanged("IsPaired");

                UpdateGlyphBitmapImage();
            }

            private async void UpdateGlyphBitmapImage()
            {
                DeviceThumbnail deviceThumbnail = await deviceInfo.GetGlyphThumbnailAsync();
                BitmapImage glyphBitmapImage = new BitmapImage();
                await glyphBitmapImage.SetSourceAsync(deviceThumbnail);
                GlyphBitmapImage = glyphBitmapImage;
                OnPropertyChanged("GlyphBitmapImage");
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }



        public class DeviceSelectorInfo
        {
            public DeviceSelectorInfo()
            {
                Kind = DeviceInformationKind.Unknown;
                DeviceClassSelector = DeviceClass.All;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public DeviceClass DeviceClassSelector
            {
                get;
                set;
            }

            public DeviceInformationKind Kind
            {
                get;
                set;
            }

            public string Selector
            {
                get;
                set;
            }
        }


        public static DeviceSelectorInfo Bluetooth
        {
            get
            {
                // Currently Bluetooth APIs don't provide a selector to get ALL devices that are both paired and non-paired.  Typically you wouldn't need this for common scenarios, but it's convenient to demonstrate the
                // various sample scenarios. 
                return new DeviceSelectorInfo() { DisplayName = "Bluetooth", Selector = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"", Kind = DeviceInformationKind.AssociationEndpoint };
            }
        }

        public static DeviceSelectorInfo BluetoothUnpairedOnly
        {
            get
            {
                return new DeviceSelectorInfo() { DisplayName = "Bluetooth (unpaired)", Selector = BluetoothDevice.GetDeviceSelectorFromPairingState(false) };
            }
        }

        public static DeviceSelectorInfo BluetoothPairedOnly
        {
            get
            {
                return new DeviceSelectorInfo() { DisplayName = "Bluetooth (paired)", Selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true) };
            }
        }

        public static DeviceSelectorInfo BluetoothLE
        {
            get
            {
                // Currently Bluetooth APIs don't provide a selector to get ALL devices that are both paired and non-paired.  Typically you wouldn't need this for common scenarios, but it's convenient to demonstrate the
                // various sample scenarios. 
                return new DeviceSelectorInfo() { DisplayName = "Bluetooth LE", Selector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"", Kind = DeviceInformationKind.AssociationEndpoint };
            }
        }

        public static DeviceSelectorInfo BluetoothLEUnpairedOnly
        {
            get
            {
                return new DeviceSelectorInfo() { DisplayName = "Bluetooth LE (unpaired)", Selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false) };
            }
        }

        public static DeviceSelectorInfo BluetoothLEPairedOnly
        {
            get
            {
                return new DeviceSelectorInfo() { DisplayName = "Bluetooth LE (paired)", Selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true) };
            }
        }



        #endregion


        public string RetrieveCapability(string cap)
        {
            switch (cap)
            {
                //StandardCapabilities
                case "musicLibrary":
                    return "Music Library";

                case "picturesLibrary":
                    return "Pictures Library";
                case "videosLibrary":
                    return "Videos Library";
                case "removableStorage":
                    return "Removable Storage";
                case "internetClient":
                    return "Internet Access";
                case "internetClientServer":
                    return "Internet Access (Client/Server)";
                case "privateNetworkClientServer":
                    return "Private Network Access";
                case "appointments":
                    return "Appointments Access";
                case "appointmentsSystem":
                    return "Appointments (System)";
                case "contacts":
                    return "Contacts Access";
                case "contactsSystem":
                    return "Access Contacts (System)";
                case "codeGeneration":
                    return "Code Generation";
                case "allJoyn":
                    return "AllJoyn Enabled";
                case "phoneCall":
                    return "Phone Calls";
                case "phoneCallHistoryPublic":
                    return "Call History (Public)";
                case "phoneCallHistory":
                    return "Call History";
                case "phoneCallHistorySystem":
                    return "Call History (System)";
                case "recordedCallsFolder":
                    return "Recrded Calls";
                case "userAccountInformation":
                    return "User Information";
                case "voipCall":
                    return "VOIP Calling";
                case "objects3D":
                    return "3D Objects Library";
                case "chat":
                    return "SMS/MMS Access";
                case "blockedChatMessages":
                    return "Read Blocked Messages";
                case "lowLevelDevices":
                    return "Low Level Device Management";
                case "lowLevel":
                    return "Low Level Management";
                case "systemManagement":
                    return "System Management";
                case "backgroundMediaPlayback":
                    return "Background Playback";
                case "remoteSystem":
                    return "Remote Connectivity";
                case "spatialPerception":
                    return "Spatial Perception";
                case "globalMediaControl":
                    return "Media Control";
                case "graphicsCapture":
                    return "Graphics Capture";
                case "graphicsCaptureWithoutBorder":
                    return "Graphics Capture (No Border)";
                case "graphicsCaptureProgrammatic":
                    return "Graphics Capture (Programmatic)";


                //DeviceCapabilities
                case "location":
                    return "Location Access";
                case "microphone":
                    return "Microphone Access";
                case "proximity":
                    return "Proximity Device";
                case "webcam":
                    return "Webcam Access";
                case "usb":
                    return "USB Devices";
                case "humaninterfacedevice":
                    return "HID Devces";
                case "pointOfService":
                    return "Point of Service";
                case "bluetooth":
                    return "Bluetooth Control";
                case "wiFiControl":
                    return "Wireless Control";
                case "radios":
                    return "Radios Control";
                case "optical":
                    return "CD/DVD Access";
                case "activity":
                    return "Motion Detection";
                case "serialcommunication":
                    return "Serial Port Access";
                case "gazeInput":
                    return "Eye Tracking";

                case "userDataTasks":
                    return "User Data Tasks";
                case "userNotificationListener":
                    return "Notification Access";
                //RestrictedCapabilities
                case "enterpriseAuthentication":
                    return "Enterprise Authentication";
                case "enterpriseDataPolicy":
                    return "Enterprise Data Policies";
                case "sharedUserCertificates":
                    return "Shared Certificates";
                case "documentsLibrary":
                    return "Documents Library";
                case "appCaptureSettings":
                    return "App Capture Settings";
                case "cellularDeviceControl":
                    return "Cellular Control";
                case "cellularDeviceIdentity":
                    return "Cellular Identity";
                case "cellularMessaging":
                    return "Cellular Messaging Access";
                case "chatSystem":
                    return "Chat (System)";
                case "smsSend":
                    return "Send/Recieve SMS";
                case "deviceUnlock":
                    return "Device Unlocking";
                case "dualSimTiles":
                    return "Dual Sim Management";
                case "enterpriseDeviceLockdown":
                    return "Device Lockdown";
                case "inputInjectionBrokered":
                    return "Input Injection";
                case "inputObservation":
                    return "Input Observation";
                case "inputSuppression":
                    return "Input Suppression";
                case "networkingVpnProvider":
                    return "VPN Management";
                case "packageManagement":
                    return "Package Management";
                case "packageQuery":
                    return "Package Query";


                case "screenDuplication":
                    return "Screen Projection";
                case "userPrincipalName":
                    return "UPN Info Access";
                case "walletSystem":
                    return "Wallet Access";
                case "locationHistory":
                    return "Location History";
                case "confirmAppClose":
                    return "Confirm App Closing";
                case "emailSystem":
                    return "Email Access (System)";
                case "email":
                    return "Email Access";


                case "userDataSystem":
                    return "User Data Access";
                case "previewStore":
                    return "App SKU Management";
                case "firstSignInSettings":
                    return "User Settings";
                case "teamEditionExperience":
                    return "Windows Teams Session *Internal API";
                case "remotePassportAuthentication":
                    return "Remote Credentials";
                case "previewUiComposition":
                    return "Preview UI APIs";
                case "secureAssessment":
                    return "Secure Assessment Management";
                case "networkConnectionManagerProvisioning":
                    return "Network Provisioning";
                case "networkDataPlanProvisioning":
                    return "Data Usage Provisioning";
                case "slapiQueryLicenseValue":
                    return "Query Software Licenses";
                case "extendedBackgroundTaskTime":
                    return "Extended Background Execution";
                case "extendedExecutionBackgroundAudio":
                    return "Background Audio";
                case "extendedExecutionCritical":
                    return "Extended Execution (Critical)";

                case "extendedExecutionUnconstrained":
                    return "Extended Execution (Unconstrained)";



                case "packagePolicySystem":
                    return "App System Policies";
                case "gameList":
                    return "Retrieve Installed Games";
                case "xboxAccessoryManagement":
                    return "Xbox Accessories";
                case "cortanaSpeechAccessory":
                    return "Cortana";
                case "accessoryManager":
                    return "Accessory Management";
                case "interopServices":
                    return "Driver Access";
                case "inputForegroundObservation":
                    return "Foreground Input Observation";
                case "oemDeployment":
                    return "OEM Deployment";
                case "appLicensing":
                    return "App Licensing";
                case "storeLicenseManagement":
                    return "Store Licensing Management";
                case "locationSystem":
                    return "Location Management";
                case "userDataAccountsProvider":
                    return "User Accounts Access";
                case "previewPenWorkspace":
                    return "Pen Capabilities";
                case "secondaryAuthenticationFactor":
                    return "Companion Authentication";

                case "userSystemId":
                    return "View System IDs";
                case "targetedContent":
                    return "Targeted Content";
                case "uiAutomation":
                    return "UI Automation";
                case "gameBarServices":
                    return "Game Bar Services";

                case "audioDeviceConfiguration":
                    return "Audio Hardware";
                case "backgroundMediaRecording":
                    return "Background Recording";

                case "startScreenManagement":
                    return "Start Screen Management";
                case "cortanaPermissions":
                    return "Cortana Permissions";
                case "allAppMods":
                    return "Mods Management";
                case "expandedResources":
                    return "Game Mode Access";
                case "protectedApp":
                    return "Protected App";
                case "gameMonitor":
                    return "Game Monitoring";
                case "appDiagnostics":
                    return "Application Diagnostics";
                case "devicePortalProvider":
                    return "Device Portal Provider";
                case "enterpriseCloudSSO":
                    return "Enterprise SSO";
                case "backgroundVoIP":
                    return "Background VoIP";
                case "oneProcessVoIP":
                    return "Exclusing VoIP";
                case "developmentModeNetwork":
                    return "Access Network Paths";
                case "broadFileSystemAccess":
                    return "Filesystem Access";
                case "smbios":
                    return "SMBIOS";
                case "runFullTrust":
                    return "Full Trust";
                case "allowElevation":
                    return "Allow Elevation";
                case "teamEditionDeviceCredential":
                    return "Windows Teams Credentials";
                case "teamEditionView":
                    return "Windows Teams UI";
                case "cameraProcessingExtension":
                    return "Camera Image Processing";
                case "networkDataUsageManagement":
                    return "Network Data Usage";
                case "phoneLineTransportManagement":
                    return "Phone Line Connectivity";
                case "unvirtualizedResources":
                    return "Unvirtualized Resources";
                case "modifiableApp":
                    return "Modifiable App";

                case "customInstallActions":
                    return "Custom Install Actions";
                case "packagedServices":
                    return "Service Installation";
                case "localSystemServices":
                    return "Local System Services";
                case "backgroundSpatialPerception":
                    return "Background Spatial Perception";
                default:
                    return cap;
            }
        }

        private void DevicesListView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {



            StackPanel Panel = new StackPanel();

            TextBlock nameHeader = new TextBlock();
            nameHeader.Margin = new Thickness(5, 5, 5, 5);
            nameHeader.Text = $"Quit application?";
            nameHeader.HorizontalAlignment = HorizontalAlignment.Stretch;
            Panel.Children.Add(nameHeader);
            ContentDialog dialog = new ContentDialog();
            dialog.Content = Panel;
            dialog.VerticalContentAlignment = VerticalAlignment.Center;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Confirm";
            dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick4;
            dialog.SecondaryButtonText = "Cancel";
            dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick4;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {


            }
        }

        private async void Dialog_PrimaryButtonClick4(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            StackPanel Panel = new StackPanel();

            TextBlock nameHeader = new TextBlock();
            nameHeader.Margin = new Thickness(5, 5, 5, 5);
            nameHeader.Text = $"Quit application?";
            nameHeader.HorizontalAlignment = HorizontalAlignment.Stretch;
            ProgressRing exitRing = new ProgressRing();
            exitRing.Width = 50;
            exitRing.Height = 50;
            exitRing.IsActive = true;
            Panel.Children.Add(exitRing);
            Panel.Children.Add(nameHeader);
            ContentDialog dialog = new ContentDialog();
            dialog.Content = Panel;
            dialog.VerticalContentAlignment = VerticalAlignment.Center;
            dialog.IsPrimaryButtonEnabled = false;
            dialog.IsSecondaryButtonEnabled = false;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await Task.Delay(2000);
                Windows.UI.Xaml.Application.Current.Exit();

            }

        }

        private void Dialog_SecondaryButtonClick4(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            return;
        }

        private void MainPivot_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();

        }

        private void ApplicationPivot_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }

        private void FilesPivot_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }

        private void PerfPivot_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }

        private void DevicePivot_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }

        private void WifiPivotItem_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }

        private void CrashDumpsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }

        private void CrashDumpsPivot_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectionNoticeVisible = Visibility.Collapsed;
            this.Bindings.Update();
        }


        private void TestFunction()
        {

        }


    }
}

