using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.PointOfService;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using PhistonUI;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScanTest1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HandScanPage : Page
    {
        BarcodeScanner scanner = null;
        ClaimedBarcodeScanner claimedScanner = null;

        public HandScanPage()
        {
            this.InitializeComponent();
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            scanner = await BarcodeScanner.GetDefaultAsync();

            if (scanner != null)
            {
                //DeviceId.Text = scanner.DeviceId;
                claimedScanner = await scanner.ClaimScannerAsync();
                if(claimedScanner != null)
                {
                    claimedScanner.IsDecodeDataEnabled = true;
                    // after successfully claiming, attach the datareceived event handler.
                    claimedScanner.DataReceived += claimedScanner_DataReceived;

                    // Ask the API to decode the data by default. By setting this, API will decode the raw data from the barcode scanner and
                    // send the ScanDataLabel and ScanDataType in the DataReceived event
                    claimedScanner.IsDecodeDataEnabled = true;

                    // enable the scanner.
                    // Note: If the scanner is not enabled (i.e. EnableAsync not called), attaching the event handler will not be any useful because the API will not fire the event
                    // if the claimedScanner has not beed Enabled
                    await claimedScanner.EnableAsync();
                }
                else
                {
                    NoScannerPopup.IsOpen = true;
                }
                // UpdateOutput("Device Id is:" + scanner.DeviceId);
            }
            else
            {
                NoScannerPopup.IsOpen = true;
            }
        }

        async void claimedScanner_DataReceived(ClaimedBarcodeScanner sender, BarcodeScannerDataReceivedEventArgs args)
        {
            // need to update the UI data on the dispatcher thread.
            // update the UI with the data received from the scan.
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (args.Report.ScanDataLabel != null)
                {

                    string barcode_type = BarcodeSymbologies.GetName(args.Report.ScanDataType);
                    var scanDataLabelReader = DataReader.FromBuffer(args.Report.ScanDataLabel);
                    string barcode = scanDataLabelReader.ReadString(args.Report.ScanDataLabel.Length);

                    if (Type1.Text == "")
                    { 
                        Type1.Text = barcode_type;
                        Code1.Text = barcode;
                    }
                    else if (Type2.Text == "")
                    {
                        Type2.Text = barcode_type;
                        Code2.Text = barcode;
                    }
                    else if (Type3.Text == "")
                    {
                        Type3.Text = barcode_type;
                        Code3.Text = barcode;
                    }
                    else if (Type4.Text == "")
                    {
                        Type4.Text = barcode_type;
                        Code4.Text = barcode;
                    }
                    else if (Type5.Text == "")
                    {
                        Type5.Text = barcode_type;
                        Code5.Text = barcode;
                    }
                    else if (Type6.Text == "")
                    {
                        Type6.Text = barcode_type;
                        Code6.Text = barcode;
                    }
                    else if (Type7.Text == "")
                    {
                        Type7.Text = barcode_type;
                        Code7.Text = barcode;
                    }

                }
            });
        }

        private void NoScanReturn_Click(object sender, RoutedEventArgs e)
        {
            NoScannerPopup.IsOpen = false;
            this.Frame.GoBack();
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Check to make sure there is at least one barcode present
            if(Code1.Text != "")
                ScanPopup.IsOpen = true;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        // Legacy code quick patch - this could be more efficient with indexed fields (to do later)
        private void SaveCodes()
        {
            int code_count = 0;

            // Check to make sure there is at least one barcode present
            if (Code1.Text != "")
            {
                Globals.hand_scan_data[0,1] = Code1.Text;
                Globals.hand_scan_data[0,0] = "Man: " + Type1.Text;
                code_count = 1;
            }

            if (Code2.Text != "")
            {
                Globals.hand_scan_data[1, 1] = Code2.Text;
                Globals.hand_scan_data[1, 0] = "Man: " + Type2.Text;
                code_count = 2;
            }

            if (Code3.Text != "")
            {
                Globals.hand_scan_data[2, 1] = Code3.Text;
                Globals.hand_scan_data[2, 0] = "Man: "  + Type3.Text;
                code_count = 3;
            }

            if (Code4.Text != "")
            {
                Globals.hand_scan_data[3, 1] = Code4.Text;
                Globals.hand_scan_data[3, 0] = "Man: " + Type4.Text;
                code_count = 4;
            }

            if (Code5.Text != "")
            {
                Globals.hand_scan_data[4, 1] = Code5.Text;
                Globals.hand_scan_data[4, 0] = "Man: " + Type5.Text;
                code_count = 5;
            }
            if (Code6.Text != "")
            {
                Globals.hand_scan_data[5, 1] = Code6.Text;
                Globals.hand_scan_data[5, 0] = "Man: " + Type6.Text;
                code_count = 6;
            }

            if (Code7.Text != "")
            {
                Globals.hand_scan_data[6, 1] = Code7.Text;
                Globals.hand_scan_data[6, 0] = "Man: " + Type7.Text;
                code_count = 7;
            }

            Globals.hand_scan_count = code_count;
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCodes();                
            Globals.auto_scan_request = false;              // Do not run through the scanner, if the hand scan count is greater than 0, the data will be logged
            this.Frame.GoBack();
        }

        private void PopScanButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCodes();
            Globals.auto_scan_request = true;
            this.Frame.GoBack();
        }

        private void PopQuitButton_Click(object sender, RoutedEventArgs e)
        {
            ScanPopup.IsOpen = false;
        }


        private void delete_and_shift_data(int row_number)
        {
            switch (row_number)
            {
                case 1:
                    Code1.Text = Code2.Text;
                    Type1.Text = Type2.Text;
                    // drop through
                    goto case 2;

                case 2:
                    Code2.Text = Code3.Text;
                    Type2.Text = Type3.Text;
                    // drop through
                    goto case 3;
                   
                case 3:
                    Code3.Text = Code4.Text;
                    Type3.Text = Type4.Text;
                    // drop through
                    goto case 4;
                    
                case 4:
                    Code4.Text = Code5.Text;
                    Type4.Text = Type5.Text;
                    // drop through
                    goto case 5;

                case 5:
                    Code5.Text = Code6.Text;
                    Type5.Text = Type6.Text;
                    // drop through
                    goto case 6;

                case 6:
                    Code6.Text = Code7.Text;
                    Type6.Text = Type7.Text;
                    // drop through
                    goto case 7;

                case 7:
                    Code7.Text = "";
                    Type7.Text = "";
                    break;
            }
        }

        private void RemoveCode1_Click(object sender, RoutedEventArgs e)
        {
            delete_and_shift_data(1);
        }

        private void RemoveCode2_Click(object sender, RoutedEventArgs e)
        {
            delete_and_shift_data(2);
        }

        private void RemoveCode3_Click(object sender, RoutedEventArgs e)
        {
            delete_and_shift_data(3);
        }

        private void RemoveCode4_Click(object sender, RoutedEventArgs e)
        {
            delete_and_shift_data(4);
        }

        private void RemoveCode5_Click(object sender, RoutedEventArgs e)
        {
            delete_and_shift_data(5);
        }

        private void RemoveCode6_Click(object sender, RoutedEventArgs e)
        {
            delete_and_shift_data(6);
        }

        private void RemoveCode7_Click(object sender, RoutedEventArgs e)
        {
            delete_and_shift_data(7);
        }
                     
                          
    }
}
