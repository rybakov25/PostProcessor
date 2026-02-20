# -*- coding: ascii -*-
# ============================================================================
# FANUC INIT MACRO - Initialization for Fanuc 31i/32i/35i
# ============================================================================

def execute(context, command):
    """
    Initialize all GLOBAL and SYSTEM variables for Fanuc controllers

    Fanuc-specific settings:
    - G20/G21 for units
    - G40/G49/G80 for safety cancellations
    - G54 work coordinate system
    """

    # === Cycle globals ===
    context.globalVars.LASTCYCLE = 'DRILL'
    context.globalVars.CYCLE_LAST_PLANE = 0.0
    context.globalVars.CYCLE_LAST_DEPTH = 0.0
    context.globalVars.CYCLE_FEED_MODE = "FPM"
    context.globalVars.CYCLE_FEED_VAL = 100.0
    context.globalVars.FCYCLE = 1

    # === Tool globals ===
    context.globalVars.TOOLCNT = 0
    context.globalVars.TOOL = 0
    context.globalVars.FTOOL = -1

    # === Feedrate globals ===
    context.globalVars.FEEDMODE = "FPM"
    context.globalVars.FEED_PROG = 100.0
    context.globalVars.FEED_MODAL = 1
    context.globalVars.LAST_FEED = 0.0

    # === Spindle globals ===
    context.globalVars.SPINDLE_DEF = 'CLW'
    context.globalVars.SPINDLE_RPM = 100.0
    context.globalVars.SPINDLE_BLOCK = 1

    # === Tool change globals ===
    context.globalVars.TOOLCHG_TREG = "T"
    context.globalVars.TOOLCHG_LREG = "H"  # Fanuc uses H for length comp
    context.globalVars.TOOLCHG_SPINOFF = 1
    context.globalVars.TOOLCHG_COOLOFF = 0
    context.globalVars.TOOLCHG_BLOCK = 0

    # === Motion globals ===
    context.system.MOTION = "LINEAR"
    context.globalVars.LINEAR_TYPE = "LINEAR"
    context.globalVars.RAPID_TYPE = "RAPID_BREAK"
    context.globalVars.SURFACE = 1
    context.system.SURFACE = 1

    # === Coolant globals ===
    context.globalVars.COOLANT_DEF = 'FLOOD'
    context.globalVars.COOLANT_BLOCK = 0

    # === Cutcom globals ===
    context.globalVars.CUTCOM_BLOCK = 1
    context.globalVars.CUTCOM_OFF_CHECK = 0

    # === Seqno globals ===
    context.globalVars.SEQNO_ON = 1
    context.globalVars.SEQNO_INCREMENT = 2
    context.globalVars.BLOCK_NUMBER = 1

    # === Comment globals ===
    context.globalVars.COMMENT_ONOFF = 1
    context.globalVars.COMMENT_PREFIX = "("

    # === Setup SYSTEM variables ===
    context.system.SPINDLE_NAME = "S"
    context.system.FEEDRATE_NAME = "F"
    context.system.CIRCTYPE = 1  # Fanuc style

    # === Initialize context state ===
    context.currentFeed = None
    context.currentMotionType = "LINEAR"

    # === Setup block numbering ===
    context.setBlockNumbering(start=1, increment=2, enabled=True)

    # === Fanuc-specific safety line ===
    # This will be output at the start of the program
    context.globalVars.FANUC_SAFETY_LINE = "G17 G20 G40 G49 G80"
