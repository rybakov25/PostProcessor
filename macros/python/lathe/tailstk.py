# -*- coding: ascii -*-
# ============================================================================
# TAILSTK MACRO - Tailstock Control for Lathe
# ============================================================================

def execute(context, command):
    """
    Process TAILSTK tailstock control command

    Lathe format:
    M20 (Tailstock forward/quill extend)
    M21 (Tailstock backward/quill retract)
    M72 (Tailstock clamp)
    M73 (Tailstock unclamp)

    APT Examples:
      TAILSTK/ON
      TAILSTK/OFF
      TAILSTK/FORWARD
      TAILSTK/BACK
      TAILSTK/CLAMP
      TAILSTK/UNCLAMP
    """

    tailstock_state = context.globalVars.Get("TAILSTOCK_STATE", "BACK")

    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()

            if word_upper in ['ON', 'FORWARD', 'EXTEND', 'ADVANCE']:
                tailstock_state = 'FORWARD'
                context.globalVars.Set("TAILSTOCK_STATE", 'FORWARD')

            elif word_upper in ['OFF', 'BACK', 'RETRACT', 'RETURN']:
                tailstock_state = 'BACK'
                context.globalVars.Set("TAILSTOCK_STATE", 'BACK')

            elif word_upper == 'CLAMP':
                tailstock_state = 'CLAMP'
                context.globalVars.Set("TAILSTOCK_STATE", 'CLAMP')

            elif word_upper == 'UNCLAMP':
                tailstock_state = 'UNCLAMP'
                context.globalVars.Set("TAILSTOCK_STATE", 'UNCLAMP')

    # Output tailstock command based on controller
    # Fanuc/Haas typical codes:
    # M20/M21 - Tailstock forward/back
    # M72/M73 - Tailstock clamp/unclamp
    # M208/M209 - Tailstock advance/return (some controllers)

    if tailstock_state == 'FORWARD':
        context.write("M20")  # Tailstock forward
        context.write("(TAILSTOCK FORWARD)")

    elif tailstock_state == 'BACK':
        context.write("M21")  # Tailstock back
        context.write("(TAILSTOCK BACK)")

    elif tailstock_state == 'CLAMP':
        context.write("M72")  # Tailstock clamp
        context.write("(TAILSTOCK CLAMP)")

    elif tailstock_state == 'UNCLAMP':
        context.write("M73")  # Tailstock unclamp
        context.write("(TAILSTOCK UNCLAMP)")
