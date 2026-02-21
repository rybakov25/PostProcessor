# -*- coding: ascii -*-
"""
SIEMENS RAPID MACRO - Rapid Positioning for Siemens 840D

Sets rapid motion mode for subsequent movements.
"""


def execute(context, command):
    """
    Process RAPID positioning command
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Set motion type to RAPID for next GOTO
    context.system.MOTION = 'RAPID'
    context.currentMotionType = 'RAPID'
    
    # If coordinates in command, output G0 immediately
    if command.numeric and len(command.numeric) > 0:
        x = command.numeric[0] if len(command.numeric) > 0 else context.registers.x
        y = command.numeric[1] if len(command.numeric) > 1 else context.registers.y
        z = command.numeric[2] if len(command.numeric) > 2 else context.registers.z
        
        # Update registers
        context.registers.x = x
        context.registers.y = y
        context.registers.z = z
        
        # Write G0 with modal checking
        context.write("G0")
        context.writeBlock()
