

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef XC_EE_FUNCT_H
#define	XC_EE_FUNCT_H

#include <xc.h> // include processor files - each processor file is guarded.  

// Function prototypes
long ee_read_count(uint8_t bank);
void enable_ee_write();
void ee_read();
long get_cycle_count(uint16_t address1, uint16_t address2);
void blocking_4byte_ee_read(uint16_t address, uint8_t *buffer);
void write_ee_spi_data();
bool write_all_values_2_ee();
uint16_t get_uint16(uint16_t address1);
void read_ee_bytes(uint16_t address, uint8_t *buffer, uint8_t byte_count);
void load_variables_from_ee();
bool load_flux_gain_for_ee_write();
bool load_SN_for_ee_write();
bool load_cycle_count_for_ee_write();
bool load_mode_for_ee_write();
bool load_RFID_for_ee_write();

extern bool    ee_write_count_request;

extern uint8_t  ee_write_count;         // Total number of bytes to write to EE, must be zero before writing to buffer
extern uint8_t  ee_write_buff[];
extern uint8_t  ee_write_ptr;       // points to next data to be transmitted to EE
extern uint16_t  ee_write_start_addr;

#define COUNT_ADDR_1        0                   // 3 bytes with CSUM (4 bytes total)
#define COUNT_ADDR_2        4                   // Redundant copy with CSUM
#define SN_ADDR             8                   // two byte SN with CSUM (3 bytes total)

// current reading addressing scheme can only address 128 bytes because two of the address bits are included in the command
// current writing addressing scheme can only address 256 bytes because one of the address bits are included in the command
// if deeper access is needed the highest two bits of the address need to be placed in the lowest 2 bits of these instructions
#define     EE_WRITE_BYTE       0b00001010      // 101 is start of write, 0 to be replaced with the highest address bit A8
#define     EE_READ             0b00011000      // 110 is start of read, next two zeros are to be replaced with the highest address bits A8,A7
                                                // This is needed to byte align result that has a leading dummy byte
#define     EE_EN_WRITE         0b10011000
//#define     EE_EN_WRITE         0b01001100

#define     EE_EN_WRITE_B0      0b00001001
#define     EE_EN_WRITE_B1      0b10000000

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

