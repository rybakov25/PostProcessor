# -*- coding: ascii -*-
# ============================================================================
# FANUC LOADTL MACRO - Tool Change for Fanuc controllers
# ============================================================================

def execute(context, command):
    """
    Process LOADTL tool change command

    Fanuc format:
    T## M6 (Tool change)
    G43 H## Z##.### (Tool length compensation)

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

    # Stop spindle before tool change
    context.write("M5")

    # Turn off coolant
    context.write("M9")

    # Retract to safe Z
    context.write("G91 G28 Z0")

    # Tool change
    context.write("T" + str(tool_num) + " M6")

    # Apply tool length compensation
    context.write("G90 G00 G43 H" + str(length_offset) + " Z50.")

    # Restart spindle
    context.write("M3 S" + str(int(context.globalVars.SPINDLE_RPM)))

    # Turn on coolant
    context.write("M8")

    # Output tool comment
    context.write("(TOOL: " + str(tool_num) + ")")
