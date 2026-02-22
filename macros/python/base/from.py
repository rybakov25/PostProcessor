# -*- coding: ascii -*-
"""
FROM MACRO - Initial Position

Handles FROM command to set initial/home position.
Supports GLOBAL.FROM modes for different approach strategies.

Examples:
    FROM/X,100,Y,200,Z,50   - Set position at X100 Y200 Z50
    FROM/100,200,50         - Set position (shorthand)

GLOBAL.FROM modes:
    0 - RAPID: Use rapid traverse (G0)
    1 - GOTO: Use linear feed (G1)
    2 - HOME: Use home return (G53/G28)
"""


def execute(context, command):
    """
    Process FROM initial position command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Check for valid input
    if not command.numeric or len(command.numeric) == 0:
        return

    # Get coordinates
    x = command.numeric[0] if len(command.numeric) > 0 else 0
    y = command.numeric[1] if len(command.numeric) > 1 else 0
    z = command.numeric[2] if len(command.numeric) > 2 else 0

    # Store as initial position
    context.globalVars.SetDouble("FROM_X", x)
    context.globalVars.SetDouble("FROM_Y", y)
    context.globalVars.SetDouble("FROM_Z", z)

    # Update registers
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    # Get FROM mode (0=RAPID, 1=GOTO, 2=HOME)
    from_mode = context.globalVars.GetInt("FROM_MODE", 0)

    # Build output based on mode
    match from_mode:
        case 0:
            # RAPID mode - use G0
            context.write(f"G0 X{x:.3f} Y{y:.3f} Z{z:.3f}")

        case 1:
            # GOTO mode - use G1 with feed
            feed = context.globalVars.GetDouble("FEEDRATE", 100.0)
            if feed <= 0:
                feed = 100.0
            context.write(f"G1 X{x:.3f} Y{y:.3f} Z{z:.3f} F{feed:.1f}")

        case 2:
            # HOME mode - use home return
            # First move to intermediate position, then home
            context.write(f"G0 X{x:.3f} Y{y:.3f}")
            context.write(f"G53 Z{z:.3f}")

        case _:
            # Default to RAPID
            context.write(f"G0 X{x:.3f} Y{y:.3f} Z{z:.3f}")

    # Mark as initial position set
    context.globalVars.Set("FROM_SET", 1)
