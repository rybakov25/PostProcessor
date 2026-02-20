# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN FINI MACRO - Program End for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process FINI command and output program end

    Heidenhain format:
    M30
    END PGM {name} MM
    """

    # Stop spindle
    context.write("M5")

    # Turn off coolant
    context.write("M9")

    # Retract to safe Z
    context.write("L Z+100 R0 FMAX")

    # Program end
    context.write("M30")

    # End program marker
    mode = context.globalVars.Get("HEIDENHAIN_MODE", "MM")
    program_name = context.globalVars.Get("PROGRAM_NAME", "MAIN")
    context.write("END PGM " + program_name + " " + mode)
