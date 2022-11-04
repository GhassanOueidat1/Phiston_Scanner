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
using PhistonUI;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScanTest1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FactoryConfigurePage : Page
    {
        private DispatcherTimer refreshTimer;

        public FactoryConfigurePage()
        {
            this.InitializeComponent();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            Globals.stop_all_timers = false;
            refreshTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200) };
            refreshTimer.Tick += refreshTimer_Tick;
            refreshTimer.Start();
        }

        public void refreshTimer_Tick(object source, object e)
        {

            if (Globals.stop_all_timers)
            {
                refreshTimer.Stop();
                return;
            }

            if (Globals.remote_refresh_request)
            {
                Globals.remote_refresh_request = false;

                CurrentSN.Text = Globals.serial_number.ToString();
                CycleCount.Text = Globals.cycle_count.ToString();
                HardwareRev.Text = Globals.scanner_hw_rev.ToString();
                FirmwareRev.Text = Globals.scanner_fw_rev.ToString();
                SoftwareRev.Text = Globals.scanner_sw_rev.ToString();
            }

            HandScanCheckbox.IsChecked = Globals.HandScanOption;
            
        }

        private void AddChar(string button_val)
        {
            if (SerialNumber.Text.Length < 6)
            {
                SerialNumber.Text += button_val;
            }
            UserMsg.Text = "";
        }

        private void RemoveChar()
        {
            if (SerialNumber.Text.Length > 0)
            {
                SerialNumber.Text = SerialNumber.Text.Substring(0, SerialNumber.Text.Length - 1);
            }
            UserMsg.Text = "";
        }


        private void B0_Click(object sender, RoutedEventArgs e)
        {
            AddChar("0");
        }
        private void B1_Click(object sender, RoutedEventArgs e)
        {
            AddChar("1");
        }
        private void B2_Click(object sender, RoutedEventArgs e)
        {
            AddChar("2");
        }
        private void B3_Click(object sender, RoutedEventArgs e)
        {
            AddChar("3");
        }
        private void B4_Click(object sender, RoutedEventArgs e)
        {
            AddChar("4");
        }
        private void B5_Click(object sender, RoutedEventArgs e)
        {
            AddChar("5");
        }
        private void B6_Click(object sender, RoutedEventArgs e)
        {
            AddChar("6");
        }
        private void B7_Click(object sender, RoutedEventArgs e)
        {
            AddChar("7");
        }
        private void B8_Click(object sender, RoutedEventArgs e)
        {
            AddChar("8");
        }
        private void B9_Click(object sender, RoutedEventArgs e)
        {
            AddChar("9");
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            RemoveChar();
        }
        private void Return_Click(object sender, RoutedEventArgs e)
        {
            refreshTimer.Stop();
            Frame.Navigate(typeof(UserPage));
        }

        private async void Set_Click(object sender, RoutedEventArgs e)
        {
            UInt16 serial_number;

            LocalFileIO FileHandler = new LocalFileIO();

            // Write the hand scan selection to file
            await FileHandler.UpdateFileFromFields();

            if (UInt16.TryParse(SerialNumber.Text, out serial_number))
            {
                if (serial_number <= 65535)
                {
                    var tx_buff = new byte[3];

                    tx_buff[0] = Globals.SET_SERIAL_NUMBER;                  

                    tx_buff[1] = (byte)(serial_number & 0xFF);
                    tx_buff[2] = (byte)((serial_number >> 8) & 0xFF);

                    SerialClass.Instance.tx_buffer_write(tx_buff);
                }
                else
                {
                    SerialNumber.Text = "";
                    UserMsg.Text = "Number out of range. SN not written.";
                }
            }
            else
            {
                SerialNumber.Text = "";
                UserMsg.Text = "Number could not be read. SN not written.";
            }

        }

        private void SetLicense_Click(object sender, RoutedEventArgs e)
        {
            Globals.stop_all_timers = true;
            Frame.Navigate(typeof(LicensePage));
        }
        private void ResetCycleCount_Click(object sender, RoutedEventArgs e)
        {
            // This may need to happen at a controller level - the zero count is stored in the Controller PCB flash
            
        }

        private void HandScan_Click(object sender, RoutedEventArgs e)
        {
            Globals.HandScanOption = (bool)HandScanCheckbox.IsChecked;
        }
        private void MultiScan_Click(object sender, RoutedEventArgs e)
        {
            Globals.continuous_cycle_enabled = (bool)MultiScanCheckbox.IsChecked;
        }
    }
}
