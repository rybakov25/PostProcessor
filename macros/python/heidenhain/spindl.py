# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN SPINDL MACRO - Spindle Control for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process SPINDL spindle control command

    Heidenhain format:
    M3 S#### (Spindle CW)
    M4 S#### (Spindle CCW)
    M5 (Spindle stop)

    APT Examples:
      SPINDL/ON, CLW, 1600
      SPINDL/OFF
    """

    # Get RPM if specified
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.SPINDLE_RPM = command.numeric[0]

    # Update spindle register
    context.registers.s = context.globalVars.SPINDLE_RPM

    # Process minor words
    spindle_state = context.globalVars.SPINDLE_DEF

    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()

            if word_upper in ['ON', 'CLW', 'CLOCKWISE']:
                spindle_state = 'CLW'
                context.globalVars.SPINDLE_DEF = 'CLW'

            elif word_upper in ['CCLW', 'CCW']:
                spindle_state = 'CCLW'
                context.globalVars.SPINDLE_DEF = 'CCLW'

            elif word_upper == 'OFF':
                spindle_state = 'OFF'

    # Output spindle command
    if spindle_state == 'CLW':
        context.write("M3")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write("S" + str(int(context.globalVars.SPINDLE_RPM)))

    elif spindle_state == 'CCLW':
        context.write("M4")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write("S" + str(int(context.globalVars.SPINDLE_RPM)))

    else:  # OFF
        context.write("M5")
