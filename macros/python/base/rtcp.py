# -*- coding: ascii -*-
"""
RTCP MACRO - RTCP (TCPM) включение/выключение

Вдохновлено IMSpost rtcp.def
Интегрировано с BlockWriter для управления регистрами

APT Examples:
    RTCP/ON           - Включить RTCP
    RTCP/OFF          - Выключить RTCP
"""


def execute(context, command):
    """
    Process RTCP command - Turn RTCP (TCPM) on/off
    
    Args:
        context: Postprocessor context
        command: APT command
    """
    # Проверка направления
    rtcp_on = False
    
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            if word_upper == 'ON':
                rtcp_on = True
            elif word_upper == 'OFF':
                rtcp_on = False
    
    # Если нет minor words, проверяем numeric
    if command.numeric and len(command.numeric) > 0:
        rtcp_on = command.numeric[0] != 0
    
    if rtcp_on:
        # === ВКЛЮЧЕНИЕ RTCP ===
        
        # Выключение рабочей плоскости если включена
        if context.system.Get("WPLANE", "OFF") == "ON":
            _turn_off_wplane(context)
        
        # Вывод команды включения RTCP
        rtcp_on_code = context.config.Get("fiveAxis.tcpOn", "RTCPON")
        context.write(rtcp_on_code)
        
        # Обновление системных переменных
        if context.globalVars.Get("STRATEGY_RTCP", 1) == 1:
            context.system.COORD_RTCP = 1
            context.system.CONTROLLER_AUTO_LINTOL = 1
        else:
            context.system.COORD_RTCP = 0
            context.system.CONTROLLER_AUTO_LINTOL = 0
        
        # Принудительный вывод регистров
        _force_registers(context)
        
        # Обновление типа дуг
        context.system.CIRCTYPE = 10  # RTCP mode
        
        # Комментарий для отладки
        if context.config.Get("debugMode", False):
            context.comment("RTCP ON")
    
    else:
        # === ВЫКЛЮЧЕНИЕ RTCP ===
        
        # Вывод команды выключения RTCP
        rtcp_off_code = context.config.Get("fiveAxis.tcpOff", "RTCPOF")
        context.write(rtcp_off_code)
        
        # Восстановление типа дуг
        context.system.CIRCTYPE = context.globalVars.Get("CIRCTYPE_SAV", 0)
        
        # Сброс координат
        context.system.COORD_RTCP = 0
        context.system.CONTROLLER_AUTO_LINTOL = 0
        
        # Принудительный вывод регистров
        _force_registers(context)
        
        # Комментарий для отладки
        if context.config.Get("debugMode", False):
            context.comment("RTCP OFF")


def _force_registers(context):
    """
    Принудительный вывод всех линейных осей
    
    Args:
        context: Postprocessor context
    """
    # Получение списка линейных осей из конфига
    linear_axes = context.config.Get("formatting.linearAxes", "X,Y,Z")
    
    # Разделение на список
    axes = [a.strip().upper() for a in linear_axes.split(',')]
    
    # Принудительный вывод каждой оси
    for axis in axes:
        reg = _get_register(context, axis)
        if reg:
            # Принудительная запись
            reg.ForceChanged()
    
    # Запись блока с обновлёнными регистрами
    context.writeBlock()


def _get_register(context, name):
    """
    Получить регистр по имени
    
    Args:
        context: Postprocessor context
        name: Имя регистра
    
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
    elif name_upper == 'A':
        return registers.A
    elif name_upper == 'B':
        return registers.B
    elif name_upper == 'C':
        return registers.C
    elif name_upper == 'F':
        return registers.F
    
    return None


def _turn_off_wplane(context):
    """
    Выключить рабочую плоскость
    
    Args:
        context: Postprocessor context
    """
    # Импорт макроса wplane если доступен
    try:
        from wplane import execute as wplane_execute
        # Создание фиктивной команды
        class FakeCommand:
            minorWords = ['OFF']
            numeric = []
        wplane_execute(context, FakeCommand())
    except ImportError:
        # Если макрос недоступен, просто обновляем переменную
        context.system.WPLANE = "OFF"


def rtcp_on(context):
    """
    Включить RTCP
    
    Args:
        context: Postprocessor context
    """
    class FakeCommand:
        minorWords = ['ON']
        numeric = [1]
    
    execute(context, FakeCommand())


def rtcp_off(context):
    """
    Выключить RTCP
    
    Args:
        context: Postprocessor context
    """
    class FakeCommand:
        minorWords = ['OFF']
        numeric = [0]
    
    execute(context, FakeCommand())


def is_rtcp_active(context):
    """
    Проверить, активен ли RTCP
    
    Args:
        context: Postprocessor context
    
    Returns:
        bool: True если RTCP активен
    """
    return context.system.Get("COORD_RTCP", 0) == 1


def get_rtcp_mode(context):
    """
    Получить режим RTCP
    
    Args:
        context: Postprocessor context
    
    Returns:
        int: 0 = OFF, 1 = Table+Head, 2 = Head only
    """
    return context.globalVars.Get("STRATEGY_RTCP", 1)
