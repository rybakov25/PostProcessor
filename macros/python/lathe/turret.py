# -*- coding: ascii -*-
# ============================================================================
# TURRET MACRO - Turret Control for Lathe (Fanuc/Haas)
# ============================================================================

def execute(context, command):
    """
    Process TURRET turret rotation command

    Lathe format:
    T#### (Tool number and turret position)
    Example: T0101 (Tool 1, Offset 1)
             T0202 (Tool 2, Offset 2)

    APT Examples:
      TURRET/1
      TURRET/3,2  (Tool 3, Offset 2)
    """

    if not command.numeric or len(command.numeric) == 0:
        return

    # Get tool number (turret position)
    tool_num = int(command.numeric[0])
    context.globalVars.TOOL = tool_num
    context.registers.t = tool_num

    # Get offset number (optional, defaults to tool number)
    offset_num = tool_num
    if len(command.numeric) > 1:
        offset_num = int(command.numeric[1])

    # Fanuc/Haas turret format: T##00 for position change only
    # Or T## for full tool change with offset
    turret_pos = "T" + str(tool_num).zfill(2) + str(offset_num).zfill(2)

    # Output turret rotation
    context.write(turret_pos)

    # Output comment
    context.write("(TURRET: T" + str(tool_num) + " OFFSET: " + str(offset_num) + ")")

    # Store for later use
    context.globalVars.CURRENT_OFFSET = offset_num
