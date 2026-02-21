# -*- coding: ascii -*-
"""
SIEMENS FEDRAT MACRO - Feed Rate Control for Siemens 840D

Handles feed rate settings with modal output.
"""


def execute(context, command):
    """
    Process FEDRAT feed rate command
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    if not command.numeric or len(command.numeric) == 0:
        return
    
    feed = command.numeric[0]
    
    # Update register
    context.registers.f = feed
    
    # Force output of F register
    context.show("F")
    
    # Write block with modal checking
    context.writeBlock()
