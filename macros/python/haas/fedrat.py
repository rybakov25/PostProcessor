# -*- coding: ascii -*-
# ============================================================================
# HAAS FEDRAT MACRO - Feedrate Control for Haas NGC controllers
# ============================================================================

def execute(context, command):
    """
    Process FEDRAT feedrate control command

    Haas format:
    F###.# (Feedrate in inch/min or mm/min)

    APT Examples:
      FEDRAT/100.0
      FEDRAT/5.0,IPM
    """

    if not command.numeric or len(command.numeric) == 0:
        return

    feed = command.numeric[0]
    context.registers.f = feed

    # Check for feed mode (IPM, MMPM, IPR, MPR)
    feed_mode = "FPM"  # Feed per minute default
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper in ['IPM', 'MMPM']:
                feed_mode = "FPM"
            elif word_upper in ['IPR', 'MPR']:
                feed_mode = "RPM"

    # Update feed mode global
    context.globalVars.FEEDMODE = feed_mode

    # Output feed ONLY if it changed (modal)
    last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
    if last_feed != feed:
        context.write("F" + format_feed(feed))
        context.globalVars.SetDouble("LAST_FEED", feed)

def format_feed(value):
    """Format feedrate with 1 decimal place for Haas"""
    return str(round(value, 1))
