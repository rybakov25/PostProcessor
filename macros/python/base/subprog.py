# -*- coding: ascii -*-
"""
SUBPROG MACRO - Подпрограммы (M98/M99)

Поддержка вызова и возврата из подпрограмм:
- M98 P#### - вызов подпрограммы (P = номер подпрограммы)
- M99 - возврат из подпрограммы

APT Examples:
    CALLSUB/1001    -> M98 P1001
    ENDSUB          -> M99
"""


def execute(context, command):
    """
    Process subprogram command (CALLSUB, ENDSUB)
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    major = command.majorWord.upper()
    
    if major == 'CALLSUB':
        # Вызов подпрограммы
        _handle_call_sub(context, command)
        
    elif major == 'ENDSUB':
        # Возврат из подпрограммы
        _handle_end_sub(context, command)
        
    elif major == 'SUBCALL':
        # Альтернативный синтаксис
        _handle_call_sub(context, command)


def _handle_call_sub(context, command):
    """
    Обработка вызова подпрограммы (M98 P####)
    
    Args:
        context: Postprocessor context
        command: APT command с номером подпрограммы
    """
    # Получение номера подпрограммы
    sub_number = None
    
    if command.numeric and len(command.numeric) > 0:
        sub_number = int(command.numeric[0])
    
    # Проверка minor words (например, CALLSUB/1001)
    if sub_number is None and command.minorWords:
        for word in command.minorWords:
            try:
                sub_number = int(word)
                break
            except ValueError:
                continue
    
    # Если номер не найден, используем значение из контекста
    if sub_number is None:
        sub_number = context.system.Get("CURRENT_SUB_NUMBER", 0)
    
    # Сохраняем текущий номер подпрограммы
    context.system.Set("CURRENT_SUB_NUMBER", sub_number)
    
    # Увеличиваем счётчик вызовов подпрограмм
    call_count = context.system.Get("SUB_CALL_COUNT", 0)
    context.system.Set("SUB_CALL_COUNT", call_count + 1)
    
    # Вывод M98 P####
    if sub_number > 0:
        context.write(f"M98 P{sub_number}")
        
        # Комментарий для отладки
        if context.config.get("debugMode", False):
            context.comment(f"CALL SUB {sub_number} (call #{call_count + 1})")


def _handle_end_sub(context, command):
    """
    Обработка возврата из подпрограммы (M99)
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Уменьшаем счётчик вызовов
    call_count = context.system.Get("SUB_CALL_COUNT", 0)
    if call_count > 0:
        context.system.Set("SUB_CALL_COUNT", call_count - 1)
    
    # Вывод M99
    context.write("M99")
    
    # Комментарий для отладки
    if context.config.get("debugMode", False):
        context.comment("END SUB (return)")


def get_sub_call_count(context):
    """
    Получить текущий счётчик вызовов подпрограмм
    
    Args:
        context: Postprocessor context
    
    Returns:
        int: Количество активных вызовов подпрограмм
    """
    return context.system.Get("SUB_CALL_COUNT", 0)


def is_in_subroutine(context):
    """
    Проверить, находимся ли мы внутри подпрограммы
    
    Args:
        context: Postprocessor context
    
    Returns:
        bool: True если внутри подпрограммы
    """
    return get_sub_call_count(context) > 0
