# -*- coding: ascii -*-
"""
CIRCLE/ARC MACRO - Circular Interpolation G02/G03

Handles circular interpolation commands from APT with support for:
- IJK format (arc center offsets from start point)
- R format (arc radius with sign for angle >180°)
- Automatic format selection based on controller configuration
- Working planes: G17 (XY), G18 (XZ), G19 (YZ)
- Helical arcs with simultaneous Z axis movement
- Full circles and arcs >180° (automatically use IJK format)

APT Command Formats:
    CIRCLE/X, x, Y, y, Z, z, I, i, J, j, K, k    - IJK format
    CIRCLE/X, x, Y, y, Z, z, R, r                 - R format
    ARC/X, x, Y, y, Z, z, I, i, J, j, K, k        - Same as CIRCLE
    ARC/X, x, Y, y, Z, z, R, r                    - Same as CIRCLE

Direction Control:
    - G02 (CLW/CLOCKWISE): Default direction
    - G03 (CCLW/COUNTERCLOCKWISE): When CCLW minor word present

Output Examples:
    G2 X100.000 Y200.000 I10.000 J5.000          - IJK format, CW
    G3 X50.000 Y100.000 R25.000                   - R format, CCW
    G2 X100.000 Y200.000 Z50.000 I10.000 J0.000   - Helical arc
"""

import math
from typing import Optional, Tuple, Dict, Any


# ============================================================================
# Constants
# ============================================================================

# G-code plane selection
PLANE_G17 = 17  # XY plane
PLANE_G18 = 18  # XZ plane
PLANE_G19 = 19  # YZ plane

# Arc direction
DIRECTION_CW = 2   # G02 - Clockwise
DIRECTION_CCW = 3  # G03 - Counter-clockwise

# Arc format types
FORMAT_IJK = 0  # Center offset format
FORMAT_R = 1    # Radius format

# Default arc center offsets
DEFAULT_I = 0.0
DEFAULT_J = 0.0
DEFAULT_K = 0.0


# ============================================================================
# Main Execute Function
# ============================================================================

def execute(context, command):
    """
    Process CIRCLE/ARC circular interpolation command

    This function handles circular interpolation commands from APT,
    converting them to appropriate G02/G03 G-code with either IJK
    (center offset) or R (radius) format based on controller configuration.

    Args:
        context: Postprocessor context object providing:
            - registers: Modal register storage (x, y, z, i, j, k, f, etc.)
            - globalVars: Global variable storage
            - system: System variables (MOTION, SURFACE, etc.)
            - write(): Write text to output
            - writeBlock(): Write block with modal checking
            - show(): Force register output
            - comment(): Write comment
        command: APT command object providing:
            - numeric: List of numeric values [X, Y, Z, I, J, K, R]
            - minorWords: List of string keywords (CLW, CCLW, etc.)
            - name: Command name (CIRCLE, ARC)

    Raises:
        ValueError: If arc parameters are invalid or incomplete

    Example:
        APT: CIRCLE/X, 100, Y, 200, I, 10, J, 5
        Output: G2 X100.000 Y200.000 I10.000 J5.000
    """
    # Validate command has numeric parameters
    if not command.numeric or len(command.numeric) == 0:
        context.comment("ERROR: CIRCLE/ARC command requires coordinates")
        return

    # =========================================================================
    # Step 1: Parse command parameters
    # =========================================================================

    # Extract endpoint coordinates (X, Y, Z)
    # Format: [X, Y, Z, I, J, K] or [X, Y, Z, R]
    x_end = _get_numeric_value(command.numeric, 0, context.registers.x)
    y_end = _get_numeric_value(command.numeric, 1, context.registers.y)
    z_end = _get_numeric_value(command.numeric, 2, context.registers.z)

    # Extract arc parameters (IJK center offsets or R radius)
    # Check if R format is used (single value after Z)
    has_r_format = _has_radius_format(command)

    if has_r_format:
        # R format: [X, Y, Z, R]
        r_radius = _get_numeric_value(command.numeric, 3, None)
        i_center = DEFAULT_I
        j_center = DEFAULT_J
        k_center = DEFAULT_K
        arc_format = FORMAT_R
    else:
        # IJK format: [X, Y, Z, I, J, K]
        i_center = _get_numeric_value(command.numeric, 3, DEFAULT_I)
        j_center = _get_numeric_value(command.numeric, 4, DEFAULT_J)
        k_center = _get_numeric_value(command.numeric, 5, DEFAULT_K)
        r_radius = None
        arc_format = FORMAT_IJK

    # Determine arc direction (G02/G03) from minor words
    arc_direction = _get_arc_direction(command)

    # =========================================================================
    # Step 2: Validate arc parameters
    # =========================================================================

    validation_error = _validate_arc_parameters(
        x_end, y_end, z_end,
        i_center, j_center, k_center, r_radius,
        arc_format, context
    )

    if validation_error:
        context.comment(f"ERROR: {validation_error}")
        return

    # =========================================================================
    # Step 3: Determine output format (IJK vs R)
    # =========================================================================

    # Get controller configuration for arc format preference
    use_r_format = _should_use_radius_format(context, arc_format, r_radius)

    # Calculate arc angle to determine if >180° (requires IJK)
    arc_angle = _calculate_arc_angle(
        context.registers.x, context.registers.y, context.registers.z,
        x_end, y_end, z_end,
        i_center, j_center, k_center,
        r_radius if use_r_format else None
    )

    # Force IJK format for arcs >180° (R format ambiguous)
    if abs(arc_angle) > 180.0:
        use_r_format = False
        context.comment("Arc >180° - using IJK format")

    # =========================================================================
    # Step 4: Update context registers
    # =========================================================================

    # Update endpoint registers
    context.registers.x = x_end
    context.registers.y = y_end
    context.registers.z = z_end

    # Update arc center registers (for IJK format)
    context.registers.i = i_center
    context.registers.j = j_center
    context.registers.k = k_center

    # Store radius for potential R format output
    if r_radius is not None:
        context.globalVars.SetDouble("ARC_RADIUS", r_radius)

    # Store arc direction for reference
    context.globalVars.Set("ARC_DIRECTION", "CW" if arc_direction == DIRECTION_CW else "CCW")

    # =========================================================================
    # Step 5: Build and output G-code block
    # =========================================================================

    # Select G-code for arc direction
    g_code = "G2" if arc_direction == DIRECTION_CW else "G3"

    # Write G-code prefix
    context.write(f"{g_code} ")

    # Force output of arc-related registers
    context.show("X")
    context.show("Y")
    context.show("Z")

    if use_r_format and r_radius is not None:
        # R format output
        context.write(f"R{r_radius:.3f}")
        # Write endpoint coordinates
        context.writeBlock()
    else:
        # IJK format output
        context.show("I")
        context.show("J")
        context.show("K")
        # Write endpoint coordinates and center offsets
        context.writeBlock()

    # =========================================================================
    # Step 6: Update motion state
    # =========================================================================

    # Set motion type to circular for subsequent operations
    context.system.MOTION = "CIRCULAR"
    context.currentMotionType = "CIRCULAR"

    # Store last arc parameters for potential reuse
    context.globalVars.SetDouble("LAST_ARC_X", x_end)
    context.globalVars.SetDouble("LAST_ARC_Y", y_end)
    context.globalVars.SetDouble("LAST_ARC_Z", z_end)
    context.globalVars.SetDouble("LAST_ARC_I", i_center)
    context.globalVars.SetDouble("LAST_ARC_J", j_center)
    context.globalVars.SetDouble("LAST_ARC_K", k_center)


# ============================================================================
# Helper Functions - Parameter Parsing
# ============================================================================

def _get_numeric_value(numerics: list, index: int, default: float) -> float:
    """
    Safely get numeric value from command parameters

    Args:
        numerics: List of numeric values from command
        index: Index in the list
        default: Default value if index out of range

    Returns:
        Numeric value or default
    """
    if numerics and len(numerics) > index:
        return float(numerics[index])
    return default


def _has_radius_format(command) -> bool:
    """
    Detect if command uses R (radius) format instead of IJK

    R format is detected when:
    - Exactly 4 numeric values present (X, Y, Z, R)
    - Or R keyword explicitly present in command

    Args:
        command: APT command object

    Returns:
        True if R format detected, False for IJK format
    """
    # Check for explicit R keyword in command
    if hasattr(command, 'keywords'):
        for keyword in command.keywords:
            if keyword.upper() == 'R':
                return True

    # Check numeric count: 4 values suggests R format (X, Y, Z, R)
    # 6+ values suggests IJK format (X, Y, Z, I, J, K)
    if command.numeric:
        if len(command.numeric) == 4:
            return True
        if len(command.numeric) >= 6:
            return False

    # Default to IJK format if uncertain
    return False


def _get_arc_direction(command) -> int:
    """
    Determine arc direction (CW/CCW) from command minor words

    Args:
        command: APT command object

    Returns:
        DIRECTION_CW (2) for G02 or DIRECTION_CCW (3) for G03
    """
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()

            # Counter-clockwise (G03)
            if word_upper in ['CCLW', 'CCW', 'COUNTERCLOCKWISE', 'LEFT']:
                return DIRECTION_CCW

            # Clockwise (G02)
            if word_upper in ['CLW', 'CW', 'CLOCKWISE', 'RIGHT']:
                return DIRECTION_CW

    # Check system motion type for direction
    motion = getattr(command, 'motion', None)
    if motion:
        motion_upper = motion.upper()
        if motion_upper in ['CCLW', 'CCW', 'COUNTERCLOCKWISE']:
            return DIRECTION_CCW
        if motion_upper in ['CLW', 'CW', 'CLOCKWISE']:
            return DIRECTION_CW

    # Default to clockwise (G02)
    return DIRECTION_CW


# ============================================================================
# Helper Functions - Validation
# ============================================================================

def _validate_arc_parameters(
    x_end: float, y_end: float, z_end: float,
    i_center: float, j_center: float, k_center: float,
    r_radius: Optional[float],
    arc_format: int,
    context
) -> Optional[str]:
    """
    Validate arc parameters for correctness

    Checks:
    - Endpoint differs from start point (not a full circle without center)
    - Radius is positive and non-zero (for R format)
    - IJK values are provided for IJK format
    - Arc is geometrically possible

    Args:
        x_end, y_end, z_end: Endpoint coordinates
        i_center, j_center, k_center: Center offsets (IJK format)
        r_radius: Radius value (R format)
        arc_format: FORMAT_IJK or FORMAT_R
        context: Postprocessor context for current state

    Returns:
        Error message string if validation fails, None if valid
    """
    # Get start point from registers
    x_start = context.registers.x
    y_start = context.registers.y
    z_start = context.registers.z

    # Calculate projected distance in current plane
    plane = _get_current_plane(context)
    if plane == PLANE_G17:  # XY plane
        dx = x_end - x_start
        dy = y_end - y_start
    elif plane == PLANE_G18:  # XZ plane
        dx = x_end - x_start
        dy = z_end - z_start
    else:  # PLANE_G19 - YZ plane
        dx = y_end - y_start
        dy = z_end - z_start

    chord_length = math.sqrt(dx * dx + dy * dy)

    # Check for full circle (start == end)
    is_full_circle = chord_length < 0.0001

    if arc_format == FORMAT_R:
        # R format validation
        if r_radius is None:
            return "R format requires radius value"

        if abs(r_radius) < 0.0001:
            return "Radius must be non-zero"

        # Check if arc is geometrically possible
        # Chord length cannot exceed 2 * radius
        if chord_length > 2 * abs(r_radius) + 0.001:
            return f"Arc impossible: chord ({chord_length:.3f}) > 2*radius ({2*abs(r_radius):.3f})"

        # Full circles require IJK format (R format ambiguous)
        if is_full_circle:
            return "Full circles require IJK format (use I, J, K instead of R)"

    else:
        # IJK format validation
        # At least two center offsets should be non-zero for valid arc
        center_magnitude = math.sqrt(i_center * i_center + j_center * j_center + k_center * k_center)

        if center_magnitude < 0.0001:
            return "IJK center offsets cannot all be zero"

    return None  # Validation passed


# ============================================================================
# Helper Functions - Format Selection
# ============================================================================

def _should_use_radius_format(context, arc_format: int, r_radius: Optional[float]) -> bool:
    """
    Determine whether to output arcs in R (radius) format

    Decision based on:
    1. Controller configuration (circlesThroughRadius)
    2. Original command format
    3. Arc geometry (>180° requires IJK)

    Args:
        context: Postprocessor context
        arc_format: Original command format (FORMAT_IJK or FORMAT_R)
        r_radius: Radius value if available

    Returns:
        True if R format should be used, False for IJK format
    """
    # Check controller configuration
    # circlesThroughRadius=true in controller JSON config enables R format
    use_r_default = context.config.get("circlesThroughRadius", False)

    # Also check global variable (can be overridden by init macro)
    circle_type = context.globalVars.Get("CIRCLE_TYPE", 0)
    if circle_type == 1:
        use_r_default = True  # Force R format
    elif circle_type == 0:
        use_r_default = False  # Force IJK format

    # If original command was R format, prefer R output
    if arc_format == FORMAT_R and r_radius is not None:
        return use_r_default

    # If original was IJK format, prefer IJK output
    return False


# ============================================================================
# Helper Functions - Geometry Calculations
# ============================================================================

def _get_current_plane(context) -> int:
    """
    Get current working plane from context

    Args:
        context: Postprocessor context

    Returns:
        PLANE_G17, PLANE_G18, or PLANE_G19
    """
    # Check global variable for active plane
    plane_var = context.globalVars.Get("ACTIVE_PLANE", "XYPLAN")

    plane_map = {
        "XYPLAN": PLANE_G17,
        "YZPLAN": PLANE_G18,
        "ZXPLAN": PLANE_G19,
        "XZPLAN": PLANE_G18,  # Alternative naming
    }

    return plane_map.get(plane_var, PLANE_G17)


def _calculate_arc_angle(
    x_start: float, y_start: float, z_start: float,
    x_end: float, y_end: float, z_end: float,
    i_center: float, j_center: float, k_center: float,
    r_radius: Optional[float]
) -> float:
    """
    Calculate the sweep angle of an arc in degrees

    Uses vector math to determine the angle between start and end
    vectors from the arc center.

    Args:
        x_start, y_start, z_start: Start point coordinates
        x_end, y_end, z_end: End point coordinates
        i_center, j_center, k_center: Center offsets from start point
        r_radius: Radius (alternative to IJK)

    Returns:
        Arc sweep angle in degrees (positive for CCW, negative for CW)
    """
    # Calculate center point from start + offsets
    x_center = x_start + i_center
    y_center = y_start + j_center
    z_center = z_start + k_center

    # If R format, estimate center (simplified - assumes XY plane)
    if r_radius is not None and (i_center == 0 and j_center == 0 and k_center == 0):
        # Calculate chord midpoint
        x_mid = (x_start + x_end) / 2
        y_mid = (y_start + y_end) / 2

        # Calculate perpendicular distance from chord to center
        dx = x_end - x_start
        dy = y_end - y_start
        chord_length = math.sqrt(dx * dx + dy * dy)

        if chord_length < 0.0001:
            return 360.0  # Full circle

        # Distance from chord midpoint to center
        h = math.sqrt(abs(r_radius * r_radius - (chord_length / 2) ** 2))

        # Perpendicular direction
        if abs(dy) > abs(dx):
            px = -dy / chord_length
            py = dx / chord_length
        else:
            px = dy / chord_length
            py = -dx / chord_length

        # Center point (choose one of two possible centers)
        x_center = x_mid + h * px
        y_center = y_mid + h * py

    # Vectors from center to start and end points
    vx_start = x_start - x_center
    vy_start = y_start - y_center
    vz_start = z_start - z_center

    vx_end = x_end - x_center
    vy_end = y_end - y_center
    vz_end = z_end - z_center

    # Calculate magnitudes
    mag_start = math.sqrt(vx_start**2 + vy_start**2 + vz_start**2)
    mag_end = math.sqrt(vx_end**2 + vy_end**2 + vz_end**2)

    if mag_start < 0.0001 or mag_end < 0.0001:
        return 0.0  # Invalid arc

    # Normalize vectors
    vx_start /= mag_start
    vy_start /= mag_start
    vz_start /= mag_start

    vx_end /= mag_end
    vy_end /= mag_end
    vz_end /= mag_end

    # Calculate angle using dot product
    dot_product = vx_start * vx_end + vy_start * vy_end + vz_start * vz_end

    # Clamp to valid range for acos (handle floating point errors)
    dot_product = max(-1.0, min(1.0, dot_product))

    angle = math.degrees(math.acos(dot_product))

    # Determine sign based on cross product (direction)
    cross_z = vx_start * vy_end - vy_start * vx_end

    if cross_z < 0:
        angle = -angle

    return angle


def _calculate_arc_radius(
    x_start: float, y_start: float, z_start: float,
    i_center: float, j_center: float, k_center: float
) -> float:
    """
    Calculate arc radius from center offsets

    Args:
        x_start, y_start, z_start: Start point (not used, radius from IJK)
        i_center, j_center, k_center: Center offsets from start point

    Returns:
        Arc radius
    """
    return math.sqrt(i_center**2 + j_center**2 + k_center**2)


# ============================================================================
# Utility Functions for External Use
# ============================================================================

def get_arc_gcode(direction: int) -> str:
    """
    Get G-code for arc direction

    Args:
        direction: DIRECTION_CW or DIRECTION_CCW

    Returns:
        "G2" for CW, "G3" for CCW
    """
    return "G2" if direction == DIRECTION_CW else "G3"


def calculate_helix_pitch(
    z_start: float, z_end: float,
    i_center: float, j_center: float, k_center: float
) -> float:
    """
    Calculate helix pitch for helical arc interpolation

    Helical arcs combine circular motion in XY with linear Z movement.

    Args:
        z_start: Starting Z coordinate
        z_end: Ending Z coordinate
        i_center, j_center, k_center: Arc center offsets

    Returns:
        Helix pitch (Z change per full revolution)
    """
    # Calculate arc radius
    radius = math.sqrt(i_center**2 + j_center**2)

    if radius < 0.0001:
        return 0.0

    # Calculate arc angle (simplified - assumes XY plane)
    z_change = z_end - z_start

    # For a full circle (360°), pitch equals Z change
    # For partial arcs, scale proportionally
    arc_angle = _calculate_arc_angle(0, 0, z_start, 0, 0, z_end, i_center, j_center, k_center, None)

    if abs(arc_angle) < 0.001:
        return 0.0

    pitch = z_change * (360.0 / abs(arc_angle))

    return pitch
