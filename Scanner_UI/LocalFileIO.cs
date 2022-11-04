using System.IO;
using System.Threading.Tasks;
using PhistonUI;
using Windows.Storage;
using System;
using System.Collections.Generic;
using DataSymbol.BarcodeReader;

namespace ScanTest1
{
    public class LocalFileIO
    {
        public async Task<Globals.STATUS_CODES> CheckForDrive()
        {
            try
            {
                StorageFolder externalDevices = KnownFolders.RemovableDevices;
                IReadOnlyList<StorageFolder> externalDrives = await externalDevices.GetFoldersAsync();
                StorageFolder folder = externalDrives[0];
                Globals.driveName = folder.Name.ToString();
                var retrivedProperties = await folder.Properties.RetrievePropertiesAsync(new string[] { "System.FreeSpace" });
                Globals.driveFreeSpace = (UInt64)retrivedProperties["System.FreeSpace"];
            }
            catch
            {
                Globals.driveName = "NONE";
                return Globals.STATUS_CODES.DRIVE_NOT_PRESENT;
            }

            if (Globals.driveFreeSpace < 3000000)
            {
                Globals.driveName = "FULL!";
                return Globals.STATUS_CODES.INSUFFICIENT_DRIVE_MEMORY;
            }


            return Globals.STATUS_CODES.SUCCESS;
        }


        // This routine saves the perpetual key, 
        // If the expiring universal key is to be used, manually place it in the "User Folders \ LocalAppData \ "APPNAME" \ LocalState" folder

        public async Task<bool> SaveKey(string sKey)
        {
            try
            {
                //save the key in "key.lic" file
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile sampleFile = await storageFolder.CreateFileAsync(Globals.PerpetualLicenseFileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteTextAsync(sampleFile, sKey);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public async Task<string> ReadKey()
        {
            // First try to read the perpetual key
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(Globals.PerpetualLicenseFileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch { }

            // Did not find perpetual key try the expiring universal key
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(Globals.licenseFileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch { }

            // use a canned key August 2020
            return ("AFU9sj5iidbSHodQDxOLgCSs1xL4o37YCZ8fLgNxOUjD1omSw9zz3ewgxx++FzmS+okxkMGEtVf32YBKNGAI81MX7F9fxwmF6UYGbNC+NafZzBhhr7T42qNCSGTtnS6I9b6iCKsVz0+DvDu32TTU2U2dFgbQNC9Gd23707DUSAiPlYj3jEPfVa0ysGtkiJ+GnlkGpXInGeYKd9C0jHAWndMHdnm8W3ycZsfBceodO42+nA9j0SM3XXIN0jNjxGZG2fp3sIcUdjbPFeSQQFTH/Z54WkR6MQgIXcMq0+zQSfzyfaRwFVL8uETZVGNV1MIR3SvvhMV0YXQrwnMrLG6uGg==");
        }


        public byte[] ReadStream(Stream stream)
        {
            byte[] buf = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, read);
                }
                return ms.ToArray();
            }
        }

        public async Task LoadKey()
        {     
            Globals.decoder_lic = await ReadKey();
        }

        public async Task UpdateFieldsFromFile()
        {
            string fileContents = "";
            fileContents = await readStringFromLocalFile(Globals.configurationFileName);
            string[] words;

            if (fileContents.Length > 0)
            {
                words = fileContents.Split(';');

                // Set the remote / local / stand alone flag
                try
                {
                    switch((char)words[0][0])
                    {
                        case 'R':
                            Globals.scanner_mode = Globals.SCANNER_MODES.REMOTE;
                            break;
                        case 'L':
                            Globals.scanner_mode = Globals.SCANNER_MODES.LOCAL;
                            break;
                        case 'S':
                            Globals.scanner_mode = Globals.SCANNER_MODES.STANDALONE;
                            break;
                        default:
                            Globals.scanner_mode = Globals.SCANNER_MODES.STANDALONE;
                            break;
                    }

                }
                catch { }

                try
                {
                    Globals.minimum_decode_count = int.Parse(words[1]);
                }
                catch { }

                try
                {
                    Globals.LocalRFIDNum = uint.Parse(words[2]);
                }
                catch { }

                // Set manual confirm flag
                try
                {
                    if (int.Parse(words[3]) > 0)
                        Globals.ManualScanConfirm = true;
                    else
                        Globals.ManualScanConfirm= false;
                }
                catch { }

                // Set hand scan flag
                try
                {
                    if (int.Parse(words[4]) > 0)
                        Globals.HandScanOption = true;
                    else
                        Globals.HandScanOption = false;
                }
                catch { }

            }
        }

        // Local configuration file that is saved to SD card (local file)
        public async Task UpdateFileFromFields()
        {
            string data = "";

            switch (Globals.scanner_mode)
            {
                case Globals.SCANNER_MODES.LOCAL:
                    data += "L;";
                    break;
                case Globals.SCANNER_MODES.REMOTE:
                    data += "R;";
                    break;
                case Globals.SCANNER_MODES.STANDALONE:
                    data += "S;";
                    break;
                default:
                    data += "S;";
                    break;
            }


            data += Globals.minimum_decode_count.ToString() + ";" ;

            data += Globals.LocalRFIDNum.ToString() + ";";

            // Manual confirmation setting
            if (Globals.ManualScanConfirm == true)
                data += "1;";
            else
                data += "0;";

            // Hand scan setting
            if (Globals.HandScanOption == true)
                data += "1;";
            else
                data += "0;";

            await saveStringToLocalFile(Globals.configurationFileName, data);
        }

        async Task saveStringToLocalFile(string filename, string content)
        {
            // saves the string 'content' to a file 'filename' in the app's local storage folder
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(content.ToCharArray());

            // create a file with the given filename in the local folder; replace any existing file with the same name
            StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            // write the char array created from the content string into the file
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
            }
        }

        public async Task<string> readStringFromLocalFile(string filename)
        {
            try
            {
                // reads the contents of file 'filename' in the app's local storage folder and returns it as a string

                // access the local folder
                StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
                // open the file 'filename' for reading
                Stream stream = await local.OpenStreamForReadAsync(filename);
                string text = "";

                // copy the file contents into the string 'text'
                using (StreamReader reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }

                return (text);
            }
            catch
            {
                return ("");
            }
        }

        public async Task<string> ShowAboutInfo()
        {
            //read "key.lic" file from local storage

            SDKINFO si = MainPage.m_dec.SDKInfo;

            string sSym = "";
            if ((si.Symbology & 0x01) != 0)
                sSym += "Linear  ";
            if ((si.Symbology & 0x02) != 0)
                sSym += "PDF417  ";
            if ((si.Symbology & 0x04) != 0)
                sSym += "DataMatrix  ";
            if ((si.Symbology & 0x08) != 0)
                sSym += "QRCode  ";
            if ((si.Symbology & 0x10) != 0)
                sSym += "AztecCode  ";

            string expDate = "";
            if (si.ExpDate.Year == 1600)
                expDate = "N/A";
            else if (si.ExpDate.Year == 1700)
                expDate = "Expired";
            else
                expDate = si.ExpDate.ToString("yyyy-MM-dd");

            //show info
            string sAbout = "";
            if (si.LicenseInfo == "")
            {
                sAbout = string.Format("www.DataSymbol.com, Barcode Reader SDK\r\nVersion: {0}   [{1} Edition,   {2}]\r\nBuild Date: [{3}]   Subsc. Exp. Date: [{4}]\r\nDevice ID: [{5}]\r\n\r\nRegistered to:\r\n\r\n{6}",
                    si.Version, "Professional", sSym, si.BuildDate.ToString("yyyy-MM-dd"), "N/A", BarcodeDecoder.DeviceID, "Not Registered, Works in Demo Mode (adds *)");
            }
            else
            {
                sAbout = string.Format("www.DataSymbol.com, Barcode Reader SDK\r\nVersion: {0}   [{1} Edition,   {2}]\r\nBuild Date: [{3}]   Subsc. Exp. Date: [{4}]\r\nDevice ID: [{5}]\r\n\r\nRegistered to:\r\n\r\n{6}",
                    si.Version, (si.Edition == 1 ? "Standard" : "Professional"), sSym, si.BuildDate.ToString("yyyy-MM-dd"), expDate, BarcodeDecoder.DeviceID, si.LicenseInfo);
            }

            return (sAbout);

        }
    }
}
