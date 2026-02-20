# -*- coding: ascii -*-
# ============================================================================
# INIT MACRO - Initialization
# ============================================================================
# IMSpost: init.def
#   GLOBAL  SYSTEM 
#    
# ============================================================================

def execute(context, command):
    """
    Initialize all GLOBAL and SYSTEM variables
    
    From init.def:
    - Setup cycle globals
    - Setup tool globals
    - Setup feedrate globals
    - Setup spindle globals
    - Setup motion globals
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
    
    # === Feedrate globals ===
    context.globalVars.FEEDMODE = "FPM"
    context.globalVars.FEED_PROG = 100.0
    context.globalVars.FEED_MODAL = 1
    context.globalVars.FEED_MM_MIN = 5.0
    context.globalVars.FEED_MM_MINIMUM = 0.0
    context.globalVars.FEED_MM_MAXIMUM = 1000.0
    
    # === Spindle globals ===
    context.globalVars.SPINDLE_DEF = 'CLW'
    context.globalVars.SPINDLE_RPM = 100.0
    context.globalVars.SPINDLE_BLOCK = 1
    
    # === Tool change globals ===
    context.globalVars.TOOLCHG_TREG = "T"
    context.globalVars.TOOLCHG_LREG = "D"
    context.globalVars.TOOLCHG_SPINOFF = 0
    context.globalVars.TOOLCHG_COOLOFF = 0
    context.globalVars.TOOLCHG_BLOCK = 0
    context.globalVars.TOOLCHG_TIME = 0.0
    context.globalVars.TOOLCHNG_FORCE_OUTREG = "X,Y"
    context.globalVars.FTOOL = -1
    context.globalVars.TOOL = 0
    
    # === Motion globals ===
    context.system.MOTION = "LINEAR"
    context.globalVars.LINEAR_TYPE = "LINEAR"
    context.globalVars.RAPID_TYPE = "RAPID_BREAK"
    context.globalVars.RAPID_RESTORE_FEED = 0
    context.globalVars.SURFACE = 1
    context.system.SURFACE = 1
    
    # === Coolant globals ===
    context.globalVars.COOLANT_DEF = 'FLOOD'
    context.globalVars.COOLANT_BLOCK = 0
    
    # === Cutcom globals ===
    context.globalVars.CUTCOM_BLOCK = 1
    context.globalVars.CUTCOM_OFF_CHECK = 0
    
    # === Circle globals ===
    context.globalVars.CIRCLE_TYPE = 4
    context.globalVars.CIRCLE_90 = 0
    
    # === Seqno globals ===
    context.globalVars.SEQNO_ON = 1
    context.globalVars.SEQNO_INCREMENT = 2
    
    # === Comment globals ===
    context.globalVars.COMMENT_ONOFF = 1
    context.globalVars.COMMENT_PREFIX = ";"
    
    # === Setup SYSTEM variables ===
    context.system.SPINDLE_NAME = "S"
    context.system.FEEDRATE_NAME = "F"
    context.system.CIRCTYPE = 0  # Siemens style
    
    # === Initialize context state ===
    context.currentFeed = None
    context.currentMotionType = "LINEAR"
    
    # === Setup block numbering ===
    context.setBlockNumbering(start=1, increment=2, enabled=True)
