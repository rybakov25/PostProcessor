# MMILL INIT MACRO

def execute(context, command):
    # Initialize block numbering in globalVars
    context.globalVars.SetInt("BLOCK_NUMBER", 1)
    context.globalVars.SetInt("BLOCK_INCREMENT", 2)

    # Initialize feed modality
    context.globalVars.SetDouble("LAST_FEED", 0.0)

    # Header from controller config (already written by Program.cs)
    # Just output initial blocks
    
    # Initial blocks
    context.write("G54 G40 G90 G94 CUT2DF G17")
    context.write("TRANS")
    context.write("RTCPOF")

    # CYCLE800
    cycle = context.machine.config.fiveAxis.cycle800
    params = cycle.parameters
    context.write('CYCLE800({},"{}",{},{},{},{},{},{},{},{},{},{},{},{},{},{})'.format(
        params['mode'], params['table'], params['rotation'], params['plane'],
        params.get('x', 0), params.get('y', 0), params.get('z', 0),
        params.get('a', 0), params.get('b', 0), params.get('c', 0),
        params.get('dx', 0), params.get('dy', 0), params.get('dz', 0),
        params['direction'], params['feed'], params['maxFeed']
    ))

    context.write("G64 SOFT FFWON")
    context.write(context.machine.config.head.clampCommand + "; TCB6 HEAD")
