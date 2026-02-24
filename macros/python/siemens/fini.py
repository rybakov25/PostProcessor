# -*- coding: ascii -*-
"""
SIEMENS FINI MACRO - Program End for Siemens 840D

Outputs program end commands for Siemens controllers.
"""


def execute(context, command):
    """
    Process FINI command for Siemens
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Stop spindle
    context.write("M5")
    
    # Coolant off
    context.write("M9")
    
    # RTCP off if active
    if context.system.Get("COORD_RTCP", 0) == 1:
        context.write("RTCPOF")
    
    # Return to safe position
    context.write("G53 Z0")
    
    # Program end
    context.write("M30")
    
    # Final comment
    context.comment("End of program")
