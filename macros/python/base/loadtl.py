# -*- coding: ascii -*-
"""
LOADTL MACRO - Tool Change

Uses BlockWriter for automatic modal checking of S register.
"""


def execute(context, command):
    """
    Process LOADTL tool change command
    
    Args:
        context: Postprocessor context
        command: APT command object
    """
    # Check if same tool
    if context.globalVars.TOOLCHG_IGNORE_SAME:
        new_tool = int(command.numeric[0]) if command.numeric and len(command.numeric) > 0 else 0
        if context.globalVars.TOOL == new_tool:
            return

    # Get tool number
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.TOOL = int(command.numeric[0])

    # Get spindle speed
    spindle_speed = 1600
    if command.numeric and len(command.numeric) > 1:
        spindle_speed = command.numeric[1]

    context.registers.s = spindle_speed

    # Output tool change
    context.write("T" + str(context.globalVars.TOOL))
    context.write("D1")
    context.write("M6")
    
    # Output spindle speed with modal checking
    if spindle_speed > 0:
        context.show("S")
        context.writeBlock()

    # Set flags
    context.globalVars.TOOLCHNG = 1
    context.globalVars.FTOOL = context.globalVars.TOOL
