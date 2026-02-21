# -*- coding: ascii -*-
"""
SIEMENS PARTNO MACRO - Program Number for Siemens 840D

Outputs program number and name in Siemens format.
Enables block numbering after header output.
"""


def execute(context, command):
    """
    Process PARTNO command for Siemens
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Enable block numbering after header
    context.globalVars.SEQNO_ON = 1
    context.BlockWriter.BlockNumberingEnabled = True
    context.BlockWriter.BlockNumberStart = 10
    
    # Get program name
    program_name = ""
    if command.minorWords:
        program_name = command.minorWords[0]
    
    # Get program number if provided
    program_number = 0
    if command.numeric and len(command.numeric) > 0:
        program_number = int(command.numeric[0])
    
    # Output in Siemens format
    if program_number > 0:
        context.write(f"{program_number}")
    
    if program_name:
        context.comment(f"Program: {program_name}")
    
    # Store for later use
    context.globalVars.PARTNO_NAME = program_name
    context.globalVars.PARTNO_NUMBER = program_number
