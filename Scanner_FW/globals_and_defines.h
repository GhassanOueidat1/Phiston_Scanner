/* Microchip Technology Inc. and its subsidiaries.  You may use this software 
 * and any derivatives exclusively with Microchip products. 
 * 
 * THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS".  NO WARRANTIES, WHETHER 
 * EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED 
 * WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A 
 * PARTICULAR PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION 
 * WITH ANY OTHER PRODUCTS, OR USE IN ANY APPLICATION. 
 *
 * IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
 * INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
 * WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS 
 * BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE.  TO THE 
 * FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS 
 * IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF 
 * ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.
 *
 * MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE 
 * TERMS. 
 */

/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef XC_HEADER_TEMPLATE_H
#define	XC_HEADER_TEMPLATE_H
#include <xc.h> // include processor files - each processor file is guarded.  
#include "mcc_generated_files/mcc.h"

#define     SCANNER_HW_REV          1
//#define     SCANNER_FW_REV          3           // modified eject position
#define     SCANNER_FW_REV          4           // modified scan position for new metal

// 168:1 motor with 64 count encoder = 10,752 counts per rev
#define     INITIALIZE_POSITION     -60000       // Drive it up to hard stop and stall to zero
#define     SEEK_HOME_POSITION      60000        // Drive it forward until sensor detects edge (stall)
//#define     SCAN_POSITION         3100         // Encoder counts 
//#define     SCAN_POSITION           2990         // Encoder counts - second gen china hardware
#define     SCAN_POSITION           2900        // Encoder counts - second gen china hardware [modified]

#define     READY_POSITION          2000  //2143
//#define     EJECT_POSITION          210 // Previous value
#define     EJECT_POSITION          220
#define     TRANSFER_POSITION       3950
#define     ENCODER_HYSTERESIS      0           // TJV- check this - How close encoder needs to be to target to actually require move
#define     BACKLASH                100         // Compensates for direction of rotation


// Commands from UI (Pi)
#define     MOVE_TO_READY           0x80
#define     MOVE_TO_SCAN            0x81
#define     MOVE_TO_EJECT           0x82
#define     MOVE_TO_TRANSFER        0x83
#define     MOTOR_HALT              0x84
#define     INITIALIZE_SYSTEM       0x85
#define     SET_CONTINUOUS_PROX     0x86
#define     INCREMENT_CYCLE_COUNT   0x87
#define     SET_SERIAL_NUMBER       0x88
#define     ARM_NOT_READY_FLAG      0x89


// Commands from RS485
// RS485 Broadcast packet from degausser
#define DEGAUSSER_STATUS_OUTPUT     0xC0

// Response packet types (to the Pi)
#define     CONTROLLER_STATUS   0x90

// 50Hz clock, 20mS per tick
#define     PACKET_PITCH        25              // 50Hz clock, 20mS per tick
#define     STALL_PERIOD        10
#define     STALL_ENCODER_LINES 5               // Minimum encoder move per stall period

#define     CAMERA_LED0_ON()   CAMERA_LED0_SetHigh();
#define     CAMERA_LED0_OFF()  CAMERA_LED0_SetLow();
#define     CAMERA_LED1_ON()   CAMERA_LED1_SetHigh();
#define     CAMERA_LED1_OFF()  CAMERA_LED1_SetLow();
#define     CAMERA_LED2_ON()   CAMERA_LED2_SetHigh();
#define     CAMERA_LED2_OFF()  CAMERA_LED2_SetLow();
#define     CAMERA_LED3_ON()   CAMERA_LED3_SetHigh();
#define     CAMERA_LED3_OFF()  CAMERA_LED3_SetLow();

extern volatile int encoder_hysteresis;
extern long terminal_count;
extern volatile bool check_stall_flag;
extern volatile bool moving_flag;
extern volatile bool prev_home_state;
extern volatile bool home_state;
extern volatile int stall_count;
extern unsigned good_csum, bad_csum;


void PWM_OFF();
void FWD_PWM_ON();
void REV_PWM_ON();
void update_state();
void status_tx();
void check_stall();
void move_to_position(int32_t pos);
void update_init_statemachine();
void camera_led_pwr(bool state);
void unlockMapping();
void lockMapping();
void QEI_Initialize();

typedef enum 
{
    INITIALIZING,
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
}STATE;




typedef union{
    uint16_t allflags;
    struct {
        unsigned stall  :1;
        unsigned chute_full         :1;
        unsigned in_range           :1;
        unsigned continuous_prox    :1;   // used to test for a continuous device presence during the scan process
        unsigned unused             :1;     
        unsigned DG_AutoStart       :1;
        unsigned local_ready        :1;     // Local device is ready (high) or not ready (low)
        unsigned not_ready_latch   :1;      // Latches a not ready condition, cleared by arming packet
        unsigned eject_full   :1;
        unsigned bit9   :1;
        unsigned bit10   :1;
        unsigned bit11   :1;
        unsigned bit12   :1;
        unsigned bit13   :1;
        unsigned bit14   :1;
        unsigned bit15   :1;
        
    };
}flags;

#define SCANNER_DATA_LEN    32

typedef union
{
    // uC stores as little endian
    struct scanner_data
    {
        volatile uint16_t   SystemState;    // right now these are stored as 16 bit, try and reduce                       
        flags               flag_reg;       // Right now 16 bit as well 3
        uint16_t            encoder_count;
        uint16_t            serial_number;
        uint32_t            cycle_count;

        uint32_t            DG_RFID;
        volatile uint32_t   RFID_PROX;
        uint16_t            DG_SerialNumber;
        uint32_t            DG_CycleCount;
        uint16_t            DG_MaxFlux;             // for debug this has the good and bad RFID count good count is LSB
        uint8_t             DG_SystemState;       
        uint8_t             DG_HDDState;         
        uint8_t             scanner_hw_rev;
        uint8_t             scanner_fw_rev;
    }vars;
    
    uint8_t     data_array[SCANNER_DATA_LEN];         // used for packet readout
} scanner_status;

extern scanner_status scanner;


#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

