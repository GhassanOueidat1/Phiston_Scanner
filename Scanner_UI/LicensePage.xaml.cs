using DataSymbol.BarcodeReader;
using PhistonUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScanTest1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    


    public sealed partial class LicensePage : Page
    {
        LocalFileIO FileHandler = new LocalFileIO();

        public LicensePage()
        {
            this.InitializeComponent();

            DateTime now = DateTime.Now;
            string asString = now.ToString("MM/dd/yyyy");
            TimeBox.Text = asString;
        }

        private async void SaveLicenseButton_Click(object sender, RoutedEventArgs e)
        {


            StatusBox.Text = "Please wait...";

            string sActivationKey = "";

            sActivationKey = LicenseKey.Text;

            sActivationKey.Trim();

            if (sActivationKey.Length < 10)
            {
                StatusBox.Text = "Improper Key Length.";             
                return;
            }

            string sKey;

            try
            {
                //request the key via HTTP
                Uri addrUri = new Uri(string.Format("http://rkdsoft.com/gethidkey.php?tid={0}&hid={1}&sresp=0", sActivationKey, BarcodeDecoder.DeviceID), UriKind.Absolute);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(addrUri);
                WebResponse resp = await req.GetResponseAsync();
                Stream stream = resp.GetResponseStream();
                byte[] buf = FileHandler.ReadStream(stream);
                sKey = System.Text.Encoding.UTF8.GetString(buf);
            }
            catch
            {
                StatusBox.Text = "Communication Error. Check Internet Connection";
                return;
            }

            //check the received key
            if (!ParseResponse(out sKey, sKey))
            {
                StatusBox.Text = "Error. Received Key is incorrect: " + sKey;
                return;
            }

            if (await FileHandler.SaveKey(sKey))
            {
                Globals.decoder_lic = sKey;
                StatusBox.Text = "Key Saved";
                // Optionally display the full key status
                StatusBox.Text = await FileHandler.ShowAboutInfo();
            }
            else
                StatusBox.Text = "Error. Cannot save.";
        }
      
        private bool ParseResponse(out string sKeyErr, string sResp)
        {
            sKeyErr = "Error. Wrong Response.";

            //error, No key
            if (sResp == null || sResp.Length < 14)
                return false;

            if (!sResp.StartsWith("Error-"))
                return false;

            int err = 1;
            try
            {
                //get error code
                string sErrCode = sResp.Substring(6, 4);
                err = System.Convert.ToInt32(sErrCode);

                //get the key or error
                sKeyErr = sResp.Substring(11);
            }
            catch (System.Exception)
            {
            }

            return err == 0 ? true : false;
        }

      

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(UserPage));
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            LicenseStatusBox.Text = await FileHandler.ShowAboutInfo();
        }
    }
}
