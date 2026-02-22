# -*- coding: ascii -*-
"""
SIEMENS SEQNO MACRO - Block Numbering Control for Siemens 840D

Handles sequence number (block numbering) control commands.
Integrates with BlockWriter for N-prefix output.

Examples:
    SEQNO/ON            - Enable block numbering
    SEQNO/OFF           - Disable block numbering
    SEQNO/START,100     - Set starting sequence number to 100
    SEQNO/INCR,5        - Set increment to 5
"""


def execute(context, command):
    """
    Process SEQNO block numbering control command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Check for minor words (ON, OFF, START, INCR)
    if not command.minorWords:
        return

    for word in command.minorWords:
        word_upper = word.upper()

        if word_upper == 'ON':
            # Enable block numbering
            context.globalVars.Set("BLOCK_NUMBERING_ENABLED", 1)
            # Also set the internal flag for BlockWriter
            context.system.SEQNO = 1

        elif word_upper == 'OFF':
            # Disable block numbering
            context.globalVars.Set("BLOCK_NUMBERING_ENABLED", 0)
            context.system.SEQNO = 0

        elif word_upper == 'START':
            # Set starting sequence number
            if command.numeric and len(command.numeric) > 0:
                start_num = int(command.numeric[0])
                context.globalVars.SetInt("BLOCK_NUMBER", start_num)
                context.globalVars.Set("BLOCK_NUMBERING_ENABLED", 1)
                context.system.SEQNO = 1

        elif word_upper == 'INCR':
            # Set increment value
            if command.numeric and len(command.numeric) > 0:
                incr_value = int(command.numeric[0])
                context.globalVars.SetInt("BLOCK_INCREMENT", incr_value)

    # Output current state for debugging (optional)
    # context.write(f"(SEQNO: ON={context.globalVars.Get('BLOCK_NUMBERING_ENABLED', 0)})")
