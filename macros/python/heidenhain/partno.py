# -*- coding: ascii -*-
# ============================================================================
# HEIDENHAIN PARTNO MACRO - Program Start for Heidenhain controllers
# ============================================================================

def execute(context, command):
    """
    Process PARTNO command and output program header

    Heidenhain format:
    BEGIN PGM {name} MM
    (Comment lines)

    APT Example:
      PARTNO/12345
    """

    # Get program name
    program_name = "MAIN"
    if command.numeric and len(command.numeric) > 0:
        program_name = str(int(command.numeric[0]))
    elif command.majorWord and len(command.majorWord) > 0:
        parts = command.majorWord.split('/')
        if len(parts) > 1:
            program_name = parts[1].strip()

    # Output Heidenhain-style program header
    mode = context.globalVars.Get("HEIDENHAIN_MODE", "MM")
    context.write("BEGIN PGM " + program_name + " " + mode)
    context.write("; PROGRAM: " + program_name)
    context.write("; DATE: " + get_date())
    context.write("; POST: UNIVERSAL POSTPROCESSOR")
    context.write("")

    # Safety line
    context.write("TOOL DEF 99 L+0 R+0")  # Tool definition
    context.write("TOOL CALL 99 Z S" + str(int(context.globalVars.SPINDLE_RPM)))
    context.write("")

    # Set working plane and datum
    context.write("L Z+100 R0 FMAX")  # Safe Z
    context.write("M3")  # Spindle on

def get_date():
    """Get current date string"""
    import datetime
    return datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
