# -*- coding: ascii -*-
"""
FSQ-100 SPINDL MACRO - Spindle Control for TOS KURIM FSQ100 with Siemens 840D

Handles spindle control with FSQ-100 specific format:
- M3 - Spindle clockwise (CLW)
- M4 - Spindle counter-clockwise (CCLW)
- M5 - Spindle stop (OFF)
- M19 - Spindle orient

Uses BlockWriter for modal S register output.
"""


def execute(context, command):
    """
    Process SPINDL spindle control command for FSQ-100

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

    # Process minor words
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

            elif word_upper == 'ON':
                # Use stored spindle state
                spindle_state = context.globalVars.SPINDLE_DEF

            elif word_upper == 'SFM':
                context.system.SPINDLE = "SFM"
                spindle_state = 'CLW'

            elif word_upper == 'SMM':
                context.system.SPINDLE = "SMM"
                spindle_state = 'CLW'

            elif word_upper == 'RPM':
                context.system.SPINDLE = "RPM"

            elif word_upper == 'MAXRPM':
                if command.numeric and len(command.numeric) > 1:
                    context.system.MAX_CSS = command.numeric[1]

    # Output M-code with S value on same line
    if spindle_state == 'CLW':
        context.write("M3")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write(" ")
            context.registers.s = context.globalVars.SPINDLE_RPM
            context.show("S")
        context.writeBlock()

    elif spindle_state == 'CCLW':
        context.write("M4")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write(" ")
            context.registers.s = context.globalVars.SPINDLE_RPM
            context.show("S")
        context.writeBlock()

    elif spindle_state == 'ORIENT':
        context.write("M19")
        context.writeBlock()

    else:  # OFF
        context.write("M5")
        context.writeBlock()
