# -*- coding: ascii -*-
"""
SIEMENS SPINDL MACRO - Spindle Control for Siemens 840D

Handles spindle on/off, direction, and speed control.
"""


def execute(context, command):
    """
    Process SPINDL spindle control command
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Set spindle RPM if provided
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.SPINDLE_RPM = command.numeric[0]
    
    # Update S register
    context.registers.s = context.globalVars.SPINDLE_RPM
    
    # Determine spindle state
    spindle_state = context.globalVars.SPINDLE_DEF
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            
            if word_upper in ['ON', 'CLW', 'CLOCKWISE']:
                spindle_state = 'CLW'
                context.globalVars.SPINDLE_DEF = 'CLW'
                
            elif word_upper in ['CCLW', 'CCW', 'COUNTER-CLOCKWISE']:
                spindle_state = 'CCLW'
                context.globalVars.SPINDLE_DEF = 'CCLW'
                
            elif word_upper == 'ORIENT':
                spindle_state = 'ORIENT'
                
            elif word_upper == 'OFF':
                spindle_state = 'OFF'
    
    # Output spindle command
    if spindle_state == 'CLW':
        context.write("M3")
        if context.globalVars.SPINDLE_RPM > 0:
            context.registers.s = context.globalVars.SPINDLE_RPM
            context.show("S")
            context.writeBlock()
            
    elif spindle_state == 'CCLW':
        context.write("M4")
        if context.globalVars.SPINDLE_RPM > 0:
            context.registers.s = context.globalVars.SPINDLE_RPM
            context.show("S")
            context.writeBlock()
            
    elif spindle_state == 'ORIENT':
        context.write("M19")
        
    else:  # OFF
        context.write("M5")
