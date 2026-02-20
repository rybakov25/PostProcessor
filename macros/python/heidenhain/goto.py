# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN GOTO MACRO - Linear Motion for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process GOTO linear motion command

    Heidenhain format:
    L X###.### Y###.### Z###.### F### M3
    L X###.### Y###.### Z###.### R0 FMAX M2 (for rapid)

    APT format: GOTO/X, Y, Z
    """

    if not command.numeric or len(command.numeric) == 0:
        return

    # Get coordinates
    x = command.numeric[0] if len(command.numeric) > 0 else 0
    y = command.numeric[1] if len(command.numeric) > 1 else 0
    z = command.numeric[2] if len(command.numeric) > 2 else 0

    # Get rotary axes (for 5-axis)
    a = command.numeric[3] if len(command.numeric) > 3 else None
    b = command.numeric[4] if len(command.numeric) > 4 else None
    c = command.numeric[5] if len(command.numeric) > 5 else None

    # Update registers
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    if a is not None:
        context.registers.a = a
    if b is not None:
        context.registers.b = b
    if c is not None:
        context.registers.c = c

    # Determine motion type
    motion_type = context.system.MOTION
    is_rapid = (motion_type == 'RAPID' or
                motion_type == 'RAPID_BREAK' or
                context.currentMotionType == 'RAPID')

    # Build Heidenhain line
    if is_rapid:
        # Rapid move with FMAX
        line = "L X" + format_num(x) + " Y" + format_num(y) + " Z" + format_num(z) + " R0 FMAX"

        # Add rotary axes for 5-axis
        if a is not None:
            line += " A" + format_num(a)
        if b is not None:
            line += " B" + format_num(b)
        if c is not None:
            line += " C" + format_num(c)

        context.write(line)

        # Reset motion type
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'

    else:
        # Linear move with feed
        line = "L X" + format_num(x) + " Y" + format_num(y) + " Z" + format_num(z)

        # Add rotary axes
        if a is not None:
            line += " A" + format_num(a)
        if b is not None:
            line += " B" + format_num(b)
        if c is not None:
            line += " C" + format_num(c)

        # Add feedrate (modal)
        if context.registers.f and context.registers.f > 0:
            last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
            if last_feed != context.registers.f:
                line += " F" + format_feed(context.registers.f)
                context.globalVars.SetDouble("LAST_FEED", context.registers.f)

        context.write(line)

def format_num(value):
    """Format number with 3 decimal places for Heidenhain"""
    rounded = round(value, 3)
    # Heidenhain uses + sign for positive values
    if rounded >= 0:
        return "+" + str(rounded)
    return str(rounded)

def format_feed(value):
    """Format feedrate with 1 decimal place"""
    return str(round(value, 1))
