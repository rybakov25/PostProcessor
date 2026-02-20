# -*- coding: ascii -*-
# ============================================================================
# COOLNT MACRO - Coolant Control
# ============================================================================
# IMSpost: coolnt.def
#   (FLOOD/MIST/OFF)
# ============================================================================

def execute(context, command):
    """
    Process COOLNT coolant control command
    
    IMSpost logic:
    - CASE CLDATAM:
      - 'FLOOD'/'ON' -> OUTPUT(MODE.COOLNT.FLOOD)
      - 'MIST' -> OUTPUT(MODE.COOLNT.MIST)
      - 'OFF' -> OUTPUT(MODE.COOLNT.OFF)
    - GLOBAL.COOLANT_DEF = state
    
    APT Examples:
      COOLNT/ON
      COOLNT/FLOOD
      COOLNT/MIST
      COOLNT/OFF
    """
    
    coolant_state = context.globalVars.COOLANT_DEF
    
    # ===  minor words ===
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            
            if word_upper in ['ON', 'FLOOD']:
                coolant_state = 'FLOOD'
                context.globalVars.COOLANT_DEF = 'FLOOD'
                
            elif word_upper == 'MIST':
                coolant_state = 'MIST'
                context.globalVars.COOLANT_DEF = 'MIST'
                
            elif word_upper == 'OFF':
                coolant_state = 'OFF'
                context.globalVars.COOLANT_DEF = 'OFF'
    
    # ===    ===
    
    if coolant_state == 'FLOOD':
        context.write("M8")
        
    elif coolant_state == 'MIST':
        context.write("M7")
        
    else:  # OFF
        context.write("M9")
