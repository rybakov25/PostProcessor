# -*- coding: ascii -*-
"""
FSQ-100 FINI MACRO - Program End for TOS KURIM FSQ100 with Siemens 840D

Outputs FSQ-100 specific program end commands:
- M5 - Spindle stop
- M9 - Coolant off
- M30 - Program end
"""


def execute(context, command):
    """
    Process FINI command for FSQ-100

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Stop spindle
    context.write("M5")

    # Coolant off
    context.write("M9")

    # FFWOF - Feed forward off (FSQ-100 specific)
    context.write("FFWOF")

    # Optional stop
    context.write("M0")

    # Program end (M30 for Siemens)
    # Note: Etalon shows M2 at very end, but M30 is standard for Siemens 840D
    context.write("M30")

    # Final comment
    context.comment("End of program")
