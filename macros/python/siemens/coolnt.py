# -*- coding: ascii -*-
"""
SIEMENS COOLNT MACRO - Coolant Control for Siemens 840D

Handles coolant on/off and type selection.
"""


def execute(context, command):
    """
    Process COOLNT coolant control command
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    coolant_state = context.globalVars.COOLANT_DEF
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            
            if word_upper in ['ON', 'FLOOD']:
                coolant_state = 'FLOOD'
                context.globalVars.COOLANT_DEF = 'FLOOD'
                
            elif word_upper == 'MIST':
                coolant_state = 'MIST'
                context.globalVars.COOLANT_DEF = 'MIST'
                
            elif word_upper == 'THRU':
                coolant_state = 'THRU'
                context.globalVars.COOLANT_DEF = 'THRU'
                
            elif word_upper == 'AIR':
                coolant_state = 'AIR'
                context.globalVars.COOLANT_DEF = 'AIR'
                
            elif word_upper == 'OFF':
                coolant_state = 'OFF'
                context.globalVars.COOLANT_DEF = 'OFF'
    
    # Output coolant command
    if coolant_state == 'FLOOD':
        context.write("M8")
    elif coolant_state == 'MIST':
        context.write("M7")
    elif coolant_state == 'THRU':
        context.write("M50")  # Through-tool coolant
    elif coolant_state == 'AIR':
        context.write("M51")  # Air blast
    else:  # OFF
        context.write("M9")
