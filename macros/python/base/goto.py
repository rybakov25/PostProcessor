# -*- coding: ascii -*-
# GOTO MACRO - Linear Motion (supports 3-axis and 5-axis)

import math

def execute(context, command):
    """
    Process GOTO linear motion command
    
    APT format: GOTO/X, Y, Z, I, J, K
    where I,J,K are tool direction vectors for 5-axis
    
    Logic:
    - If SYSTEM.MOTION = 'RAPID' -> output G0
    - Else -> output G1
    - For 5-axis: output A, B, C angles from I,J,K vectors
    - Feed is modal - only output when changed
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
        # Rapid move G0
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
            # C axis typically not used for 3+2 positioning
        
        context.write(line)
        
        # Reset motion type after rapid
        context.system.MOTION = 'LINEAR'
        context.currentMotionType = 'LINEAR'
    
    else:
        # Linear move G1
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
                context.write("F" + str(round(context.registers.f, 1)))
                context.globalVars.SetDouble("LAST_FEED", context.registers.f)

def ijk_to_abc(i, j, k):
    """
    Convert IJK direction vector to ABC angles (degrees)
    
    For Siemens 840D:
    - A = rotation around X axis
    - B = rotation around Y axis
    
    Simplified conversion for common cases:
    - I=0, J=0, K=1 -> A=0, B=0 (vertical)
    - I=1, J=0, K=0 -> A=90, B=0 (horizontal X)
    - I=0, J=1, K=0 -> A=0, B=90 (horizontal Y)
    """
    # Calculate angles using atan2
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
    """Format number without trailing zeros"""
    rounded = round(value, 3)
    formatted = str(rounded).rstrip('0').rstrip('.')
    if '.' not in formatted:
        formatted += '.'
    return formatted
