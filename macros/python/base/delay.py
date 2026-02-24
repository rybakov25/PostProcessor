# -*- coding: ascii -*-
"""
DELAY MACRO - Dwell/Pause

Handles DELAY commands for dwell/pause operations.
Supports time-based (seconds) and revolution-based delays.

Examples:
    DELAY/2.5           - Dwell for 2.5 seconds
    DELAY/REV,10        - Dwell for 10 spindle revolutions
"""


def execute(context, command):
    """
    Process DELAY dwell/pause command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Check for valid input
    if not command.numeric or len(command.numeric) == 0:
        return

    # Check for revolution-based delay (DELAY/REV,n)
    is_revolution = False
    if command.minorWords:
        for word in command.minorWords:
            if word.upper() == 'REV':
                is_revolution = True
                break

    # Get delay value
    delay_value = command.numeric[0]

    if is_revolution:
        # Revolution-based delay
        # Convert to time based on spindle RPM
        spindle_rpm = context.globalVars.GetDouble("SPINDLE_RPM", 1000.0)
        if spindle_rpm <= 0:
            spindle_rpm = 1000.0  # Default fallback

        # Time = revolutions / (RPM / 60) = revolutions * 60 / RPM
        delay_seconds = (delay_value * 60.0) / spindle_rpm

        # Output G04 P (seconds format)
        context.write(f"G04 P{delay_seconds:.3f}")
    else:
        # Time-based delay (seconds)
        # Supports both G04 X (seconds) and G04 P (milliseconds)
        use_x_format = context.globalVars.Get("DELAY_USE_X", 1)

        if use_x_format:
            # G04 X for seconds
            context.write(f"G04 X{delay_value:.3f}")
        else:
            # G04 P for milliseconds
            delay_ms = delay_value * 1000.0
            context.write(f"G04 P{delay_ms:.0f}")

    # Update MTIME global variable (total machine time)
    current_mtime = context.globalVars.GetDouble("MTIME", 0.0)
    if is_revolution:
        current_mtime += delay_seconds
    else:
        current_mtime += delay_value
    context.globalVars.SetDouble("MTIME", current_mtime)
