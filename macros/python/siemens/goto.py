# -*- coding: ascii -*-
"""
SIEMENS GOTO MACRO - Linear Motion for Siemens 840D

Handles GOTO commands with support for 3-axis and 5-axis motion.
A and B axes are modal - only output when changed.
"""

import math


def execute(context, command):
    """
    Process GOTO linear motion command
    
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

    # Get rotary axes (I, J, K direction vectors)
    i = command.numeric[3] if len(command.numeric) > 3 else None
    j = command.numeric[4] if len(command.numeric) > 4 else None
    k = command.numeric[5] if len(command.numeric) > 5 else None

    # Update linear registers (always)
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    # Determine motion type
    motion_type = context.system.MOTION
    is_rapid = (motion_type == 'RAPID' or 
                motion_type == 'RAPID_BREAK' or 
                context.currentMotionType == 'RAPID')

    # Build output parts list
    if is_rapid:
        # Rapid move G0
        parts = ["G0"]
        
        # Reset motion type after rapid
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'
    else:
        # Linear move G1
        parts = ["G1"]

    # Add linear coordinates (always output with G-code)
    parts.append(f"X{x:.3f}")
    parts.append(f"Y{y:.3f}")
    parts.append(f"Z{z:.3f}")

    # Add rotary axes ONLY if present in command (5-axis)
    # A/B are modal via globalVars comparison
    if i is not None and j is not None and k is not None:
        a, b, c = ijk_to_abc(i, j, k)
        
        # Get previous values (modal check)
        prev_a = context.globalVars.GetDouble("PREV_A", -999.0)
        prev_b = context.globalVars.GetDouble("PREV_B", -999.0)
        
        # Only add if changed (modal check)
        if abs(a - prev_a) > 0.001:
            parts.append(f"A{a:.3f}")
            context.globalVars.SetDouble("PREV_A", a)
        
        if abs(b - prev_b) > 0.001:
            parts.append(f"B{b:.3f}")
            context.globalVars.SetDouble("PREV_B", b)

    # Output complete block
    context.write(" ".join(parts))


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
