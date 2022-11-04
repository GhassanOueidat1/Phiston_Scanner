// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
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

namespace ScanTest1
{
    public class CommunicationLibrary
    {

        public async Task WaitForHW()
        {
            while (Globals.hw_initializing)
            {
                await Task.Delay(200);
            }
        }

        public async Task WaitForHWReset()
        {
            while (!Globals.hw_initializing)
            {
                await Task.Delay(200);
            }
        }

        public async Task WaitForComm()
        {
            while (!Globals.comm_init)
            {
                await Task.Delay(200);
            }
        }


        public async Task WaitForCamera()
        {
            while (!Globals.scan_init)
            {
                await Task.Delay(200);
            }
        }

        public async Task WaitForReset()
        {
            //int i = 0;
            while (Globals.resetting)
            {
                await Task.Delay(200);
                //if (i++ > 100)          // Timeout 20 seconds
                 //   return;
            }
        }

        public async Task WaitForExposure()
        {
            while (!Globals.exposure_init)
            {
                await Task.Delay(200);
            }
        }

        public async Task WaitForCycleCountUpdate(UInt32 previousCount)
        {
            while (Globals.cycle_count == previousCount)
            {
                await Task.Delay(200);
            }
        }
       
    }
}
