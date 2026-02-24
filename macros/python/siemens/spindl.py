# -*- coding: ascii -*-
"""
SIEMENS SPINDL MACRO - Spindle Control for Siemens 840D

Handles spindle on/off, direction, and speed control.
M-code and S are output together in one block.
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

    # Build output parts list
    parts = []

    # Output spindle command
    if spindle_state == 'CLW':
        parts.append("M3")
        # Add S value
        if context.globalVars.SPINDLE_RPM > 0:
            parts.append(f"S{int(context.globalVars.SPINDLE_RPM)}")

    elif spindle_state == 'CCLW':
        parts.append("M4")
        # Add S value
        if context.globalVars.SPINDLE_RPM > 0:
            parts.append(f"S{int(context.globalVars.SPINDLE_RPM)}")

    elif spindle_state == 'ORIENT':
        parts.append("M19")

    else:  # OFF
        parts.append("M5")

    # Output complete block
    if parts:
        context.write(" ".join(parts))
