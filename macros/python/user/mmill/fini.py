# -*- coding: ascii -*-
# ============================================================================
# MMILL FINI MACRO - End of Program
# ============================================================================
#    MMILL
#     RTCPOF
# ============================================================================

def execute(context, command):
    """
    Process FINI for MMILL
    
    Output:
    - G0 Z100. (retract)
    - M5 (spindle off)
    - M9 (coolant off)
    - RTCPOF
    - M30
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
