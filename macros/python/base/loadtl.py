# LOADTL MACRO - Tool Change

def execute(context, command):
    """
    Process LOADTL tool change command
    """
    # Check if same tool
    if context.globalVars.TOOLCHG_IGNORE_SAME:
        new_tool = int(command.numeric[0]) if command.numeric and len(command.numeric) > 0 else 0
        if context.globalVars.TOOL == new_tool:
            return
    
    # Get tool number
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.TOOL = int(command.numeric[0])
    
    # Get spindle speed
    spindle_speed = 1600
    if command.numeric and len(command.numeric) > 1:
        spindle_speed = command.numeric[1]
    
    context.registers.s = spindle_speed
    
    # Output tool change (using global block numbering)
    context.write("T" + str(context.globalVars.TOOL))
    context.write("D1")
    context.write("M6")
    
    # Set flags
    context.globalVars.TOOLCHNG = 1
    context.globalVars.FTOOL = context.globalVars.TOOL
