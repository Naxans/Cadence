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

        private bool NoError = true;
        private bool SubscribeMeasurement = false;
        //private bool StartOnce = false;
        private int valueprevious;
        private int valuetimeprevious;
        private int valuetime;
        private int secondtestnull = 0;
        private int value;
        private float Rotationsresult;

        //StringBuilder hex;
        private string cadenceCounter;
        private string cadenceTime;
        private string Temp0;
        private string Temp1;
        private string Temp2;
        private string Temp3;
        private string Temp4;

        private BluetoothLEDevice cadence;
        private GattCharacteristicWrapper selectedCharacteristicWrapper;
        private GattDeviceServicesResult result;
        private GattDeviceService service2;
        private GattCharacteristicsResult result2;
        private GattCharacteristic characteristic2;

        private DataReader reader;

        private DispatcherTimer TimerConnectToSensor = new DispatcherTimer(); //timer connect sensor

        private ObservableCollection<BluetoothInformationWrapper> informationOfFoundDevices;
        private ObservableCollection<GattServiceWrapper> observableServices;
        private ObservableCollection<GattCharacteristicWrapper> observableCharacteristics;
        private BluetoothInformationWrapper selectedDeviceInfoWrapper;

        private DeviceWatcher deviceWatcher;

        private string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
        private byte[] inputx;

        //!!!!!the sensors must be non-paired!!!!
        public MainPage()
        {
            try
            {
                StorageBluetoothAddress = localSettings.Values["StorageBluetoothAddress"]; //laden van de info 
                if (StorageBluetoothAddress == null) //als de parameter nog niet is aangemaakt (spel wordt voor de eerste keer gestart) wordt die hier aangemaakt enopgeslagen
                {
                    localSettings.Values["StorageBluetoothAddress"] = "";
                    StorageBluetoothAddress = localSettings.Values["StorageBluetoothAddress"];
                }
                //StorageBluetoothAddress = ""; ////only for testing
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

                TimerConnectToSensor.Interval = TimeSpan.FromMilliseconds(1000);
                TimerConnectToSensor.Tick += TimerConnectToSensor_Tick;
                TimerConnectToSensor.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault Mainpage " + ex);
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e) //OnNavigatedTo is invoked when the Page is loaded and becomes the current source of a parent Frame.
        {
            try
            {
                var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                appView.Title = "Test Wahoo ";
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
            catch (Exception ex)
            {
                Debug.WriteLine("Fault OnNavigatedTo " + ex);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) //OnNavigatedFrom This event will be fired -invoked- when you're leaving the page .. and before navigating from it ..
        {
            deviceWatcher.Stop();
            base.OnNavigatedFrom(e);
        }

        private async void TimerConnectToSensor_Tick(object sender, object e)
        {
            try
            {
                //NoError = true;
                TimerConnectToSensor.Stop();
                await GetAllServices();
                if (NoError == false)
                {
                    ErrorTextBlock.Text = "Sensor: Searching...";
                    //ErrorTextBlock.Text = "Error sensor!";
                    //ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                    //characteristic2.ValueChanged -= Characteristic_ValueChanged;
                    NoError = true;
                    TimerConnectToSensor.Start();
                    return;
                }


                await GetValuesSensor();
                //await  GetInformationSensor();
                if (NoError == false)
                {
                    ErrorTextBlock.Text = "Sensor: Error!";
                    ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    if (SubscribeMeasurement == true)
                    {
                        characteristic2.ValueChanged -= Characteristic_ValueChanged;
                    }
                    TimerConnectToSensor.Start();
                    return;
                }
                //await  GetValuesSensor();
                await GetInformationSensor();
                //Debug.WriteLine("Getinformationsensor timer");
                if (NoError == false)
                {
                    ErrorTextBlock.Text = "Sensor: Error!";
                    ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    if (SubscribeMeasurement == true)
                    {
                        characteristic2.ValueChanged -= Characteristic_ValueChanged;
                    }
                    TimerConnectToSensor.Start();
                    return;
                }
                ErrorTextBlock.Text = "Sensor: Found.";
                ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                //<begincode*****get the value of the cadence counter and te time between two measurements*****>
                cadence.ConnectionStatusChanged += Cadence_ConnectionStatusChanged;
                //<endcode*****get the value of the cadence counter and te time between two measurements*****>

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault TimerConnectToSensor_Tick " + ex);
            }
        }
        private async void deviceListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                TimerConnectToSensor.Stop();

                //I have to do 7 rows of code below if I choose another sensor in the list, to avoid the fact that the measurement character string continues to subscribe and therefore gets the value rotations of this but also from the previous sensor
                if (cadence != null && characteristic2 != null)
                {
                    //unsubscribe measurements characterstic2, is necessary to disconnect the existing measurement from the sensor, otherwise when re-opening the app or you choose another sensor we get an unstable measurement
                    await characteristic2.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                    characteristic2.Service.Dispose();
                    cadence.Dispose();
                    cadence = null;
                }
                ErrorTextBlock.Text = "Sensor: Searching...";
                ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                connectionStatusTextBlock.Text = "";
                DeviceNameTextBlock.Text = "";
                BTAddresTextBlock.Text = "";
                FirmwareTextBlock.Text = "";
                HardwareTextBlock.Text = "";
                readingsTextBlock.Text = "";
                subscriptionStatusTextBlock.Text = "";
                subscriptionResultTextBlock.Text = "";
                subscriptionResultTextBlock2.Text = "";
                NoError = true;
                SubscribeMeasurement = false;

                selectedDeviceInfoWrapper = (BluetoothInformationWrapper)e.ClickedItem;

                cadence = await BluetoothLEDevice.FromIdAsync(selectedDeviceInfoWrapper.DeviceInformation.Id);
                //await GetInformationSensor();
                if (selectedDeviceInfoWrapper != null)
                {
                    await GetAllServices();
                    if (NoError == false)
                    {
                        ErrorTextBlock.Text = "Sensor: Error!";
                        ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        //characteristic2.ValueChanged -= Characteristic_ValueChanged;
                        TimerConnectToSensor.Start();
                        return;
                    }
                    await GetValuesSensor();
                    //await GetInformationSensor();
                    if (NoError == false)
                    {
                        ErrorTextBlock.Text = "Sensor: Error!";
                        ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        //characteristic2.ValueChanged -= Characteristic_ValueChanged;
                        TimerConnectToSensor.Start();
                        return;
                    }
                    //Debug.WriteLine("2 sensor is " + cadence.ConnectionStatus.ToString());
                    await GetInformationSensor();
                    //Debug.WriteLine("Getinformationsensor itemclick");
                    //await GetValuesSensor();
                    if (NoError == false)
                    {
                        ErrorTextBlock.Text = "Sensor: Error!";
                        ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        //characteristic2.ValueChanged -= Characteristic_ValueChanged;
                        TimerConnectToSensor.Start();
                        return;
                    }
                    ErrorTextBlock.Text = "Sensor: Found.";
                    ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                    localSettings.Values["StorageBluetoothAddress"] = selectedDeviceInfoWrapper.DeviceInformation.Id; //save to the hard disk
                    //<begincode*****get the value of the cadence counter and te time between two measurements*****>
                    cadence.ConnectionStatusChanged += Cadence_ConnectionStatusChanged;
                    //<endcode*****get the value of the cadence counter and te time between two measurements*****>
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ErrorTextBlock.Text = "Sensor: Error!";
                ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
        //private async void AutoStart(int ii)
        private async void AutoStart()
        {
            try
            {
                ErrorTextBlock.Text = "Sensor: Searching...";
                ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                connectionStatusTextBlock.Text = "";
                DeviceNameTextBlock.Text = "";
                BTAddresTextBlock.Text = "";
                FirmwareTextBlock.Text = "";
                HardwareTextBlock.Text = "";
                readingsTextBlock.Text = "";
                subscriptionStatusTextBlock.Text = "";
                subscriptionResultTextBlock.Text = "";
                subscriptionResultTextBlock2.Text = "";
                NoError = true;
                SubscribeMeasurement = false;
                //cadence = await BluetoothLEDevice.FromIdAsync(informationOfFoundDevices[ii].DeviceInformation.Id);
                cadence = await BluetoothLEDevice.FromIdAsync(StorageBluetoothAddress.ToString());
                if (cadence != null)
                {
                    await GetAllServices();
                    if (NoError == false)
                    {
                        //ErrorTextBlock.Text = "Error sensor!";
                        //ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        ErrorTextBlock.Text = "Sensor: Searching...";
                        ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                        //characteristic2.ValueChanged -= Characteristic_ValueChanged;
                        TimerConnectToSensor.Start();
                        return;
                    }
                    await GetValuesSensor();
                    //await GetInformationSensor();
                    if (NoError == false)
                    {
                        ErrorTextBlock.Text = "Sensor: Error!";
                        ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        TimerConnectToSensor.Start();
                        return;
                    }
                    //Debug.WriteLine("2 sensor is " + cadence.ConnectionStatus.ToString());
                    //await GetValuesSensor();
                    await GetInformationSensor();
                    //Debug.WriteLine("Getinformationsensor autostart");
                    if (NoError == false)
                    {
                        ErrorTextBlock.Text = "Sensor: Error!";
                        ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        TimerConnectToSensor.Start();
                        return;
                    }
                    ErrorTextBlock.Text = "Sensor: Found.";
                    //Debug.WriteLine("3 sensor is " + cadence.ConnectionStatus.ToString());
                    ErrorTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                    //<begincode*****get the value of the cadence counter and te time between two measurements*****>
                    cadence.ConnectionStatusChanged += Cadence_ConnectionStatusChanged;
                    //<endcode*****get the value of the cadence counter and te time between two measurements*****>
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ErrorTextBlock.Text = "Sensor: Error!";
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
            try
            {
                //The following TestDoubleDevices bool is to prevent a ble device from being written more than once in the list
                bool DoubleDevices = false;
                //search in real-time for bluetooth device that are non-paired
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    for (int i = 0; i < informationOfFoundDevices.Count; i += 1)
                    {
                        if (informationOfFoundDevices[i].DeviceInformation.Name.Contains(args.Name))
                        {
                            DoubleDevices = true;
                        }
                    }
                    if (DoubleDevices == false && args.Name.Contains("CADENCE")) //filter only a cadence sens is permitted
                    {
                        informationOfFoundDevices.Add(new BluetoothInformationWrapper(args));
                    }
                    AutoStart();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault DeviceWatcher_Added " + ex);
            }
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
                        connectionStatusTextBlock.Text = "Sensor: Connection lost!";
                        connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        TimerConnectToSensor.Start();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault Cadence_ConnectionStatusChanged " + ex);

            }
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) //event handler used to process characteristic value change notification and indication events sent by a Bluetooth LE device.
        {
            try
            {
                if (SubscribeMeasurement == false) return;
                if (NoError == false) return;
                reader = DataReader.FromBuffer(args.CharacteristicValue);
                inputx = new byte[reader.UnconsumedBufferLength];

                reader.ReadBytes(inputx);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        Temp0 = string.Format("{0:x}", inputx[0]);//convert from decimal to hexadecimal
                        Temp1 = string.Format("{0:x}", inputx[1]);
                        Temp2 = string.Format("{0:x}", inputx[2]);
                        Temp3 = string.Format("{0:x}", inputx[3]);
                        Temp4 = string.Format("{0:x}", inputx[4]);
                        if (Temp0 != "0")//filter because sometimes Temp0 t/m Temp4 = 00:00:00:00:00
                        {
                            //checks whether the 2nd bit counting from the right is a 1, if so the sensor has a cadence function
                            bool IsCadenceSensor = (Temp0[0] & (1 << 1)) != 0; //(myByte & (1 << position)) != 0 This works by using the Left Shift operator (<<) 
                            if (IsCadenceSensor == false)
                            {
                                ErrorTextBlock.Text = "Sensor: Error selected has no cadence function.";
                                NoError = false;
                                return;
                            }
                            cadenceCounter = Temp2 + Temp1;//formaat is little endian betekent dat we de volgorde moeten veranderen
                            value = Convert.ToInt32(cadenceCounter, 16); //converteren van hexadecimaal naar decimaal
                            subscriptionResultTextBlock.Text = "rotations counter = " + value.ToString();
                            cadenceTime = Temp4 + Temp3;
                            valuetime = Convert.ToInt32(cadenceTime, 16);
                            //Debug.WriteLine(value + " " + valueprevious + " " + valuetime + " " + valuetimeprevious);

                            //calculation rotations per minute
                            int deltavalue = value - valueprevious;
                            int deltavaluetime = valuetime - valuetimeprevious;
                            //  Debug.WriteLine(deltavalue + " " + deltavaluetime);
                            if (deltavaluetime > 0 && deltavalue > 0)
                            {
                                Rotationsresult = deltavalue * 1024f / deltavaluetime; //rotations per unit of time
                                Rotationsresult = (int)(Rotationsresult * 60);// x 60 gives amount of rotations per minute
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
                            valuetimeprevious = valuetime;
                            valueprevious = value;
                        }

                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault Characteristic_ValueChanged " + ex);
            }
        }
        private async Task GetAllServices()
        {
            try
            {
                string BluetoothAddressHex = cadence.BluetoothAddress.ToString("x");  // convert decimal to hex
                for (int i = 2; i < BluetoothAddressHex.Length; i += 2) // om het leesbaar te houden plaatsen we om de 2 hex getallen een :
                {
                    BluetoothAddressHex = BluetoothAddressHex.Insert(i, ":");
                    i = i + 1;
                }
                BTAddresTextBlock.Text = "Sensor: Bluetooth address = " + BluetoothAddressHex;
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
                        observableServices.Clear();
                        foreach (GattDeviceService service in services)
                        {
                            observableServices.Add(new GattServiceWrapper(service));
                        }
                        connectionStatusTextBlock.Text = "Sensor: Connection established!";
                        connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                        TimerConnectToSensor.Stop();
                    }
                    else
                    {
                        Debug.WriteLine("could not read the services");
                        connectionStatusTextBlock.Text = "Sensor: Couldn't establish connection";
                        connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        NoError = false;
                        // TimerConnectToSensor.Start();
                        return;
                    }
                    //<endcode***** get all services and putt them in the list of observableServices*****>
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault GetAllServices " + ex);
                NoError = false;

            }
        }
        GattServiceWrapper wrapper3;
        private async Task GetInformationSensor()
        {
            try
            {
                //<begincode***** select characteristic GenericAccess level and putt them in the list of observableCharacteristics*****>
                var wrapper2 = observableServices.Single(i => i.Service.Uuid.ToString() == "00001800-0000-1000-8000-00805f9b34fb");
                service2 = wrapper2.Service;
                result2 = await service2.GetCharacteristicsAsync();

                if (result2.Status == GattCommunicationStatus.Success)
                {
                    bool Doublecharastic = false; //preventing to add the same charateristic in de list observableCharacteristics, else get a fault
                    foreach (GattCharacteristic characteristic in result2.Characteristics)
                    {
                        Doublecharastic = false;
                        for (int i = 0; i < observableCharacteristics.Count; i += 1)
                        {
                            if (observableCharacteristics[i].Characteristic.Uuid == characteristic.Uuid)
                            {
                                Doublecharastic = true;
                            }
                        }
                        if (Doublecharastic == false)
                        {
                            observableCharacteristics.Add(new GattCharacteristicWrapper(characteristic)); ;
                        }

                    }
                    //Debug.WriteLine("Getinformationsensor add");
                }
                else
                {
                    Debug.WriteLine("could not read the characterstics");
                    NoError = false;
                    return;
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
                    DeviceNameTextBlock.Text = "Sensor: name = " + utf8result;
                }
                else
                {
                    Debug.WriteLine("could not read the device name");
                    NoError = false;
                    return;
                }
                //<endcode*****read Firmware revision*****>

                //<begincode***** select characteristic Device information and putt them in the list of observableCharacteristics*****>
                // Het is niet zeker dat de service firmware bestaat, daarom eerst controleren of de service wel bestaat, want als er niets is te selecteren dan krijgen we een fout
                for (int ii = 0; ii < observableServices.Count; ii += 1)
                {
                    if (observableServices[ii].Service.Uuid.ToString() == "0000180a-0000-1000-8000-00805f9b34fb")
                    {
                        wrapper3 = observableServices.Single(i => i.Service.Uuid.ToString() == "0000180a-0000-1000-8000-00805f9b34fb");
                        NoError = true;
                        break;
                    }
                    else
                    {
                        NoError = false;
                    }
                }
                if (NoError == false)
                {
                    return;
                }
                // var wrapper3 = observableServices.Single(i => i.Service.Uuid.ToString() == "0000180a-0000-1000-8000-00805f9b34fb");
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
                    NoError = false;
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
                    FirmwareTextBlock.Text = "Sensor: Firmware = " + utf8result;
                }
                else
                {
                    Debug.WriteLine("could not read the firmware revision");
                    NoError = false;
                    return;
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
                    HardwareTextBlock.Text = "Sensor: Hardware Revision = " + Hardwarevalue.ToString();
                }
                else
                {
                    Debug.WriteLine("could not read the hardware revision");
                    NoError = false;
                    return;
                }
                //<endcode*****read hardware*****>

                //<begincode***** select characteristic battery level and putt them in the list of observableCharacteristics*****>
                var wrapper4 = observableServices.Single(i => i.Service.Uuid.ToString() == "0000180f-0000-1000-8000-00805f9b34fb");
                service2 = wrapper4.Service;
                result2 = await service2.GetCharacteristicsAsync();
                if (result2.Status == GattCommunicationStatus.Success)
                {
                    observableCharacteristics.Clear();
                    foreach (GattCharacteristic characteristic in result2.Characteristics)
                    {
                        observableCharacteristics.Add(new GattCharacteristicWrapper(characteristic));
                    }
                }
                else
                {
                    Debug.WriteLine("could not read the characteristic battery level");
                    NoError = false;
                    return;
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
                    StringBuilder hex2 = new StringBuilder(input.Length * 2);
                    foreach (byte b in input)
                        hex2.AppendFormat("{0:x2}", b);
                    int Batteryvalue = Convert.ToInt32(hex2.ToString(), 16);
                    readingsTextBlock.Text = "Sensor: Battery level = " + Batteryvalue.ToString() + "%";
                    if (Batteryvalue < 40)
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
                    Debug.WriteLine("could not read the battery level");
                    NoError = false;
                    return;
                }
                //<endcode*****read battery level*****>
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault GetInformationSensor " + ex);
                NoError = false;
            }
        }

        private async Task GetValuesSensor()
        {
            try
            {
                //<begincode***** select characteristic Measurement and putt them in the list of observableCharacteristics*****>
                var wrapper5 = observableServices.Single(i => i.Service.Uuid.ToString() == "00001816-0000-1000-8000-00805f9b34fb");
                service2 = wrapper5.Service;
                result2 = await service2.GetCharacteristicsAsync();
                if (result2.Status == GattCommunicationStatus.Success)
                {
                    observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
                    observableCharacteristics.Clear();
                    foreach (GattCharacteristic characteristic in result2.Characteristics)
                    {
                        observableCharacteristics.Add(new GattCharacteristicWrapper(characteristic));
                    }
                }
                else
                {
                    Debug.WriteLine("could not read the characteristic measurement");
                    NoError = false;
                    return;
                }
                //<endcode***** select characteristic device Measurement and putt them in the list of observableCharacteristics*****>
                //<begincode***** select characteristic measurement*****>
                selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.Uuid.ToString() == "00002a5b-0000-1000-8000-00805f9b34fb");
                //<endcode***** select characteristic measurement*****>
                //<begincode***** subscribe to measurement*****>
                characteristic2 = selectedCharacteristicWrapper.Characteristic;
                GattCharacteristicProperties properties = characteristic2.CharacteristicProperties;
                if (properties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    GattCommunicationStatus status = await characteristic2.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Success)
                    {
                        // Server has been informed of clients interest.
                        SubscribeMeasurement = true;
                        subscriptionStatusTextBlock.Text = "Sensor: Measurement Subscribed.";
                        subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                        characteristic2.ValueChanged += Characteristic_ValueChanged;
                    }
                    else
                    {
                        subscriptionStatusTextBlock.Text = "Sensor: Measurement " + status.ToString();
                        subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        Debug.WriteLine("could not subscribe to measurement");
                        NoError = false;
                        return;
                    }
                }
                else if (properties.HasFlag(GattCharacteristicProperties.Indicate))
                {
                    GattCommunicationStatus status = await characteristic2.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                    if (status == GattCommunicationStatus.Success)
                    {
                        subscriptionStatusTextBlock.Text = "Sensor: Measurement.";
                        subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                        characteristic2.ValueChanged += Characteristic_ValueChanged;
                    }
                    else
                    {
                        subscriptionStatusTextBlock.Text = "Sensor: Measurement " + status.ToString();
                        subscriptionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                        Debug.WriteLine("could not indicate to measurement");
                        NoError = false;
                        return;
                    }
                }
                //<endcode***** subscribe to measurement*****>
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fault GetValuesSensor " + ex);
                NoError = false;

            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if(characteristic2 != null) //unsubscribe measurements characterstic2, is necessary to disconnect the existing measurement from the sensor, otherwise when re-opening the app or you choose another sensor we get an unstable measurement
            {
                await characteristic2.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                characteristic2.Service.Dispose();
            }
            if (cadence != null)
            {
                cadence.Dispose();
                cadence = null;
            }
            deviceWatcher.Stop();
            TimerConnectToSensor.Stop();
            Windows.UI.Xaml.Application.Current.Exit();
        }
    }
}
