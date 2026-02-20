# -*- coding: ascii -*-
# ============================================================================
# HAAS FINI MACRO - Program End for Haas NGC controllers
# ============================================================================

def execute(context, command):
    """
    Process FINI command and output program end

    Haas format:
    M5 (Spindle stop)
    M9 (Coolant off)
    G91 G28 Z0 (Return to Z home)
    G28 X0 Y0 (Return to XY home)
    M30 (Program end)
    % (Program end marker)
    """

    # Stop spindle
    context.write("M5")

    # Turn off coolant
    context.write("M9")

    # Cancel tool compensation
    context.write("G40")

    # Cancel work coordinate system
    context.write("G53")

    # Return to reference point (Z first)
    context.write("G91 G28 Z0")

    # Return to reference point (XY)
    context.write("G28 X0 Y0")

    # Program end
    context.write("M30")

    # Program end marker
    context.write("%")

    # Output completion message
    context.write("(PROGRAM END)")
