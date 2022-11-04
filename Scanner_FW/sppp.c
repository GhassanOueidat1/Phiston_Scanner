/*
 * ------------------------------------------------------------------
 *  Modified PPP type protocol handler
 * ------------------------------------------------------------------
 */
#include <xc.h>
#include "sppp.h"
#include "mcc_generated_files/uart2.h"
#include "mcc_generated_files/uart1.h"
#include "mcc_generated_files/uart3.h"
#include "globals_and_defines.h"

#define CRC16_POLY      (0x1021)    /* standard crc16 polynomial */
#define CRC16_POLY_LSB  (0x8408)    /* same but bit-reversed */
#define CRC_LEN         (2u)
#define PPP_SYNC        (0x7eu)
#define PPP_ESC         (0x7du)

// RFID packet markers
#define     STX                         0x02
#define     ETX                         0x03
#define     CR                          0x0d
#define     LF                          0x0a

unsigned char pktTxType;
static unsigned char pktRxIn;
static unsigned char lastPppRx;
static unsigned pktRxCrc;
static unsigned pktTxCrc;
static unsigned rx_errors;

unsigned char pktTxType_U3;
static unsigned char pktRxIn_U3;
static unsigned char lastPppRx_U3;
static unsigned pktRxCrc_U3;
static unsigned pktTxCrc_U3;
static unsigned rx_errors_U3 = 0;
static unsigned rx_good_U3 = 0;
unsigned bad_csum = 0;
unsigned good_csum = 0;

uint8_t rfid_ptr = 0;
uint8_t rfid_buff[16];

extern volatile uint8_t rfid_num[4];


/*
 * ----------------------------------------------------------------------------
 *  Variables for received datagrams
 * ----------------------------------------------------------------------------
 */
unsigned char rx_buffer[RX_BUFFER_SIZE];
unsigned char rx_packet_len;

unsigned char rx_buffer_U3[RX_BUFFER_SIZE];
unsigned char rx_packet_len_U3;
/*
 * ----------------------------------------------------------------------------
 *  CRC16 implementation
 *  DLP: Optimize a bit for the sucky optimizer in the free Microchip compiler.
 * ----------------------------------------------------------------------------
 */
unsigned crc16Next(unsigned crc)
{
    unsigned char i;
    for(i=8;;)
    {
        if ((crc & 1) != 0)
        {
            /*
             * Splitting this to 2 lines makes it faster and smaller on a PIC
             * It also saves RAM, as it allows the shift and xor to occur
             * directly on the crc value.
             */
            crc >>= 1;
            crc ^= CRC16_POLY_LSB;
        }
        else
        {
            crc >>= 1;
        }
        if (!--i)
        {
            break;
        }
    }
    return crc;
}

/*
 * ----------------------------------------------------------------------------
 *  Send a packet character, possibly escaping it.
 * ----------------------------------------------------------------------------
 */
void pktTxCharSend(unsigned char c)
{
    pktTxCrc = crc16Next(pktTxCrc ^ c); /* Update the CRC: */

    if (PPP_SYNC == c || PPP_ESC == c)
    {
        UART1_Write(PPP_ESC);
        c ^= 0x20;
    }
    UART1_Write(c);
}

void pktTxCharSend_U3(unsigned char c)
{
    pktTxCrc_U3 = crc16Next(pktTxCrc_U3 ^ c); /* Update the CRC: */

    if (PPP_SYNC == c || PPP_ESC == c)
    {
        UART3_Write(PPP_ESC);
        c ^= 0x20;
    }
    UART3_Write(c);
}

/*
 * ----------------------------------------------------------------------------
 *  Start a packet transmission
 * ----------------------------------------------------------------------------
 */
void pktTxStart(void)
{
    UART1_Write(PPP_SYNC);
    pktTxCrc = CRC16_INIT;
    pktTxCharSend(pktTxType); /* Send the first byte with the packet type, or command */
}

void pktTxStart_U3(void)
{
    UART3_Write(PPP_SYNC);
    pktTxCrc_U3 = CRC16_INIT;
    pktTxCharSend_U3(pktTxType_U3); /* Send the first byte with the packet type, or command */
}

/*
 * ----------------------------------------------------------------------------
 *  Finish a packet transmission
 * ----------------------------------------------------------------------------
 */
void pktTxFinish(void)
{
    unsigned char crcHigh;

    crcHigh = (unsigned char)(pktTxCrc >> 8); /* Save high CRC */
    pktTxCharSend((unsigned char)~pktTxCrc);
    pktTxCharSend((unsigned char)~crcHigh);
    UART1_Write(PPP_SYNC);      /* Send final sync */
}

void pktTxFinish_U3(void)
{
    unsigned char crcHigh;

    crcHigh = (unsigned char)(pktTxCrc_U3 >> 8); /* Save high CRC */
    pktTxCharSend_U3((unsigned char)~pktTxCrc_U3);
    pktTxCharSend_U3((unsigned char)~crcHigh);
    UART3_Write(PPP_SYNC);      /* Send final sync */
}

/*
 * ----------------------------------------------------------------------------
 *  packetTx:  Send a packet
 * ----------------------------------------------------------------------------
 */
void packetTx_U3(unsigned char pktType, unsigned char len, unsigned char *p)
{
    unsigned char i;

    pktTxType_U3 = pktType;
    pktTxStart_U3();
    for( i=0; i<len; i++ )
    {
        pktTxCharSend_U3(p[i]);
    }
    pktTxFinish_U3();
}

void packetTx(unsigned char pktType, unsigned char len, unsigned char *p)
{
    unsigned char i;

    pktTxType = pktType;
    pktTxStart();
    for( i=0; i<len; i++ )
    {
        pktTxCharSend(p[i]);
    }
    pktTxFinish();
}

uint16_t adc2flux(uint16_t flux_data)
{
    uint16_t flux;
    
    flux = flux_data;
    
    return(flux);
}


void RawPacketTx(unsigned char pktType, unsigned char len, unsigned char *p)
{
    unsigned char i;

    for( i=0; i<len; i++ )
    {
        UART1_Write(p[i]);
    }
}


/*
 * ----------------------------------------------------------------------------
 *  pktErrorRepsponseSend:  Send an error response packet, for when we get an
 *      incoming packet we don't understand.
 * ----------------------------------------------------------------------------
 */
void pktErrorResponseSend(void)
{
    UART1_Write(PPP_ESC);   /* Send abort */
    packetTx(CMD_ERROR_RESPONSE, 6, rx_buffer);
}

// Converts two ascii hex characters into a hex byte.  First character is the
// high order nibble
/*
unsigned char atob(unsigned char * ptr)
{
    unsigned char result;
    unsigned char offset;
    
    if (*ptr > 57)          // Letter not number
    {
        offset = 55;
    }
    else
    {
        offset = 48;
    }
    result = (*ptr - offset)*16;
    
    if (*(ptr+1) > 57)
    {
        offset = 55;
    }
    else
    {
        offset = 48;
    }
    result += (*(ptr+1) - offset);
    
    return(result);
}
*/
unsigned char atob_wrap(unsigned char high, unsigned char low)
{
    unsigned char result;
    unsigned char offset;
    
    if (high > 57)          // Letter not number
    {
        offset = 55;
    }
    else
    {
        offset = 48;
    }
    result = (high - offset)*16;
    
    if (low > 57)
    {
        offset = 55;
    }
    else
    {
        offset = 48;
    }
    result += (low - offset);
    
    return(result);
}
void process_RFID_RX(void)
{
    uint8_t CSUM, state;

    // 10 ascii bytes representing hex numbers
    // Process incoming byte, check the status flag bit
    if(UART2_TRANSFER_STATUS_RX_DATA_PRESENT & UART2_TransferStatusGet())
    {
        rfid_ptr = (rfid_ptr + 1) & 0x0F;           // 16 byte ring
        rfid_buff[rfid_ptr] = UART2_Read();       // Read in the data
                
        // Check for completed pattern
        if((rfid_buff[rfid_ptr] == ETX) && (rfid_buff[(rfid_ptr+1)&0x0F] == STX))
        {
            {
                if((rfid_buff[(rfid_ptr+14)&0x0F] == CR) && (rfid_buff[(rfid_ptr+15)&0x0F] == LF))
                {
                    // Patterns match check the CSUM of the 10 data bytes
                    // Pointer is currently pointing to the last byte of packet
                    // Compute the checksum
                    
                    CSUM = 0;
                    for(state = 2; state < 12; state+=2)
                    {
                        CSUM ^= atob_wrap(rfid_buff[(rfid_ptr+state)&0x0F], rfid_buff[(rfid_ptr+state+1)&0x0F]);
                    }
                    // See if CSUM is a match
                    if(CSUM == atob_wrap(rfid_buff[(rfid_ptr+12)&0x0F], rfid_buff[(rfid_ptr+13)&0x0F]))
                    {
                        ++good_csum;
                        // Data is good - use it here
                        CSUM = 0;
                        // Point to the first byte of data
                        rfid_ptr = (rfid_ptr + 2) & 0x0F;
                        // Load the RFID buffer - converting ascii to binary values
                        for(state = 0; state < 9 ; state+=2)        // 5 bytes at two ascii nibbles each
                            rfid_num[state/2] = atob_wrap(rfid_buff[(rfid_ptr+state)&0x0F], rfid_buff[(rfid_ptr+state+1)&0x0F]); 
                        
                        // Throw away the first byte
                        scanner.vars.RFID_PROX = ((uint32_t)rfid_num[1])<<24;
                        scanner.vars.RFID_PROX += ((uint32_t)rfid_num[2])<<16;
                        scanner.vars.RFID_PROX += ((uint32_t)rfid_num[3])<<8;
                        scanner.vars.RFID_PROX += rfid_num[4];
                    }
                    else
                    {
                        ++bad_csum;
                    }
                }
            }
        }
    }
}

/*
 * ----------------------------------------------------------------------------
 *  Check for received packets.
 *  Return 0 if a packet is not ready.
 *  Return length of the packet if a packet is ready and the CRC passes.
 *  The returned length does not include the CRC.
 * ----------------------------------------------------------------------------
 */
unsigned char processUartRx( void )
{
    unsigned char c, clast;
   
    //if((EUSART1_TRANSFER_STATUS_RX_DATA_PRESENT & EUSART1_TransferStatusGet()) == 0)
    // Returns the number of Rx bytes waiting
    if(!UART1_DataReady)
    {
        return 0; /* Nothing to do? */
    }

    /* Receive data in an modified AHDLC format: */
    clast = lastPppRx;
    c = UART1_Read();
    
    lastPppRx = c;  /* For future parsing */

    if (PPP_ESC == c)
    {
        if (PPP_ESC == clast)
        {
            pktRxIn = 0;   /* Silently abort the incoming packet */
        }
    }
    else if (PPP_SYNC == c) /* Are we at the end of a packet? */
    {
        if (pktRxIn && PPP_ESC != clast)  /* ESC + SYNC silently aborts a packet */
        {
            /* Check CRC */
            if (pktRxIn > CRC_LEN)
            {
                if (CRC16_FINAL == pktRxCrc)
                {
                    unsigned char pktLen;
                    pktLen = pktRxIn - CRC_LEN;
                    pktRxIn = 0;
                    return pktLen;  /* Return a good packet */
                }
                else
                {
                    //INC_STAT(IDX_RX_CRC_ERRORS);
                    rx_errors++;
                }
            }
            else
            {
                //INC_STAT(IDX_RX_TOO_SHORT_ERRORS);
            }
        }
        pktRxIn = 0;
    }
    else if (pktRxIn || PPP_SYNC == clast)    /* Have we started a packet? */
    {
        /* Initialize the CRC */
        if (!pktRxIn)
        {
            pktRxCrc = CRC16_INIT;
        }

        if (PPP_ESC == clast)
        {
            c ^= 0x20;  /* Modify RX character for ESC */
        }
        if (pktRxIn < RX_BUFFER_SIZE)
        {
            rx_buffer[pktRxIn] = c;
            pktRxIn++;
        }

        pktRxCrc = crc16Next(pktRxCrc ^ c);
    }
    return 0;
}

// receive packets on the Rs422 port
unsigned char processRS485Rx( void )
{
    unsigned char c, clast;
   
    //if((EUSART1_TRANSFER_STATUS_RX_DATA_PRESENT & EUSART1_TransferStatusGet()) == 0)
    // Returns the number of Rx bytes waiting
    if(!UART3_DataReady)
    {
        return 0; /* Nothing to do? */
    }

    /* Receive data in an modified AHDLC format: */
    clast = lastPppRx_U3;
    c = UART3_Read();
    
    lastPppRx_U3 = c;  /* For future parsing */

    if (PPP_ESC == c)
    {
        if (PPP_ESC == clast)
        {
            pktRxIn_U3 = 0;   /* Silently abort the incoming packet */
        }
    }
    else if (PPP_SYNC == c) /* Are we at the end of a packet? */
    {
        if (pktRxIn_U3 && PPP_ESC != clast)  /* ESC + SYNC silently aborts a packet */
        {
            /* Check CRC */
            if (pktRxIn_U3 > CRC_LEN)
            {
                if (CRC16_FINAL == pktRxCrc_U3)
                {
                    unsigned char pktLen;
                    pktLen = pktRxIn_U3 - CRC_LEN;
                    pktRxIn_U3 = 0;
                    rx_good_U3++;
                    return pktLen;  /* Return a good packet */
                }
                else
                {
                    //INC_STAT(IDX_RX_CRC_ERRORS);
                    rx_errors_U3++;
                }
            }
            else
            {
                //INC_STAT(IDX_RX_TOO_SHORT_ERRORS);
            }
        }
        pktRxIn_U3 = 0;
    }
    else if (pktRxIn_U3 || PPP_SYNC == clast)    /* Have we started a packet? */
    {
        /* Initialize the CRC */
        if (!pktRxIn_U3)
        {
            pktRxCrc_U3 = CRC16_INIT;
        }

        if (PPP_ESC == clast)
        {
            c ^= 0x20;  /* Modify RX character for ESC */
        }
        if (pktRxIn_U3 < RX_BUFFER_SIZE)
        {
            rx_buffer_U3[pktRxIn_U3] = c;
            pktRxIn_U3++;
        }

        pktRxCrc_U3 = crc16Next(pktRxCrc_U3 ^ c);
    }
    return 0;
}
