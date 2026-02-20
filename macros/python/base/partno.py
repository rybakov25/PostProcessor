# -*- coding: ascii -*-
# ============================================================================
# PARTNO MACRO - Part Number / Program Start
# ============================================================================
# IMSpost: partno.def
#    PARTNO/
#     init
# ============================================================================

def execute(context, command):
    """
    Process PARTNO command - start of program
    
    APT Example:
      PARTNO/PMO_22_201_01_R1_U1_01
    """
    #   
    part_number = ""
    if command.numeric and len(command.numeric) > 0:
        part_number = str(command.numeric[0])
    elif command.strings and len(command.strings) > 0:
        part_number = command.strings[0]
    
    #   
    context.globalVars.PARTNO = part_number
    
    #   
    # (    init.py)
    
    #   
    if part_number:
        context.comment("Part No: " + part_number)
