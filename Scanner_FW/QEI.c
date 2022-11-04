#include "mcc_generated_files/system.h"
#include "mcc_generated_files/mcc.h"
#include "globals_and_defines.h"

void QEI_Initialize()
{
    
    QEI1CONbits.QEIEN = 1;      // Counter enabled
    QEI1CONbits.PIMOD = 0b011;  // First index event after home event initializes position counter with contents of QEIxIC register
    QEI1CONbits.CCM = 0b00;     // Quadrature Encoder mode
    
    QEI1IOCbits.QCAPEN = 1;     // HOMEx input event (positive edge) triggers a position capture event
    QEI1IOCbits.FLTREN = 1;     // Input Pin Digital filter is enabled
    QEI1IOCbits.QFDIV = 0b010;  // 1:4 clock divide
    QEI1IOCbits.OUTFNC = 0b11;  // The CNTCMPx pin goes high when POSxCNT ? QEIxLEC or POSxCNT ? QEIxGEC
    
    QEI1STATbits.PCHEQIEN = 0;  // Position Counter Greater Than or Equal Compare Interrupt Enable bit
    QEI1STATbits.PCLEQIEN = 0;  // Position Counter Less Than or Equal Compare Interrupt Enable bit
    
    // Need to use the index feature rather than home for setting count value
    // on start up drive back and forth to stall at either end until proper polarity index is experienced
    // Proper polarity index should zero the count
    
}

void __attribute__ ((interrupt, no_auto_psv)) _QEI1Interrupt (void)
{
    PWM_OFF();          // Stop motor destination has been reached
    // Clear interrupts
    QEI1STATbits.PCHEQIEN = 0;  // Position Counter Greater Than or Equal Compare Interrupt Enable bit
    QEI1STATbits.PCLEQIEN = 0;  // Position Counter Less Than or Equal Compare Interrupt Enable bit
    // clear the flags to remove interrupt request condition
    QEI1STATbits.PCLEQIRQ = 0;
    QEI1STATbits.PCHEQIRQ = 0; 
    update_state();    
    IFS3bits.QEI1IF = 0;        // Clear the interrupt flag
    //IEC3bits.QEI1IE = 0;        // Turn off the interrupt
}
