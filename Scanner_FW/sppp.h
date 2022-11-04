/*
 * ------------------------------------------------------------------
 *  Modified PPP type protocol
 *

 */
#ifndef __SPPP_H__
#define __SPPP_H__


// Incoming Datagram Commands
#define CMD_ERROR_RESPONSE      0x89    /* Response when we get an error command */

/* Commands that have responses use the LSB to indicate a response */
#define CMD_READ_BOARD_STATUS               0x90
#define CMD_READ_BOARD_STATUS_RESPONSE      0x91

#define     CMD_DYNAMIC_SET                 0x80
#define     CMD_NVM_SET                     0x81

#define CRC16_INIT      (0xffff)
#define CRC16_FINAL     (0xf0b8)

/* Make the RX buffer big enough to hold a memory program packet */
#define RX_BUFFER_SIZE  (128)
extern unsigned char rx_buffer[RX_BUFFER_SIZE];
extern unsigned char rx_packet_len;

extern unsigned char rx_buffer_U3[RX_BUFFER_SIZE];
extern unsigned char rx_packet_len_U3;

extern unsigned char debug[0x100];
extern unsigned char ptr;


unsigned crc16Next(unsigned crc);
void pktTxCharSend(unsigned char c);
void pktTxStart(void);
extern unsigned char pktTxType; /* Parameter for pktTxStart */
void pktTxFinish(void);
void pktErrorResponseSend(void);
void packetTx(unsigned char pktType, unsigned char len, unsigned char *p);
void RawPacketTx(unsigned char pktType, unsigned char len, unsigned char *p);
unsigned char processUartRx( void );
unsigned char processRS485Rx( void );
void process_RFID_RX(void);
 
#endif
