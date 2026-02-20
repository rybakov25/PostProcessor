# -*- coding: ascii -*-
# ============================================================================
# HAAS GOTO MACRO - Linear Motion for Haas NGC controllers
# ============================================================================

def execute(context, command):
    """
    Process GOTO linear motion command

    Haas format:
    G0/G1 X###.### Y###.### Z###.###

    APT format: GOTO/X, Y, Z, I, J, K
    where I,J,K are tool direction vectors for 5-axis
    """

    # Check for coordinates
    if not command.numeric or len(command.numeric) == 0:
        return

    # Get linear axes
    x = command.numeric[0] if len(command.numeric) > 0 else 0
    y = command.numeric[1] if len(command.numeric) > 1 else 0
    z = command.numeric[2] if len(command.numeric) > 2 else 0

    # Get rotary axes (I, J, K direction vectors)
    i = command.numeric[3] if len(command.numeric) > 3 else None
    j = command.numeric[4] if len(command.numeric) > 4 else None
    k = command.numeric[5] if len(command.numeric) > 5 else None

    # Update linear registers
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    # Update rotary registers if present
    if i is not None:
        context.registers.i = i
    if j is not None:
        context.registers.j = j
    if k is not None:
        context.registers.k = k

    # Determine motion type from SYSTEM.MOTION
    motion_type = context.system.MOTION

    # Check if this should be rapid (G0)
    is_rapid = (motion_type == 'RAPID' or
                motion_type == 'RAPID_BREAK' or
                context.currentMotionType == 'RAPID')

    if is_rapid:
        # Rapid move G0 (Haas uses G0)
        line = "G0 X" + format_num(x)
        if len(command.numeric) > 1:
            line += " Y" + format_num(y)
        if len(command.numeric) > 2:
            line += " Z" + format_num(z)

        # Add rotary axes for 5-axis (convert IJK to ABC)
        if i is not None and j is not None and k is not None:
            a, b, c = ijk_to_abc(i, j, k)
            line += " A" + format_num(a)
            line += " B" + format_num(b)

        context.write(line)

        # Reset motion type after rapid
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'

    else:
        # Linear move G1 (Haas uses G1)
        line = "G1 X" + format_num(x)
        if len(command.numeric) > 1:
            line += " Y" + format_num(y)
        if len(command.numeric) > 2:
            line += " Z" + format_num(z)

        # Add rotary axes for 5-axis
        if i is not None and j is not None and k is not None:
            a, b, c = ijk_to_abc(i, j, k)
            line += " A" + format_num(a)
            line += " B" + format_num(b)

        context.write(line)

        # Output feed ONLY if it changed (modal)
        if context.registers.f and context.registers.f > 0:
            last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
            if last_feed != context.registers.f:
                context.write("F" + format_feed(context.registers.f))
                context.globalVars.SetDouble("LAST_FEED", context.registers.f)

def ijk_to_abc(i, j, k):
    """
    Convert IJK direction vector to ABC angles (degrees)
    """
    import math

    # A angle (around X axis)
    a = math.degrees(math.atan2(j, k))

    # B angle (around Y axis)
    b = math.degrees(math.atan2(i, math.sqrt(j*j + k*k)))

    # Normalize to 0-360 range
    if a < 0:
        a += 360
    if b < 0:
        b += 360

    return round(a, 3), round(b, 3), 0.0

def format_num(value):
    """Format number with 3 decimal places for Haas"""
    rounded = round(value, 3)
    return str(rounded)

def format_feed(value):
    """Format feedrate with 1 decimal place"""
    return str(round(value, 1))
