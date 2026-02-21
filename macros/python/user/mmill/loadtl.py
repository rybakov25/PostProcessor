# -*- coding: ascii -*-
# ============================================================================
# MMILL LOADTL MACRO - Tool Change
# ============================================================================
#     MMILL
#  RTCPON, M101H0, G0 B0
# ============================================================================

def execute(context, command):
    """
    Process LOADTL for MMILL

    Adds:
    - RTCPON after tool change
    - M101H0 (head clamp)
    - G0 B0 (rotate B axis to 0)
    """
    # Check if same tool
    if context.globalVars.TOOLCHG_IGNORE_SAME:
        new_tool = int(command.numeric[0]) if command.numeric and len(command.numeric) > 0 else 0
        if context.globalVars.TOOL == new_tool:
            return

    # Get tool number
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.TOOL = int(command.numeric[0])

    context.globalVars.HVAL = 1

    # Get spindle speed
    spindle_speed = 1600
    if command.numeric and len(command.numeric) > 1:
        spindle_speed = command.numeric[1]

    context.registers.s = spindle_speed

    # Output tool change (using block numbering from context)
    context.write("T" + str(context.globalVars.TOOL))
    context.write("D1")
    context.write("M6")

    # Post-tool positioning
    context.write("G0 B0")
    context.write("M101H0")
    context.write("RTCPON")

    # Enable spindle
    context.write("S" + str(int(spindle_speed)) + " M3")

    # Set flags
    context.globalVars.TOOLCHNG = 1
    context.globalVars.FTOOL = context.globalVars.TOOL
