# -*- coding: ascii -*-
"""
FORCE MACRO - Принудительное направление вращения осей

Вдохновлено IMSpost force.def
Интегрировано с SYSTEM.FORCE_WAY для управления направлением

APT Examples:
    FORCE/MINUS,AAXIS     - Принудительно минус для оси A
    FORCE/PLUS,BAXIS,5AXIS - Принудительно плюс для оси B в 5-осевой
    FORCE/MINUS,CAXIS,ALL,45 - Принудительно минус для C с значением 45
"""


def execute(context, command):
    """
    Process FORCE command - Force rotary axis direction
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Проверка направления (обязательный параметр)
    direction = ""
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper in ['MINUS', 'NEGATIVE']:
                direction = "<"
            elif word_upper in ['PLUS', 'POSITIVE']:
                direction = ">"
    
    if not direction:
        context.warning("FORCE: Direction (MINUS/PLUS) is required")
        return
    
    # Определение оси (по умолчанию главная ось рабочей плоскости)
    axis = context.globalVars.Get("WPLANE_MAIN_ROTARY_AXIS", "B")
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper in ['AAXIS', 'A']:
                axis = "A"
            elif word_upper in ['BAXIS', 'B']:
                axis = "B"
            elif word_upper in ['CAXIS', 'C']:
                axis = "C"
    
    # Проверка существования оси
    if not _axis_exists(context, axis):
        context.warning(f"FORCE: Axis {axis} does not exist on this machine")
        return
    
    # Определение типа операции
    operation_type = "ALL"  # ALL, 3AXIS, 5AXIS
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper == '3AXIS':
                operation_type = "3AXIS"
            elif word_upper == '5AXIS':
                operation_type = "5AXIS"
    
    # Получение значения (по умолчанию 0)
    value = 0
    if command.numeric and len(command.numeric) > 0:
        value = command.numeric[0]
    
    # Применение FORCE инструкции
    _apply_force(context, axis, direction, value, operation_type)
    
    # Обновление глобальных переменных если это главная ось
    if axis == context.globalVars.Get("WPLANE_MAIN_ROTARY_AXIS", "B"):
        context.globalVars.WPLANE_MAIN_ROTARY_AXIS_LIMIT = value
        context.globalVars.WPLANE_MAIN_ROTARY_AXIS_DIR = -1 if direction == "<" else 1
    
    # Комментарий для отладки
    if context.config.Get("debugMode", False):
        context.comment(f"FORCE {axis}{direction}{value} ({operation_type})")


def _axis_exists(context, axis):
    """
    Проверить существование оси на станке
    
    Args:
        context: Postprocessor context
        axis: Имя оси (A, B, C)
    
    Returns:
        bool: True если ось существует
    """
    # Проверка через Machine state
    machine = context.Machine
    axis_upper = axis.upper()
    
    # Проверка через доступные оси
    available_axes = ['X', 'Y', 'Z', 'A', 'B', 'C']
    return axis_upper in available_axes


def _apply_force(context, axis, direction, value, operation_type):
    """
    Применить FORCE инструкцию
    
    Args:
        context: Postprocessor context
        axis: Имя оси
        direction: Направление (< или >)
        value: Значение
        operation_type: Тип операции (ALL, 3AXIS, 5AXIS)
    """
    # Формирование условия
    condition = f'MACHINE.{axis}.ABSOLUTE{direction}{value}'
    
    # Обновление GLOBAL переменных
    if operation_type == "ALL":
        context.globalVars.STRATEGY_BEST_SOL_3X = ""
        context.globalVars.STRATEGY_BEST_SOL_5X = ""
        context.globalVars.FORCE_WAY = condition
    elif operation_type == "3AXIS":
        context.globalVars.STRATEGY_BEST_SOL_3X = condition
    elif operation_type == "5AXIS":
        context.globalVars.STRATEGY_BEST_SOL_5X = condition
    
    # Обновление SYSTEM переменной (применяется немедленно)
    context.system.FORCE_WAY = condition


def force_axis(context, axis, direction, value=0, operation_type="ALL"):
    """
    Установить принудительное направление для оси
    
    Args:
        context: Postprocessor context
        axis: Имя оси (A, B, C)
        direction: Направление ('<' или '>')
        value: Значение (по умолчанию 0)
        operation_type: Тип операции (ALL, 3AXIS, 5AXIS)
    """
    _apply_force(context, axis, direction, value, operation_type)
    
    # Обновление если это главная ось
    if axis == context.globalVars.Get("WPLANE_MAIN_ROTARY_AXIS", "B"):
        context.globalVars.WPLANE_MAIN_ROTARY_AXIS_LIMIT = value
        context.globalVars.WPLANE_MAIN_ROTARY_AXIS_DIR = -1 if direction == "<" else 1


def clear_force(context):
    """
    Очистить все FORCE инструкции
    """
    context.globalVars.STRATEGY_BEST_SOL_3X = ""
    context.globalVars.STRATEGY_BEST_SOL_5X = ""
    context.globalVars.FORCE_WAY = ""
    context.system.FORCE_WAY = ""


def get_force_condition(context, operation_type="ALL"):
    """
    Получить текущее условие FORCE
    
    Args:
        context: Postprocessor context
        operation_type: Тип операции (ALL, 3AXIS, 5AXIS)
    
    Returns:
        str: Условие FORCE или пустая строка
    """
    if operation_type == "3AXIS":
        return context.globalVars.Get("STRATEGY_BEST_SOL_3X", "")
    elif operation_type == "5AXIS":
        return context.globalVars.Get("STRATEGY_BEST_SOL_5X", "")
    else:
        return context.globalVars.Get("FORCE_WAY", "")
