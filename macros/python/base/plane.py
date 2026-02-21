# -*- coding: ascii -*-
"""
PLANE MACRO - Рабочая плоскость обработки (G17, G18, G19)

Устанавливает рабочую плоскость для:
- Интерполяции дуг (G02/G03)
- Компенсации радиуса инструмента (G41/G42)
- Циклов сверления

Плоскости:
- G17: XY плоскость (основная для фрезерных станков)
- G18: ZX плоскость (основная для токарных станков)
- G19: YZ плоскость (специальные операции)

APT Examples:
    PLANE/XY      -> G17
    PLANE/ZX      -> G18
    PLANE/YZ      -> G19
"""


def execute(context, command):
    """
    Process PLANE command
    
    Args:
        context: Postprocessor context
        command: APT command (PLANE with minor words)
    """
    # Определение плоскости по minor words
    plane = None
    plane_name = None
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            
            if word_upper in ['XY', 'XAXIS', 'YAXIS']:
                plane = 17
                plane_name = 'XY'
                
            elif word_upper in ['ZX', 'ZAXIS', 'XAXIS_Z']:
                plane = 18
                plane_name = 'ZX'
                
            elif word_upper in ['YZ', 'YAXIS_Z', 'ZAXIS_Y']:
                plane = 19
                plane_name = 'YZ'
    
    # Если плоскость не определена по minor words, проверяем numeric
    if plane is None and command.numeric:
        plane_code = int(command.numeric[0])
        if plane_code == 17:
            plane = 17
            plane_name = 'XY'
        elif plane_code == 18:
            plane = 18
            plane_name = 'ZX'
        elif plane_code == 19:
            plane = 19
            plane_name = 'YZ'
    
    # Если плоскость всё ещё не определена, используем текущую
    if plane is None:
        plane = context.system.Get("PLANE_CODE", 17)
        plane_name = context.system.Get("PLANE_NAME", "XY")
    
    # Сохраняем текущую плоскость в системных переменных
    context.system.Set("PLANE_CODE", plane)
    context.system.Set("PLANE_NAME", plane_name)
    
    # Определяем третью ось для каждой плоскости
    if plane == 17:  # XY
        context.system.Set("PLANE_THIRD_AXIS", "Z")
        context.system.Set("PLANE_I_AXIS", "I")
        context.system.Set("PLANE_J_AXIS", "J")
        context.system.Set("PLANE_K_AXIS", "K")
        
    elif plane == 18:  # ZX
        context.system.Set("PLANE_THIRD_AXIS", "Y")
        context.system.Set("PLANE_I_AXIS", "I")
        context.system.Set("PLANE_J_AXIS", "K")
        context.system.Set("PLANE_K_AXIS", "J")
        
    elif plane == 19:  # YZ
        context.system.Set("PLANE_THIRD_AXIS", "X")
        context.system.Set("PLANE_I_AXIS", "J")
        context.system.Set("PLANE_J_AXIS", "K")
        context.system.Set("PLANE_K_AXIS", "I")
    
    # Вывод G-кода плоскости
    context.write(f"G{plane}")
    
    # Комментарий для отладки (опционально)
    if context.config.get("debugMode", False):
        context.comment(f"PLANE: {plane_name} (G{plane})")


def get_current_plane(context):
    """
    Получить текущую рабочую плоскость
    
    Args:
        context: Postprocessor context
    
    Returns:
        int: Код плоскости (17, 18, или 19)
    """
    return context.system.Get("PLANE_CODE", 17)


def get_plane_axes(context):
    """
    Получить оси текущей плоскости
    
    Args:
        context: Postprocessor context
    
    Returns:
        tuple: (axis1, axis2, third_axis) - названия осей
    """
    plane = get_current_plane(context)
    
    if plane == 17:  # XY
        return ('X', 'Y', 'Z')
    elif plane == 18:  # ZX
        return ('Z', 'X', 'Y')
    elif plane == 19:  # YZ
        return ('Y', 'Z', 'X')
    else:
        return ('X', 'Y', 'Z')  # По умолчанию XY
