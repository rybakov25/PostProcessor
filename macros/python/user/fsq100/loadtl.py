# -*- coding: ascii -*-
"""
FSQ-100 LOADTL MACRO - Tool Change for TOS KURIM FSQ100 with Siemens 840D

Handles tool changes with FSQ-100 specific format:
- T="toolname" (quoted name from TOOLINF)
- TC (tool check)
- D1 (tool offset)
- FFWON (feed forward on)
- SOFT (soft surface)

Uses BlockWriter for modal S register output.
"""


def execute(context, command):
    """
    Process LOADTL tool change command for FSQ-100

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

    # Update S register
    context.registers.s = spindle_speed

    # Get tool name from TOOLINF (stored in globalVars)
    tool_name = context.globalVars.Get("TOOL_NAME", "")

    # Output tool command (all parts on one line)
    context.write(f'T="{tool_name}" TC D1 FFWON SOFT')
    
    # Output spindle speed with modal checking via BlockWriter
    if spindle_speed > 0:
        context.show("S")
    
    # Write block with newline
    context.writeBlock()

    # Set flags
    context.globalVars.TOOLCHNG = 1
    context.globalVars.FTOOL = context.globalVars.TOOL
