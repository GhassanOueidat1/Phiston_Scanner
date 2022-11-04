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
using PhistonUI;
using System.Diagnostics;

namespace PhistonUI
{
    public sealed class SerialClass
    {
   
        // constructor
        private SerialClass()
        {
            //comPortStart();
        }

        //Singleton Instance
        private static readonly Lazy<SerialClass>
        lazy =    new Lazy<SerialClass>(() => new SerialClass());

        public static SerialClass Instance
        {
            get
            {
                return lazy.Value; 
            }
        }

        private static DispatcherTimer TxTimer;


        public class Packet_class
        {
            public byte[] Packet;
           
            public Packet_class(byte[] data)
            {
                Packet = new byte[data.Length];

                Array.Copy(data, Packet, data.Length);
            }

        }

        public static bool tx_busy = false;

        public static int lastPppRx = 0;
        public static int pktRxIn = 0;
        public static uint pktRxCrc = 0;
        public static uint crcCount = 0;
        public static uint pktCount = 0;
        public static uint tooShortCount = 0;
        public const int CRC16_FINAL = 0xf0b8;
        public const int CRC16_INIT = 0xffff;
        public const int sync = 0x7e;
        public const int esc = 0x7d;
        public static SerialDevice serialPort = null;
        static DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

               
        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
        public static List<Packet_class> pkt_buffer_list = new List<Packet_class>();
               
        public static void TxTimer_Tick(object source, object e)
        {
            if (tx_busy)
                return;

            byte[] pkt;

            if (pkt_buffer_list.Count > 0)
            {
                pkt = pkt_buffer_list[0].Packet;
                pkt_buffer_list.RemoveAt(0);

               _ = write_ppp(pkt);
            }
        }
        private static void SetupTxTimer()
        {
            TxTimer = new DispatcherTimer();
            TxTimer.Tick += TxTimer_Tick;
            TxTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);      //250mS check queue 4x per second
            TxTimer.Start();
        }

        public async Task comPortStart()
        {
            listOfDevices = new ObservableCollection<DeviceInformation>();

            Globals.pktBuf[0] = 0;

            try
            {
                //serialPort = await SerialDevice.FromIdAsync(entry.Id);
                // Select the first (only) serial port
                string aqs = SerialDevice.GetDeviceSelector();
                //string aqs = SerialDevice.GetDeviceSelector("UART0");
                var dis = await DeviceInformation.FindAllAsync(aqs);

                serialPort = await SerialDevice.FromIdAsync(dis[0].Id);
                if (serialPort == null) return;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(100);                        
                serialPort.BaudRate = 19200;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                Globals.comm_init = true;

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // Create the DataWriter object and attach to OutputStream
                dataWriteObject = new DataWriter(serialPort.OutputStream);

                SetupTxTimer();                 // Packet dispatch timer

                await Listen();             // never returns from this await


            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async Task Listen()

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
                else
                {

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
                    dataReaderObject.Dispose();
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
                    if (Globals.buffer_cleared)
                    {
                        RxProcess(dataReaderObject.ReadByte());
                    }
                    else
                    {
                        dataReaderObject.ReadByte();             //Dump it
                    }
                    //status.Text = "bytes read successfully!";
                    --bytesRead;
                }

                Globals.buffer_cleared = true;

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

        // Process the incoming PPP data stream
        private async Task RxProcess(byte data)
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
                            parse_pkt();
                            pktCount++;
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

                if (pktRxIn < Globals.pktBuf.Length)
                {
                    Globals.pktBuf[pktRxIn] = (byte)c;
                    pktRxIn++;
                }

                /* Update CRC */
                pktRxCrc = crc_next(pktRxCrc, (byte)c);
            }
        }


        public static bool GetBit(UInt16 b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
        
        // this is used to write the serial number, it is written when TX timer fires
        // TJV find out why we don't use write_ppp instead?
        public void tx_buffer_write(byte[] data)
        {
            Packet_class buffer_entry = new Packet_class(data);

            pkt_buffer_list.Add(buffer_entry);
        }

        public async static Task write_ppp(byte[] data)
        {
            tx_busy = true;

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

                //Launch the WriteAsync task to perform the write
                await WriteAsync(out_buf);
            }
            catch (Exception ex)
            {

            }
            finally
            {
               tx_busy = false;
            }
        }


  
        private async static Task WriteAsync(byte[] raw_packet)
        {
            Task<UInt32> storeAsyncTask;

            dataWriteObject.WriteBytes(raw_packet); // send out the packet

            // Launch an async task to complete the write operation
            storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

            UInt32 bytesWritten = await storeAsyncTask;

            if (bytesWritten > 0)               // checks to see if it is written
            { }
        }


        private const uint POLY = 0x8408;
        private static uint crc_next(uint crc, byte c)
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

        /*
        struct scanner_data
        {
            volatile char SystemState;    // right now these are stored as 16 bit, try and reduce       1-2                
            flags flag_reg;       // Right now 16 bit as well 3-4
            uint16_t encoder_count; 5-6
            uint16_t serial_number; 7-8
            uint32_t cycle_count; 9-12

            uint32_t DG_RFID;               //13-16
            volatile uint32_t RFID_PROX;    //17-20
            uint16_t DG_SerialNumber;       //21-22
            uint32_t DG_CycleCount;         //23-26
            uint16_t DG_MaxFlux;            //27-28
            uint8_t DG_SystemState;         //29
            uint8_t DG_HDDState;            //30
            uint8_t             scanner_hw_rev;     //31
            uint8_t             scanner_fw_rev;     //32
            */


        public void parse_pkt()
{
// display state status
if (Globals.pktBuf[0] == Globals.CONTROLLER_STATUS)
{
    Globals.CameraState = (Globals.C_STATE)Globals.pktBuf[1];

    
    if (Globals.pktBuf[1] == (byte)Globals.C_STATE.INTIALIZING)
    {
        Globals.hw_initializing = true;
    }
    else
    {
        Globals.hw_initializing = false;
    }

    //FLAGS Upper byte
    if ((Globals.pktBuf[4] & Globals.EJECT_FULL_FLAG_MASK) > 0)        
    {
        Globals.eject_full = true;
    }
    else
    {
        Globals.eject_full = false;
    }
    
    // Flags Lower byte
    if ((Globals.pktBuf[3] & Globals.STALL_FLAG_MASK) > 0)        // stall flag
    {
        Globals.motor_stalled = true;
    }
    else
    {

        Globals.motor_stalled = false;
    }

    if ((Globals.pktBuf[3] & Globals.CHUTE_FULL_FLAG_MASK) > 0)        // chute full flag
    {
        Globals.chute_full = true;
    }
    else
    {
        Globals.chute_full = false;
    }

    if ((Globals.pktBuf[3] & Globals.IN_RANGE_FLAG_MASK) > 0)
    {
        Globals.in_range = true;
    }
    else
    {
        Globals.in_range = false;
    }

    if ((Globals.pktBuf[3] & Globals.ID_MATCH_FLAG_MASK) > 0)
    {
        Globals.id_match = true;
    }
    else
    {
        Globals.id_match = false;
    }


    if ((Globals.pktBuf[3] & Globals.CONTINUOUS_PROX_FLAG_MASK) > 0)
    {
        Globals.continuous_prox = true;
    }
    else
    {
        Globals.continuous_prox = false;
    }

    if ((Globals.pktBuf[3] & Globals.DG_AUTO_MODE_FLAG_MASK) > 0)
    {
        Globals.DG_auto_mode_flag = true;
    }
    else
    {
        Globals.DG_auto_mode_flag = false;
    }

    if ((Globals.pktBuf[3] & Globals.LOCAL_READY_FLAG_MASK) > 0)
    {
        Globals.local_ready_flag = true;
    }
    else
    {
        Globals.local_ready_flag = false;
    }

    if ((Globals.pktBuf[3] & Globals.LOCAL_NOT_READY_EVENT_FLAG_MASK) > 0)
    {
        Globals.not_ready_latch = true;
    }
    else
    {
        Globals.not_ready_latch = false;
    }

    // Little endian
    Globals.encoder = (UInt16)((UInt16)Globals.pktBuf[5] + (((UInt16)Globals.pktBuf[6]) << 8));

    Globals.serial_number = (UInt16)((UInt16)Globals.pktBuf[7] + (((UInt16)Globals.pktBuf[8]) << 8));

    Globals.cycle_count = (UInt32)Globals.pktBuf[9]
        + ((UInt32)Globals.pktBuf[10] << 8)
        + ((UInt32)Globals.pktBuf[11] << 16)
        + ((UInt32)Globals.pktBuf[12] << 24);

    Globals.RemoteRFIDNum= (UInt32)Globals.pktBuf[13]
        + ((UInt32)Globals.pktBuf[14] << 8)
        + ((UInt32)Globals.pktBuf[15] << 16)
        + ((UInt32)Globals.pktBuf[16] << 24);

    if (Globals.in_range)
    {
        Globals.ReadRFIDNum = (UInt32)Globals.pktBuf[17]
            + ((UInt32)Globals.pktBuf[18] << 8)
            + ((UInt32)Globals.pktBuf[19] << 16)
            + ((UInt32)Globals.pktBuf[20] << 24);
    }
    else    // Not in range data is bogus
        Globals.ReadRFIDNum = 0;

    Globals.DG_cycle_count = (UInt32)Globals.pktBuf[23]
        + ((UInt32)Globals.pktBuf[24] << 8)
        + ((UInt32)Globals.pktBuf[25] << 16)
        + ((UInt32)Globals.pktBuf[26] << 24);

    Globals.MaxFlux = (UInt16)((UInt16)Globals.pktBuf[27] + (((UInt16)Globals.pktBuf[28]) << 8));

    Globals.DG_system_state = (Globals.DG_system_states)Globals.pktBuf[29];

    Globals.scanner_hw_rev = Globals.pktBuf[31];
    Globals.scanner_fw_rev = Globals.pktBuf[32];

    Globals.remote_refresh_request = true;                  // request data to be updated
}
}
}
}
