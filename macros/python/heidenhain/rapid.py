# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN RAPID MACRO - Rapid Motion for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process RAPID rapid motion command

    Heidenhain format:
    L X###.### Y###.### Z###.### FMAX M2

    APT format: RAPID/X, Y, Z
    """

    if not command.numeric or len(command.numeric) == 0:
        return

    # Get coordinates
    x = command.numeric[0] if len(command.numeric) > 0 else 0
    y = command.numeric[1] if len(command.numeric) > 1 else 0
    z = command.numeric[2] if len(command.numeric) > 2 else 0

    # Update registers
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    # Set motion type to rapid
    context.system.MOTION = 'RAPID'
    context.currentMotionType = 'RAPID'

    # Output rapid move with FMAX
    line = "L X" + format_num(x) + " Y" + format_num(y) + " Z" + format_num(z) + " R0 FMAX"

    context.write(line)

def format_num(value):
    """Format number with 3 decimal places for Heidenhain"""
    rounded = round(value, 3)
    if rounded >= 0:
        return "+" + str(rounded)
    return str(rounded)
