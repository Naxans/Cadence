using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.UI.Popups;
using System.Diagnostics;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using System.Text;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.Storage;
using System.Threading.Tasks;
using System.Runtime.Serialization;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Cadence
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        object StorageBluetoothAddress;

        //private bool FirstConnectionError = false;

        private BluetoothLEDevice cadence;
        private ObservableCollection<BluetoothInformationWrapper> informationOfFoundDevices;
        private GattCharacteristicWrapper selectedCharacteristicWrapper;

        private DataReader reader;

        private DispatcherTimer TimerConnectToSensor = new DispatcherTimer(); //timer connect sensor

        private ObservableCollection<GattServiceWrapper> observableServices;

        ObservableCollection<GattCharacteristicWrapper> observableCharacteristics;
        private BluetoothInformationWrapper selectedDeviceInfoWrapper;

        private DeviceWatcher deviceWatcher;

        string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };


        //!!!!!de sensoren mogen niet paired zijn!!!!
        public MainPage()
        {
            StorageBluetoothAddress = localSettings.Values["StorageBluetoothAddress"]; //laden van de info 
            if (StorageBluetoothAddress == null) //als de parameter nog niet is aangemaakt (spel wordt voor de eerste keer gestart) wordt die hier aangemaakt enopgeslagen
            {
                localSettings.Values["StorageBluetoothAddress"] = "";
                StorageBluetoothAddress = localSettings.Values["StorageBluetoothAddress"];
            }

            this.InitializeComponent();

            informationOfFoundDevices = new ObservableCollection<BluetoothInformationWrapper>();
            deviceListView.ItemsSource = informationOfFoundDevices;

            // Query for extra properties you want returned
            //string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            ////BluetoothLEDevice.GetDeviceSelectorFromPairingState(true) dwz dat de sensor moet paired zijn anders verschijnt deze niet in de lijst van bluetooth devices
            ////BluetoothLEDevice.GetDeviceSelectorFromPairingState(false) dwz dat de sensor non paired zijn om in de lijst te verschijnen
            ////de keuze is enkel paired devices, dan zijn enkel de ble devices zichtbaar die gekend zijn door de computer. (je kan ook 1 lijst maken van paired en non paired ble devices)
            ////requestedProperties is a set of properties that should be available for a device.
            ////the DeviceInformationKind flag. In our case it should be AssociationEndpoint that is true for all Bluetooth LE devices that advertise their interface
            //deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true), requestedProperties, DeviceInformationKind.AssociationEndpoint);


            ////This method accepts several parameters.Using the first one we can provide a filter that can help us list just needed devices.
            ////It’s the same Advanced Query Syntax string that we used before, but in this case, we created it from scratch rather than using a predefined one.
            ////It’s a good idea to show different approaches and I used a filter that helps me find exactly devices that contain Wahoo string in their names. 
            ////To implement this filter I used the System.ItemNameDisplay property. 
            ////Because all my dev kit is available as Wahoo by default, my application will show just my device.Of course, we should not hardcode any names and it’s better to use less restrictive filter
            ////The second parameter is a set of properties that should be available for a device. In this case we requested DeviceAddress and IsConnected properties, but I used it just for the demo. I assume that you will not able to find many different devices with Wahoo name around, so, you can simply remove this parameter.
            ////Finally, we have to pass the DeviceInformationKind flag. In our case it should be AssociationEndpoint that is true for all Bluetooth LE devices that advertise their interface
            ////deviceWatcher = DeviceInformation.CreateWatcher("System.ItemNameDisplay:~~\"Wahoo\"", new string[] {"System.Devices.Aep.DeviceAddress","System.Devices.Aep.IsConnected" },DeviceInformationKind.AssociationEndpoint);

            // Query for extra properties you want returned
            //string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            //DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false), requestedProperties, DeviceInformationKind.AssociationEndpoint);

            ////<*****lijst maken van bleutooth devices*****>
            //// Register event handlers before starting the watcher.
            //// Added, Updated and Removed are required to get all nearby devices
            //deviceWatcher.Added += DeviceWatcher_Added;//create list of all bleutooth devices
            //deviceWatcher.Updated += DeviceWatcher_Updated;
            //deviceWatcher.Removed += DeviceWatcher_Removed;
            //// EnumerationCompleted and Stopped are optional to implement.
            //deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            //deviceWatcher.Stopped += DeviceWatcher_Stopped;
            //// Start the watcher.
            //deviceWatcher.Start();

            TimerConnectToSensor.Interval = TimeSpan.FromMilliseconds(1000);
            TimerConnectToSensor.Tick += TimerConnectToSensor_Tick;
            TimerConnectToSensor.Stop();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e) //OnNavigatedTo is invoked when the Page is loaded and becomes the current source of a parent Frame.
        {
            var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            appView.Title = "Test Wahoo ";

            //informationOfFoundDevices = new ObservableCollection<BluetoothInformationWrapper>();
            //deviceListView.ItemsSource = informationOfFoundDevices;
            //// Query for extra properties you want returned
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            // deviceWatcher = DeviceInformation.CreateWatcher("System.ItemNameDisplay:~~\"Wahoo\"", new string[] {"System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" }, DeviceInformationKind.AssociationEndpoint);
            deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false), requestedProperties, DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;
            deviceWatcher.Start();
            Debug.WriteLine(deviceWatcher.Status);
            base.OnNavigatedTo(e);
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e) //OnNavigatedFrom is invoked immediately after the Page is unloaded and is no longer the current source of a parent Frame.
        {
            deviceWatcher.Stop();
            base.OnNavigatedFrom(e);
        }
        GattDeviceServicesResult result;
        GattDeviceService service2;
        GattCharacteristicsResult result2;
        private async void TimerConnectToSensor_Tick(object sender, object e)
        {
            //cadence = await BluetoothLEDevice.FromIdAsync(selectedDeviceInfoWrapper.DeviceInformation.Id);
           // result = await cadence.GetGattServicesAsync(); //ik moet de services hier gaan halen of anders geraakt de sensor nooit geconnecteerd
            //await GetInformationSensor();
          //  if (cadence == null || cadence.ConnectionStatus.ToString() == "Disconnected")
                if (cadence == null )
                {
                connectionStatusTextBlock.Text = "Sensor: Couldn't establish connection";
                connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }
            else
            {
                connectionStatusTextBlock.Text = "Sensor: Connection established!";
                connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                TimerConnectToSensor.Stop();
            }
            await  GetInformationSensor();
            await  GetValuesSensor();
            ErrorTextBlock.Text = "Sensor is ready to work!";
            ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
            localSettings.Values["StorageBluetoothAddress"] = selectedDeviceInfoWrapper.DeviceInformation.Id; //opslaan op de hardeschijf
            //<begincode*****get the value of the cadence counter and te time between two measurements*****>
            cadence.ConnectionStatusChanged += Cadence_ConnectionStatusChanged;
            //<endcode*****get the value of the cadence counter and te time between two measurements*****>
        }
        private async void deviceListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                ErrorTextBlock.Text = "";
                connectionStatusTextBlock.Text = "";
                DeviceNameTextBlock.Text = "";
                BTAddresTextBlock.Text = "";
                FirmwareTextBlock.Text = "";
                HardwareTextBlock.Text = "";
                readingsTextBlock.Text = "";
                subscriptionStatusTextBlock.Text = "";
                subscriptionResultTextBlock.Text = "";
                subscriptionResultTextBlock2.Text = "";

                selectedDeviceInfoWrapper = (BluetoothInformationWrapper)e.ClickedItem;

                //for (int ii = 0; ii < ApprovedSensor.Length; ii += 1) //is eigenlijk niet goed omdat ik nu enkel Wahoo CADENCE BC14 sensor toelaat, eigelijk als de services en Characteristic die ik nodig aanwezig zijn is het type sensor niet belangrijk 
                //{


                //if (selectedDeviceInfoWrapper.DeviceInformation.Name == ApprovedSensor[ii] || StorageBluetoothAddress.ToString() == "")
                //if (selectedDeviceInfoWrapper.DeviceInformation.Id == StorageBluetoothAddress.ToString() || StorageBluetoothAddress.ToString() == "")
                //{
                //localSettings.Values["StorageBluetoothAddress"] = selectedDeviceInfoWrapper.DeviceInformation.Id; //opslaan op de hardeschijf
               
                cadence = await BluetoothLEDevice.FromIdAsync(selectedDeviceInfoWrapper.DeviceInformation.Id);
                //result = await cadence.GetGattServicesAsync();//ik moet de service hier gaan halen of anders geraakt de sensor nooit geconnecteerd
                //await GetInformationSensor();
                if (selectedDeviceInfoWrapper != null)
                {

                    //await SaveSensorInfo();
                    // Note: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.

                    //Debug.WriteLine("sensor is " + cadence.ConnectionStatus.ToString());
                    //await SaveSensorInfo();
                    //if (cadence == null || cadence.ConnectionStatus.ToString() == "Disconnected")
                        if (cadence == null)
                        {
                        //deviceWatcher.Stop();

                        connectionStatusTextBlock.Text = "Sensor: Couldn't establish connection";
                        connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        //FirstConnectionError = true;

                        // deviceWatcher.Start();

                        TimerConnectToSensor.Start();
                        return;
                    }
                    else
                    {
                        connectionStatusTextBlock.Text = "Sensor: Connection established!";
                        connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    }
                    await GetInformationSensor();
                    //Debug.WriteLine("2 sensor is " + cadence.ConnectionStatus.ToString());
                    await  GetValuesSensor();
                    ErrorTextBlock.Text = "Sensor is ready to work!";
                    //Debug.WriteLine("3 sensor is " + cadence.ConnectionStatus.ToString());
                    ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                    localSettings.Values["StorageBluetoothAddress"] = selectedDeviceInfoWrapper.DeviceInformation.Id; //opslaan op de hardeschijf
                    //<begincode*****get the value of the cadence counter and te time between two measurements*****>
                        cadence.ConnectionStatusChanged += Cadence_ConnectionStatusChanged;
                    //<endcode*****get the value of the cadence counter and te time between two measurements*****>
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ErrorTextBlock.Text = "Error sensor!";
                ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("DeviceWatcher_Stopped");
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("DeviceWatcher_EnumerationCompleted");
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine("DeviceWatcher_Removed");
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine("DeviceWatcher_Updated");
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            //in realtime wordt er gezocht naar bluetooth device die non paired zijn
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                //informationOfFoundDevices.Clear();
                informationOfFoundDevices.Add(new BluetoothInformationWrapper(args));
            });
        }

        private async void Cadence_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            try
            { 
            Debug.WriteLine(cadence.ConnectionStatus.ToString() == "Disconnected" ? "disconnected" : "connected");
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (cadence.ConnectionStatus.ToString() == "Disconnected")
                {
                   // deviceWatcher.Stop();
                    connectionStatusTextBlock.Text = "Sensor: Connection lost!";
                    connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                  //  deviceWatcher.Start();
                    TimerConnectToSensor.Start();
                }
            });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault Cadence_ConnectionStatusChanged " + ex);

            }
        }


        int valueprevious;
        int valuetimeprevious;
        int valuetime;
        int secondtestnull = 0;
        string cadenceCounter;
        string cadenceTime;
        float Rotationsresult;
        int value;
        byte[] inputx;
        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) //event handler used to process characteristic value change notification and indication events sent by a Bluetooth LE device.
        {
            try
            { 
                reader = DataReader.FromBuffer(args.CharacteristicValue);
                inputx = new byte[reader.UnconsumedBufferLength];
                
                reader.ReadBytes(inputx);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        StringBuilder hex = new StringBuilder(inputx.Length * 2);
                        foreach (byte b in inputx)
                        hex.AppendFormat("{0:x2}", b);
                        cadenceCounter = hex[4].ToString() + hex[5].ToString() + hex[2].ToString() + hex[3].ToString(); //formaat is little endian betekent dat we de volgorde moeten veranderen
                        value = Convert.ToInt32(cadenceCounter, 16); //converteren van hex naar decimaal
                        subscriptionResultTextBlock.Text = "rotations counter = " + value.ToString();
                        cadenceTime = hex[8].ToString() + hex[9].ToString() + hex[6].ToString() + hex[7].ToString();
                        valuetime = Convert.ToInt32(cadenceTime, 16);
                        //Debug.WriteLine(value + " " + valueprevious + " " + valuetime + " " + valuetimeprevious);

                        //calculation rotations per minute
                        int deltavalue = value - valueprevious;
                        int deltavaluetime = valuetime - valuetimeprevious;
                        //  Debug.WriteLine(deltavalue + " " + deltavaluetime);
                        //if (deltavaluetime > 0 && deltavalue > 0 && value > valueprevious && valuetime > valuetimeprevious)
                        //{
                        if (deltavaluetime > 0 && deltavalue > 0)
                        {
                            Rotationsresult = deltavalue * 1024f / deltavaluetime; //aantal toeren per tijdeenheid
                                                                                   // x 60 gives amount of rotations per minute
                            Rotationsresult = (int)(Rotationsresult * 60);
                            subscriptionResultTextBlock2.Text = "rotations per minute = " + Rotationsresult.ToString();
                            secondtestnull = 0; //soms ook al zijn we aan het fietsen krijgen we de waarden alsof we niet meer trappen, resultaat is dat 1 x een nul verschijnt bij het aantal toeren per minut. vandaar dat we dan pas een nul laten zien als we twee keer de info krijgen dat er niet meer wordt gefietst

                        }
                        else if (deltavalue <= 0 && deltavaluetime <= 0 && secondtestnull > 2) //als er niet wordt gefietst dan is het aantal toeren/minut = 0
                        {
                            subscriptionResultTextBlock2.Text = "rotations per minute = 0";
                            secondtestnull = 0;
                        }
                        else if (deltavalue <= 0 && deltavaluetime <= 0 && value > 0) // de waarde value moet > dan 0 of negatief om te vermijden dat secondtestnull = +1 
                        {
                            secondtestnull = secondtestnull + 1;
                        }
                        //Debug.WriteLine("cadencecounter hex = " + cadenceCounter);
                        //Debug.WriteLine("cadencecounter dec = " + value);
                        //Debug.WriteLine("cadencetime hex = " + cadenceTime);
                        //Debug.WriteLine("valuetime = " + valuetime);
                        //Debug.WriteLine("valuetimeprevious = " + valuetimeprevious);
                        valuetimeprevious = valuetime;
                        valueprevious = value;

                    });
                    //}

            }
            catch (Exception ex)
            {
                //Debug.WriteLine("cadencecounter hex = " + cadenceCounter);
                //Debug.WriteLine("cadencecounter dec = " + value);
                //Debug.WriteLine("cadencetime hex = " + cadenceTime);
                //Debug.WriteLine("valuetime = " + valuetime);
                //Debug.WriteLine("valuetimeprevious = " + valuetimeprevious);

                //Debug.WriteLine("cadencetime dec = " + result);
                //Debug.WriteLine("string hex = " + inputx[0] +":" + inputx[1] + ":" + inputx[2] + ":" + inputx[3] + ":" + inputx[4]);
                Debug.WriteLine("Fault Characteristic_ValueChanged " + ex);


            }
        }
        private async Task GetInformationSensor()
        {
            //if (FirstConnectionError == true) //als er een op een normale manier is opgestart en we stoppen dan met fietsen danwordt de connectie verbroken. Als we dan opnieuw beginnen met fietsen geeft onderstaande code instabiele counter en toeren per minute
            //{
            string BluetoothAddressHex = cadence.BluetoothAddress.ToString("x");  // convert decimal to hex
            for (int i = 2; i < BluetoothAddressHex.Length; i += 2) // om het leesbaar te houden plaatsen we om de 2 hex getallen een :
            {
                BluetoothAddressHex = BluetoothAddressHex.Insert(i, ":");
                i = i + 1;
            }
            BTAddresTextBlock.Text = "Bluetooth address = " + BluetoothAddressHex;
            //Debug.WriteLine("sensor is " + cadence.ConnectionStatus.ToString());
            if (cadence != null)
            {
                //<begincode***** get all services and putt them in the list of observableServices*****>
                 result = await cadence.GetGattServicesAsync();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    //Debug.WriteLine("sensor is " + cadence.ConnectionStatus.ToString());
                    List<GattDeviceService> services = result.Services.ToList();
                    observableServices = new ObservableCollection<GattServiceWrapper>();
                    //observableServices.Clear();
                    foreach (GattDeviceService service in services)
                    {
                        observableServices.Add(new GattServiceWrapper(service));
                    }
                }
                else
                {
                    MessageDialog dialog = new MessageDialog("Kon services niet uitlezen, beweeg aub met sensor en probeer opnieuw");
                }
                //<endcode***** get all services and putt them in the list of observableServices*****>
                //<begincode***** select characteristic GenericAccess level and putt them in the list of observableCharacteristics*****>
                var wrapper2 = observableServices.Single(i => i.Service.Uuid.ToString() == "00001800-0000-1000-8000-00805f9b34fb");
                service2 = wrapper2.Service;
                result2 = await service2.GetCharacteristicsAsync();
                if (result2.Status == GattCommunicationStatus.Success)
                {
                    observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
                    //observableCharacteristics.Clear();
                    foreach (GattCharacteristic characteristic in result2.Characteristics)
                    {
                        observableCharacteristics.Add(new GattCharacteristicWrapper(characteristic));
                    }
                    //characteristicsListView.ItemsSource = observableCharacteristics;
                }
                else
                {
                    //hier nog iets doen?
                }
                //<endcode***** select characteristic GenericAccess and putt them in the list of observableCharacteristics*****>
                //<begincode***** select characteristic device name*****>
                selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.Uuid.ToString() == "00002a00-0000-1000-8000-00805f9b34fb");
                //<endcode***** select characteristic device name*****>
                //<begincode*****read device name*****>
                GattReadResult result3 = await selectedCharacteristicWrapper.Characteristic.ReadValueAsync();
                if (result3.Status == GattCommunicationStatus.Success)
                {
                    reader = DataReader.FromBuffer(result3.Value);
                    byte[] input = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(input);
                    string utf8result = System.Text.Encoding.UTF8.GetString(input);
                    DeviceNameTextBlock.Text = "Device name = " + utf8result;
                }
                else
                {
                    //hier nog iets doen?
                }
                //<endcode*****read Firmware revision*****>

                //<begincode***** select characteristic Device information and putt them in the list of observableCharacteristics*****>
                var wrapper3 = observableServices.Single(i => i.Service.Uuid.ToString() == "0000180a-0000-1000-8000-00805f9b34fb");
                service2 = wrapper3.Service;
                result2 = await service2.GetCharacteristicsAsync();
                if (result2.Status == GattCommunicationStatus.Success)
                {
                    // observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
                    observableCharacteristics.Clear();
                    foreach (GattCharacteristic characteristic in result2.Characteristics)
                    {
                        observableCharacteristics.Add(new GattCharacteristicWrapper(characteristic));
                    }
                }
                else
                {
                    //hier nog iets doen?
                }
                //<endcode***** select characteristic device information and putt them in the list of observableCharacteristics*****>
                //<begincode***** select characteristic Firmware*****>
                selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.Uuid.ToString() == "00002a26-0000-1000-8000-00805f9b34fb");
                //<endcode***** select characteristic Firmware*****>
                //<begincode*****read firmware*****>
                result3 = await selectedCharacteristicWrapper.Characteristic.ReadValueAsync();
                if (result3.Status == GattCommunicationStatus.Success)
                {
                    reader = DataReader.FromBuffer(result3.Value);
                    byte[] input = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(input);
                    string utf8result = System.Text.Encoding.UTF8.GetString(input);
                    FirmwareTextBlock.Text = "Firmware Revision = " + utf8result;
                }
                else
                {
                    //hier nog iets doen?
                }
                //<endcode*****read firmware*****>
                //<begincode***** select characteristic Hardware*****>
                selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.Uuid.ToString() == "00002a27-0000-1000-8000-00805f9b34fb");
                //<endcode***** select characteristic Hardware*****>
                //<begincode*****read hardware*****>
                result3 = await selectedCharacteristicWrapper.Characteristic.ReadValueAsync();
                if (result3.Status == GattCommunicationStatus.Success)
                {
                    reader = DataReader.FromBuffer(result3.Value);
                    byte[] input = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(input);
                    //string utf8result = System.Text.Encoding.UTF8.GetString(input);
                    //HardwareTextBlock.Text = "Hardware Revision = " + utf8result;
                    StringBuilder hex = new StringBuilder(input.Length * 2);
                    foreach (byte b in input)
                        hex.AppendFormat("{0:x2}", b);
                    int Hardwarevalue = Convert.ToInt32(hex.ToString(), 16);
                    HardwareTextBlock.Text = "Hardware Revision = " + Hardwarevalue.ToString();
                }
                else
                {
                    //hier nog iets doen?
                }
                //<endcode*****read hardware*****>

                //<begincode***** select characteristic battery level and putt them in the list of observableCharacteristics*****>
                var wrapper4 = observableServices.Single(i => i.Service.Uuid.ToString() == "0000180f-0000-1000-8000-00805f9b34fb");
                service2 = wrapper4.Service;
                result2 = await service2.GetCharacteristicsAsync();
                if (result2.Status == GattCommunicationStatus.Success)
                {
                    //observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
                    observableCharacteristics.Clear();
                    foreach (GattCharacteristic characteristic in result2.Characteristics)
                    {
                        observableCharacteristics.Add(new GattCharacteristicWrapper(characteristic));
                    }
                }
                else
                {
                    //hier nog iets doen?
                }
                //<endcode***** select characteristic battery level and putt them in the list of observableCharacteristics*****>
                //<begincode***** select characteristic battery level*****>
                selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.Uuid.ToString() == "00002a19-0000-1000-8000-00805f9b34fb");
                //<endcode***** select characteristic battery level*****>
                //<begincode*****read battery level*****>
                result3 = await selectedCharacteristicWrapper.Characteristic.ReadValueAsync();
                if (result3.Status == GattCommunicationStatus.Success)
                {
                    reader = DataReader.FromBuffer(result3.Value);
                    byte[] input = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(input);
                    StringBuilder hex = new StringBuilder(input.Length * 2);
                    foreach (byte b in input)
                        hex.AppendFormat("{0:x2}", b);
                    int Batteryvalue = Convert.ToInt32(hex.ToString(), 16);
                    readingsTextBlock.Text = "Battery level = " + Batteryvalue.ToString() + "%";
                    if (Batteryvalue < 60)
                    {
                        readingsTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        readingsTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }
                else
                {
                    //hier nog iets doen?
                }
                //<endcode*****read battery level*****>
                //FirstConnectionError = false;
            }
            //}
        }

        private async Task GetValuesSensor()
        {
            //<begincode***** select characteristic Measurement and putt them in the list of observableCharacteristics*****>
            var wrapper5 = observableServices.Single(i => i.Service.Uuid.ToString() == "00001816-0000-1000-8000-00805f9b34fb");
            service2 = wrapper5.Service;
            result2 = await service2.GetCharacteristicsAsync();
            if (result2.Status == GattCommunicationStatus.Success)
            {
                //observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
                observableCharacteristics.Clear();
                foreach (GattCharacteristic characteristic in result2.Characteristics)
                {
                    observableCharacteristics.Add(new GattCharacteristicWrapper(characteristic));
                }
            }
            else
            {
                //hier nog iets doen?
            }
            //<endcode***** select characteristic device Measurement and putt them in the list of observableCharacteristics*****>
            //<begincode***** select characteristic measurement*****>
            selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.Uuid.ToString() == "00002a5b-0000-1000-8000-00805f9b34fb");
            //<endcode***** select characteristic measurement*****>
            //<begincode***** subscribe to measurement*****>
            GattCharacteristic characteristic2 = selectedCharacteristicWrapper.Characteristic;
            GattCharacteristicProperties properties = characteristic2.CharacteristicProperties;
            if (properties.HasFlag(GattCharacteristicProperties.Notify))
            {
                GattCommunicationStatus status = await characteristic2.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                {
                    // Server has been informed of clients interest.
                    subscriptionStatusTextBlock.Text = "Measurement Subscribed to cadence sensor";
                    subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    characteristic2.ValueChanged += Characteristic_ValueChanged;
                }
                else
                {
                    subscriptionStatusTextBlock.Text = "Measurement cadence sensor " + status.ToString();
                    subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            else if (properties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                GattCommunicationStatus status = await characteristic2.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                if (status == GattCommunicationStatus.Success)
                {
                    subscriptionStatusTextBlock.Text = "Measurement Subscribed to cadence sensor";
                    subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    characteristic2.ValueChanged += Characteristic_ValueChanged;
                }
                else
                {
                    subscriptionStatusTextBlock.Text = "Measurement cadence sensor " + status.ToString();
                    subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            //<endcode***** subscribe to measurement*****>
        }


    }
}
