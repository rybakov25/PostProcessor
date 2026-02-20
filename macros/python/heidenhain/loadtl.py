# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN LOADTL MACRO - Tool Change for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process LOADTL tool change command

    Heidenhain format:
    TOOL DEF ## L+0 R+0
    TOOL CALL ## Z S####

    APT Examples:
      LOADTL/5
      LOADTL/3,150
    """

    if not command.numeric or len(command.numeric) == 0:
        return

    # Get tool number
    tool_num = int(command.numeric[0])
    context.globalVars.TOOL = tool_num
    context.registers.t = tool_num

    # Get tool length offset (optional)
    length_offset = tool_num  # Default to tool number
    if len(command.numeric) > 1:
        length_offset = int(command.numeric[1])

    # Get tool radius offset
    radius_offset = tool_num

    # Stop spindle
    context.write("M5")

    # Turn off coolant
    context.write("M9")

    # Retract to safe Z
    context.write("L Z+100 R0 FMAX M9")

    # Tool definition and call
    context.write("TOOL DEF " + str(tool_num) + " L+" + str(length_offset) + " R+" + str(radius_offset))
    context.write("TOOL CALL " + str(tool_num) + " Z S" + str(int(context.globalVars.SPINDLE_RPM)))

    # Restart spindle
    context.write("M3")

    # Turn on coolant
    context.write("M8")

    # Output tool comment
    context.write("; TOOL: " + str(tool_num))
