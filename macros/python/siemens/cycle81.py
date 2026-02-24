# -*- coding: ascii -*-
"""
SIEMENS CYCLE81 MACRO - Drilling Cycle for Siemens 840D

Handles CYCLE81 drilling/centering cycle.
Supports modal parameter caching for efficient output.

Examples:
    CYCLE81/RTP,RFP,SDIS,DP,DPR
    CYCLE81/10,0,2,-25,0    - Drill to Z-25 with 2mm safety

Parameters:
    RTP   - Retract plane (absolute)
    RFP   - Reference plane (absolute)
    SDIS  - Safety distance (incremental)
    DP    - Final drilling depth (absolute)
    DPR   - Depth relative to reference plane (incremental)
"""


def execute(context, command):
    """
    Process CYCLE81 drilling cycle command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Check for valid input
    if not command.numeric or len(command.numeric) == 0:
        return

    # Get cycle parameters with defaults
    # CYCLE81(RTP, RFP, SDIS, DP, DPR)
    rtp = command.numeric[0] if len(command.numeric) > 0 else 0.0
    rfp = command.numeric[1] if len(command.numeric) > 1 else 0.0
    sdis = command.numeric[2] if len(command.numeric) > 2 else 2.0
    dp = command.numeric[3] if len(command.numeric) > 3 else 0.0
    dpr = command.numeric[4] if len(command.numeric) > 4 else 0.0

    # Check for modal caching
    use_cache = context.globalVars.Get("CYCLE_CACHE_ENABLED", 1)

    # Get cached parameters
    cached_rtp = context.globalVars.GetDouble("CYCLE81_RTP", -999.0)
    cached_rfp = context.globalVars.GetDouble("CYCLE81_RFP", -999.0)
    cached_sdis = context.globalVars.GetDouble("CYCLE81_SDIS", -999.0)
    cached_dp = context.globalVars.GetDouble("CYCLE81_DP", -999.0)
    cached_dpr = context.globalVars.GetDouble("CYCLE81_DPR", -999.0)

    # Check if parameters changed (modal optimization)
    params_changed = (
        abs(rtp - cached_rtp) > 0.001 or
        abs(rfp - cached_rfp) > 0.001 or
        abs(sdis - cached_sdis) > 0.001 or
        abs(dp - cached_dp) > 0.001 or
        abs(dpr - cached_dpr) > 0.001
    )

    # If caching enabled and no change, skip full output
    if use_cache and not params_changed and cached_rtp != -999.0:
        # Output simplified call or skip if already active
        cycle_active = context.globalVars.Get("CYCLE81_ACTIVE", 0)
        if cycle_active:
            return  # Already active with same parameters

    # Build CYCLE81 call
    # Siemens format: CYCLE81(RTP, RFP, SDIS, DP, DPR)
    cycle_parts = []

    # Check if we need to output the full cycle or just position
    cycle_call_needed = params_changed or not use_cache

    if cycle_call_needed:
        # Full cycle definition
        cycle_str = f"CYCLE81({rtp:.1f},{rfp:.1f},{sdis:.1f},{dp:.1f},{dpr:.1f})"
        cycle_parts.append(cycle_str)

        # Cache parameters
        context.globalVars.SetDouble("CYCLE81_RTP", rtp)
        context.globalVars.SetDouble("CYCLE81_RFP", rfp)
        context.globalVars.SetDouble("CYCLE81_SDIS", sdis)
        context.globalVars.SetDouble("CYCLE81_DP", dp)
        context.globalVars.SetDouble("CYCLE81_DPR", dpr)
        context.globalVars.Set("CYCLE81_ACTIVE", 1)
    else:
        # Use cached parameters - output position only
        # The cycle is already active, just move to position
        pass

    # Output cycle call
    if cycle_parts:
        context.write(" ".join(cycle_parts))

    # Store current cycle state
    context.globalVars.Set("ACTIVE_CYCLE", "CYCLE81")
