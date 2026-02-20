# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN COOLNT MACRO - Coolant Control for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process COOLNT coolant control command

    Heidenhain format:
    M8 (Coolant on)
    M9 (Coolant off)

    APT Examples:
      COOLNT/ON
      COOLNT/OFF
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

            elif word_upper in ['OFF', 'AIR']:
                coolant_state = 'OFF'
                context.globalVars.COOLANT_DEF = 'OFF'

    # Output coolant command
    if coolant_state == 'FLOOD' or coolant_state == 'MIST':
        context.write("M8")
    else:  # OFF
        context.write("M9")
