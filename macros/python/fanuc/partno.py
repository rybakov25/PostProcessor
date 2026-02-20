# -*- coding: ascii -*-
# ============================================================================
# FANUC PARTNO MACRO - Program Start for Fanuc controllers
# ============================================================================

def execute(context, command):
    """
    Process PARTNO command and output program header

    Fanuc format:
    O#### (Program number)
    (Comment lines)

    APT Example:
      PARTNO/12345
    """

    # Get program number/name from command
    program_name = "MAIN"
    if command.numeric and len(command.numeric) > 0:
        program_name = str(int(command.numeric[0]))
    elif command.majorWord and len(command.majorWord) > 0:
        # Extract any text after PARTNO/
        parts = command.majorWord.split('/')
        if len(parts) > 1:
            program_name = parts[1].strip()

    # Output Fanuc-style program header
    context.write("O" + program_name.zfill(4))
    context.write("(PROGRAM: " + program_name + ")")
    context.write("(DATE: " + get_date() + ")")
    context.write("(POST: UNIVERSAL POSTPROCESSOR)")
    context.write("")

    # Output safety line from init
    safety_line = context.globalVars.Get("FANUC_SAFETY_LINE", "G17 G20 G40 G49 G80")
    context.write(safety_line)
    context.write("G54 G90 G94")  # Work coord, absolute, feed per min
    context.write("G43 H1 Z50.")  # Tool length comp, safe Z

def get_date():
    """Get current date string"""
    import datetime
    return datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
