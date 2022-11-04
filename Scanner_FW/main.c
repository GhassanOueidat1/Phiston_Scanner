/**
  Generated main.c file from MPLAB Code Configurator

  @Company
    Microchip Technology Inc.

  @File Name
    main.c

  @Summary
    This is the generated main.c using PIC24 / dsPIC33 / PIC32MM MCUs.

  @Description
    This source file provides main entry point for system intialization and application code development.
    Generation Information :
        Product Revision  :  PIC24 / dsPIC33 / PIC32MM MCUs - 1.95-b-SNAPSHOT
        Device            :  dsPIC33EP128GM304
    The generated drivers are tested against the following:
        Compiler          :  XC16 v1.36
        MPLAB 	          :  MPLAB X v5.10
*/

/*
    (c) 2016 Microchip Technology Inc. and its subsidiaries. You may use this
    software and any derivatives exclusively with Microchip products.

    THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS". NO WARRANTIES, WHETHER
    EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED
    WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A
    PARTICULAR PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION
    WITH ANY OTHER PRODUCTS, OR USE IN ANY APPLICATION.

    IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE,
    INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND
    WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS
    BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE. TO THE
    FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS IN
    ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF ANY,
    THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.

    MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE
    TERMS.
*/

/**
  Section: Included Files
*/
//#include <p33EP128GM304.h>
#include <p33EP512GM304.h>

#include "mcc_generated_files/system.h"
#include "mcc_generated_files/mcc.h"
#include "globals_and_defines.h"
#include "sppp.h"
#include "EE_functions.h"

volatile int encoder_hysteresis = 0;
long terminal_count = 0;
volatile bool check_stall_flag = false;
volatile bool prev_home_state = false;
volatile bool home_state = false;
volatile bool moving_flag;
volatile uint8_t rfid_num[5];
//TJV change this to zero and have it filled in by the degausser comm
uint8_t target_rfid[5] = {0x01, 0x11,0x99,0x93,0x4c};
bool write_SN_ee_data_rqst = false;
bool write_count_ee_data_rqst = false;
volatile int stall_count = 0;

// data structure to pass data to Pi
scanner_status scanner;


// This is called by the encoder interrupt when the motor has reached target location
void update_state()
{
    switch(scanner.vars.SystemState)
    {
        case SEEKING_READY:
            scanner.vars.SystemState = READY_POS;
            break;
        case SEEKING_EJECT:
            scanner.vars.SystemState = EJECT_POS;
            break;  
        case SEEKING_SCAN:
            scanner.vars.SystemState = SCAN_POS;
            camera_led_pwr(true);
            break;
        case SEEKING_TRANSFER:
            scanner.vars.SystemState = TRANSFER_POS;
            break;
        case INITIALIZING:
            break;
        case STALLED:
            PWM_OFF();
            break;
        default:
            scanner.vars.SystemState = UNDEFINED;          
            break;
    }
}

void move_to_position (int32_t target_position)
{
    // Make sure motor is off
    PWM_OFF();
    
    int32_t actual_position;
    
    actual_position = POS1CNTL;
    actual_position += ((int32_t)POS1HLD)<<16;
    
    if (target_position < actual_position)
    {
        target_position += BACKLASH;
        QEI1LECH = target_position >> 16;
        QEI1LECL = target_position & 0xFFFF;
        QEI1STATbits.PCHEQIEN = 0;  // Position Counter Greater Than or Equal Compare Interrupt Enable bit
        QEI1STATbits.PCLEQIEN = 1;  // Position Counter Less Than or Equal Compare Interrupt Enable bit   
        //IEC3bits.QEI1IE = 1;
        REV_PWM_ON();
    }    
    else
    {
        target_position -= BACKLASH;
        QEI1GECH = target_position >> 16;
        QEI1GECL = target_position & 0xFFFF;
        QEI1STATbits.PCLEQIEN = 0;  // Position Counter Less Than or Equal Compare Interrupt Enable bit
        QEI1STATbits.PCHEQIEN = 1;  // Position Counter Greater Than or Equal Compare Interrupt Enable bit
        //IEC3bits.QEI1IE = 1;
        FWD_PWM_ON();
    }
     
    scanner.vars.flag_reg.stall = false;         // Going to attempt a move clear the stall
}


void camera_led_pwr(bool light_state)
{
    if(light_state == true)
    {
        CAMERA_LED0_ON();
        CAMERA_LED1_ON();
        CAMERA_LED2_ON();
        CAMERA_LED3_ON();
    }
    else
    {
       CAMERA_LED0_OFF();
       CAMERA_LED1_OFF();
       CAMERA_LED2_OFF();
       CAMERA_LED3_OFF();
    }     
}

//tjv change this to read directly into structure
// might need to mess with endianness
void parse_RS485_packet(uint8_t byte_count)
{   
    switch(rx_buffer_U3[0])        // Byte zero contains the packet type
    {
        case DEGAUSSER_STATUS_OUTPUT:
            // Load the degausser variables
            //RFID number (4), serial number (2), cycle_count (3), max_flux (2), SystemState(1), HDD_State(1), DG_AutoStart(1)
            scanner.vars.DG_RFID = ((uint32_t)rx_buffer_U3[1])<<24;
            scanner.vars.DG_RFID += ((uint32_t)rx_buffer_U3[2])<<16;
            scanner.vars.DG_RFID += ((uint32_t)rx_buffer_U3[3])<<8;
            scanner.vars.DG_RFID += rx_buffer_U3[4];   
            
            scanner.vars.DG_SerialNumber = ((uint16_t)rx_buffer_U3[5])<<8;
            scanner.vars.DG_SerialNumber += rx_buffer_U3[6];
            
            scanner.vars.DG_CycleCount = ((uint32_t)rx_buffer_U3[7])<<16;
            scanner.vars.DG_CycleCount += ((uint32_t)rx_buffer_U3[8])<<8;
            scanner.vars.DG_CycleCount += rx_buffer_U3[9]; 
            
            scanner.vars.DG_MaxFlux = ((uint16_t)rx_buffer_U3[10])<<8;           // Units on flux is milliT (~2100 is nominal)
            scanner.vars.DG_MaxFlux += rx_buffer_U3[11];
            
            scanner.vars.DG_SystemState = rx_buffer_U3[12];
            scanner.vars.DG_HDDState = rx_buffer_U3[13];
            
            if(rx_buffer_U3[14] > 0)
                scanner.vars.flag_reg.DG_AutoStart = true;
            else
                scanner.vars.flag_reg.DG_AutoStart = false;
            
            break;
           
    }
}

void parse_rx_packet(uint8_t byte_count)
{   
    switch(rx_buffer[0])        // Byte zero contains the packet type
    {
        case MOVE_TO_READY:
            scanner.vars.SystemState = SEEKING_READY;
            move_to_position(READY_POSITION);
            camera_led_pwr(false);
            break;
            
        case MOVE_TO_SCAN:
            scanner.vars.SystemState = SEEKING_SCAN;
            move_to_position(SCAN_POSITION);
            break;
            
        case MOVE_TO_EJECT:
            scanner.vars.SystemState = SEEKING_EJECT;
            move_to_position(EJECT_POSITION);
            camera_led_pwr(false);
            break;
            
        case MOVE_TO_TRANSFER:
            scanner.vars.SystemState = SEEKING_TRANSFER;
            move_to_position(TRANSFER_POSITION);
            camera_led_pwr(false);
            break;
            
        case MOTOR_HALT:
            PWM_OFF();
            camera_led_pwr(false);
            break;
            
        case INITIALIZE_SYSTEM:         
            terminal_count = 0;
 
            PWM_OFF();
            camera_led_pwr(false);

            //INTERRUPT_GlobalInterruptDisable();
            scanner.vars.SystemState = INITIALIZING; 
            //INTERRUPT_GlobalInterruptEnable();
            
            move_to_position(INITIALIZE_POSITION);              // Drive to hard stop and let initialization state machine take over

            break;
        case SET_CONTINUOUS_PROX:
            scanner.vars.flag_reg.continuous_prox = true;
            break;
        case INCREMENT_CYCLE_COUNT:
            scanner.vars.cycle_count++;
            write_count_ee_data_rqst = true;
            break;
        case SET_SERIAL_NUMBER:
            scanner.vars.serial_number = rx_buffer[1] + (((uint16_t)rx_buffer[2])<<8);
            write_SN_ee_data_rqst = true;
            break;
        case ARM_NOT_READY_FLAG:
            scanner.vars.flag_reg.not_ready_latch = false;
            break;

    }
}

void FWD_PWM_ON()
{
    // FWD Duty Cycle Set
    PWM_MasterDutyCycleSet(0xAA);
    // Disable High
    IOCON1bits.OVRDAT1 = 0;
    IOCON1bits.OVRENH = 1;

    // Enable Low
    IOCON1bits.OVRENL = 0;
}

void REV_PWM_ON()
{
    // REV Duty Cycle Set
    PWM_MasterDutyCycleSet(0x99);
    // Disable low
    IOCON1bits.OVRDAT0 = 0;
    IOCON1bits.OVRENL = 1;
    // Enable High
    IOCON1bits.OVRENH = 0;
}

void PWM_OFF()          // this also brakes
{
        // Disable High
    IOCON1bits.OVRDAT = 0b11;
    IOCON1bits.OVRENH = 1;
    IOCON1bits.OVRENL = 1;
}

// Transmit to Pi
void status_tx()
{
    uint8_t tx_buff[SCANNER_DATA_LEN];
    uint8_t i;

    // Update real time signals
    scanner.vars.encoder_count = POS1CNTL;
    scanner.vars.flag_reg.local_ready = LOCAL_HANDSHAKE_GetValue();
    
    for(i=0;i<SCANNER_DATA_LEN;i++)
        tx_buff[i] = scanner.data_array[i];
        
    packetTx(CONTROLLER_STATUS,SCANNER_DATA_LEN,tx_buff);
}

void check_stall()
{
    static bool was_moving = false;
    bool is_moving;
    static int32_t old_encoder_pos = 0;
    int32_t new_encoder_pos = 0;
    int32_t distance;
    
    is_moving = ((scanner.vars.SystemState == SEEKING_EJECT) ||(scanner.vars.SystemState == SEEKING_READY) ||
            (scanner.vars.SystemState == SEEKING_TRANSFER) || (scanner.vars.SystemState == SEEKING_SCAN)
            || (scanner.vars.SystemState == INITIALIZING));
    
    new_encoder_pos = POS1CNTL;
    new_encoder_pos += ((int32_t)(POS1HLD)) << 16;
    
    if(is_moving && was_moving)
    {
        if(new_encoder_pos > old_encoder_pos)
            distance = new_encoder_pos - old_encoder_pos;
        else
            distance = old_encoder_pos - new_encoder_pos;
        
        if(distance <= STALL_ENCODER_LINES)
        {
            scanner.vars.flag_reg.stall = 1;         // Indicate stall
            PWM_OFF();
            if(scanner.vars.SystemState == INITIALIZING)
            {
                POS1HLD = 0;
                POS1CNTL = 0;      // Set home
                scanner.vars.SystemState = SEEKING_EJECT;
                move_to_position(EJECT_POSITION); 
            }
            else
            {
                scanner.vars.SystemState = STALLED;
            }
        }
        else
        {
            scanner.vars.flag_reg.stall = 0;
        }
    }
    
    old_encoder_pos = new_encoder_pos;
    was_moving = is_moving;
}

void check_transfer_chute()
{     
    if((!DROP_RX1_GetValue())||(!DROP_RX2_GetValue()))
    {
        scanner.vars.flag_reg.chute_full = 1;
    }
    else
    {
        scanner.vars.flag_reg.chute_full = 0;
    }
}

void check_eject_chute()
{     
    if((!EJECT1_GetValue())||(!EJECT2_GetValue()))
    {
        scanner.vars.flag_reg.eject_full = 1;
    }
    else
    {
        scanner.vars.flag_reg.eject_full = 0;
    }
}

void check_ee()
{
    if(ee_write_count != 0 )        // Write in progress
    {
         write_ee_spi_data();       // non blocking write process
    }
    else
    {
        if(write_SN_ee_data_rqst == true)
        {
            if(load_SN_for_ee_write())
            {
                write_SN_ee_data_rqst = false;
            }
        }
        
        if(write_count_ee_data_rqst == true)
        {
            if(load_cycle_count_for_ee_write())
            {
                write_count_ee_data_rqst = false;
            }
        }

    }
}

void check_rfid_prox()
{
    if(IN_RANGE_GetValue())
    {
        scanner.vars.flag_reg.in_range = true;
        //INDICATOR_LED_SetHigh();
    }
    else
    {
        scanner.vars.flag_reg.continuous_prox = false;
        scanner.vars.flag_reg.in_range = false;
        //scanner.vars.RFID_PROX = 0; 
        //INDICATOR_LED_SetLow();
    } 
}

void check_not_ready()
{
    if( LOCAL_HANDSHAKE_GetValue() == false)
    {
        scanner.vars.flag_reg.not_ready_latch = true;
    }
}

int main(void)
{ 
    // Initialize the device    
    SYSTEM_Initialize();
    QEI_Initialize();

    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:
    scanner.vars.SystemState = INITIALIZING;           
    CS_MEM_SetLow();
    
    IEC3bits.QEI1IE = 1;            // enable the QEI interrupts
    // Enable the Global Interrupts
    INTERRUPT_GlobalEnable();

    //TMR0_StartTimer();
    terminal_count = 0;
    
    INDICATOR_LED_SetHigh();
         
    PWM_OFF();
    
    DROP_TX1_SetHigh();
    DROP_TX2_SetHigh();
    
    uint8_t x;
  
    // enables the eeprom for writing - this is a blocking routine
    enable_ee_write();

    load_variables_from_ee();
         
    camera_led_pwr(false);
    scanner.vars.DG_MaxFlux = 0;

    // Does it go into stall shutdown after this?
    move_to_position(INITIALIZE_POSITION);              // Drive to hard stop and let initialization state machine take over
    
    //camera_led_pwr(true);
    
    // Load static info
    scanner.vars.scanner_fw_rev = SCANNER_FW_REV;
    scanner.vars.scanner_hw_rev = SCANNER_HW_REV;
    
    while (1)
    {
        check_ee();                                     // See if an ee write is needed
          
        //UART1 is the Pi
        if((x = processUartRx())>0)                     // **** May want to do this under interrupt TJV ****
        {
            // interpret the packet
            parse_rx_packet(x);
        }
        
        if((x = processRS485Rx())>0)                     // ***** May want to do this under interrupt TJV *****
        {
            // interpret the packet
            parse_RS485_packet(x);
        }
        
        //UART2 is the RFID
        process_RFID_RX();
           
        check_rfid_prox();
        
        if(check_stall_flag)
        {
            check_stall();
            check_stall_flag = false;

        }
        
        check_transfer_chute();
        
        check_eject_chute();
        
        check_not_ready();
    }
}
/**
 End of File
*/

