# -*- coding: ascii -*-
# ============================================================================
# FINI MACRO - End of Program
# ============================================================================
# IMSpost: fini_cfg.def
#     
# ============================================================================

def execute(context, command):
    """
    Process FINI end of program command
    
    IMSpost logic:
    - Output footer from config
    - Retract axes
    - Spindle off, coolant off
    - M30
    
    APT Example:
      FINI
    """
    
    #   Z
    context.write("G0 Z100.")
    
    #  
    context.write("M5")
    
    #  
    context.write("M9")
    
    #  RTCP
    context.write("RTCPOF")
    
    #  
    context.write("M30")
