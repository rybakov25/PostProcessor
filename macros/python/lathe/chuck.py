# -*- coding: ascii -*-
# ============================================================================
# CHUCK MACRO - Chuck Control for Lathe
# ============================================================================

def execute(context, command):
    """
    Process CHUCK chuck control command

    Lathe format:
    M10 (Chuck clamp)
    M11 (Chuck unclamp)
    M70 (Chuck forward/high pressure)
    M71 (Chuck reverse/low pressure)

    APT Examples:
      CHUCK/CLAMP
      CHUCK/UNCLAMP
      CHUCK/ON
      CHUCK/OFF
      CHUCK/HIGH
      CHUCK/LOW
    """

    chuck_state = context.globalVars.Get("CHUCK_STATE", "CLAMP")

    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()

            if word_upper in ['CLAMP', 'ON', 'CLOSE']:
                chuck_state = 'CLAMP'
                context.globalVars.Set("CHUCK_STATE", 'CLAMP')

            elif word_upper in ['UNCLAMP', 'OFF', 'OPEN']:
                chuck_state = 'UNCLAMP'
                context.globalVars.Set("CHUCK_STATE", 'UNCLAMP')

            elif word_upper in ['HIGH', 'FORWARD', 'HIGH_PRESSURE']:
                chuck_state = 'HIGH'
                context.globalVars.Set("CHUCK_STATE", 'HIGH')

            elif word_upper in ['LOW', 'REVERSE', 'LOW_PRESSURE']:
                chuck_state = 'LOW'
                context.globalVars.Set("CHUCK_STATE", 'LOW')

    # Output chuck command based on controller
    # Fanuc/Haas typical codes:
    # M10/M11 - Standard chuck clamp/unclamp
    # M70/M71 - High/low pressure chuck
    # M208/M209 - Chuck forward/back (some controllers)

    if chuck_state == 'CLAMP':
        context.write("M10")  # Chuck clamp
        context.write("(CHUCK CLAMP)")

    elif chuck_state == 'UNCLAMP':
        context.write("M11")  # Chuck unclamp
        context.write("(CHUCK UNCLAMP)")

    elif chuck_state == 'HIGH':
        context.write("M70")  # Chuck high pressure
        context.write("(CHUCK HIGH PRESSURE)")

    elif chuck_state == 'LOW':
        context.write("M71")  # Chuck low pressure
        context.write("(CHUCK LOW PRESSURE)")
