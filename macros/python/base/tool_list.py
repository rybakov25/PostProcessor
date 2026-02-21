# -*- coding: ascii -*-
"""
TOOL_LIST MACRO - Вывод списка инструментов

Выводит список всех инструментов, используемых в программе,
в начале управляющей программы.

APT Examples:
    TOOL_LIST/ALL     -> Вывод всех инструментов
    TOOL_LIST/USED    -> Вывод только используемых инструментов
"""


def execute(context, command):
    """
    Process TOOL_LIST command
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Определение режима (ALL или USED)
    mode = 'ALL'
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper in ['USED', 'ACTIVE']:
                mode = 'USED'
            elif word_upper in ['ALL', 'COMPLETE']:
                mode = 'ALL'
    
    # Получение списка инструментов
    tools = _get_tools(context, mode)
    
    if not tools:
        context.comment("NO TOOLS FOUND")
        return
    
    # Вывод заголовка
    context.comment("=" * 40)
    context.comment("TOOL LIST")
    context.comment("=" * 40)
    
    # Сортировка по номеру инструмента
    sorted_tools = sorted(tools, key=lambda t: t.get('number', 0))
    
    # Вывод каждого инструмента
    for tool in sorted_tools:
        _print_tool(context, tool)
    
    # Вывод итога
    context.comment("=" * 40)
    context.comment(f"TOTAL TOOLS: {len(tools)}")
    context.comment("=" * 40)


def _get_tools(context, mode):
    """
    Получить список инструментов
    
    Args:
        context: Postprocessor context
        mode: 'ALL' или 'USED'
    
    Returns:
        list: Список словарей с информацией об инструментах
    """
    # Попытка получить инструменты из проекта
    if hasattr(context, 'get_project_tools'):
        all_tools = context.get_project_tools()
    else:
        # Если нет API, пробуем получить из кэша
        all_tools = list(context.ToolCache.values()) if hasattr(context, 'ToolCache') else []
    
    if mode == 'USED':
        # Вернуть только используемые инструменты
        # (те, у которых есть номер и название)
        return [t for t in all_tools if t.get('number', 0) > 0]
    else:
        # Вернуть все инструменты
        return all_tools


def _print_tool(context, tool):
    """
    Вывести информацию об инструменте
    
    Args:
        context: Postprocessor context
        tool: Словарь с информацией об инструменте
    """
    number = tool.get('number', 0)
    name = tool.get('name', tool.get('caption', 'UNKNOWN'))
    diameter = tool.get('diameter', None)
    length = tool.get('length', None)
    
    # Формирование строки
    line = f"T{number} - {name}"
    
    if diameter is not None and diameter > 0:
        line += f" (D={diameter:.2f})"
    
    if length is not None and length > 0:
        line += f" (L={length:.2f})"
    
    context.comment(line)


def print_tool_change(context, tool_number, tool_name=None):
    """
    Вывести комментарий о смене инструмента
    
    Args:
        context: Postprocessor context
        tool_number: Номер инструмента
        tool_name: Название инструмента (опционально)
    """
    if tool_name:
        context.comment(f"TOOL CHANGE: T{tool_number} - {tool_name}")
    else:
        context.comment(f"TOOL CHANGE: T{tool_number}")
