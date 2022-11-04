using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using PhistonUI;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Capture;
using System.Collections.ObjectModel;
using Windows.Devices.Enumeration;
using Windows.Media.MediaProperties;
using DataSymbol.BarcodeReader;
using System.Diagnostics;
using Windows.Storage;
using System.Net;
using Windows.UI.Popups;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.Media;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Devices.Printers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScanTest1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UserPage : Page
    {
        public CommunicationLibrary ComLib = new CommunicationLibrary();

        public MediaCapture mediaCapture = new MediaCapture();

        private ObservableCollection<DeviceInformation> listOfDevices;

        private DispatcherTimer refreshTimer;

        public SoftwareBitmap colorBitmap;
        public SoftwareBitmap colorBitmap2;
        public SoftwareBitmapSource source = new SoftwareBitmapSource();

        LocalFileIO FileHandler = new LocalFileIO();

        public UserPage()
        {
            //Debug.Print("starting user page\n");
            this.InitializeComponent();

            LogInButton.IsEnabled = false;
            ConfigureButton.IsEnabled = false;
            ResetButton.IsEnabled = false;
            ScanButton.IsEnabled = false;
            ManScanButton.IsEnabled = false;

            listOfDevices = new ObservableCollection<DeviceInformation>();

            //Debug.Print("display initial logo\n");
            // Clear the picture and replace with Logo
            var pictureMap = new BitmapImage(new Uri("ms-appx:///Assets/Pi Logo v1 90 4-3.png", UriKind.RelativeOrAbsolute));
            image.Source = pictureMap;
            //Debug.Print("Logo has been displayed\n");
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            //Debug.Print("OnLoad()\n");
            UserIDBox.Text = Globals.user_id;
            DateCodeBox.Text = Globals.date_code;
            CycleCountBox.Text = (Globals.cycle_count - Globals.cycle_count_zero).ToString();
            StorageDriveBox.Text = Globals.driveName;

            if (!Globals.user_page_initialized)
            {
                //Debug.Print("Initialize scanner hardware (one time)\n");
                await InitScanner();                                  
            }

            //Force a refresh when the page is re-entered
            Globals.remote_refresh_request = true;

            if (Globals.user_id.Length > 3)
            {
                LogInButton.Content = "LOG OUT";
            }
            else
                LogInButton.Content = "LOG IN";

            //Debug.Print("Starting Timer\n");

            refreshTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200) };
            Globals.stop_all_timers = false;
            refreshTimer.Tick += refreshTimer_Tick;
            refreshTimer.Start();

            if (Globals.HandScanOption)
                ShowHandScanButton();
            else
            {
                HideHandScanButton();
            }

            // Create / refresh media capture

            if (Globals.hand_scan_count > 0)
            {
                if (!Globals.auto_scan_request)
                {
                    // This has been returned to from a manual scan event
                    await SaveBarcodeData(null, "Manual Barcodes Saved to File.", 0);
                }
                else
                {
                    ScanButtonPressedFunction();
                }
            }

            //Debug.Print("Leaving UserPage OnLoad\n");
        }

        public void refreshTimer_Tick(object source, object e)
        {
            // currently 5hz
            //Debug.Print("refreshTimer_Tick()\n");

            if (Globals.stop_all_timers)
            {
                //Debug.Print("stopping timers\n");
                refreshTimer.Stop();
                return;
            }

            if (Globals.reset_required)
            {

            }

            StorageDriveBox.Text = Globals.driveName;

            // refresh remote variables only after a packet has come in and request flag has been set
            bool scan_button_true = true;           // any inhibiting event will set this low
            string remote_rx_msg = "Receiver is ready.";
            bool color_is_red = false;

            if (++Globals.user_timeout_count > Globals.user_timout)
            {
                // Log out the user
                Globals.user_id = "";
            }

            if (Globals.user_id.Length == 0)             //user has not logged in (might have been automatically logged out
            {
                scan_button_true = false;
                display_user_message(Globals.STATUS_CODES.NOT_LOGGED_IN, true);
            }

            // Remote variables have been updated - refresh
            if (Globals.remote_refresh_request)
            {
                // refresh has been serviced
                Globals.remote_refresh_request = false;
            }

            // Perform this every timer tick
            //Debug.Print("Update screen values\n");
            UserIDBox.Text = Globals.user_id;
            DateCodeBox.Text = Globals.date_code;
            CycleCountBox.Text = (Globals.cycle_count - Globals.cycle_count_zero).ToString();

            switch (Globals.scanner_mode)
            {
                case Globals.SCANNER_MODES.STANDALONE:


                    RXDeviceStatus.Foreground = new SolidColorBrush(Colors.Green);
                    RXDeviceStatus.Text = "Standalone";


                    if (Globals.in_range == true)
                    {
                        if (Globals.LocalRFIDNum == Globals.ReadRFIDNum)
                        {
                            // Matching RFID Numbers
                            LinkStatusBox.Foreground = new SolidColorBrush(Colors.Green);
                            LinkStatusBox.Text = "Standalone Secured";

                        }
                        else
                        {
                            LinkStatusBox.Foreground = new SolidColorBrush(Colors.Red);
                            LinkStatusBox.Text = "Standalone Conflicted";
                            scan_button_true = false;
                        }
                    }
                    else
                    {
                        LinkStatusBox.Foreground = new SolidColorBrush(Colors.Orange);
                        LinkStatusBox.Text = "Standalone Unsecured";
                        scan_button_true = false;
                    }

                    break;


                case Globals.SCANNER_MODES.LOCAL:

                    if (Globals.local_ready_flag)
                    {
                        RXDeviceStatus.Foreground = new SolidColorBrush(Colors.Green);
                        RXDeviceStatus.Text = "Receiver is ready.";
                    }
                    else
                    {
                        RXDeviceStatus.Foreground = new SolidColorBrush(Colors.Red);
                        RXDeviceStatus.Text = "Receiver is not ready.";
                        //scan_button_true = false;               // Not ready disable scan
                    }

                    if (Globals.in_range == true)
                    {
                        if (Globals.LocalRFIDNum == Globals.ReadRFIDNum)
                        {
                            // Matching RFID Numbers
                            LinkStatusBox.Foreground = new SolidColorBrush(Colors.Green);
                            LinkStatusBox.Text = "Local Secured";

                        }
                        else
                        {
                            LinkStatusBox.Foreground = new SolidColorBrush(Colors.Red);
                            LinkStatusBox.Text = "Local Conflicted";
                            scan_button_true = false;
                        }
                    }
                    else
                    {
                        LinkStatusBox.Foreground = new SolidColorBrush(Colors.Orange);
                        LinkStatusBox.Text = "Local Unsecured";
                        scan_button_true = false;
                    }

                    break;

                case Globals.SCANNER_MODES.REMOTE:

                    // Check to see if the device is in auto mode
                    if (Globals.DG_auto_mode_flag == false)
                    {
                        remote_rx_msg = "Receiver not in auto mode.";
                        color_is_red = true;
                        scan_button_true = false;
                    }

                    if (Globals.DG_system_state != Globals.DG_system_states.IDLE)
                    {
                        remote_rx_msg = "Receiver is not ready.";
                        color_is_red = true;
                        scan_button_true = false;

                    }

                    if (Globals.in_range == true)
                    {
                        if (Globals.RemoteRFIDNum == Globals.ReadRFIDNum)
                        {
                            // Matching RFID Numbers
                            LinkStatusBox.Foreground = new SolidColorBrush(Colors.Green);
                            LinkStatusBox.Text = "Remote Secured";
                        }
                        else
                        {
                            LinkStatusBox.Foreground = new SolidColorBrush(Colors.Red);
                            LinkStatusBox.Text = "Remote Conflicted";
                            scan_button_true = false;
                        }
                    }
                    else
                    {
                        LinkStatusBox.Foreground = new SolidColorBrush(Colors.Orange);
                        LinkStatusBox.Text = "Remote Unsecured";
                        scan_button_true = false;
                    }

                    // Update remote receive message
                    RXDeviceStatus.Text = remote_rx_msg;
                    if (color_is_red)
                        RXDeviceStatus.Foreground = new SolidColorBrush(Colors.Red);
                    else
                        RXDeviceStatus.Foreground = new SolidColorBrush(Colors.Green);

                    break;
            }

            //user_page_initialized should no longer be needed now using async
            if ((!Globals.scanning) && (Globals.user_page_initialized) && (!Globals.resetting))
            {
                if (Globals.reset_required)
                {
                    scan_button_true = false;
                    ConfigureButton.IsEnabled = false;
                    LogInButton.IsEnabled = false;
                    ResetButton.IsEnabled = true;
                }
                else
                {
                    LogInButton.IsEnabled = true;
                    ConfigureButton.IsEnabled = true;
                    ResetButton.IsEnabled = true;

                    // see if user is logged in
                    if (Globals.user_id.Length > 0)
                    {
                        ConfigureButton.IsEnabled = true;
                        ResetButton.IsEnabled = true;
                    }
                    else
                    {
                        ResetButton.IsEnabled = false;
                        scan_button_true = false;
                    }
                }

                ScanButton.IsEnabled = scan_button_true;
                ManScanButton.IsEnabled = scan_button_true;
            }
            else
            {
                LogInButton.IsEnabled = false;
                ConfigureButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
                ScanButton.IsEnabled = false;
                ManScanButton.IsEnabled = false;
            }

            // continuous cycle for testing
            if (scan_button_true)
            {
                if (Globals.continuous_cycle_enabled && Globals.continuous_cycle_running)
                {
                    if(++Globals.continuous_cycle_delay > Globals.TESTING_DELAY_CYCLES)
                    {
                        Globals.continuous_cycle_delay = 0;
                        if((Globals.continuous_cycle_count <= Globals.TESTING_CYCLES))
                        {
                            ScanButtonPressedFunction();
                        }
                        else
                        {
                            Globals.continuous_cycle_running = false;
                        }
                    }
                }
            }
        }
       

        async Task<Globals.STATUS_CODES> InitExposure()
        {
            //Debug.Print("InitExposure()\n");

            Globals.STATUS_CODES code;

            display_user_message("Initializing: System Communication.", true);

            await ComLib.WaitForComm();                // Comm from pi to controller needs to be running for this routine to run

            await ResetSys();                   // Recalibrate the position

            await Task.Delay(100);              // Let the reset button thread set the resetting flag

            await ComLib.WaitForReset();               // Waits for reset to complete


            // Display the logo in the picture frame
            var pictureMap = new BitmapImage(new Uri("ms-appx:///Assets/Pi Logo v1 90 4-3.png", UriKind.RelativeOrAbsolute));
            image.Source = pictureMap;

            display_user_message(Globals.STATUS_CODES.CALIBRATING_EXPOSURE, true);

            await ComLib.WaitForCamera();
            await ComLib.WaitForHW();

            display_user_message("Initializing exposure settings...", true);

            code = await move_to_scan();
            if (code != Globals.STATUS_CODES.SUCCESS)
            {
                return code;
            }

            LowLagPhotoCapture lowLagCapture;
            CapturedPhoto capturedPhoto;

            mediaCapture = null;
            mediaCapture = new MediaCapture();

            await mediaCapture.InitializeAsync(Globals.mcis);

            // Prepare and capture photo
            lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
            capturedPhoto = await lowLagCapture.CaptureAsync();


            colorBitmap = capturedPhoto.Frame.SoftwareBitmap;
            await lowLagCapture.FinishAsync();
            colorBitmap.Dispose();

            // Once more
            // Prepare and capture photo

            lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
            capturedPhoto = await lowLagCapture.CaptureAsync();

            colorBitmap = capturedPhoto.Frame.SoftwareBitmap;
            await lowLagCapture.FinishAsync();

            colorBitmap.Dispose();

            code = await move_to_ready();

            if (code != Globals.STATUS_CODES.SUCCESS)
            {
                return code;
            }


            display_user_message(Globals.STATUS_CODES.SUCCESS, true);         // This is called from main and does not run async when that happens so change the message here
            Globals.exposure_init = true;

            return Globals.STATUS_CODES.SUCCESS;

        }

        public async Task InitScanner()
        {
            //Debug.Print("InitScanner\n");
            LocalFileIO FileIO = new LocalFileIO();

            Globals.STATUS_CODES code = new Globals.STATUS_CODES();


            try
            {
                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);

                if (devices.Count > 1)
                {
                    for (var i = 0; i < devices.Count; i++)
                    {
                        if (devices[i].Name == "HD USB Camera")
                        {
                            Globals.mcis.VideoDeviceId = devices[i].Id;
                            display_user_message("Initializing system: Camera initialized.", true);
                        }
                    }
                }


            }
            catch (Exception e)
            {
                //Debug.WriteLine(e.Message);
                display_user_message("Initializing system: Unable to initialize camera.", true);
            }

            Globals.scan_init = true;

            await InitExposure();

            code = await FileIO.CheckForDrive();

            if (code != Globals.STATUS_CODES.SUCCESS)
                display_user_message(code, true);

            if (Globals.decoder_lic.Length < 2)
            {
                display_user_message("Cannot read license key. Full decode inhibited.", false);
                ResultsBorder.Background = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                ResultsBorder.Background = new SolidColorBrush(Colors.LightGray);
            }

            // should not be needed
            Globals.user_page_initialized = true;

            // Enable buttons
            LogInButton.IsEnabled = true;
            ConfigureButton.IsEnabled = true;
            ResetButton.IsEnabled = true;

        }

        void DisplayData2(BarcodeDecoder dec)
        {
            //Debug.Print("DisplayData2\n");
            string results = "";
            string temp;
            uint i;

            for(i=0; i<Globals.hand_scan_count;i++)
            {
                temp = Globals.hand_scan_data[i,0];   // Barcode type

                results += Globals.hand_scan_data[i,1] + Environment.NewLine + temp + Environment.NewLine + "------------------" + Environment.NewLine;
            }

            if (dec != null)
            {
                for (i = 0; i < 7 - Globals.hand_scan_count; ++i)
                {
                    if ((i + 1) <= dec.barcodes.length)
                    {
                        temp = dec.barcodes.item(i).BarcodeType.ToString();
                        temp = temp.Substring(6);

                        results += dec.barcodes.item(i).Text + Environment.NewLine + temp + Environment.NewLine + "------------------" + Environment.NewLine;
                    }
                }
            }

            Results.Text = results;
        }

        // Call this with dec = null to log only manual data
        string CreateRecord(BarcodeDecoder dec, string condition, int code)
        {

            //Debug.Print("Create Record\n");
            string dataString = "";
            uint i;

            dataString += DateCodeBox.Text + ",";
            dataString += UserIDBox.Text + ",";
            dataString += Globals.serial_number + ",";      // This is the scanner serial number
            dataString += Globals.cycle_count + ",";

            for(i=0; i<Globals.hand_scan_count; i++)
            {
                dataString += Globals.hand_scan_data[i, 0] + ",";                 // Barcode type
                dataString += Globals.hand_scan_data[i, 1] + ",";
            }

            if (dec != null)                    // Auto decode data is present
            {
                for (i = 0; i < 7 - Globals.hand_scan_count; i++)                              // up to 7 barcodes are stored
                {
                    if (i < dec.barcodes.length)
                    {
                        dataString += dec.barcodes.item(i).BarcodeType.ToString().Substring(6) + ",";    //this is the barcode type
                        dataString += dec.barcodes.item(i).Text + ",";                      // This is  the barcode value
                    }
                    else
                    {
                        dataString += "Unused Type,";
                        dataString += "Unused Barcode,";
                    }
                }
            }
            else    // There are no automatic barcodes
            {
                for (i = 0; i < 7 - Globals.hand_scan_count; i++)                              // up to 7 barcodes are stored
                {
                    dataString += "Unused Type,";
                    dataString += "Unused Barcode,";
                }
            }

            switch(Globals.scanner_mode)
            {
                case Globals.SCANNER_MODES.LOCAL:
                    dataString += "Local,";                 // Indicate mode
                    dataString += Globals.LocalRFIDNum.ToString() + ",";
                    dataString += "Not Applicable,";        // Flux is not saved when in local/digital mode
                    dataString += code.ToString() + ",";
                    dataString += condition;                // No comma on last entry 
                    break;
                case Globals.SCANNER_MODES.REMOTE:
                    dataString += "Remote,";
                    dataString += Globals.RemoteRFIDNum.ToString() + ",";
                    dataString += Globals.MaxFluxCapture.ToString() + ",";
                    dataString += code.ToString() + ",";
                    dataString += condition;       // No comma on last entry
                    break;
                case Globals.SCANNER_MODES.STANDALONE:
                    dataString += "Standalone,";                 // Indicate mode
                    dataString += Globals.LocalRFIDNum.ToString() + ",";
                    dataString += "Not Applicable,";        // Flux is not saved when in local/digital mode
                    dataString += code.ToString() + ",";
                    dataString += condition;                // No comma on last entry 
                    break;
            }
           
            dataString += Environment.NewLine;
            
            // Data has been read, clear count
            Globals.hand_scan_count = 0;

            return (dataString);

        }

        void HideHandScanButton()
        {
            //Debug.Print("HideHandScanButton\n");
            ManScanButton.Visibility = Visibility.Collapsed;
            ScanButtonText.VerticalAlignment = VerticalAlignment.Top;
            ScanButtonText.Margin = new Thickness(0, 0, 0, 2);
            if (Globals.continuous_cycle_enabled)
            {
                ScanButtonText.Text = "100X";
            }
            else
            {
                ScanButtonText.Text = "SCAN";
            }
        }

        void ShowHandScanButton()
        {
            //Debug.Print("ShowHandScanButton()\n");
            ScanButtonText.VerticalAlignment = VerticalAlignment.Bottom;
            ScanButtonText.Text = "AUTO SCAN";
            ScanButtonText.Margin = new Thickness(0, 0, 0, -3);
            ManScanButton.Visibility = Visibility.Visible;
        }

        async Task<Globals.STATUS_CODES> SaveBarcodeData(BarcodeDecoder dec, string condition, int code)
        {
            //Debug.Print("SaveBarcodeData\n");
            string dataString = "";
            string headerString = "Date Code, User ID, Scanner SN, Cycle Count, Code Type 1, Code Value 1, Code Type 2, Code Value 2, Code Type 3, Code Value 3, Code Type 4, Code Value 4, Code Type 5, Code Value 5, Code Type 6, Code Value 6, Code Type 7, Code Value 7, Link, RFID, Flux (T), Result Code, Comment";
            try
            {
                StorageFolder externalDevices = KnownFolders.RemovableDevices;
                IReadOnlyList<StorageFolder> externalDrives = await externalDevices.GetFoldersAsync();
                StorageFolder folder = externalDrives[0];

                dataString = CreateRecord(dec, condition, code);
                string filename = UserIDBox.Text + "_" + DateCodeBox.Text + ".csv";

                display_user_message(condition, true);

                // Check to see if file exist - if it is a new file, first line will be a description header
                var item = await folder.TryGetItemAsync(filename);
                if (item == null)
                {
                    // Add header to new file
                    dataString = headerString + Environment.NewLine + dataString;
                }

                StorageFile BarcodeFile = await folder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.OpenIfExists);
                await Windows.Storage.FileIO.AppendTextAsync(BarcodeFile, dataString);

                return (Globals.STATUS_CODES.SUCCESS);
            }
            catch
            {
                return (Globals.STATUS_CODES.ERROR_WRITING_FILE);
            }
        }

        private async Task<Globals.STATUS_CODES> SaveImageToFile()
        {
            // TJV may want to override with custom name?
            // Create imageFileName userId + Date Code + cycle_count
            //Debug.Print("SaveImageToFile()\n");
            try
            {
                string filename = Globals.cycle_count.ToString() + "_" + Globals.serial_number.ToString() + "_" + UserIDBox.Text + "_" + DateCodeBox.Text + ".jpg";

                StorageFolder externalDevices = KnownFolders.RemovableDevices;
                IReadOnlyList<StorageFolder> externalDrives = await externalDevices.GetFoldersAsync();
                StorageFolder folder = externalDrives[0];

                // Check how much room remains on the USB Stick
                var retrivedProperties = await folder.Properties.RetrievePropertiesAsync(new string[] { "System.FreeSpace" });
                var USBFreeSpace = (UInt64)retrivedProperties["System.FreeSpace"];

                if (USBFreeSpace < 500000)
                    return (Globals.STATUS_CODES.INSUFFICIENT_MEMORY);

                StorageFile outputFile = await folder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.FailIfExists);

                using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // Create an encoder with the desired format
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                    // Set the software bitmap to the currently scanned image
                    encoder.SetSoftwareBitmap(colorBitmap);

                    // Set additional encoding parameters, if needed
                    //encoder.BitmapTransform.ScaledWidth = 320;
                    //encoder.BitmapTransform.ScaledHeight = 240;
                    //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                    //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    encoder.IsThumbnailGenerated = false;

                    await encoder.FlushAsync();
                }

                return (Globals.STATUS_CODES.SUCCESS);
            }
            catch
            {
                return (Globals.STATUS_CODES.ERROR_WRITING_FILE);
            }

        }


        void ClearData()
        {
            Results.Text = "";
        }

        private async Task<Globals.STATUS_CODES> CaptureAndDecode()
        {
            //Debug.Print("CaptureAndDecode\n");

            ClearData();                    // clear the display data

            Globals.STATUS_CODES err = Globals.STATUS_CODES.SUCCESS;

            // Multiple pictures are taken to allow for exposure and 
            // Prepare and capture photo
            LowLagPhotoCapture lowLagCapture;
            CapturedPhoto capturedPhoto;
            SoftwareBitmap softwareBitmap;

            mediaCapture = null;
            mediaCapture = new MediaCapture();

            await mediaCapture.InitializeAsync(Globals.mcis);
            /*
            try
            {
                lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
                capturedPhoto = await lowLagCapture.CaptureAsync();


                colorBitmap = capturedPhoto.Frame.SoftwareBitmap;
                await lowLagCapture.FinishAsync();
                colorBitmap.Dispose();
            }
            catch (Exception e)
            {
                err = Globals.STATUS_CODES.CAMERA_READ_FAILURE;
                //Debug.WriteLine(e.Message);
                return err;
            }
            */
            try
            {
                lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
                capturedPhoto = await lowLagCapture.CaptureAsync();

                colorBitmap = capturedPhoto.Frame.SoftwareBitmap;
                await lowLagCapture.FinishAsync();
                colorBitmap.Dispose();
            }
            catch (Exception e)
            {
                err = Globals.STATUS_CODES.CAMERA_READ_FAILURE;
                //Debug.WriteLine(e.Message);
                return err;
            }
            
            // Prepare and capture photo\
            try
            {
                lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
                capturedPhoto = await lowLagCapture.CaptureAsync();
                colorBitmap = capturedPhoto.Frame.SoftwareBitmap;
                await lowLagCapture.FinishAsync();

                //convert so it can be displayed, colorBitmap is a top level software bitmap
                // must be Bgra8, and premultipled or no alpha
                colorBitmap2 = SoftwareBitmap.Convert(colorBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                await source.SetBitmapAsync(colorBitmap2);
                image.Source = source;                      // Display the picture


                // convert to barcode readable
                softwareBitmap = SoftwareBitmap.Convert(colorBitmap, BitmapPixelFormat.Gray8);
 
                byte[] bytes;
                Windows.Storage.Streams.Buffer buf = new Windows.Storage.Streams.Buffer((uint)(softwareBitmap.PixelHeight * softwareBitmap.PixelWidth));

                softwareBitmap.CopyToBuffer(buf);
                bytes = buf.ToArray(0, softwareBitmap.PixelHeight * softwareBitmap.PixelWidth);

                DW_ERRORS dw_err = MainPage.m_dec.Decode(bytes, (int)softwareBitmap.PixelWidth, (int)softwareBitmap.PixelHeight, MainPage.m_rect);
                if (dw_err != DW_ERRORS.DW_OK)
                {
                    err = Globals.STATUS_CODES.BARCODE_CONVERSION_ERROR;
                }
            }
            catch (Exception e)
            {
                err = Globals.STATUS_CODES.CAMERA_READ_FAILURE;
                //Debug.WriteLine(e.Message);
            }

            return err;
        }

        private async Task<bool> CheckForDrive()
        {
            //Debug.Print("CheckForDrive()\n");
            Globals.STATUS_CODES code;
            //check for valid prox ID before starting connection
            // if there is a serial connection, then the ID must match, if there is not a serial connection, the ID must be
            LocalFileIO FileIO = new LocalFileIO();

            // First check to see if a drive is available and there is sufficient memory
            code = await FileIO.CheckForDrive();

            if (code != Globals.STATUS_CODES.SUCCESS)
            {
                if (code == Globals.STATUS_CODES.DRIVE_NOT_PRESENT)
                {
                    display_user_message("USB drive is not present.  Scanning not started.", true);
                    return false;
                }
                else if (code == Globals.STATUS_CODES.INSUFFICIENT_DRIVE_MEMORY)
                {
                    display_user_message("Insufficient USB drive memory.  Scanning not started.", true);
                    return false;
                }
                else // Unknown response
                {
                    display_user_message("Failure to Open USB drive. Scanning not started.", true);
                    return false;
                }
            }
            return true;
        }


        // Main scanning state machine
        private async Task ScanCycle()
        {
            //Debug.Print("ScanCycle()\n");
            Globals.STATUS_CODES code;
            //check for valid prox ID before starting connection
            // if there is a serial connection, then the ID must match, if there is not a serial connection, the ID must be
            LocalFileIO FileIO = new LocalFileIO();

            // First check to see if a drive is available and there is sufficient memory
            code = await FileIO.CheckForDrive();

            if (!await CheckForDrive())
            {
                return;     // USB drive is not present, return
            }

            await SetContinuousProx();               // Sets the continous prox flag on the controller board to monitor connection during cycle
            UInt32 prevCycleCount = Globals.cycle_count;

            display_user_message(Globals.STATUS_CODES.WAITING_TO_SCAN, true);

            // This should not be needed and may be a place where things could hang if it gets here without the exposure being calibrated.
            //await ComLib.WaitForExposure();        // this checks to see that the camera has been initialized

            code = await move_to_scan();

            if (code != Globals.STATUS_CODES.SUCCESS)
            {
                display_user_message("Carriage halted while moving to scan position! Please clear jam and press RESET.", true);
                Globals.reset_required = true;
                return;
            }


            Globals.STATUS_CODES err = await CaptureAndDecode();         // read scan results

            if (err == Globals.STATUS_CODES.SUCCESS)
            {
                // Display the data to be saved
                DisplayData2(MainPage.m_dec);

                // Confirm proceeding if configured for manual confirmation
                if (Globals.ManualScanConfirm == true)
                {
                    Globals.ManualForceEject = false;

                    // Display Confirmation popup
                    ConfirmPopup.IsOpen = true;

                    // Wait for popup to close
                    while (ConfirmPopup.IsOpen == true)
                        await Task.Delay(50);

                    if (Globals.ManualForceEject == true)
                    {
                        Globals.ManualForceEject = false;
                        // Eject the drive
                        code = await move_to_eject();
                        if (code != Globals.STATUS_CODES.SUCCESS)
                        {
                            display_user_message("Manual Eject. Carriage halted while moving to eject! Please clear jam and press RESET.", true);
                            Globals.reset_required = true;
                            return;
                        }

                        await Task.Delay(400);          // Let the disk slide out

                        if (Globals.eject_full)
                        {
                            display_user_message("Manual Eject. Eject chute is full. Please clear jam and press RESET.", true);
                            Globals.reset_required = true;
                            return;
                        }

                        code = await move_to_ready();
                        if (code != Globals.STATUS_CODES.SUCCESS)
                        {
                            display_user_message("Manual Eject. Carriage halted while moving to load position! Please clear jam and press RESET.", true);
                            Globals.reset_required = true;
                            return;
                        }

                        display_user_message("Manual Eject. Drive ejected.", true);
                        return;
                    }

                }
                else
                {
                    // Proceed as usual
                }
            }
            else
            {
                string msg = String.Format("Capture and Decode Error Number: {0}.  Press RESET.", err);
                display_user_message(msg, true);
                Globals.reset_required = true;
                return;
            }


            // Don't go any further if there are insufficient bar codes, unless this is following a hand scan and is just there to capture the picture even when there are no codes

            if ((MainPage.m_dec.barcodes.length < Globals.minimum_decode_count) && (!Globals.auto_scan_request))
            {
                code = await move_to_eject();
                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    display_user_message( "Insufficient barcodes. Carriage halted while moving to eject! Please clear jam and press RESET.", true);
                    Globals.reset_required = true;
                    return;
                }
                    
                await Task.Delay(400);          // Let the disk slide out

                if(Globals.eject_full)
                {
                    display_user_message("Insufficient barcodes. Eject chute is full. Please clear jam and press RESET.", true);
                    Globals.reset_required = true;
                    return;
                }

                code = await move_to_ready();
                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    display_user_message( "Insufficient barcodes. Carriage halted while moving to load position! Please clear jam and press RESET.", true);
                    Globals.reset_required = true;
                    return;
                }
                    
                display_user_message( "Insufficient barcodes. Drive ejected.", true);
                return;
            }

            

            //*********************************************************************************
            // NOTE ANY RETURNS PAST THIS POINT REQUIRE DATA TO BE SAVED FIRST
            //*********************************************************************************
            await IncrementCycleCount();            // Tell the controller the cycle has happened

            // Check to see that cycle count has incremented - this assures that the continuous prox flag has been updated as well
            // This looks at the scanner cycle counter
            await ComLib.WaitForCycleCountUpdate(prevCycleCount);

            // TJV Should be more selective as to when to save - for now always do it TJV
            code = await SaveImageToFile();

           switch(code)
            {
                case Globals.STATUS_CODES.INSUFFICIENT_MEMORY:
                    UserMsg.Text += "Image not saved - Insufficient Space!";
                    break;
                case Globals.STATUS_CODES.ERROR_WRITING_FILE:
                    UserMsg.Text += "File write error.  Unable to write image file.";
                    break;
                case Globals.STATUS_CODES.SUCCESS:
                    break;
                default:
                    UserMsg.Text += "Unknown error writing image file";
                    break;
            }

            // do we want to record the tamper event to the file?
            if (!Globals.continuous_prox)
            {
                // There has been tampering during the cycle
                Globals.tamper_flag = true;         // Log a tamper event

                code = await move_to_eject();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    // Save with comment
                    code = await SaveBarcodeData(MainPage.m_dec, "TAMPER EVENTS: Scanner tamper detected during scanning, and motion halted on move to eject positon.", 1);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                await Task.Delay(400);          // Let the disk slide out

                code = await move_to_ready();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    code = await SaveBarcodeData(MainPage.m_dec, "TAMPER EVENTS: Scanner tamper detected during scanning, device ejected but motion was halted while moving to input position.",2);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                // Successfully ejected the tampered drive
                code = await SaveBarcodeData(MainPage.m_dec, "TAMPER EVENT: Scanner tamper detected during scanning.  Device ejected.",3);
                if(code != Globals.STATUS_CODES.SUCCESS)
                {
                    UserMsg.Text += " Error writing to log file!!";
                }

                return;
            }

            //********************************************************************************
            // No tamper event, sufficient barcodes, start handshake with external machine
            //********************************************************************************

            // Send a command to controller to look for a not ready event.  
            // This is used for digital handshake, ignored for serial
            await arm_not_ready();
        
            //*****************************************************************************
            if(Globals.scanner_mode == Globals.SCANNER_MODES.LOCAL)
            {

                code = await move_to_transfer();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    code = await SaveBarcodeData(MainPage.m_dec, "TAMPER EVENT: Scanner motion halted during movement to transfer position.", 7);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                code = await TransferEmpty();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    code = await SaveBarcodeData(MainPage.m_dec, "Failure to transfer drive to receiving unit.  Jam in exit chute.", 8);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                // Local Handshake
                code = await LocalDeviceNotReady();

                string msg = "";

                if (code != Globals.STATUS_CODES.SUCCESS)
                    msg = "No confirmation from receiving device. ";

                code = await move_to_ready();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    msg += "Failure to move to load position.";
                    code = await SaveBarcodeData(MainPage.m_dec, msg, 9);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                msg += "Barcodes read and logged.";

                code = await SaveBarcodeData(MainPage.m_dec, msg, 0);

                if (code != Globals.STATUS_CODES.SUCCESS)
                    UserMsg.Text += " Error writing to log file!!";
                  
                return;
            }


            //**********************************************************************************

            if (Globals.scanner_mode == Globals.SCANNER_MODES.STANDALONE)
            {

                code = await move_to_transfer();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    code = await SaveBarcodeData(MainPage.m_dec, "TAMPER EVENT: Scanner motion halted during movement to transfer position.", 7);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                code = await TransferEmpty();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    code = await SaveBarcodeData(MainPage.m_dec, "Failure to transfer drive to receiving unit.  Jam in exit chute.", 8);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                // No Local Handshake
                string msg = "";

                code = await move_to_ready();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    msg += "Failure to move to load position.";
                    code = await SaveBarcodeData(MainPage.m_dec, msg, 9);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                msg += "Barcodes read and logged.";

                code = await SaveBarcodeData(MainPage.m_dec, msg, 0);

                if (code != Globals.STATUS_CODES.SUCCESS)
                    UserMsg.Text += " Error writing to log file!!";

                return;
            }

            //*******************************************************************************************

            if (Globals.scanner_mode == Globals.SCANNER_MODES.REMOTE)    // Remote serial handshake mode
            {
                // Save the current cycle count
                uint dg_cycle_count = Globals.DG_cycle_count;

                Globals.MaxFluxCapture = 0;

                UserMsg.Text = "Transfering drive to Degausser.";

                code = await move_to_transfer();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    code = await SaveBarcodeData(MainPage.m_dec, "TAMPER EVENT: Scanner motion halted during movement to transfer position.",11);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                code = await TransferEmpty();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    code = await SaveBarcodeData(MainPage.m_dec, "Failure to transfer drive to receiving unit.  Jam in exit chute.",12);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                UserMsg.Text = "Degaussing Drive...";

                string msg = "";            // Message for report

                // Call with the cycle count that was present before the device was passed to the degausser - to be safe if there is a fast cycle

                code = await DG_CycleCountIncremented(dg_cycle_count);

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    msg += "No confirmation from receiving device. ";
                    UserMsg.Text += " No confirmation from receiving device.";
                }
                else
                {
                    Globals.MaxFluxCapture = ((double)Globals.MaxFlux) / 1000;
                }

                code = await move_to_ready();

                if (code != Globals.STATUS_CODES.SUCCESS)
                {
                    msg += "Failure to move to load position.";
                    code = await SaveBarcodeData(MainPage.m_dec, msg,13);
                    UserMsg.Text += " Please clear jam and press RESET.";
                    Globals.reset_required = true;
                    if (code != Globals.STATUS_CODES.SUCCESS)
                        UserMsg.Text += " Error writing to log file!!";

                    return;
                }

                msg += "Barcodes read and logged.";

                code = await SaveBarcodeData(MainPage.m_dec, msg, 0);                   // Good read

                if (code != Globals.STATUS_CODES.SUCCESS)
                    UserMsg.Text += " Error writing to log file!!";

                return;
            }





        }

        private void display_user_message(string msg_text, bool clear)
        {
            //Debug.Print("display_user_message()\n");
            if (clear)
                UserMsg.Text = msg_text;
            else
            {
                if (UserMsg.Text.Length > 0)
                    UserMsg.Text += " ";
                UserMsg.Text += msg_text;
            }
        }

        private void display_user_message(Globals.STATUS_CODES code, bool clear)
        {
            //Debug.Print("display_user_message(2)\n");
            string msg_text = "";
            if (Globals.user_page_initialized)          // initializing messages are dominant
            {
                switch (code)
                {
                    case Globals.STATUS_CODES.FULL_CHUTE:
                        msg_text += "Transfer chute is full.  Please clear and press reset.";
                        Globals.reset_required = true;
                        break;
                    case Globals.STATUS_CODES.STALL:
                        msg_text += "Movement has been halted.  Please clear jam and press reset.";
                        Globals.reset_required = true;
                        break;
                    case Globals.STATUS_CODES.INSUFFICIENT_BARCODES:
                        msg_text += "Insufficient number of barcodes decoded, device ejected.";
                        break;
                    case Globals.STATUS_CODES.MOTION_TIMEOUT:
                        msg_text += "Movement has been halted.  Please clear jam and press reset.";
                        Globals.reset_required = true;
                        break;
                    case Globals.STATUS_CODES.SUCCESS:
                        msg_text += "";
                        break;
                    case Globals.STATUS_CODES.WAITING_TO_SCAN:
                        msg_text += "Scanning and Decode in process. Please wait.";
                        break;
                    case Globals.STATUS_CODES.RESETTING:
                        msg_text += "System is resetting. Please wait.";
                        break;
                    case Globals.STATUS_CODES.CALIBRATING_EXPOSURE:
                        msg_text += "Calibrating Camera Exposure. Please wait.";
                        break;
                    case Globals.STATUS_CODES.DECODING:
                        msg_text += "Decoding Image...";
                        break;
                    case Globals.STATUS_CODES.TAMPER:
                        msg_text += "Process Tampering Detected!!";
                        break;
                    case Globals.STATUS_CODES.INSUFFICIENT_DRIVE_MEMORY:
                        msg_text += "USB Drive has insufficient memory, scanning disallowed.";
                        break;
                    case Globals.STATUS_CODES.DRIVE_NOT_PRESENT:
                        msg_text += "USB Drive not present, scanning disallowed.";
                        break;
                    case Globals.STATUS_CODES.NOT_LOGGED_IN:
                        msg_text += "Log in required.";
                        LogInButton.Content = "LOG IN";
                        break;
                }

                if (clear)
                    UserMsg.Text = msg_text;
                else
                {
                    if (UserMsg.Text.Length > 0)
                        UserMsg.Text += " ";
                    UserMsg.Text += msg_text;
                }
            }
        }

        private async void ScanButtonPressedFunction()
        {
            
            //Debug.Print("ScanButtonClick()\n");
            Globals.user_timeout_count = 0;             // User is still active

            if (Globals.eject_full)
            {
                display_user_message("Eject chute is full, please clear and press RESET before scanning.", true);
                return;
            }

            if (Globals.chute_full)
            {
                display_user_message("Transfer chute is full, please clear and press RESET before scanning.", true);
                return;
            }

            if ((Globals.scanner_mode == Globals.SCANNER_MODES.LOCAL) && (!Globals.local_ready_flag))
            {
                display_user_message("Local device not ready, scanning inhibited.", true);
                return;
            }


            Globals.tamper_flag = false;

            if (Globals.scanning)
                return;
            Globals.scanning = true;

            // Clear the picture and replace with Logo
            var pictureMap = new BitmapImage(new Uri("ms-appx:///Assets/Pi Logo v1 90 4-3.png", UriKind.RelativeOrAbsolute));
            image.Source = pictureMap;

            if(Globals.continuous_cycle_running)
                Globals.continuous_cycle_count++;

            await ScanCycle();                      // main scanning state machine

            Globals.auto_scan_request = false;      // Clear the request if present
            Globals.hand_scan_count = 0;            // Clear if present

            Globals.scanning = false;
        }
        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {

            if (Globals.continuous_cycle_enabled && Globals.continuous_cycle_running)
            {
                // The button has been pressed to abort
                Globals.continuous_cycle_running = false;
                return;
            }

            if (Globals.continuous_cycle_enabled && (!Globals.continuous_cycle_running))
            {
                Globals.continuous_cycle_count = 1;
                Globals.continuous_cycle_running = true;
            }

            ScanButtonPressedFunction();
        }


        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            await ResetSys();
        }


        private async Task ResetSys()
        {
            //Debug.Print("ResetButton_click()\n");
            if (Globals.resetting == true)
                return;

            if (Globals.eject_full)
            {
                display_user_message("Eject chute is full, please clear and press RESET.", true);
                Globals.resetting = false;
                return;
            }

            if (Globals.chute_full)
            {
                display_user_message("Transfer chute is full, please clear and press RESET.", true);
                Globals.resetting = false;
                return;
            }

            Globals.resetting = true;

            Globals.STATUS_CODES code;

            var tx_buff = new byte[2];
            tx_buff[0] = Globals.INITIALIZE_SYSTEM;
            display_user_message(Globals.STATUS_CODES.RESETTING, true);

            // send out the reset packet
            int retry_delay_counter = 0;
            int retry_delay = 20;            // 2 second
            await SerialClass.write_ppp(tx_buff);
            // Retry until the system starts intializing
            while (Globals.hw_initializing != true)
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }

            await ComLib.WaitForHW();                  // Wait for the HW to be ready

            // HW reset should cause the drum to move to eject position

            await Task.Delay(1200);                  // Wait for Reset movement to complete and let sit at eject position


            // This should be redundant, but it makes sure we are in the eject position before moving on
            code = await move_to_eject();

            if (code != Globals.STATUS_CODES.SUCCESS)
            {
                display_user_message(code, true);
                Globals.resetting = false;
                return;
            }

            // Let it sit at the eject position for a moment to allow drive to slide out if present
            await Task.Delay(400);

            code = await move_to_ready();


            if (code != Globals.STATUS_CODES.SUCCESS)
            {
                display_user_message(code, true);
                Globals.resetting = false;
                return;
            }

            // display the message from the code if any
            display_user_message(Globals.STATUS_CODES.SUCCESS, true);

            Globals.resetting = false;
            Globals.reset_required = false;


            // should not be needed
            if (Globals.user_page_initialized)
            {
                LogInButton.IsEnabled = true;
                ConfigureButton.IsEnabled = true;
            }
        }


        private async Task<Globals.STATUS_CODES> move_to_scan()
        {
            //Debug.Print("move_to_scan()\n");

            var tx_buff = new byte[2];
            tx_buff[0] = Globals.MOVE_TO_SCAN;

            // send out the move to scan packet
            int retry_delay_counter = 0;
            int retry_delay = 20;
            await SerialClass.write_ppp(tx_buff);
            // Retry until the system starts moving to scan position
            while ((Globals.CameraState != Globals.C_STATE.SCAN_POS) && (Globals.CameraState != Globals.C_STATE.SEEKING_SCAN))
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }

            int wait_count = 0;
            Globals.motor_stalled = false;
            while (Globals.CameraState != Globals.C_STATE.SCAN_POS)
            {
                if (Globals.motor_stalled)
                {
                    return Globals.STATUS_CODES.STALL;
                }
                wait_count++;
                if (wait_count > Globals.motion_timeout_ticks)
                {
                    return Globals.STATUS_CODES.MOTION_TIMEOUT;               // Failed to reach scan position in time
                }
                await Task.Delay(200);          // 200 ms delay for before checking position again
            }
            return Globals.STATUS_CODES.SUCCESS;
        }

        private async Task arm_not_ready()
        {
            // This clears the not ready flag
            var tx_buff = new byte[2];
            tx_buff[0] = Globals.ARM_NOT_READY_FLAG;

            int retry_delay_counter = 0;
            int retry_delay = 20;

            await SerialClass.write_ppp(tx_buff);
            // Retry until the flag is cleared to a false
            while (Globals.not_ready_latch)
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }
        }

        //Care needs to be taken to not send a double increment on retries
        private async Task IncrementCycleCount()
        {
            UInt32 previous_count = Globals.cycle_count;

            var tx_buff = new byte[2];
            tx_buff[0] = Globals.INCREMENT_CYCLE_COUNT;
            
            // send out the move to scan packet
            int retry_delay_counter = 0;
            int retry_delay = 21;       // 4 seconds to make sure we don't double hit
            await SerialClass.write_ppp(tx_buff);
            // Retry until the count is incremented - this needs to have a large delay between tries to avoid double increments
            while (previous_count == Globals.cycle_count)
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }
        }

        private async Task SetContinuousProx()
        {
            //Debug.Print("SetContinuousProx()\n");
            var tx_buff = new byte[2];
            tx_buff[0] = Globals.SET_CONTINUOUS_PROX;

            int retry_delay_counter = 0;
            int retry_delay = 20;
            await SerialClass.write_ppp(tx_buff);
            // Retry until the flag is cleared to a false
            while (!Globals.continuous_prox)
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }
        }

        private async Task<Globals.STATUS_CODES> move_to_eject()
        {
            var tx_buff = new byte[2];
            tx_buff[0] = Globals.MOVE_TO_EJECT;

            int retry_delay_counter = 0;
            int retry_delay = 20;
            await SerialClass.write_ppp(tx_buff);
            while ((Globals.CameraState != Globals.C_STATE.EJECT_POS) && (Globals.CameraState != Globals.C_STATE.SEEKING_EJECT))
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }

            int wait_count = 0;
            while (Globals.CameraState != Globals.C_STATE.EJECT_POS)
            {
                if (Globals.motor_stalled)
                {
                    return Globals.STATUS_CODES.STALL;
                }
                wait_count++;
                if (wait_count > Globals.motion_timeout_ticks)
                {
                    return Globals.STATUS_CODES.MOTION_TIMEOUT;               // Failed to reach scan position in time
                }

                await Task.Delay(200);          // 200 ms delay for before checking position again
            }
            return Globals.STATUS_CODES.SUCCESS;
        }

        private async Task<Globals.STATUS_CODES> move_to_transfer()
        {
            //Debug.Print("move_to_transfer()\n");
            var tx_buff = new byte[2];
            tx_buff[0] = Globals.MOVE_TO_TRANSFER;

            int retry_delay_counter = 0;
            int retry_delay = 20;
           
            await SerialClass.write_ppp(tx_buff);
            while ((Globals.CameraState != Globals.C_STATE.SEEKING_TRANSFER) && (Globals.CameraState != Globals.C_STATE.TRANSFER_POS))
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }

            int wait_count = 0;
            while (Globals.CameraState != Globals.C_STATE.TRANSFER_POS)
            {
                if (Globals.motor_stalled)
                {
                    return Globals.STATUS_CODES.STALL;
                }
                wait_count++;
                if (wait_count > Globals.motion_timeout_ticks)
                {
                    return Globals.STATUS_CODES.MOTION_TIMEOUT;               // Failed to reach scan position in time
                }

                await Task.Delay(200);          // 200 ms delay for before checking position again
            }
            return Globals.STATUS_CODES.SUCCESS;
        }

        private async Task<Globals.STATUS_CODES> move_to_ready()
        {
            //Debug.Print("move_to_ready()\n");
            var tx_buff = new byte[2];
            tx_buff[0] = Globals.MOVE_TO_READY;

            int retry_delay_counter = 0;
            int retry_delay = 20;

            await SerialClass.write_ppp(tx_buff);
            while ((Globals.CameraState != Globals.C_STATE.SEEKING_READY) && (Globals.CameraState != Globals.C_STATE.READY_POS))
            {
                if (++retry_delay_counter >= retry_delay)
                {
                    await SerialClass.write_ppp(tx_buff);
                    retry_delay_counter = 0;
                }
                await Task.Delay(200);
            }

            int wait_count = 0;
            Globals.motor_stalled = false;

            while (Globals.CameraState != Globals.C_STATE.READY_POS)
            {
                if (Globals.motor_stalled)
                {
                    return Globals.STATUS_CODES.STALL;
                }

                wait_count++;
                if (wait_count > Globals.motion_timeout_ticks)
                {
                    return Globals.STATUS_CODES.MOTION_TIMEOUT;               // Failed to reach scan position in time
                }
                await Task.Delay(200);          // 200 ms delay for before checking position again
            }
            return Globals.STATUS_CODES.SUCCESS;
        }


        private async Task<Globals.STATUS_CODES> TransferEmpty()
        {
            //Debug.Print("TransferEmpty()\n");
            int wait_count = 0;
            while (Globals.chute_full)
            {
                wait_count++;
                if (wait_count > Globals.motion_timeout_ticks)
                {
                    return Globals.STATUS_CODES.FULL_CHUTE;      // Failed to reach scan position in time
                }
                await Task.Delay(200);                  // 200 ms delay for before checking position again
            }
            return Globals.STATUS_CODES.SUCCESS;
        }

        private async Task<Globals.STATUS_CODES> LocalDeviceNotReady()
        {
            //Debug.Print("LocalDeviceNotReady()\n");
            int wait_count = 0;
            while (!Globals.not_ready_latch)            // wait for a not ready event to be latched
            {
                wait_count++;
                if (wait_count > Globals.wait_not_ready_ticks)
                {
                    return Globals.STATUS_CODES.WAIT_TIMEOUT;      // Failed to go not ready in time
                }
                await Task.Delay(200);                  // 200 ms delay for before checking position again
            }
            return Globals.STATUS_CODES.SUCCESS;
        }

        private async Task<Globals.STATUS_CODES> DG_CycleCountIncremented(uint cycle_count)
        {
            //Debug.Print("DG_cycelcountincremented()\n");
            int wait_count = 0;
            while (cycle_count == Globals.DG_cycle_count)          // Wait for the cycle count to increment
            {
                wait_count++;
                if (wait_count > Globals.wait_cycle_done_ticks)
                {
                    return Globals.STATUS_CODES.WAIT_TIMEOUT;      // Failed to go not ready in time
                }
                await Task.Delay(200);                  // 200 ms delay for before checking position again
            }
            return Globals.STATUS_CODES.SUCCESS;
        }


        private async Task old_FileButton_Click(object sender, RoutedEventArgs e)
        {
            //Debug.Print("odl_filebutton_click()\n");
            var openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".txt");
            StorageFile file = await openPicker.PickSingleFileAsync();
            // Process picked file
            if (file != null)
            {
                // Store file for future access
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
            }
            else
            {
                // The user didn't pick a file
            }
        }

        private async Task test_FileButton_Click(object sender, RoutedEventArgs e)
        {
            //Debug.Print("test_filebutton_click()\n");
            await move_to_ready();
        }

        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            //Debug.Print("loginbuttonclick()\n");
            // user is trying to log out
            if (LogInButton.Content.ToString() == "LOG OUT")
            {
                Globals.user_id = "";
                LogInButton.Content = "LOG IN";
                return;
            }

            // Reset user timeout value
            Globals.user_timeout_count = 0;

            // User is trying to log in, open the log in page
            refreshTimer.Stop();
            Globals.remote_refresh_request = true;
            Frame.Navigate(typeof(UserIDPage));
            //await CheckForDrive();


            /*
            StorageFolder externalDevices = KnownFolders.RemovableDevices;
            IReadOnlyList<StorageFolder> externalDrives = await externalDevices.GetFoldersAsync();
            StorageFolder folder = externalDrives[0];
            //StorageFile sampleFile = await x.CreateFileAsync("sample.txt");
            StorageFile sampleFile = await folder.CreateFileAsync("sample.txt", Windows.Storage.CreationCollisionOption.OpenIfExists);
            await Windows.Storage.FileIO.AppendTextAsync(sampleFile, "Swift as a shadow");
            */

        }

        private void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {

            //Debug.Print("Configurebuttonclick()\n");

            refreshTimer.Stop();
            Globals.remote_refresh_request = true;
            //Frame.Navigate(typeof(ConfigureMachinePage));

            // These values eventually need to be read from local disk?
            Globals.passwordValue = "123456";
            Globals.passwordTitle = "SUPERVISOR PASSWORD";
            Globals.pageType = Globals.PAGE_TYPES.SUPERVISOR_PAGE;
            //Debug.Print("Navigating to Password Page\n");
            Frame.Navigate(typeof(PasswordPage));
        }

        private void ZeroButton_Click(object sender, RoutedEventArgs e)
        {
            //Debug.Print("zerobuttonclick()\n");
            Globals.cycle_count_zero = Globals.cycle_count;
        }

        private async void ManScanButton_Click(object sender, RoutedEventArgs e)
        {
            //Debug.Print("manscanbuttonclick()\n");
            Globals.user_timeout_count = 0;         // User is still active

            if (await CheckForDrive())  // Only navigate to hand scan page if memory storage is ready
            {
                Frame.Navigate(typeof(HandScanPage));
            }
        }

        private void PopContinueButton_Click(object sender, RoutedEventArgs e)
        {
            //Debug.Print("popcontinuebuttonclick()\n");
            Globals.ManualForceEject = false;
            ConfirmPopup.IsOpen = false;
        }

        private void PopEjectButton_Click(object sender, RoutedEventArgs e)
        {
            //Debug.Print("PopEjectButton_Click()\n");
            Globals.ManualForceEject = true;
            ConfirmPopup.IsOpen = false;
        }
    }
}
