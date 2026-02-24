# -*- coding: ascii -*-
"""
FSQ-100 COOLNT MACRO - Coolant Control for TOS KURIM FSQ100

Handles coolant on/off and type selection:
- M7 - Mist coolant
- M8 - Flood coolant
- M9 - Coolant off
- M50 - Through-tool coolant
- M51 - Air blast

Uses BlockWriter for modal output (coolant is non-modal M-code).
"""


def execute(context, command):
    """
    Process COOLNT coolant control command for FSQ-100

    Args:
        context: Postprocessor context
        command: APT command
    """
    coolant_state = context.globalVars.COOLANT_DEF

    # Process minor words
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

    # Output coolant command for FSQ-100 (non-modal M-codes)
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
