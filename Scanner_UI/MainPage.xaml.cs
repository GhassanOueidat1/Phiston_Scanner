using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using System.Net;


using DataSymbol.BarcodeReader;
using System.Collections.ObjectModel;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Pickers;
using PhistonUI;
using Windows.UI.ViewManagement;
using ScanTest1;
using Windows.System;
using Windows.ApplicationModel;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanTest1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {

        public CommunicationLibrary ComLib = new CommunicationLibrary();

        public LocalFileIO FileHandler = new LocalFileIO();

        // Bug work around Instantiate the decoder only once in MainPage
        public static BarcodeDecoder m_dec = null;
        public static DW_RECT m_rect;


        public MainPage()
        {

            this.InitializeComponent();

            // Initialize the start time
            // Aug 15 2019
            DateTime time = new DateTime(2019, 8, 15);
            DateTimeOffset offset = new DateTimeOffset(time);

            // get the package architecure
            Package package = Package.Current;
            string systemArchitecture = package.Id.Architecture.ToString();
                                 
            if (systemArchitecture == "Arm")
                DateTimeSettings.SetSystemDateTime(offset);

                //ApplicationView.PreferredLaunchViewSize = new Size(800, 480);
                //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

        }

        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.scanning != true)
            {
                // Stop the timers?
                Globals.stop_all_timers = true;

                Globals.passwordValue = "654321";
                Globals.passwordTitle = "FACTORY SETUP";
                Globals.pageType = Globals.PAGE_TYPES.FACTORY_PAGE;
                MainFrame.Navigate(typeof(PasswordPage));
            }
        }
        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            // Read in the configuration
            await FileHandler.UpdateFieldsFromFile();

            // Read in the decoder key
            await FileHandler.LoadKey();
            
            // Initialize the decoder object
            Init();
            
            // Serial Class never returns, stays instantiated looking at serial port
            SerialClass.Instance.comPortStart();

            MainFrame.Navigate(typeof(UserPage));
        }


        private void Init()
        {
            //Debug.Print("Init() - ");
            //Debug.Print("Create Barcode object\n");
            m_dec = new BarcodeDecoder(Globals.decoder_lic);
            //Debug.Print("Barcode object created\n");
            m_rect.left = m_rect.top = m_rect.right = m_rect.bottom = 0;
            DW_DECODEPARAMS par = new DW_DECODEPARAMS();
            par.BarcodeTypes = (uint)(DW_BARTYPES.DW_ST_AZTECCODE | DW_BARTYPES.DW_ST_CODABAR | DW_BARTYPES.DW_ST_CODE11 | DW_BARTYPES.DW_ST_CODE128 | DW_BARTYPES.DW_ST_CODE39
                        | DW_BARTYPES.DW_ST_CODE93 | DW_BARTYPES.DW_ST_DATABAR_EXP | DW_BARTYPES.DW_ST_DATABAR_EXP_STACKED | DW_BARTYPES.DW_ST_DATABAR_LIM | DW_BARTYPES.DW_ST_DATABAR_OMNI
                        | DW_BARTYPES.DW_ST_DATABAR_STACKED | DW_BARTYPES.DW_ST_DATAMATRIX | DW_BARTYPES.DW_ST_EAN13 | DW_BARTYPES.DW_ST_EAN8 | DW_BARTYPES.DW_ST_INDUSTR25 | DW_BARTYPES.DW_ST_INTERL25
                        | DW_BARTYPES.DW_ST_PDF417 | DW_BARTYPES.DW_ST_QRCODE | DW_BARTYPES.DW_ST_UPCA | DW_BARTYPES.DW_ST_UPCE);
            
            // Set decode speeds for maximum readability
            par.LinearDecSpeed = DW_DECSPEED.DWS_SLOW;
            par.PDF417DecSpeed = DW_DECSPEED.DWS_SLOW;
            par.DataMatrixDecSpeed = DW_DECSPEED.DWS_SLOW;
            par.QRCodeDecSpeed = DW_DECSPEED.DWS_SLOW;
            par.AztecCodeDecSpeed = DW_DECSPEED.DWS_SLOW;

            par.LinearFindBarcodes = 7;
            par.PDF417FindBarcodes = 7;
            par.DataMatrixFindBarcodes = 7;
            par.QRCodeFindBarcodes = 7;
            par.AztecCodeFindBarcodes = 7;

            //Debug.Print("Barcode set parameters\n");
            m_dec.SetDecoderParams(par);
            //Debug.Print("Barcode parameters set\n");
        }

    }
}
