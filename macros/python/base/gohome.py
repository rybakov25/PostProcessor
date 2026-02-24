# -*- coding: ascii -*-
"""
GOHOME MACRO - Return to Home

Handles GOHOME command to return machine to home position.
Supports individual axis selection and modal output.

Examples:
    GOHOME/X,Y,Z      - Return all axes to home
    GOHOME/Z          - Return Z axis only to home
    GOHOME/X,Y        - Return X and Y axes to home

Configuration:
    Use G53 for absolute home (machine coordinate system)
    Use G28 for reference point return (controller dependent)
"""


def execute(context, command):
    """
    Process GOHOME return to home command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Determine which axes to home
    home_x = False
    home_y = False
    home_z = False

    # Check minor words for axis selection
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper == 'X':
                home_x = True
            elif word_upper == 'Y':
                home_y = True
            elif word_upper == 'Z':
                home_z = True

    # If no axes specified, default to all axes
    if not home_x and not home_y and not home_z:
        home_x = True
        home_y = True
        home_z = True

    # Get home positions from global vars (or use current as default)
    home_x_pos = context.globalVars.GetDouble("HOME_X", 0.0)
    home_y_pos = context.globalVars.GetDouble("HOME_Y", 0.0)
    home_z_pos = context.globalVars.GetDouble("HOME_Z", 0.0)

    # Determine home method (G53 vs G28)
    use_g53 = context.globalVars.Get("HOME_USE_G53", 1)

    # Build output parts
    parts = []

    if use_g53:
        # G53 - Machine coordinate system (absolute home)
        parts.append("G53")

        # Add modal axis output (only changed axes)
        if home_x:
            prev_x = context.globalVars.GetDouble("PREV_X", -999.0)
            if abs(home_x_pos - prev_x) > 0.001 or home_x:
                parts.append(f"X{home_x_pos:.3f}")
                context.globalVars.SetDouble("PREV_X", home_x_pos)

        if home_y:
            prev_y = context.globalVars.GetDouble("PREV_Y", -999.0)
            if abs(home_y_pos - prev_y) > 0.001 or home_y:
                parts.append(f"Y{home_y_pos:.3f}")
                context.globalVars.SetDouble("PREV_Y", home_y_pos)

        if home_z:
            prev_z = context.globalVars.GetDouble("PREV_Z", -999.0)
            if abs(home_z_pos - prev_z) > 0.001 or home_z:
                parts.append(f"Z{home_z_pos:.3f}")
                context.globalVars.SetDouble("PREV_Z", home_z_pos)
    else:
        # G28 - Reference point return
        # G28 requires intermediate point, then G28 alone for home
        # For simplicity, output G28 with axes
        parts.append("G28")

        if home_x:
            parts.append("X0")
        if home_y:
            parts.append("Y0")
        if home_z:
            parts.append("Z0")

    # Output if we have parts
    if parts:
        context.write(" ".join(parts))

    # Update current position registers
    if home_x:
        context.registers.x = home_x_pos
    if home_y:
        context.registers.y = home_y_pos
    if home_z:
        context.registers.z = home_z_pos
