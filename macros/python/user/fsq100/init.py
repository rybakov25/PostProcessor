# -*- coding: ascii -*-
"""
FSQ-100 INIT MACRO - Initialization for TOS KURIM FSQ100 with Siemens 840D

Initializes FSQ-100 specific variables:
- Block numbering: N1, N3, N5... (increment by 2)
- Number format: Remove trailing zeros
"""


def execute(context, command):
    """
    Initialize FSQ-100 specific variables

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Cycle globals
    context.globalVars.LASTCYCLE = None
    context.globalVars.CYCLE_LAST_PLANE = 0.0
    context.globalVars.CYCLE_LAST_DEPTH = 0.0
    context.globalVars.CYCLE_FEED_MODE = "FPM"
    context.globalVars.CYCLE_FEED_VAL = 100.0
    context.globalVars.FCYCLE = 1

    # Tool globals
    context.globalVars.TOOLCNT = 0
    context.globalVars.TOOL = 0
    context.globalVars.FTOOL = -1

    # Feedrate globals
    context.globalVars.FEEDMODE = "FPM"
    context.globalVars.FEED_PROG = 100.0
    context.globalVars.FEED_MODAL = 1

    # Spindle globals
    context.globalVars.SPINDLE_DEF = 'CLW'
    context.globalVars.SPINDLE_RPM = 100.0
    context.globalVars.SPINDLE_BLOCK = 1

    # Tool change globals
    context.globalVars.TOOLCHG_TREG = "T"
    context.globalVars.TOOLCHG_LREG = "D"
    context.globalVars.TOOLCHG_BLOCK = 0
    context.globalVars.TOOLCHG_TIME = 0.0
    context.globalVars.TOOLCHG_IGNORE_SAME = 1

    # Motion globals
    context.system.MOTION = "LINEAR"
    context.globalVars.LINEAR_TYPE = "LINEAR"
    context.globalVars.RAPID_TYPE = "RAPID_BREAK"
    context.globalVars.SURFACE = 1
    context.system.SURFACE = 1

    # Coolant globals
    context.globalVars.COOLANT_DEF = 'FLOOD'
    context.globalVars.COOLANT_BLOCK = 0

    # Cutcom globals
    context.globalVars.CUTCOM_BLOCK = 1
    context.globalVars.CUTCOM_OFF_CHECK = 0
    context.globalVars.CUTCOM_REG = "D"

    # Circle globals
    context.globalVars.CIRCLE_TYPE = 4
    context.globalVars.CIRCLE_90 = 0

    # Seqno globals - DISABLED until after header
    context.globalVars.SEQNO_ON = 0  # Disabled initially
    context.globalVars.SEQNO_INCREMENT = 2  # FSQ-100 uses increment of 2

    # Comment globals
    context.globalVars.COMMENT_ONOFF = 1
    context.globalVars.COMMENT_PREFIX = ";"

    # Setup SYSTEM variables for Siemens
    context.system.SPINDLE_NAME = "S"
    context.system.FEEDRATE_NAME = "F"
    context.system.CIRCTYPE = 0  # Siemens style circles

    # Initialize context state
    context.currentFeed = None
    context.currentMotionType = "LINEAR"

    # Setup block numbering - DISABLED until after header
    # Block numbering will be enabled after header is output
    context.BlockWriter.BlockNumberingEnabled = False
    context.globalVars.BLOCK_NUMBER = 0
    context.globalVars.BLOCK_INCREMENT = 2  # FSQ-100: N1, N3, N5...

    # Store tool name for later use
    context.globalVars.TOOL_NAME = ""
