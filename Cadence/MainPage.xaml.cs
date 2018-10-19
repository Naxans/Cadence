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

        private BluetoothLEDevice cadence;
        private ObservableCollection<BluetoothInformationWrapper> informationOfFoundDevices;
        private GattCharacteristicWrapper selectedCharacteristicWrapper;

        private DataReader reader;

        private DispatcherTimer TimerConnectToSensor = new DispatcherTimer(); //timer connect sensor

        private ObservableCollection<GattServiceWrapper> observableServices;

        ObservableCollection<GattCharacteristicWrapper> observableCharacteristics;
        private BluetoothInformationWrapper selectedDeviceInfoWrapper;

        //With Windows 10 devices, in order to connect to any BLE device, you need to pair them first.Thus you need to go to the Bluetooth settings, enable the Bluetooth.And then find the device you with to communicate with and get it paired. Which after you can find the device in your own code by searching for it


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
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false), requestedProperties, DeviceInformationKind.AssociationEndpoint);

            //<*****lijst maken van bleutooth devices*****>
            // Register event handlers before starting the watcher.
            // Added, Updated and Removed are required to get all nearby devices
            deviceWatcher.Added += DeviceWatcher_Added;//create list of all bleutooth devices
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;
            // Start the watcher.
            deviceWatcher.Start();

            //if (StorageBluetoothAddress != null)
            //{
            //    for (int i = 0; i < informationOfFoundDevices.Count; i += 1) // zoeken naar de bluetooth device
            //    {
            //        if (informationOfFoundDevices[i].DeviceInformation.Id == StorageBluetoothAddress.ToString())
            //        {
            //            //select the device in de devicelist
            //            //deviceListView.SelectedIndex = i;
            //            //  deviceListView.SelectedItem = i;
            //            deviceListView.SelectedItems.Add(i);
            //            // deviceListView.Select();
            //        }
            //    }
            //}

            TimerConnectToSensor.Interval = TimeSpan.FromMilliseconds(1000);
            TimerConnectToSensor.Tick += TimerConnectToSensor_Tick;
            TimerConnectToSensor.Start();
        }

        private void TimerConnectToSensor_Tick(object sender, object e)
        {
            TimerConnectToSensor.Stop();

            //TimerConnectToSensor.Start();
        }
        private async void deviceListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            selectedDeviceInfoWrapper = (BluetoothInformationWrapper)e.ClickedItem;
            localSettings.Values["StorageBluetoothAddress"] = selectedDeviceInfoWrapper.DeviceInformation.Id; //opslaan op de hardeschijf
            if (selectedDeviceInfoWrapper != null)
            {
               //await SaveSensorInfo();
                // Note: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                cadence = await BluetoothLEDevice.FromIdAsync(selectedDeviceInfoWrapper.DeviceInformation.Id);
                //await SaveSensorInfo();
                if (cadence == null)
                {
                    connectionStatusTextBlock.Text = "Sensor: Couldn't establish connection";
                    connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    connectionStatusTextBlock.Text = "Sensor: Connection established!";
                    connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                }
                //connectionStatusTextBlock.Text = cadence == null ? "Sensor: Couldn't establish connection" : "Sensor: Connection established!";
                string BluetoothAddressHex = cadence.BluetoothAddress.ToString("x");  // convert decimal to hex
                for (int i = 2; i< BluetoothAddressHex.Length; i +=2) // om het leesbaar te houden plaatsen we om de 2 hex getallen een :
                {
                    BluetoothAddressHex = BluetoothAddressHex.Insert(i, ":");
                    i = i + 1;
                }
                BTAddresTextBlock.Text = "Bluetooth address = " + BluetoothAddressHex;

                if (cadence != null)
                {
                    //<begincode***** get all services and putt them in the list of observableServices*****>
                    GattDeviceServicesResult result = await cadence.GetGattServicesAsync();
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        List<GattDeviceService> services = result.Services.ToList();
                        observableServices = new ObservableCollection<GattServiceWrapper>();
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
                    var wrapper = observableServices.Single(i => i.Service.Uuid.ToString() == "00001800-0000-1000-8000-00805f9b34fb");
                    GattDeviceService service2 = wrapper.Service;
                    GattCharacteristicsResult result2 = await service2.GetCharacteristicsAsync();
                    if (result2.Status == GattCommunicationStatus.Success)
                    {
                        observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
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
                    selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.AttributeHandle.ToString() == "2");
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
                    wrapper = observableServices.Single(i => i.Service.Uuid.ToString() == "0000180a-0000-1000-8000-00805f9b34fb");
                    service2 = wrapper.Service;
                    result2 = await service2.GetCharacteristicsAsync();
                    if (result2.Status == GattCommunicationStatus.Success)
                    {
                        observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
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
                    selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.AttributeHandle.ToString() == "21");
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
                    selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.AttributeHandle.ToString() == "19");
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
                    wrapper = observableServices.Single(i => i.Service.Uuid.ToString() == "0000180f-0000-1000-8000-00805f9b34fb");
                    service2 = wrapper.Service;
                   result2 = await service2.GetCharacteristicsAsync();
                    if (result2.Status == GattCommunicationStatus.Success)
                    {
                        observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
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
                    selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.AttributeHandle.ToString() == "13");
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
                        readingsTextBlock.Text = "Battery level = "+ Batteryvalue.ToString() +"%";
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

                    //<begincode***** select characteristic Measurement and putt them in the list of observableCharacteristics*****>
                    wrapper = observableServices.Single(i => i.Service.Uuid.ToString() == "00001816-0000-1000-8000-00805f9b34fb");
                    service2 = wrapper.Service;
                    result2 = await service2.GetCharacteristicsAsync();
                    if (result2.Status == GattCommunicationStatus.Success)
                    {
                        observableCharacteristics = new ObservableCollection<GattCharacteristicWrapper>();
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
                    selectedCharacteristicWrapper = observableCharacteristics.Single(i => i.Characteristic.AttributeHandle.ToString() == "35");
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

                //<begincode*****get the value of the cadence counter and te time between two measurements*****>
                cadence.ConnectionStatusChanged += Cadence_ConnectionStatusChanged;
                //<endcode*****get the value of the cadence counter and te time between two measurements*****>


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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                informationOfFoundDevices.Add(new BluetoothInformationWrapper(args));

                if (StorageBluetoothAddress != null)
                {
                    for (int i = 0; i < informationOfFoundDevices.Count; i += 1) // zoeken naar de bluetooth device
                    {
                        if (informationOfFoundDevices[i].DeviceInformation.Id == StorageBluetoothAddress.ToString())
                        {
                            //select the device in de devicelist
                            //deviceListView.Focus();
                            deviceListView.SelectedIndex = i;
                            //deviceListView.SelectedItems.Select(i) = true;

                            //deviceListView.SelectedItem = i;
                            //deviceListView.SelectedItems.Add(i);
                           
                        }
                    }
                }

            });
        }
        //private void deviceListViewEnterKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        //{
        //    try
        //    {
        //        args.Handled = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }
        //}

        private async void Cadence_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            Debug.WriteLine(cadence.ConnectionStatus.ToString() == "Disconnected" ? "disconnected" : "connected");
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (cadence.ConnectionStatus.ToString() == "Disconnected")
                {
                    connectionStatusTextBlock.Text = "Sensor: Lost connection";
                    connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    //TimerConnectToSensor.Start();
                }
            });
        }


        int valueprevious;
        int valuetimeprevious;
        int secondtestnull = 0;
        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) //event handler used to process characteristic value change notification and indication events sent by a Bluetooth LE device.
        {
            reader = DataReader.FromBuffer(args.CharacteristicValue);
                byte[] input = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(input);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StringBuilder hex = new StringBuilder(input.Length * 2);
                foreach (byte b in input)
                    hex.AppendFormat("{0:x2}", b);

                string cadence = hex[4].ToString() + hex[5].ToString() + hex[2].ToString() + hex[3].ToString(); //formaat is little endian betekent dat we de volgorde moeten veranderen
                int value = Convert.ToInt32(cadence,16); //converteren van hex naar decimaal
                subscriptionResultTextBlock.Text = "revolutions counter = " + value.ToString();

                string cadencetime = hex[8].ToString() + hex[9].ToString() + hex[6].ToString() + hex[7].ToString();
                int valuetime = Convert.ToInt32(cadencetime, 16);
                //Debug.WriteLine(value + " " + valueprevious + " " + valuetime + " " + valuetimeprevious);

                //calculation rotations per minute
                int deltavalue = value - valueprevious;
                int deltavaluetime = valuetime - valuetimeprevious;
              //  Debug.WriteLine(deltavalue + " " + deltavaluetime);
                if (deltavaluetime > 0 && deltavalue > 0 && value > valueprevious && valuetime > valuetimeprevious)
                {
                float result = deltavalue * 1024f / deltavaluetime; //aantal toeren per tijdeenheid
                    // x 60 gives amount of rotations per minute
                    result = (int)(result * 60);
                    subscriptionResultTextBlock2.Text = "revolutions per minute = " + result.ToString();
                    secondtestnull = 0; //soms ook al zijn we aan het fietsen krijgen we de waarden alsof we niet meer trappen, resultaat is dat 1 x een nul verschijnt bij het aantal toeren per minut. vandaar dat we dan pas een nul laten zien als we twee keer de info krijgen dat er niet meer wordt gefietst
                }
                else if (deltavalue == 0 && deltavaluetime == 0 && secondtestnull > 2) //als er niet wordt gefietst dan is het aantal toeren/minut = 0
                {
                    subscriptionResultTextBlock2.Text = "revolutions per minute = 0";
                    secondtestnull = 0;
                }
                else if (deltavalue == 0 && deltavaluetime == 0 && value > 0) // de waarde value moet > dan 0 om te vermijden dat secondtestnull = +1 
                {
                    secondtestnull = secondtestnull + 1;
                }
                valuetimeprevious = valuetime;
                valueprevious = value;
            });
        }


    }
}
