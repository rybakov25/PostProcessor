# -*- coding: ascii -*-
"""
USE1SET MACRO - Установка модальности для функций

Вдохновлено IMSpost use1set.def
Интегрировано с BlockWriter для управления модальностью

APT Examples:
    USE1SET/LINEAR,POSITION - Установить модальность для LINEAR и POSITION
    USE1SET/CLW,CCLW,CYCLE  - Установить модальность для шпинделя и циклов
"""


def execute(context, command):
    """
    Process USE1SET command - Set modality for functions
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    if not command.numeric or len(command.numeric) == 0:
        return
    
    # Получение типа модальности (первый параметр)
    modality_type = command.numeric[0]
    
    # Обработка каждого параметра
    for i in range(len(command.numeric)):
        param = command.numeric[i]
        param_str = str(param).upper() if isinstance(param, (int, float)) else param.upper()
        
        # Сопоставление с типами движений
        if param_str in ['LINEAR', 'G1', 'G01']:
            _add_modality(context, 'LINEAR', modality_type)
            
        elif param_str in ['POSITION', 'G0', 'G00', 'RAPID']:
            _add_modality(context, 'POSITION', modality_type)
            
        elif param_str in ['NURBS', 'SPLINE']:
            _add_modality(context, 'NURBS', modality_type)
            
        elif param_str in ['CLW', 'CW', 'M3']:
            _add_modality(context, 'CLW', modality_type)
            
        elif param_str in ['CCLW', 'CCW', 'M4']:
            _add_modality(context, 'CCLW', modality_type)
            
        elif param_str in ['CYCLE', 'DRILL']:
            _add_modality(context, 'CYCLE', modality_type)


def _add_modality(context, function_name, modality_type):
    """
    Добавить модальность для функции
    
    Args:
        context: Postprocessor context
        function_name: Имя функции (LINEAR, POSITION, etc.)
        modality_type: Тип модальности
    """
    # Получение текущего состояния USE1
    use1_key = f"USE1_{function_name}"
    current_use1 = context.globalVars.Get(use1_key, "")
    
    # Добавление новой модальности
    if current_use1:
        new_use1 = f"{current_use1},{modality_type}"
    else:
        new_use1 = modality_type
    
    # Сохранение
    context.globalVars.Set(use1_key, new_use1)
    
    # Применение к BlockWriter
    _apply_to_blockwriter(context, function_name, modality_type)


def _apply_to_blockwriter(context, function_name, modality_type):
    """
    Применить модальность к BlockWriter
    
    Args:
        context: Postprocessor context
        function_name: Имя функции
        modality_type: Тип модальности
    """
    # Сопоставление с регистрами
    register_map = {
        'LINEAR': ['X', 'Y', 'Z', 'F'],
        'POSITION': ['X', 'Y', 'Z'],
        'CLW': ['S'],
        'CCLW': ['S'],
        'CYCLE': ['X', 'Y', 'Z', 'R', 'F']
    }
    
    if function_name in register_map:
        for reg_name in register_map[function_name]:
            reg = _get_register(context, reg_name)
            if reg:
                # Установка модальности
                reg.IsModal = True


def _get_register(context, name):
    """
    Получить регистр по имени
    
    Args:
        context: Postprocessor context
        name: Имя регистра (X, Y, Z, F, S...)
    
    Returns:
        Register или None
    """
    registers = context.Registers
    name_upper = name.upper()
    
    if name_upper == 'X':
        return registers.X
    elif name_upper == 'Y':
        return registers.Y
    elif name_upper == 'Z':
        return registers.Z
    elif name_upper == 'F':
        return registers.F
    elif name_upper == 'S':
        return registers.S
    elif name_upper == 'T':
        return registers.T
    elif name_upper == 'A':
        return registers.A
    elif name_upper == 'B':
        return registers.B
    elif name_upper == 'C':
        return registers.C
    elif name_upper == 'I':
        return registers.I
    elif name_upper == 'J':
        return registers.J
    elif name_upper == 'K':
        return registers.K
    elif name_upper == 'R':
        return registers.R
    
    return None


def use1add(context, function_name, modality_type):
    """
    Добавить модальность к функции (аналог USE1ADD)
    
    Args:
        context: Postprocessor context
        function_name: Имя функции
        modality_type: Тип модальности
    """
    use1_key = f"USE1_{function_name}"
    current_use1 = context.globalVars.Get(use1_key, "")
    
    if current_use1:
        new_use1 = f"{current_use1},{modality_type}"
    else:
        new_use1 = modality_type
    
    context.globalVars.Set(use1_key, new_use1)


def get_use1(context, function_name):
    """
    Получить список модальностей для функции
    
    Args:
        context: Postprocessor context
        function_name: Имя функции
    
    Returns:
        list: Список модальностей
    """
    use1_key = f"USE1_{function_name}"
    use1_str = context.globalVars.Get(use1_key, "")
    
    if use1_str:
        return [m.strip() for m in use1_str.split(',')]
    
    return []


def is_modal(context, function_name, modality_type):
    """
    Проверить, установлена ли модальность
    
    Args:
        context: Postprocessor context
        function_name: Имя функции
        modality_type: Тип модальности
    
    Returns:
        bool: True если модальность установлена
    """
    use1_list = get_use1(context, function_name)
    return modality_type in use1_list
