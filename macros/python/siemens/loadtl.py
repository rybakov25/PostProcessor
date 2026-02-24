# -*- coding: ascii -*-
"""
SIEMENS LOADTL MACRO - Tool Change for Siemens 840D

Handles tool changes with T, D, and M6 codes.
"""


def execute(context, command):
    """
    Process LOADTL tool change command
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Check if same tool (ignore if enabled)
    if context.globalVars.Get("TOOLCHG_IGNORE_SAME", 1):
        new_tool = int(command.numeric[0]) if command.numeric and len(command.numeric) > 0 else 0
        if context.globalVars.Get("TOOL", 0) == new_tool:
            return
    
    # Get tool number
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.TOOL = int(command.numeric[0])
    
    # Get spindle speed if provided
    spindle_speed = 1600
    if command.numeric and len(command.numeric) > 1:
        spindle_speed = command.numeric[1]
    
    context.registers.s = spindle_speed
    
    # Output tool change (Siemens format)
    # T code - select tool
    context.write(f"T{context.globalVars.TOOL}")
    
    # D code - tool offset
    context.write("D1")
    
    # M6 - tool change
    context.write("M6")
    
    # Output spindle speed with modal checking
    if spindle_speed > 0:
        context.show("S")
        context.writeBlock()
    
    # Set flags
    context.globalVars.TOOLCHNG = 1
    context.globalVars.FTOOL = context.globalVars.TOOL
