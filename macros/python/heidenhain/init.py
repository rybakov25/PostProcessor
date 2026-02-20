# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN INIT MACRO - Initialization for Heidenhain TNC 640/620
# ============================================================================

def execute(context, command):
    """
    Initialize all GLOBAL and SYSTEM variables for Heidenhain controllers

    Heidenhain-specific settings:
    - Metric/inch mode (MM/INCH)
    - FK (free contour programming)
    - M118 for TCPM
    - Tool table management
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
    context.globalVars.TOOLCHG_LREG = "L"  # Heidenhain uses L for length
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
    context.globalVars.SEQNO_ON = 0  # Heidenhain doesn't use block numbers
    context.globalVars.SEQNO_INCREMENT = 0

    # === Comment globals ===
    context.globalVars.COMMENT_ONOFF = 1
    context.globalVars.COMMENT_PREFIX = ";"

    # === Setup SYSTEM variables ===
    context.system.SPINDLE_NAME = "S"
    context.system.FEEDRATE_NAME = "F"
    context.system.CIRCTYPE = 2  # Heidenhain style

    # === Initialize context state ===
    context.currentFeed = None
    context.currentMotionType = "LINEAR"

    # === Setup block numbering (disabled for Heidenhain) ===
    context.setBlockNumbering(start=0, increment=0, enabled=False)

    # === Heidenhain-specific settings ===
    context.globalVars.HEIDENHAIN_MODE = "MM"  # Metric mode
    context.globalVars.HEIDENHAIN_PLANE = 0  # Working plane
