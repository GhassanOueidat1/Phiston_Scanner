//#define setconfig                     // This is used to switch on a set configuration for debugging

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;


namespace PhistonUI
{
    
    public static class Globals
    {
        // Incoming packets from controller
        public const byte CONTROLLER_STATUS = 0x90;

        public const int TESTING_CYCLES = 100;
        public const int TESTING_DELAY_CYCLES = 15;          // Number of 5Hz cycel before button is automatical pressed during continuous testing mode

        // States of the scanner controller state machine
        public enum C_STATE
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

        public enum PAGE_TYPES
        {
            SUPERVISOR_PAGE,
            FACTORY_PAGE,
            LICENSE_PAGE,
            UNKNOWN
        }

 
        public enum DG_system_states : byte
        {
            POWER_UP,
            IDLE,
            OPEN_TOP_GATE,
            CLOSE_TOP_GATE,
            CHARGE,
            FIRE,
            CHECK_HDD_READY,
            WAIT_HDD_CYCLE,
            WAIT_HDD_JAM_AUTO_RECOVERY,
            WAIT_HDD_JAM_MANUAL_RECOVERY,
            WAIT_HDD_POWER_ON,
            OPEN_BOTTOM_GATE,
            CLOSE_BOTTOM_GATE,
            WAIT_CYCLE_DONE,
            COOLING_DELAY,
            THERMAL_STOP,
            DISCHARGE1,
            DISCHARGE2
        };

        public enum hdd_states : byte
        {
            HDD_OFF,
            HDD_CYCLING,
            HDD_READY,
            HDD_JAM_AUTO_RECOVERY1,
            HDD_JAM_AUTO_RECOVERY2,
            HDD_JAM_MANUAL_RECOVERY,
            HDD_NOT_PRESENT
        };

        // Error codes
        public enum STATUS_CODES
        {
            SUCCESS = 0,
            STALL,
            MOTION_TIMEOUT,
            FULL_CHUTE,
            INSUFFICIENT_BARCODES,
            WAITING_TO_SCAN,
            RESETTING,
            CALIBRATING_EXPOSURE,
            DECODING,
            TAMPER,
            DRIVE_NOT_PRESENT,
            INSUFFICIENT_DRIVE_MEMORY,
            ERROR_WRITING_FILE,
            NOT_LOGGED_IN,
            NO_MESSAGE,
            WAIT_TIMEOUT,
            INSUFFICIENT_MEMORY,
            CAMERA_READ_FAILURE,
            BARCODE_CONVERSION_ERROR
        }

        public enum SCANNER_MODES
        {
            LOCAL,
            REMOTE,
            STANDALONE
        }

        // Software rev - this is loaded directly and needs to be updated here on new releases
        //public static double scanner_sw_rev = 0.6;              // Reset bug fix, and up to 7 manual bar codes; workaround for decoder bug
        //public static double scanner_sw_rev = 0.71;              // New Decoder Library 5.67.4, slow decode speed setting for decoder, 17763 OS reimaged, build 17763 min/max, uwp 6.2.10,
        //public static double scanner_sw_rev = 0.8;              // Added continuous test mode
        //public static double scanner_sw_rev = 0.81;              // abort on capture and decode errors 
        //public static double scanner_sw_rev = 0.82;              // added retrys to controller 
                                                                 // two pictures for scan decode rather than three to set exposure
        public static double scanner_sw_rev = 0.83;              // fixed bug in setting (with retries) the not_ready_latch flag, code was looking for a not ready condition instead of looking at the not_ready_latch itself 


        // These are passed up
        public static int scanner_fw_rev = 0;
        public static int scanner_hw_rev = 0;

        public static bool buffer_cleared = false;
        //public static bool auto_mode_debounce = false;
        //public static bool auto_start = false;

        // 1 tick = 200mS
        public static int motion_timeout_ticks = 20;
        public static int wait_not_ready_ticks = 30;        // How long to wait for receiving device to go not ready when in local mode
        public static int wait_cycle_done_ticks = 120;      // 24 seconds
        public static bool not_ready_latch = false;         // a not ready event has happened since the set command

        public static bool scan_init = false;
        public static bool comm_init = false;
        public static bool exposure_init = false;
        public static bool chute_full = false;
        public static bool user_page_initialized = false;
        public static UInt32 cycle_count = 0;           // Scanner cycle count
        public static UInt16 serial_number = 0;
        public static UInt64 driveFreeSpace = 0;

        public static UInt32 DG_cycle_count = 0;

        // Timer runs at 5Hz
        public static int user_timeout_count = 0;
        // User timout 10 minutes
        public static int user_timout = (int)(10 * 60 * 5);          // Logs out after 10 minutes of inactivity
        //public static int user_timout = (int)(1 * 60 * 5);
        public static long cycle_count_zero = 0;         // Stores the cycle count for computing the "trip odometer"

        public static string user_id ="";
        public static string date_code = "";

        // For calls to password page - there should be a cleaner solution than this!!
        public static string passwordTitle = "";
        public static string passwordValue = "";
        public static PAGE_TYPES pageType = PAGE_TYPES.UNKNOWN;


        public static C_STATE CameraState = C_STATE.UNDEFINED;

        // Semiphores to prevent reentry
        public static bool resetting = false;
        public static bool scanning = false;

       public static bool motor_stalled = false;
       public static bool hw_initializing = true;        // Motor controller board status
         public static bool continuous_prox = false;
         public static bool in_range = false;
        public static bool id_match = false;
        public static bool eject_full = false;
        public static UInt16 encoder = 0;
        public static bool remote_refresh_request = true;
        public static bool tamper_flag = false;
        public static bool DG_auto_mode_flag = false;
        public static bool local_ready_flag = false;
        public static DG_system_states DG_system_state = DG_system_states.POWER_UP;
        public static UInt16 MaxFlux = 0;
        public static double MaxFluxCapture = 0;
        public static bool stop_all_timers = true;
        public static bool reset_required = false;
        public static bool continuous_cycle_enabled = false;           // Check box to allow multiple cycles for each button press
        public static bool continuous_cycle_running = false;         // Continuous cycle  

        public static int continuous_cycle_delay = 0;               // Counts how many 5Hz timer cycles before pressing button again
        public static int continuous_cycle_count = 0;               // Counts how many automatic cycles have been performed



#if setconfig
        public static int minimum_decode_count = 1;             // Minimum number of decoded bar codes to declare a good read
        public static SCANNER_MODES scanner_mode = SCANNER_MODES.STANDALONE;                  // true => RFID number comes from local configuration, false it comes from serial port
        public static UInt32 LocalRFIDNum = 2364792;                  // Value to be matched when in local mode
        public static UInt32 ReadRFIDNum = 0;                   // Value of RFID tag read at scanner, only valid if in_range is set
        public static UInt32 RemoteRFIDNum = 0;                 // Value to be matched when in remote mode
        public static bool ManualScanConfirm = false;
        public static bool ManualForceEject = false;
        public static bool HandScanOption = false;
#else
        // These values should be initialized from NVM
        public static int minimum_decode_count = 3;             // Minimum number of decoded bar codes to declare a good read
        public static SCANNER_MODES scanner_mode = SCANNER_MODES.LOCAL;                  // true => RFID number comes from local configuration, false it comes from serial port
        public static UInt32 LocalRFIDNum = 0;                  // Value to be matched when in local mode
        public static UInt32 ReadRFIDNum = 0;                   // Value of RFID tag read at scanner, only valid if in_range is set
        public static UInt32 RemoteRFIDNum = 0;                 // Value to be matched when in remote mode
        public static bool ManualScanConfirm = true;
        public static bool ManualForceEject = false;
        public static bool HandScanOption = false;
#endif

        public static MediaCaptureInitializationSettings mcis = new MediaCaptureInitializationSettings();


        public static byte[] pktBuf = new byte[256];

        // Commands to controller
        public const byte MOVE_TO_READY = 0x80;
        public const byte MOVE_TO_SCAN = 0x81;
        public const byte MOVE_TO_EJECT = 0x82;
        public const byte MOVE_TO_TRANSFER = 0x83;
        public const byte MOTOR_HALT = 0x84;
        public const byte INITIALIZE_SYSTEM = 0x85;
        public const byte SET_CONTINUOUS_PROX = 0x86;               // initializes continuous prox detection
        public const byte INCREMENT_CYCLE_COUNT = 0x87;
        public const byte SET_SERIAL_NUMBER = 0x88;
        public const byte ARM_NOT_READY_FLAG = 0x89;

        // Flag locations (mask)
        public const UInt16 STALL_FLAG_MASK = 0x01;
        public const byte CHUTE_FULL_FLAG_MASK = 0x02;
        public const byte IN_RANGE_FLAG_MASK = 0x04;
        public const byte CONTINUOUS_PROX_FLAG_MASK = 0x08;
        public const byte ID_MATCH_FLAG_MASK = 0x10;
        public const byte DG_AUTO_MODE_FLAG_MASK = 0x20;
        public const byte LOCAL_READY_FLAG_MASK = 0x40;
        public const byte LOCAL_NOT_READY_EVENT_FLAG_MASK = 0x80;
        // Flag locations second byte
        public const byte EJECT_FULL_FLAG_MASK = 0x01;


        //Main page information

        public static string configurationFileName = "scan_config.txt";
        public static string licenseFileName = "key.lic";
        public static string PerpetualLicenseFileName = "pkey.lic";
        public static string driveName = "---";

        public static string key1 = "";
        public static string key2 = "";

        // Manual Scan Data
        public static int hand_scan_count = 0;
        public static string[,] hand_scan_data = new string [7,2];
        public static string manual_scan_message = "";
        public static bool auto_scan_request = false;

        // Decoder Key
        public static string decoder_lic = "";

        // Debug only
        public static int CharCount = 0;

    }
}
