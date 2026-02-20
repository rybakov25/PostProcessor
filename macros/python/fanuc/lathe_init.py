# -*- coding: ascii -*-
# ============================================================================
# FANUC LATHE INIT MACRO - Initialization for Fanuc Lathe
# ============================================================================

def execute(context, command):
    """
    Initialize all GLOBAL and SYSTEM variables for Fanuc Lathe controllers

    Lathe-specific settings:
    - G28 U0 W0 for reference point return
    - G50 for coordinate system preset
    - T#### for tool turret
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
    context.globalVars.CURRENT_OFFSET = 0

    # === Feedrate globals ===
    context.globalVars.FEEDMODE = "FPM"
    context.globalVars.FEED_PROG = 100.0
    context.globalVars.FEED_MODAL = 1
    context.globalVars.LAST_FEED = 0.0

    # === Spindle globals ===
    context.globalVars.SPINDLE_DEF = 'CLW'
    context.globalVars.SPINDLE_RPM = 100.0
    context.globalVars.SPINDLE_BLOCK = 1

    # === Chuck/Tailstock globals ===
    context.globalVars.CHUCK_STATE = 'CLAMP'
    context.globalVars.TAILSTOCK_STATE = 'BACK'

    # === Motion globals ===
    context.system.MOTION = "LINEAR"
    context.globalVars.LINEAR_TYPE = "LINEAR"
    context.globalVars.RAPID_TYPE = "RAPID_BREAK"
    context.globalVars.SURFACE = 1
    context.system.SURFACE = 1

    # === Seqno globals ===
    context.globalVars.SEQNO_ON = 0
    context.globalVars.SEQNO_INCREMENT = 0
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
    context.setBlockNumbering(start=1, increment=0, enabled=False)

    # === Lathe-specific safety line ===
    context.globalVars.FANUC_LATHE_SAFETY = "G21 G40 G80"
