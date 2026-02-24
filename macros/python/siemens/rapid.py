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
