# -*- coding: ascii -*-
"""
CUTCOM MACRO - Cutter Compensation

Handles cutter radius compensation (G41/G42/G40).
Supports plane selection (XY, YZ, ZX) and modal output.

Examples:
    TLCOMP/ON,LEFT      - Enable left compensation (G41)
    TLCOMP/ON,RIGHT     - Enable right compensation (G42)
    TLCOMP/OFF          - Disable compensation (G40)
    WPLANE/XYPLAN       - Set XY working plane (G17)
"""


def execute(context, command):
    """
    Process CUTCOM cutter compensation command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Determine compensation state
    comp_state = None  # None, LEFT, RIGHT, OFF
    plane = context.globalVars.Get("WORK_PLANE", "XYPLAN")

    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()

            if word_upper in ['ON', 'LEFT']:
                comp_state = 'LEFT'
            elif word_upper == 'RIGHT':
                comp_state = 'RIGHT'
            elif word_upper == 'OFF':
                comp_state = 'OFF'

    # Check for plane selection in numeric values or additional words
    if command.numeric and len(command.numeric) > 0:
        # Check for plane indicator
        plane_val = int(command.numeric[0]) if command.numeric[0] == int(command.numeric[0]) else 0
        if plane_val == 17 or (len(command.minorWords) > 0 and 'XYPLAN' in [w.upper() for w in command.minorWords]):
            plane = 'XYPLAN'
        elif plane_val == 18 or (len(command.minorWords) > 0 and 'YZPLAN' in [w.upper() for w in command.minorWords]):
            plane = 'YZPLAN'
        elif plane_val == 19 or (len(command.minorWords) > 0 and 'ZXPLAN' in [w.upper() for w in command.minorWords]):
            plane = 'ZXPLAN'

    # Also check minor words for plane
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper == 'XYPLAN':
                plane = 'XYPLAN'
            elif word_upper == 'YZPLAN':
                plane = 'YZPLAN'
            elif word_upper == 'ZXPLAN':
                plane = 'ZXPLAN'

    # Store current plane
    context.globalVars.Set("WORK_PLANE", plane)

    # Get previous compensation state for modal check
    prev_comp = context.globalVars.Get("CUTTER_COMP", "OFF")

    # If state unchanged, skip output (modal)
    if comp_state is None:
        comp_state = prev_comp
    elif comp_state == prev_comp:
        return  # No change, skip output

    # Build output parts
    parts = []

    # Output plane selection if changed
    prev_plane = context.globalVars.Get("ACTIVE_PLANE", "XYPLAN")
    if plane != prev_plane:
        if plane == 'XYPLAN':
            parts.append("G17")
        elif plane == 'YZPLAN':
            parts.append("G18")
        elif plane == 'ZXPLAN':
            parts.append("G19")
        context.globalVars.Set("ACTIVE_PLANE", plane)

    # Output compensation code
    if comp_state == 'LEFT':
        parts.append("G41")
        context.globalVars.Set("CUTTER_COMP", "LEFT")
    elif comp_state == 'RIGHT':
        parts.append("G42")
        context.globalVars.Set("CUTTER_COMP", "RIGHT")
    else:  # OFF
        parts.append("G40")
        context.globalVars.Set("CUTTER_COMP", "OFF")

    # Add D code for tool offset (modal)
    tool_offset = context.globalVars.GetInt("TOOL_OFFSET", 1)
    if comp_state != 'OFF':
        parts.append(f"D{tool_offset}")

    # Output if we have parts
    if parts:
        context.write(" ".join(parts))
