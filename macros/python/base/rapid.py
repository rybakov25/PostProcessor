# -*- coding: ascii -*-
# RAPID MACRO - Rapid Positioning

def execute(context, command):
    """
    Process RAPID positioning command
    
    Logic:
    - Set SYSTEM.MOTION = RAPID for next movement
    - If coordinates present, output G0 immediately
    """
    
    # Set motion type to RAPID for next GOTO
    context.system.MOTION = 'RAPID'
    context.currentMotionType = 'RAPID'
    
    # If coordinates in command, output G0 immediately
    if command.numeric and len(command.numeric) > 0:
        x = command.numeric[0] if len(command.numeric) > 0 else context.registers.x
        y = command.numeric[1] if len(command.numeric) > 1 else context.registers.y
        z = command.numeric[2] if len(command.numeric) > 2 else context.registers.z
        
        context.registers.x = x
        context.registers.y = y
        context.registers.z = z
        
        line = "G0 X" + format_num(x)
        if len(command.numeric) > 1:
            line += " Y" + format_num(y)
        if len(command.numeric) > 2:
            line += " Z" + format_num(z)
        
        context.write(line)

def format_num(value):
    """Format number without trailing zeros"""
    rounded = round(value, 3)
    formatted = str(rounded).rstrip('0').rstrip('.')
    if '.' not in formatted:
        formatted += '.'
    return formatted
