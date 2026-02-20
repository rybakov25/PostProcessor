# -*- coding: ascii -*-
# FEDRAT MACRO - Feed Rate (MODAL)

def execute(context, command):
    """
    Process FEDRAT feed rate command
    
    Logic:
    - Feed is MODAL - only output when CHANGED
    - Store current feed in globalVars.LAST_FEED
    """
    
    if not command.numeric or len(command.numeric) == 0:
        return
    
    feed = command.numeric[0]
    
    # Update register
    context.registers.f = feed
    
    # MODAL check - only output if feed CHANGED
    last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
    if last_feed == feed:
        return  # Same feed, don't output
    
    # Feed changed - output and remember
    context.globalVars.SetDouble("LAST_FEED", feed)
    context.write("F" + str(round(feed, 1)))
