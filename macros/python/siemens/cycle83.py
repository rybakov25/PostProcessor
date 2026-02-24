# -*- coding: ascii -*-
"""
SIEMENS CYCLE83 MACRO - Deep Hole Drilling for Siemens 840D

Handles CYCLE83 deep hole drilling with chip breaking/pecking.
Supports modal parameter caching for efficient output.

Examples:
    CYCLE83/RTP,RFP,SDIS,DP,DPR,FDEP,FDPR,DAM,DTB,DTS,FRF,AXN,OLDP,AXS
    CYCLE83/10,0,2,-50,0,0,0,5,0.5,0,0.5,1,0,0

Parameters:
    RTP   - Retract plane (absolute)
    RFP   - Reference plane (absolute)
    SDIS  - Safety distance (incremental)
    DP    - Final drilling depth (absolute)
    DPR   - Depth relative to reference plane (incremental)
    FDEP  - First drilling depth (absolute)
    FDPR  - First drilling depth relative to reference (incremental)
    DAM   - Degression amount (chip breaking)
    DTB   - Dwell time at bottom (seconds)
    DTS   - Dwell time at start (seconds)
    FRF   - Feed rate factor (0.001-1.0)
    AXN   - Axis selection (1=X, 2=Y, 3=Z)
    OLDP  - Chip breaking distance
    AXS   - Axis direction (0=positive, 1=negative)
"""


def execute(context, command):
    """
    Process CYCLE83 deep hole drilling cycle command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Check for valid input
    if not command.numeric or len(command.numeric) == 0:
        return

    # Get cycle parameters with defaults
    # CYCLE83(RTP, RFP, SDIS, DP, DPR, FDEP, FDPR, DAM, DTB, DTS, FRF, AXN, OLDP, AXS)
    rtp = command.numeric[0] if len(command.numeric) > 0 else 0.0
    rfp = command.numeric[1] if len(command.numeric) > 1 else 0.0
    sdis = command.numeric[2] if len(command.numeric) > 2 else 2.0
    dp = command.numeric[3] if len(command.numeric) > 3 else 0.0
    dpr = command.numeric[4] if len(command.numeric) > 4 else 0.0
    fdep = command.numeric[5] if len(command.numeric) > 5 else 0.0
    fdpr = command.numeric[6] if len(command.numeric) > 6 else 0.0
    dam = command.numeric[7] if len(command.numeric) > 7 else 0.0
    dtb = command.numeric[8] if len(command.numeric) > 8 else 0.0
    dts = command.numeric[9] if len(command.numeric) > 9 else 0.0
    frf = command.numeric[10] if len(command.numeric) > 10 else 1.0
    axn = command.numeric[11] if len(command.numeric) > 11 else 3
    oldp = command.numeric[12] if len(command.numeric) > 12 else 0.0
    axs = command.numeric[13] if len(command.numeric) > 13 else 0

    # Check for modal caching
    use_cache = context.globalVars.Get("CYCLE_CACHE_ENABLED", 1)

    # Get cached parameters
    cached_params = {
        'RTP': context.globalVars.GetDouble("CYCLE83_RTP", -999.0),
        'RFP': context.globalVars.GetDouble("CYCLE83_RFP", -999.0),
        'SDIS': context.globalVars.GetDouble("CYCLE83_SDIS", -999.0),
        'DP': context.globalVars.GetDouble("CYCLE83_DP", -999.0),
        'DPR': context.globalVars.GetDouble("CYCLE83_DPR", -999.0),
        'FDEP': context.globalVars.GetDouble("CYCLE83_FDEP", -999.0),
        'FDPR': context.globalVars.GetDouble("CYCLE83_FDPR", -999.0),
        'DAM': context.globalVars.GetDouble("CYCLE83_DAM", -999.0),
        'DTB': context.globalVars.GetDouble("CYCLE83_DTB", -999.0),
        'DTS': context.globalVars.GetDouble("CYCLE83_DTS", -999.0),
        'FRF': context.globalVars.GetDouble("CYCLE83_FRF", -999.0),
        'AXN': context.globalVars.GetInt("CYCLE83_AXN", -1),
        'OLDP': context.globalVars.GetDouble("CYCLE83_OLDP", -999.0),
        'AXS': context.globalVars.GetInt("CYCLE83_AXS", -1),
    }

    # Current parameters
    current_params = {
        'RTP': rtp, 'RFP': rfp, 'SDIS': sdis, 'DP': dp, 'DPR': dpr,
        'FDEP': fdep, 'FDPR': fdpr, 'DAM': dam, 'DTB': dtb, 'DTS': dts,
        'FRF': frf, 'AXN': int(axn), 'OLDP': oldp, 'AXS': int(axs)
    }

    # Check if parameters changed
    params_changed = False
    for key in cached_params:
        if key in ['AXN', 'AXS']:
            if current_params[key] != cached_params[key]:
                params_changed = True
                break
        else:
            if abs(current_params[key] - cached_params[key]) > 0.001:
                params_changed = True
                break

    # If caching enabled and no change, skip full output
    if use_cache and not params_changed and cached_params['RTP'] != -999.0:
        cycle_active = context.globalVars.Get("CYCLE83_ACTIVE", 0)
        if cycle_active:
            return  # Already active with same parameters

    # Build CYCLE83 call
    cycle_parts = []
    cycle_call_needed = params_changed or not use_cache

    if cycle_call_needed:
        # Full cycle definition
        cycle_str = (
            f"CYCLE83({rtp:.1f},{rfp:.1f},{sdis:.1f},{dp:.1f},{dpr:.1f},"
            f"{fdep:.1f},{fdpr:.1f},{dam:.3f},{dtb:.2f},{dts:.2f},"
            f"{frf:.3f},{axn},{oldp:.3f},{axs})"
        )
        cycle_parts.append(cycle_str)

        # Cache all parameters
        for key, value in current_params.items():
            if key in ['AXN', 'AXS']:
                context.globalVars.SetInt(f"CYCLE83_{key}", value)
            else:
                context.globalVars.SetDouble(f"CYCLE83_{key}", value)

        context.globalVars.Set("CYCLE83_ACTIVE", 1)

    # Output cycle call
    if cycle_parts:
        context.write(" ".join(cycle_parts))

    # Store current cycle state
    context.globalVars.Set("ACTIVE_CYCLE", "CYCLE83")
