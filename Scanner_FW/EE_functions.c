// Functions for accessing the external EEPROM
#include "mcc_generated_files/mcc.h"
#include "sppp.h"
#include <stdint.h>
#include "globals_and_defines.h"
#include "EE_functions.h"

uint8_t  ee_write_buff[30];
uint8_t  ee_write_count = 0;            // Total number of bytes to write to EE, must be zero before writing to buffer
uint8_t  ee_write_ptr = 0;          // points to next data in buffer to be transmitted to EE
uint16_t  ee_write_start_addr = 0;

// Non-blocking ee write
// uses globals ee_data_count, ee_data_ptr, ee_data_buff, ee_data_start_address
void write_ee_spi_data()
{
    uint16_t address;
    
    if(ee_write_count == 0)                 // No data pending
        return;
    
    // Make sure there are no pending transmissions before executing
    if(SPI2STATbits.SPIBEC != 0)            // Transmit buffer contains data
        return;
            
    if(!SPI2STATbits.SRMPT)                 // Tx SR has not completed
        return;
    
    if(!SDI2_GetValue())                    // MISO is low indicating chip is not ready
        return;
    

    // byte data has been completed and written, see if that was the last
    if(ee_write_ptr == ee_write_count)        // buffer has been transmitted
    {
        ee_write_count = 0;                  // A zero count indicates there is no message pending or active
        ee_write_ptr = 0;
        return;
    }
    
    // Ready for a write to the next address
    // This is typically entered with CE high and in monitor write status mode, toggle to get out of that mode
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetLow();                // 0.5uS minimum low
    CS_MEM_SetHigh();               // enabled for writing
    address = ee_write_start_addr + ee_write_ptr;
    SPI2BUF = EE_WRITE_BYTE | ((address >> 8)  & 0x01);     // Command and first address bit A8
    SPI2BUF = (uint8_t)(address & 0xFF);
    SPI2BUF = ee_write_buff[ee_write_ptr];
    ee_write_ptr++;
}


//Not this does not seem to turn interrupts back on
void blocking_4byte_ee_read(uint16_t address, uint8_t *buffer)
{
    
    uint8_t temp;
    
    // De select
    CS_MEM_SetLow();
    CS_MEM_SetLow();
    
    //Turn off the automatic assertion of CE low under interrupt
    IEC2bits.SPI2IE = 0;

    
    // Reset SPI and clear buffers
    SPI2STATbits.SPIEN = 0;
    SPI2STATbits.SPIEN = 1;
    
    //Read in the memory of first bank (set low in case it was left in high state by a previous write monitoring command)
    CS_MEM_SetHigh();
    // first two bits of address A8 and A7 are included in the EE_READ command
    SPI2BUF = EE_READ | ((uint8_t)((address>>7) & 0x03));     // lower two bits are the first address bits
    SPI2BUF = address<<1;       // Point to first byte, leave space for dummy bit to come out
    SPI2BUF = 0; SPI2BUF = 0; SPI2BUF = 0; SPI2BUF = 0;
    
    // Wait for transaction to complete - wait until count in tx fifo is zero
    while(SPI2STATbits.SPIBEC != 0);
    while(!SPI2STATbits.SRMPT);         // Make sure the SR is empty too
    
    
    CS_MEM_SetLow();
    
    //Turn on the automatic assertion of CE low under interrupt
    IFS2bits.SPI2IF = 0;
    IEC2bits.SPI2IE = 0;
        
    // Read in the data from the fifo
    temp = SPI2BUF;
    temp = SPI2BUF;
    buffer[0] = SPI2BUF;
    buffer[1] = SPI2BUF;
    buffer[2] = SPI2BUF;
    buffer[3] = SPI2BUF;
}

bool load_SN_for_ee_write()
{
    // This should only be called when count = 0
    if(ee_write_count != 0)
        return(false);

    //Load serial number
    ee_write_buff[0] = (uint8_t)((scanner.vars.serial_number >> 8) & 0xFF);
    ee_write_buff[1] = (uint8_t)(scanner.vars.serial_number & 0xFF);
    ee_write_buff[2] = (uint8_t)(~(ee_write_buff[0]+ee_write_buff[1])) + 1; 
    
    //Set up pointers for transfer function called in polling loop
    ee_write_start_addr = SN_ADDR;
    ee_write_count = 3;
    ee_write_ptr = 0;
    return(true);
}


bool load_cycle_count_for_ee_write()
{
    // This should only be called when count = 0
    if(ee_write_count != 0)
        return(false);
    
    // Write redundant copies of the cycle count and crc
    ee_write_buff[0] = (uint8_t)((scanner.vars.cycle_count >> 16) & 0xFF);
    ee_write_buff[1] = (uint8_t)((scanner.vars.cycle_count >> 8) & 0xFF);
    ee_write_buff[2] = (uint8_t)(scanner.vars.cycle_count & 0xFF);
    ee_write_buff[3] = (~(ee_write_buff[0]+ee_write_buff[1]+ee_write_buff[2])) + 1;             // Two's compliment csum
    
    // Load redundant copy into buffer
    ee_write_buff[4] = ee_write_buff[0];
    ee_write_buff[5] = ee_write_buff[1];
    ee_write_buff[6] = ee_write_buff[2];
    ee_write_buff[7] = ee_write_buff[3];
    
    ee_write_start_addr = 0;
    ee_write_count = 8;
    ee_write_ptr = 0;
    return(true);
}

// This is a blocking function and should only be called at initialization
// or at a time when the foreground delay will not matter
long get_cycle_count(uint16_t address1, uint16_t address2)
{
    uint8_t buffer[4], csum;
    long cycle_count;
    
    blocking_4byte_ee_read(address1, buffer);
    // check the checksum
    csum = buffer[0] + buffer[1] + buffer[2] + buffer[3];
    
    if(csum != 0)   // bad data try second bank
    {
         blocking_4byte_ee_read(address2, buffer);
        // check the checksum
        csum = buffer[0] + buffer[1] + buffer[2] + buffer[3];   
        if(csum != 0)
            return(0);      // Cycle count has been lost, reset
    }

    cycle_count = (((long)buffer[0])<<16) + (((long)buffer[1])<<8) + ((long)buffer[2]);
    return(cycle_count);
}

// This is a blocking function and should only be called at initialization
// or at a time when the foreground delay will not matter
uint16_t get_uint16(uint16_t address1)
{
    uint8_t buffer[4], csum;
    uint16_t result;
    
    blocking_4byte_ee_read(address1, buffer);
    // check the checksum - there are two bytes and a csum
    csum = buffer[0] + buffer[1] + buffer[2];
    
    if(csum != 0)       // bad crc
        return(0);
    
    result = (((uint16_t)buffer[0])<<8) + ((uint16_t)buffer[1]);
    return(result);
}

void read_ee_bytes(uint16_t address, uint8_t *buffer, uint8_t byte_count)
{
    uint8_t temp;
     // empty the FIFO
    
    //Turn off the automatic assertion of CE low under interrupt
    // This is blocking
    IEC2bits.SPI2IE = 0;
    
    // Reset SPI
    SPI2STATbits.SPIEN = 0;
    SPI2STATbits.SPIEN = 1;
    
    //Read in the memory of first bank (set low in case it was left in high state by a previous write monitoring command)
    CS_MEM_SetLow();
    CS_MEM_SetLow();
    CS_MEM_SetLow();
    CS_MEM_SetHigh();
    // first two bits of address A8 and A7 are included in the EE_READ command
    SPI2BUF = EE_READ | ((uint8_t)((address>>7) & 0x03));     // lower two bits are the first address bits
    SPI2BUF = address<<1;       // Point to first byte, leave space for dummy bit to come out
    while(SPI2STATbits.SPIBEC != 0);        //Number of TX transfers pending
    while(!SPI2STATbits.SRMPT);         // Make sure the SR is empty too

    // read in two bogus bytes
    temp = SPI2BUF;
    temp = SPI2BUF;
    
    
    for(temp=0;temp<byte_count;temp++)
    {
        SPI2BUF = 0;                        // Clock in byte
    
        // Wait for transaction to complete - wait until count in tx fifo is zero
        while(SPI2STATbits.SPIBEC != 0);
        while(!SPI2STATbits.SRMPT);         // Make sure the SR is empty too

        buffer[temp] = SPI2BUF;                // Read the received byte
    }
    
    CS_MEM_SetLow();
 
    // Turn the CE assertion back on
    IFS2bits.SPI2IF = 0;
    IEC2bits.SPI2IE = 1;
}

void load_variables_from_ee()
{
    uint8_t temp;
    uint8_t buffer[30];
    
    // Read the data from the EE
    read_ee_bytes(0, buffer, 11);
    
    // Process the redundant count data
    temp = buffer[0]+buffer[1]+buffer[2]+buffer[3];
    if(temp == 0)       // good csum
        scanner.vars.cycle_count = (((uint32_t)(buffer[0]))<<16) + (((uint32_t)(buffer[1]))<<8) + (uint32_t)buffer[2];
    else
    {
        temp = buffer[4] + buffer[5] + buffer[6] + buffer[7];
        if(temp == 0)       // good csum
            scanner.vars.cycle_count = (((uint32_t)buffer[4])<<16) + (((uint32_t)buffer[5])<<8) + (uint32_t)buffer[6];
        else
            scanner.vars.cycle_count = 0;        // no good data
    }
    
    // Process the SN data
    temp = buffer[8]+buffer[9]+buffer[10];
    if(temp == 0)       // good csum
        scanner.vars.serial_number = (((uint16_t)buffer[8])<<8) + (uint16_t)buffer[9];
    else
        scanner.vars.serial_number = 0;          // bad data -reset
 
}

// This is blocking and should only be called on initialization
void enable_ee_write()
{
    //Turn off the automatic assertion of CE low under interrupt
    IEC2bits.SPI2IE = 0;
    IFS2bits.SPI2IF = 0;

    CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();
    CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();
    CS_MEM_SetHigh();              
    SPI2BUF = EE_EN_WRITE_B0;
    
     // Wait for transaction to complete - wait until count in tx fifo is zero
    while(SPI2STATbits.SPIBEC != 0);    // Buffer count does not equal zero
    while(SPI2STATbits.SRMPT != 1);         // Make sure the SR is empty too
    SPI2BUF = EE_EN_WRITE_B1;
    
    // Wait for transaction to complete - wait until count in tx fifo is zero
    while(SPI2STATbits.SPIBEC != 0);    // Buffer count does not equal zero
    while(SPI2STATbits.SRMPT != 1);         // Make sure the SR is empty too
    
    CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();
    CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();CS_MEM_SetLow();
   
    //Turn on the automatic assertion of CE low under interrupt
    IFS2bits.SPI2IF = 0;
    IEC2bits.SPI2IE = 1;
}
