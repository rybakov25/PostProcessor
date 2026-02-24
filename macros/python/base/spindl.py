# -*- coding: ascii -*-
# ============================================================================
# SPINDL MACRO - Spindle Control
# ============================================================================
# IMSpost: spindl.def
#
# Uses BlockWriter for automatic modal checking of S register
# ============================================================================

def execute(context, command):
    """
    Process SPINDL spindle control command

    IMSpost logic:
    - IF CLDATAN.0 -> GLOBAL.SPINDLE_RPM = CLDATAN.1
    - REGISTER.[SYSTEM.SPINDLE_NAME].VALUE = GLOBAL.SPINDLE_RPM
    - CASE CLDATAM:
      - 'CLW' -> SPINDLE = "CLW", GLOBAL.SPINDLE_DEF = 'CLW'
      - 'CCLW' -> SPINDLE = "CCLW"
      - 'ORIENT' -> SPINDLE = "ORIENT"
      - 'OFF' -> REGISTER.ONCE = 1
      - 'ON' -> SPINDLE = GLOBAL.SPINDLE_DEF
      - 'SFM'/'SMM'/'RPM' -> speed mode
      - 'MAXRPM' -> SYSTEM.MAX_CSS
    - If SPINDLE_BLOCK -> USE1SET (modal)
    - Else -> OUTPUT

    APT Examples:
      SPINDL/ON, CLW, 1600
      SPINDL/OFF
      SPINDL/1200
    """

    # ===   ===

    #    ,  RPM
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.SPINDLE_RPM = command.numeric[0]

    #
    context.registers.s = context.globalVars.SPINDLE_RPM

    # ===  minor words ===
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

            elif word_upper == 'ON':
                #
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

    # ===    ===

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
