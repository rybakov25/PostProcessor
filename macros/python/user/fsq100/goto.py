# -*- coding: ascii -*-
"""
FSQ-100 GOTO MACRO - Linear and Circular Motion for TOS KURIM FSQ100

Handles GOTO commands with support for:
- G0/G1 linear motion
- G2/G3 circular interpolation (CW/CCW)
- I, J, K arc center offsets
- Number format: Remove trailing zeros (X800. not X800.000)

Uses BlockWriter for modal X/Y/Z/A/B/C output.
"""

import math


def execute(context, command):
    """
    Process GOTO motion command for FSQ-100

    Args:
        context: Postprocessor context
        command: APT command
    """
    if not command.numeric or len(command.numeric) == 0:
        return

    # Get linear axes
    x = command.numeric[0] if len(command.numeric) > 0 else 0
    y = command.numeric[1] if len(command.numeric) > 1 else 0
    z = command.numeric[2] if len(command.numeric) > 2 else 0

    # Get rotary axes (I, J, K direction vectors for 5-axis)
    i_dir = command.numeric[3] if len(command.numeric) > 3 else None
    j_dir = command.numeric[4] if len(command.numeric) > 4 else None
    k_dir = command.numeric[5] if len(command.numeric) > 5 else None

    # Get arc center offsets (for circular interpolation)
    # These come from CIRCLE command context
    i_center = context.globalVars.GetDouble("CIRCLE_I", 0.0)
    j_center = context.globalVars.GetDouble("CIRCLE_J", 0.0)
    k_center = context.globalVars.GetDouble("CIRCLE_K", 0.0)

    # Update linear registers first (before writeBlock)
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    # Determine motion type
    motion_type = context.system.MOTION
    is_rapid = (motion_type == 'RAPID' or
                motion_type == 'RAPID_BREAK' or
                context.currentMotionType == 'RAPID')

    # Check for circular interpolation
    circle_type = context.globalVars.Get("CIRCLE_TYPE", 0)
    is_circle = circle_type in [2, 3]  # G2 or G3

    if is_circle:
        # Circular interpolation G2/G3 - output directly
        if circle_type == 2:
            gcode = "G2"
        else:
            gcode = "G3"
        
        # Update arc center registers
        context.registers.i = i_center
        context.registers.j = j_center
        context.registers.k = k_center
        
        # Show arc center registers for output
        context.show("I")
        context.show("J")
        context.show("K")
        
        # Reset circle type after output
        context.globalVars.CIRCLE_TYPE = 0
        
    elif is_rapid:
        # Rapid move G0
        gcode = "G0"
        
        # Reset motion type after rapid
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'
        
    else:
        # Linear move G1
        gcode = "G1"
    
    # Build output parts list
    parts = []
    
    if is_circle:
        # Circular interpolation G2/G3
        if circle_type == 2:
            parts.append("G2")
        else:
            parts.append("G3")
        
        # Update arc center registers
        context.registers.i = i_center
        context.registers.j = j_center
        context.registers.k = k_center
        
        # Show arc center registers for output
        context.show("I")
        context.show("J")
        context.show("K")
        
        # Reset circle type after output
        context.globalVars.CIRCLE_TYPE = 0
        
    elif is_rapid:
        # Rapid move G0
        parts.append("G0")
        
        # Reset motion type after rapid
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'
        
    else:
        # Linear move G1
        parts.append("G1")
    
    # Handle rotary axes for 5-axis (convert IJK to ABC)
    if i_dir is not None and j_dir is not None and k_dir is not None:
        a, b, c = ijk_to_abc(i_dir, j_dir, k_dir)
        context.registers.a = a
        context.registers.b = b
    
    # Build output line: G-code first (no newline), then writeBlock for coordinates
    if is_circle:
        # Circular interpolation
        if circle_type == 2:
            context.write("G2 ")
        else:
            context.write("G3 ")
        
        # Update arc center registers
        context.registers.i = i_center
        context.registers.j = j_center
        context.registers.k = k_center
        
        # Show arc center registers for output
        context.show("I")
        context.show("J")
        context.show("K")
        
        # Reset circle type after output
        context.globalVars.CIRCLE_TYPE = 0
        
    elif is_rapid:
        # Rapid move G0
        context.write("G0 ")
        
        # Reset motion type after rapid
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'
        
    else:
        # Linear move G1
        context.write("G1 ")
    
    # Output coordinates with modal checking (adds newline)
    context.writeBlock()


def format_number(value_str):
    """
    Format number for FSQ-100: remove trailing zeros after decimal point

    Examples:
        X800.000 -> X800.
        X20.200 -> X20.2
        X20.000 -> X20.
        I0.000 -> I0.

    Args:
        value_str: String like "X800.000" or "I0"

    Returns:
        Formatted string with trailing zeros removed
    """
    # Split into letter and number parts
    if len(value_str) < 2:
        return value_str

    letter = value_str[0]
    num_part = value_str[1:]

    try:
        # Parse the number
        num = float(num_part)

        # Format with enough precision, then strip trailing zeros
        # Use general format to remove unnecessary zeros
        if num == int(num):
            # Whole number - add decimal point only
            formatted = f"{int(num)}."
        else:
            # Decimal number - format and strip trailing zeros
            formatted = f"{num:.4f}".rstrip('0').rstrip('.')
            # Ensure we have at least one decimal place shown if there was a decimal
            if '.' not in formatted and '.' in num_part:
                formatted = f"{num:.1f}".rstrip('0')
                if formatted.endswith('.'):
                    pass  # Keep the decimal point
                elif '.' not in formatted:
                    formatted += '.'

        return f"{letter}{formatted}"

    except ValueError:
        return value_str


def ijk_to_abc(i, j, k):
    """
    Convert IJK direction vector to ABC angles (degrees)

    For Siemens 840D:
    - A = rotation around X axis
    - B = rotation around Y axis

    Args:
        i: I direction vector component
        j: J direction vector component
        k: K direction vector component

    Returns:
        tuple: (A, B, C) angles in degrees
    """
    # Calculate angles using atan2
    a = math.degrees(math.atan2(j, k))
    b = math.degrees(math.atan2(i, math.sqrt(j*j + k*k)))

    # Normalize to 0-360 range
    if a < 0:
        a += 360
    if b < 0:
        b += 360

    return round(a, 3), round(b, 3), 0.0
