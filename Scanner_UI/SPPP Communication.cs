using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;

namespace ScanTest1
{
    public sealed partial class MainPage : Page
    {
        
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        private byte[] pktBuf = new byte[256];
        private int lastPppRx;
        private int pktRxIn;
        private uint pktRxCrc;
        private uint crcCount = 0;
        private uint pktCount = 0;
        private uint tooShortCount;
        private const int CRC16_FINAL = 0xf0b8;
        private const int CRC16_INIT = 0xffff;
        private const int sync = 0x7e;
        private const int esc = 0x7d;

        // Commands to controller
        private const byte MOVE_TO_READY = 0x80;
        private const byte MOVE_TO_SCAN = 0x81;
        private const byte MOVE_TO_EJECT = 0x82;
        private const byte MOVE_TO_TRANSFER = 0x83;
        private const byte MOTOR_HALT = 0x84;
        private const byte INITIALIZE_SYSTEM = 0x85;

        // Flag locations (mask)
        private const byte STALL_FLAG_MASK = 0x01;
        private const byte CHUTE_FULL_FLAG_MASK = 0x02;
        private const byte IN_RANGE_FLAG_MASK = 0x04;
        private const byte CONTINUOUS_PROX_FLAG_MASK = 0x08;
        private const byte ID_MATCH_FLAG_MASK = 0x10;

        // Error codes
        enum STATUS_CODES
        {
            SUCCESS = 0,
            STALL,
            MOTION_TIMEOUT,
            FULL_CHUTE,
            INSUFFICIENT_BARCODES,
            WAITING_TO_SCAN,
            RESETTING,
            CALIBRATING_EXPOSURE,
            DECODING
        }


        // Incoming packets from controller
        private const byte CONTROLLER_STATUS = 0x90;

        private CancellationTokenSource ReadCancellationTokenSource;

        bool motor_stalled = false;
        bool hw_initializing = true;        // Motor controller board status
        bool continuous_prox = false;
        bool in_range = false;
        bool id_match = false;


        enum C_STATE
        {
            INTIALIZING,
            SEEKING_READY,
            SEEKING_SCAN,
            SEEKING_TRANSFER,
            SEEKING_EJECT,
            READY_POS,
            SCAN_POS,
            TRANSFER_POS,
            EJECT_POS,
            STALLED,
            UNDEFINED
        }

        C_STATE CameraState = C_STATE.UNDEFINED;

        private async void comPortStart()
        {

            string aqs = SerialDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);

            try
            {
                // Select the first (only) serial port
                serialPort = await SerialDevice.FromIdAsync(dis[0].Id);


                if (serialPort == null)
                {
                    var messageDialog = new MessageDialog("serial port null.");
                    await messageDialog.ShowAsync();
                    return;
                }

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(100);            // How long the port needs to be quiet before the message is returned              
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;


                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                comm_init = true;
                Listen();
            }
            catch (Exception ex)
            {
                //TJV add error catching here
                var messageDialog = new MessageDialog("Unable to find serial port.");
                await messageDialog.ShowAsync();

            }
        }

        private const uint POLY = 0x8408;
        private uint crc_next(uint crc, byte c)
        {
            crc ^= (uint)c;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ POLY;
                }
                else
                {
                    crc >>= 1;
                }
            }
            return crc;
        }

        // Process the incoming PPP data stream
        private void RxProcess(byte data)

        {
            int c, clast;

            c = (int)data;

            /* Receive data in an modified AHDLC format: */
            clast = lastPppRx;
            lastPppRx = c;  /* For future parsing */
            if (esc == c)
            {
                if (esc == clast)
                {
                    pktRxIn = 0;    /* Silently abort the incoming packet */
                }
            }
            else if (sync == c) /* Are we at the end of a packet? */
            {
                if (pktRxIn > 0 && esc != clast)  /* ESC + SYNC silently aborts a packet */
                {
                    /* Check CRC */
                    if (pktRxIn > 2)
                    {
                        if (CRC16_FINAL == pktRxCrc)
                        {
                            pktCount++;
                            parse_pkt();
                        }
                        else
                        {
                            //Debug.WriteLine("Got CRC error");
                            crcCount++;
                            //crcCountBox.Text = crcCount.ToString();
                        }
                    }
                    else
                    {
                        //Debug.WriteLine("Got short packet");
                        tooShortCount++;
                    }
                }
                pktRxIn = 0;

            }
            else if (pktRxIn > 0 || sync == clast)    /* Have we started a packet? */
            {
                /* Initialize the CRC on the first byte */
                if (0 == pktRxIn)
                {
                    pktRxCrc = CRC16_INIT;
                }
                if (esc == clast)
                {
                    c ^= 0x20;  /* Modify RX character for ESC */
                }

                if (pktRxIn < pktBuf.Length)
                {
                    pktBuf[pktRxIn] = (byte)c;
                    pktRxIn++;
                }

                /* Update CRC */
                pktRxCrc = crc_next(pktRxCrc, (byte)c);
            }
        }

        /*Packet format CONTROLLER_STATUS Packet
        volatile STATE      SystemState;                           
        flags               flag_reg;
        uint16_t            encoder_count;

        uint32_t            DG_RFID;
        uint16_t            DG_SerialNumber;
        uint32_t            DG_CycleCount;
        uint16_t            DG_MaxFlux;
        uint8_t             DG_SystemState;       
        uint8_t             DG_HDDState;
        uint8_t             DG_AutoStart;
        */

            //TJV Fix Microchip 16bit for 8bit values
        private void parse_pkt()
        {
            // display state status
            if (pktBuf[0] == CONTROLLER_STATUS)
            {
                CameraState = (C_STATE)pktBuf[1];

                switch(pktBuf[1])
                {
                    case (byte)C_STATE.INTIALIZING:
                        MotorStatusMsg.Text = "Initializing system. Please wait.";
                        break;
                    case (byte)C_STATE.READY_POS:
                        MotorStatusMsg.Text = "Input Position";
                        break;
                    case (byte)C_STATE.EJECT_POS:
                        MotorStatusMsg.Text = "Eject Position";
                        break;
                    case (byte)C_STATE.SCAN_POS:
                        MotorStatusMsg.Text = "Scan Position";
                        break;
                    case (byte)C_STATE.TRANSFER_POS:
                        MotorStatusMsg.Text = "Transfer Position";
                        break;
                    case (byte)C_STATE.SEEKING_EJECT:
                        MotorStatusMsg.Text = "Moving to Eject Pos.";
                        break;
                    case (byte)C_STATE.SEEKING_READY:
                        MotorStatusMsg.Text = "Moving to Input Pos.";
                        break;
                    case (byte)C_STATE.SEEKING_SCAN:
                        MotorStatusMsg.Text = "Moving to Scan Pos.";
                        break;
                    case (byte)C_STATE.SEEKING_TRANSFER:
                        MotorStatusMsg.Text = "Moving to Transfer Pos.";
                        break;
                    case (byte)C_STATE.UNDEFINED:
                        MotorStatusMsg.Text = "Unknown";
                        break;
                }

                string flag_string = "";

                if(pktBuf[1] == (byte)C_STATE.INTIALIZING)
                {
                    hw_initializing = true;
                }
                else
                {
                    hw_initializing = false;
                }

                //FLAGS
                if((pktBuf[3] & STALL_FLAG_MASK) > 0)        // stall flag
                {
                    flag_string += "S";
                    motor_stalled = true;
                }
                else
                {

                    motor_stalled = false;
                }

                if ((pktBuf[3] & CHUTE_FULL_FLAG_MASK) > 0)        // chute full flag
                {
                    chute_full = true;
                }
                else
                {
                    chute_full = false;
                }

                if ((pktBuf[3] & IN_RANGE_FLAG_MASK) > 0)        
                {
                    in_range = true;
                    flag_string += "R";
                }
                else
                {
                    in_range = false;
                }

                if ((pktBuf[3] & ID_MATCH_FLAG_MASK) > 0)
                {
                    id_match = true;
                    flag_string += "M";
                }
                else
                {
                    id_match = false;
                }


                if ((pktBuf[3] & CONTINUOUS_PROX_FLAG_MASK) > 0)        
                {
                    flag_string += "C";
                    continuous_prox = true;
                }
                else
                {
                    continuous_prox = false;
                }

                if(in_range && id_match)
                {
                    ScanButton.Background = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    ScanButton.Background = new SolidColorBrush(Colors.Red);
                }

                StallStatusMsg.Text = flag_string;

                // Little endian
                UInt16 encoder = (UInt16)((UInt16)pktBuf[5] + (((UInt16)pktBuf[6]) << 8));;

                EncoderCount.Text = encoder.ToString();



            }
        }

        private async Task write_ppp(byte[] data)
        {
            byte[] data2 = new byte[data.Length + 2];           // add room for CRC
            byte[] raw = new byte[(data.Length + 4) * 2];
            int rawi = 0;
            uint crc = 0xffff;

            // Load packet data and compute CRC (skip sync at beginning and sync and crc at endend)
            for (int i = 0; i < data.Length; i++)
            {
                data2[i] = data[i];
                crc = crc_next(crc, data[i]);
            }

            // Append CRC
            crc ^= 0xffff;
            data2[data.Length] = (byte)crc;
            data2[data.Length + 1] = (byte)(crc >> 8);

            // prepend the sync
            raw[rawi++] = sync;

            // Escape where necessary
            for (int i = 0; i < data2.Length; i++)
            {
                if (data2[i] == sync || data2[i] == esc)
                {
                    raw[rawi++] = esc;
                    raw[rawi++] = (byte)((uint)data2[i] ^ 0x20);
                }
                else
                {
                    raw[rawi++] = data2[i];
                }
            }

            // append final sync
            raw[rawi++] = sync;

            byte[] out_buf = new byte[rawi];

            // put in proper length buffer
            for (int i = 0; i < rawi; i++)
            {
                out_buf[i] = raw[i];
            }

            try
            {

                // Create the DataWriter object and attach to OutputStream
                dataWriteObject = new DataWriter(serialPort.OutputStream);

                //Launch the WriteAsync task to perform the write
                await WriteAsync(out_buf);
            }
            catch (Exception ex)
            {
                //var dialog = new MessageDialog(ex.ToString());
                //await dialog.ShowAsync();
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }


        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        /// TJV Modify to send out packet
        private async Task WriteAsync(byte[] raw_packet)
        {
            Task<UInt32> storeAsyncTask;

            // Load the text from the sendText input text box to the dataWriter object
            // Generate the packet

            dataWriteObject.WriteBytes(raw_packet); // send out the packet

            // Launch an async task to complete the write operation
            storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

            UInt32 bytesWritten = await storeAsyncTask;
            if (bytesWritten > 0)               // checks to see if it is written
            { }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;
            listOfDevices.Clear();
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                // status.Text = "Reading task was cancelled, closing device and cleaning up";
                CloseDevice();
            }
            catch (Exception ex)
            {
                // status.Text = ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            using (var childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Create a task object to wait for data on the serialPort.InputStream
                loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(childCancellationTokenSource.Token);

                // Launch the task and wait
                UInt32 bytesRead = await loadAsyncTask;
                while (bytesRead > 0)
                {
                    RxProcess(dataReaderObject.ReadByte());
                    //status.Text = "bytes read successfully!";
                    --bytesRead;
                }
            }
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

    }

}
 