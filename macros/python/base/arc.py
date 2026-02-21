# -*- coding: ascii -*-
"""
ARC - Обработка круговых дуг G02/G03

Поддержка двух форматов вывода:
- IJK - центр дуги (относительно начальной точки)
- R  - радиус дуги

Выбор формата определяется:
1. Настройкой в конфиге контроллера (circlesThroughRadius)
2. Углом дуги (>180° всегда используется IJK)
3. Наличием параметров в команде

Пример APT:
    CIRCLE/X, 100, Y, 200, Z, 50, I, 10, J, 0, K, 0
    CIRCLE/X, 100, Y, 200, Z, 50, R, 25, ANGLE, 90
"""

import math


def execute(context, command):
    """
    Обработка команды CIRCLE/ARC
    
    Args:
        context: Контекст постпроцессора
        command: APT команда (CIRCLE, ARC, G02, G03)
    """
    if not command.numeric:
        return
    
    # Определение направления (G02=CW, G03=CCW)
    major = command.majorWord.upper()
    if major in ('ARC', 'CIRCLE'):
        # По умолчанию G02 (по часовой), если не указано иное
        g_code = 2
        if command.minorWords:
            for word in command.minorWords:
                if word.upper() in ('CCW', 'CCLW'):
                    g_code = 3
    elif major == 'G02':
        g_code = 2
    elif major == 'G03':
        g_code = 3
    else:
        return
    
    # Получение координат конечной точки
    x = command.getNumeric(0, context.registers.x)
    y = command.getNumeric(1, context.registers.y)
    z = command.getNumeric(2, context.registers.z)
    
    # Получение параметров дуги
    # IJK - центр относительно старта
    i = command.getNumeric(3, 0.0)
    j = command.getNumeric(4, 0.0)
    k = command.getNumeric(5, 0.0)
    
    # R - радиус (альтернатива IJK)
    r = command.getNumeric(6, 0.0)
    
    # Угол дуги (в градусах)
    angle = command.getNumeric(7, 0.0)
    
    # Плоскость обработки (по умолчанию G17/XY)
    plane = context.system.Get("PLANE", "G17")
    
    # Настройка вывода радиуса из конфига
    use_radius = context.config.get("circlesThroughRadius", False)
    
    # Выбор формата: IJK или R
    # Для углов > 180° или полном круге всегда используем IJK
    use_radius_format = (
        use_radius and 
        r != 0.0 and 
        angle != 0.0 and 
        abs(angle) < 180.0
    )
    
    # Обновление регистров
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z
    
    # Формирование вывода
    if use_radius_format:
        # Вывод через радиус
        _output_arc_radius(context, g_code, x, y, z, r, angle)
    else:
        # Вывод через центр (IJK)
        _output_arc_center(context, g_code, x, y, z, i, j, k, plane)
    
    # Запись блока с учётом модальности
    context.writeBlock()


def _output_arc_radius(context, g_code, x, y, z, r, angle):
    """
    Вывод дуги через радиус (R формат)
    
    Args:
        context: Контекст постпроцессора
        g_code: 2 (G02) или 3 (G03)
        x, y, z: Конечные координаты
        r: Радиус
        angle: Угол дуги
    """
    # Форматирование координат
    x_str = context.format(x, "F3")
    y_str = context.format(y, "F3")
    z_str = context.format(z, "F3")
    r_str = context.format(r, "F3")
    
    # Для дуг > 180° радиус должен быть отрицательным
    if abs(angle) > 180:
        r = -r
        r_str = context.format(r, "F3")
    
    # Построение команды
    parts = [f"G{g_code}", f"X{x_str}", f"Y{y_str}"]
    
    if z_str != "0.000" and z != 0:
        parts.append(f"Z{z_str}")
    
    parts.append(f"R{r_str}")
    
    # Добавление подачи если изменена
    if context.registers.f.HasChanged:
        f_str = context.format(context.registers.f.Value, "F1")
        parts.append(f"F{f_str}")
    
    # Вывод
    context.write(" ".join(parts))


def _output_arc_center(context, g_code, x, y, z, i, j, k, plane):
    """
    Вывод дуги через центр (IJK формат)
    
    Args:
        context: Контекст постпроцессора
        g_code: 2 (G02) или 3 (G03)
        x, y, z: Конечные координаты
        i, j, k: Координаты центра относительно старта
        plane: Плоскость обработки (G17, G18, G19)
    """
    # Форматирование координат
    x_str = context.format(x, "F3")
    y_str = context.format(y, "F3")
    z_str = context.format(z, "F3")
    i_str = context.format(i, "F3")
    j_str = context.format(j, "F3")
    k_str = context.format(k, "F3")
    
    # Построение команды в зависимости от плоскости
    parts = [f"G{g_code}"]
    
    if plane == "G17":  # XY плоскость
        parts.extend([f"X{x_str}", f"Y{y_str}", f"I{i_str}", f"J{j_str}"])
        if z_str != "0.000" and z != 0:
            parts.append(f"Z{z_str}")
    elif plane == "G18":  # ZX плоскость
        parts.extend([f"X{x_str}", f"Z{z_str}", f"I{i_str}", f"K{k_str}"])
        if y_str != "0.000" and y != 0:
            parts.append(f"Y{y_str}")
    elif plane == "G19":  # YZ плоскость
        parts.extend([f"Y{y_str}", f"Z{z_str}", f"J{j_str}", f"K{k_str}"])
        if x_str != "0.000" and x != 0:
            parts.append(f"X{x_str}")
    else:
        # По умолчанию XY
        parts.extend([f"X{x_str}", f"Y{y_str}", f"I{i_str}", f"J{j_str}"])
        if z_str != "0.000" and z != 0:
            parts.append(f"Z{z_str}")
    
    # Добавление подачи если изменена
    if context.registers.f.HasChanged:
        f_str = context.format(context.registers.f.Value, "F1")
        parts.append(f"F{f_str}")
    
    # Вывод
    context.write(" ".join(parts))


def calculate_radius_from_ijk(i, j, k):
    """
    Вычисление радиуса из координат центра
    
    Args:
        i, j, k: Координаты центра относительно старта
    
    Returns:
        float: Радиус дуги
    """
    return math.sqrt(i * i + j * j + k * k)


def calculate_angle_from_points(sx, sy, ex, ey, ix, iy):
    """
    Вычисление угла дуги по точкам старта, конца и центра
    
    Args:
        sx, sy: Координаты старта
        ex, ey: Координаты конца
        ix, iy: Координаты центра
    
    Returns:
        float: Угол в градусах (положительный = CCW, отрицательный = CW)
    """
    # Векторы от центра к точкам
    v1x = sx - ix
    v1y = sy - iy
    v2x = ex - ix
    v2y = ey - iy
    
    # Длины векторов (радиусы)
    r1 = math.sqrt(v1x * v1x + v1y * v1y)
    r2 = math.sqrt(v2x * v2x + v2y * v2y)
    
    if r1 == 0 or r2 == 0:
        return 0.0
    
    # Скалярное произведение
    dot = v1x * v2x + v1y * v2y
    
    # Косинус угла
    cos_angle = dot / (r1 * r2)
    
    # Ограничение диапазона для acos
    cos_angle = max(-1.0, min(1.0, cos_angle))
    
    # Угол в радианах
    angle_rad = math.acos(cos_angle)
    
    # Векторное произведение для определения направления
    cross = v1x * v2y - v1y * v2x
    
    # Определение направления (CCW = положительно, CW = отрицательно)
    if cross < 0:
        angle_rad = -angle_rad
    
    # Конвертация в градусы
    return math.degrees(angle_rad)


# === Вспомогательные функции для винтовых дуг ===

def execute_helical(context, command):
    """
    Обработка винтовой дуги (G02/G03 с движением по Z)
    
    Args:
        context: Контекст постпроцессора
        command: APT команда с винтовым движением
    """
    # Базовая обработка дуги
    execute(context, command)
    
    # Винтовая интерполяция обрабатывается автоматически
    # если Z изменяется во время дуги
