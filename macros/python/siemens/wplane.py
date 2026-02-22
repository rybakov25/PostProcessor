# -*- coding: ascii -*-
"""
SIEMENS WPLANE MACRO - Working Plane Control for Siemens 840D

Handles working plane selection and control.
Supports CYCLE800 for 5-axis plane definition.
Integrates with RTCP (TCPM) for tool center point control.

Examples:
    WPLANE/ON           - Enable working plane
    WPLANE/OFF          - Disable working plane
    WPLANE/XYPLAN       - Set XY plane (G17)
    WPLANE/YZPLAN       - Set YZ plane (G18)
    WPLANE/ZXPLAN       - Set ZX plane (G19)
"""


def execute(context, command):
    """
    Process WPLANE working plane command

    Args:
        context: Postprocessor context
        command: APT command
    """
    # Default values
    plane_enabled = context.globalVars.Get("WPLANE_ENABLED", 1)
    plane = context.globalVars.Get("WORK_PLANE", "XYPLAN")

    # Process minor words
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()

            if word_upper == 'ON':
                plane_enabled = 1
                context.globalVars.Set("WPLANE_ENABLED", 1)

            elif word_upper == 'OFF':
                plane_enabled = 0
                context.globalVars.Set("WPLANE_ENABLED", 0)

            elif word_upper == 'XYPLAN':
                plane = 'XYPLAN'
                context.globalVars.Set("WORK_PLANE", "XYPLAN")

            elif word_upper == 'YZPLAN':
                plane = 'YZPLAN'
                context.globalVars.Set("WORK_PLANE", "YZPLAN")

            elif word_upper == 'ZXPLAN':
                plane = 'ZXPLAN'
                context.globalVars.Set("WORK_PLANE", "ZXPLAN")

    # Check numeric values for plane selection
    if command.numeric and len(command.numeric) > 0:
        plane_code = int(command.numeric[0])
        if plane_code == 17:
            plane = 'XYPLAN'
            context.globalVars.Set("WORK_PLANE", "XYPLAN")
        elif plane_code == 18:
            plane = 'YZPLAN'
            context.globalVars.Set("WORK_PLANE", "YZPLAN")
        elif plane_code == 19:
            plane = 'ZXPLAN'
            context.globalVars.Set("WORK_PLANE", "ZXPLAN")

    # Get previous plane for modal check
    prev_plane = context.globalVars.Get("ACTIVE_PLANE", "XYPLAN")

    # Build output parts
    parts = []

    # Output plane selection G-code if changed
    if plane != prev_plane and plane_enabled:
        if plane == 'XYPLAN':
            parts.append("G17")
        elif plane == 'YZPLAN':
            parts.append("G18")
        elif plane == 'ZXPLAN':
            parts.append("G19")
        context.globalVars.Set("ACTIVE_PLANE", plane)

    # Check for CYCLE800 (5-axis plane definition)
    use_cycle800 = context.globalVars.Get("USE_CYCLE800", 0)
    if use_cycle800 and plane_enabled:
        # CYCLE800 parameters for 5-axis
        # CYCLE800(RTP, RFP, SDIS, DP, DPR, NUM, AX1, AX2, AX3, AX4, AX5, MA1, MA2, MA3, MA4, MA5, M2, M3, M4, M5)
        # Simplified version with common parameters
        rtp = context.globalVars.GetDouble("CYCLE800_RTP", 0.0)
        rfp = context.globalVars.GetDouble("CYCLE800_RFP", 0.0)
        sdis = context.globalVars.GetDouble("CYCLE800_SDIS", 2.0)

        # Get rotary angles if available
        ax1 = context.globalVars.GetDouble("WPLANE_A", 0.0)
        ax2 = context.globalVars.GetDouble("WPLANE_B", 0.0)
        ax3 = context.globalVars.GetDouble("WPLANE_C", 0.0)

        # Output CYCLE800 call
        cycle_params = f"CYCLE800({rtp:.1f},{rfp:.1f},{sdis:.1f},0,0,0,{ax1:.3f},{ax2:.3f},{ax3:.3f})"
        parts.append(cycle_params)

    # Check for RTCP/TCPM integration
    use_rtcp = context.globalVars.Get("RTCP_ENABLED", 0)
    if use_rtcp and plane_enabled:
        # TCPM (Tool Center Point Management) for Siemens
        # TCPM ON / TCPM OFF
        rtcp_state = context.globalVars.Get("RTCP_STATE", "OFF")
        if rtcp_state == "OFF":
            parts.append("TCPM ON")
            context.globalVars.Set("RTCP_STATE", "ON")

    # Output if we have parts
    if parts:
        context.write(" ".join(parts))
