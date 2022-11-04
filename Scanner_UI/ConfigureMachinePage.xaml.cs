using System;
using System.Collections.Generic;
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
using Windows.UI;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

// add timer and add RFID message display
// also update message when save is pressed 

namespace ScanTest1
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class ConfigureMachinePage : Page
    {

        private DispatcherTimer refreshTimer;

        public ConfigureMachinePage()
        {
            this.InitializeComponent();
            switch(Globals.minimum_decode_count)
            {
                case 1:
                    CountButton1.IsChecked = true;
                    break;
                case 2:
                    CountButton2.IsChecked = true;
                    break;
                case 3:
                    CountButton3.IsChecked = true;
                    break;
                case 4:
                    CountButton4.IsChecked = true;
                    break;
                default:
                    CountButton1.IsChecked = true;
                    break;
            }

            if (Globals.ManualScanConfirm == true)
                ConfirmationRequired.IsChecked = true;
            else
                ConfirmationNotRequired.IsChecked = true;

            Globals.remote_refresh_request = true;

            // Leave it empty?
            RFIDBox.Text = Globals.LocalRFIDNum.ToString();

            switch (Globals.scanner_mode)
            {
                case Globals.SCANNER_MODES.LOCAL:
                    LocalButton.IsChecked = true;
                    break;
                case Globals.SCANNER_MODES.REMOTE:
                    RemoteButton.IsChecked = true;
                    break;
                default:
                    StandaloneButton.IsChecked = true;
                    break;
            }

        }

        public void OnLoad(object sender, RoutedEventArgs e)
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

            update_match_color();

            //Update RFID display
            if (Globals.remote_refresh_request)
            {
                if (Globals.in_range == true)
                    RFIDMsg.Text = "Current RFID: " + Globals.ReadRFIDNum.ToString();
                else
                    RFIDMsg.Text = "No RFID found";

                Globals.remote_refresh_request = false;

            }
        }

        LocalFileIO FileHandler = new LocalFileIO();
        private void SourceButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CountButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        void update_match_color()
        {
            UInt32 result;
            if (RFIDBox.Text.Length > 0)
            {
                if (UInt32.TryParse(RFIDBox.Text, out result))
                {
                    if (result == Globals.ReadRFIDNum)
                    {
                        UserMsg.Foreground = new SolidColorBrush(Colors.Green);
                        UserMsg.Text = "Match";

                    }
                    else
                    {
                        UserMsg.Foreground = new SolidColorBrush(Colors.Black);
                        UserMsg.Text = "";
                    }
                }
                else
                {
                    // can't parse 
                    UserMsg.Foreground = new SolidColorBrush(Colors.Red);
                    UserMsg.Text = "Illegal Number";
                }
            }
            else
            {
                UserMsg.Foreground = new SolidColorBrush(Colors.Black);
                UserMsg.Text = "";
            }
        }

        void AddChar(string input)
        {
            RFIDBox.Text += input;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (RFIDBox.Text.Length > 0)
            {
                RFIDBox.Text = RFIDBox.Text.Substring(0, (RFIDBox.Text.Length - 1));
            }

        }
        private void BA_Click(object sender, RoutedEventArgs e)
        {
            AddChar("A");
        }
        private void BB_Click(object sender, RoutedEventArgs e)
        {
            AddChar("B");
        }
        private void BC_Click(object sender, RoutedEventArgs e)
        {
            AddChar("C");
        }
        private void BD_Click(object sender, RoutedEventArgs e)
        {
            AddChar("D");
        }
        private void BE_Click(object sender, RoutedEventArgs e)
        {
            AddChar("E");
        }
        private void BF_Click(object sender, RoutedEventArgs e)
        {
            AddChar("F");
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

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            refreshTimer.Stop();
            Frame.Navigate(typeof(UserPage));
        }
        private async void Save_Click(object sender, RoutedEventArgs e)
        {

            UserMsg.Text = "Saved";

            if (LocalButton.IsChecked == true)             
            {
                Globals.scanner_mode = Globals.SCANNER_MODES.LOCAL;
            }
            
            if(RemoteButton.IsChecked == true)
            {
                Globals.scanner_mode = Globals.SCANNER_MODES.REMOTE;
            }

            if (StandaloneButton.IsChecked == true)
            {
                Globals.scanner_mode = Globals.SCANNER_MODES.STANDALONE;
            }


            if (CountButton1.IsChecked == true)
                Globals.minimum_decode_count = 1;
            if (CountButton2.IsChecked == true)
                Globals.minimum_decode_count = 2;
            if (CountButton3.IsChecked == true)
                Globals.minimum_decode_count = 3;
            if (CountButton4.IsChecked == true)
                Globals.minimum_decode_count = 4;

            Globals.LocalRFIDNum = UInt32.Parse(RFIDBox.Text);

            await FileHandler.UpdateFileFromFields();
        }

        private void Confirm_Checked(object sender, RoutedEventArgs e)
        {
            if(ConfirmationRequired.IsChecked == true)
            {
                Globals.ManualScanConfirm = true;
            }
            else 
            {
                Globals.ManualScanConfirm = false;
            }
        }

    }
}
