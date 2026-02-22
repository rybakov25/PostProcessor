# -*- coding: ascii -*-
"""
SUBPROG MACRO - Subroutine Control

Handles subroutine calls and returns.
Tracks call count for debugging and optimization.

Examples:
    CALLSUB/1001        - Call subroutine O1001 (M98 P1001)
    ENDSUB              - End subroutine (M99)

Notes:
    Controller-specific formats:
    - Siemens 840D uses L... for subroutines or M17/M99 for returns
    - Fanuc uses M98 P... / M99 format
    This macro provides compatibility with standard M98/M99 format
"""


def execute(context, command):
    """
    Process SUBPROG subroutine command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Check for minor words (CALLSUB, ENDSUB)
    is_callsub = False
    is_endsub = False

    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper == 'CALLSUB':
                is_callsub = True
            elif word_upper == 'ENDSUB':
                is_endsub = True

    # Handle subroutine call
    if is_callsub:
        # Get subroutine number
        if command.numeric and len(command.numeric) > 0:
            sub_num = int(command.numeric[0])

            # Get call count for tracking
            call_count = context.globalVars.GetInt(f"SUBCALL_{sub_num}", 0)
            call_count += 1
            context.globalVars.SetInt(f"SUBCALL_{sub_num}", call_count)

            # Track total subroutine calls
            total_calls = context.globalVars.GetInt("SUBCALL_TOTAL", 0)
            total_calls += 1
            context.globalVars.SetInt("SUBCALL_TOTAL", total_calls)

            # Output subroutine call
            # Format options:
            # - L1001 (Siemens standard)
            # - M98 P1001 (Fanuc-style, for compatibility)
            use_m98 = context.globalVars.Get("SUBPROG_USE_M98", 1)

            if use_m98:
                # M98 P format (Fanuc-style)
                context.write(f"M98 P{sub_num}")
            else:
                # L format (Siemens standard)
                context.write(f"L{sub_num}")

            # Store current subroutine level
            current_level = context.globalVars.GetInt("SUB_LEVEL", 0)
            current_level += 1
            context.globalVars.SetInt("SUB_LEVEL", current_level)
            context.globalVars.SetInt(f"SUB_LEVEL_{current_level}", sub_num)

    # Handle subroutine end
    elif is_endsub:
        # Output subroutine return
        # Format options:
        # - M17 (Siemens standard for subprogram end)
        # - M99 (Fanuc-style, for compatibility)
        use_m99 = context.globalVars.Get("SUBPROG_USE_M99", 1)

        if use_m99:
            context.write("M99")
        else:
            context.write("M17")

        # Update subroutine level
        current_level = context.globalVars.GetInt("SUB_LEVEL", 0)
        if current_level > 0:
            context.globalVars.SetInt(f"SUB_LEVEL_{current_level}", 0)
            current_level -= 1
            context.globalVars.SetInt("SUB_LEVEL", current_level)

    # Handle direct numeric call (e.g., SUBPROG/1001 without CALLSUB word)
    elif command.numeric and len(command.numeric) > 0:
        sub_num = int(command.numeric[0])

        # Get call count for tracking
        call_count = context.globalVars.GetInt(f"SUBCALL_{sub_num}", 0)
        call_count += 1
        context.globalVars.SetInt(f"SUBCALL_{sub_num}", call_count)

        # Output subroutine call
        use_m98 = context.globalVars.Get("SUBPROG_USE_M98", 1)

        if use_m98:
            context.write(f"M98 P{sub_num}")
        else:
            context.write(f"L{sub_num}")

        # Update level
        current_level = context.globalVars.GetInt("SUB_LEVEL", 0)
        current_level += 1
        context.globalVars.SetInt("SUB_LEVEL", current_level)
