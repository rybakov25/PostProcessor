# -*- coding: ascii -*-
"""
FSQ-100 TOOL_LIST MACRO - Tool Information for TOS KURIM FSQ100

Parses TOOLINF APT command and stores tool name.
Example: TOOLINF/D20R0.8L70 -> TOOL_NAME = "D20R0.8L70"
"""


def execute(context, command):
    """
    Process TOOLINF command to extract tool name

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Extract tool name from first minor word
    # Format: TOOLINF/D20R0.8L70
    if command.minorWords and len(command.minorWords) > 0:
        tool_name = command.minorWords[0].upper()
        context.globalVars.Set("TOOL_NAME", tool_name)
        
        # Output tool name comment for reference
        context.comment(f"TOOL NAME: {tool_name}")
