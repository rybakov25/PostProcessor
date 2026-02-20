# -*- coding: ascii -*-
# ============================================================================
# FANUC LATHE GOTO MACRO - Linear Motion for Fanuc Lathe
# ============================================================================

def execute(context, command):
    """
    Process GOTO linear motion command for lathe

    Fanuc Lathe format:
    G0/G1 X###.### Z###.###
    Note: Lathe uses X (diameter) and Z (length)

    APT format: GOTO/X, Z
    """

    if not command.numeric or len(command.numeric) == 0:
        return

    # Get linear axes (X is diameter, Z is length)
    x = command.numeric[0] if len(command.numeric) > 0 else 0
    z = command.numeric[1] if len(command.numeric) > 1 else 0

    # Update registers
    context.registers.x = x
    context.registers.z = z

    # Determine motion type
    motion_type = context.system.MOTION
    is_rapid = (motion_type == 'RAPID' or
                motion_type == 'RAPID_BREAK' or
                context.currentMotionType == 'RAPID')

    if is_rapid:
        # Rapid move G00
        line = "G00 X" + format_num(x) + " Z" + format_num(z)
        context.write(line)

        # Reset motion type
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'

    else:
        # Linear move G01
        line = "G01 X" + format_num(x) + " Z" + format_num(z)

        # Add feedrate (modal)
        if context.registers.f and context.registers.f > 0:
            last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
            if last_feed != context.registers.f:
                line += " F" + format_feed(context.registers.f)
                context.globalVars.SetDouble("LAST_FEED", context.registers.f)

        context.write(line)

def format_num(value):
    """Format number with 3 decimal places for Fanuc Lathe"""
    rounded = round(value, 3)
    return str(rounded)

def format_feed(value):
    """Format feedrate with 2 decimal places for lathe"""
    return str(round(value, 2))
