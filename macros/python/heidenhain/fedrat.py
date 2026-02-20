# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN FEDRAT MACRO - Feedrate Control for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process FEDRAT feedrate control command

    Heidenhain format:
    F###.# (Feedrate in mm/min)

    APT Examples:
      FEDRAT/100.0
    """

    if not command.numeric or len(command.numeric) == 0:
        return

    feed = command.numeric[0]
    context.registers.f = feed

    # Output feed ONLY if it changed (modal)
    last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
    if last_feed != feed:
        context.write("F" + format_feed(feed))
        context.globalVars.SetDouble("LAST_FEED", feed)

def format_feed(value):
    """Format feedrate with 1 decimal place"""
    return str(round(value, 1))
