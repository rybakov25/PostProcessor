# -*- coding: ascii -*-
# ============================================================================
# HAAS COOLNT MACRO - Coolant Control for Haas NGC controllers
# ============================================================================

def execute(context, command):
    """
    Process COOLNT coolant control command

    Haas format:
    M8 (Coolant flood on)
    M7 (Coolant mist on)
    M9 (Coolant off)

    APT Examples:
      COOLNT/ON
      COOLNT/OFF
      COOLNT/FLOOD
      COOLNT/MIST
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

            elif word_upper == 'AIR':
                coolant_state = 'AIR'
                context.globalVars.COOLANT_DEF = 'AIR'

            elif word_upper in ['OFF', 'THRU']:
                coolant_state = 'OFF'
                context.globalVars.COOLANT_DEF = 'OFF'

    # Output coolant command
    if coolant_state == 'FLOOD':
        context.write("M8")
    elif coolant_state == 'MIST':
        context.write("M7")
    elif coolant_state == 'AIR':
        context.write("M50")  # Air blast
    else:  # OFF
        context.write("M9")
