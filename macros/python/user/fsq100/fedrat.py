# -*- coding: ascii -*-
"""
FSQ-100 FEDRAT MACRO - Feed Rate (MODAL) for TOS KURIM FSQ100

Feed is MODAL - only output when CHANGED.
Uses BlockWriter for automatic modal checking.
"""


def execute(context, command):
    """
    Process FEDRAT feed rate command for FSQ-100

    Args:
        context: Postprocessor context
        command: APT command
    """
    if not command.numeric or len(command.numeric) == 0:
        return

    feed = command.numeric[0]

    # Update register (this sets HasChanged flag automatically)
    context.registers.f = feed

    # Force output of F register (it may be modal but we want it now)
    context.show("F")

    # Write block with F register
    context.writeBlock()
