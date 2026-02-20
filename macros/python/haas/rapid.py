# -*- coding: ascii -*-
# ============================================================================
# HAAS RAPID MACRO - Rapid Motion for Haas NGC controllers
# ============================================================================

def execute(context, command):
    """
    Process RAPID rapid motion command

    Haas format:
    G0 X###.### Y###.### Z###.###

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

    # Output rapid move
    line = "G0 X" + format_num(x)
    if len(command.numeric) > 1:
        line += " Y" + format_num(y)
    if len(command.numeric) > 2:
        line += " Z" + format_num(z)

    context.write(line)

def format_num(value):
    """Format number with 3 decimal places for Haas"""
    rounded = round(value, 3)
    return str(rounded)
