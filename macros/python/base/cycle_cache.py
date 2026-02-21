# -*- coding: ascii -*-
"""
CYCLE_CACHE - Кэширование состояния циклов для оптимизации вывода

Этот макрос предоставляет класс CycleCache для кэширования параметров циклов.
Если параметры цикла не изменились, выводится только вызов цикла без повторного определения.

Пример использования:
    # В макросе цикла (например, cycle800.py или hole_cycle.py)
    from cycle_cache import CycleCache
    
    def execute(context, command):
        # Получение или создание кэша для этого типа цикла
        cache = context.globalVars.Get("CYCLE800_CACHE", None)
        if cache is None:
            cache = CycleCache(context, "CYCLE800")
            context.globalVars.Set("CYCLE800_CACHE", cache)
        
        # Параметры цикла
        params = {
            'MODE': 1,
            'TABLE': 'TABLE1',
            'X': 100.0,
            'Y': 200.0,
            'Z': 50.0
        }
        
        # Вывод (автоматически выберет полную форму или сокращённую)
        cache.write_if_different(params)
"""


class CycleCache:
    """
    Кэш для оптимизации вывода циклов ЧПУ
    
    Если параметры цикла идентичны предыдущему вызову,
    выводится только команда вызова без повторного определения.
    """
    
    def __init__(self, context, cycle_name):
        """
        Инициализация кэша цикла
        
        Args:
            context: Контекст постпроцессора
            cycle_name: Имя цикла (например, "CYCLE800", "CYCLE81")
        """
        self.context = context
        self.cycle_name = cycle_name
        self.cached_params = None
        self.call_count = 0
    
    def write_if_different(self, params_dict, call_command=None):
        """
        Выводит цикл только если параметры изменились
        
        Args:
            params_dict: dict с параметрами цикла
            call_command: Команда для вызова цикла (по умолчанию cycle_name)
        
        Returns:
            bool: True если выведено полное определение, False если только вызов
        """
        # Сортируем параметры для стабильного сравнения
        params_str = str(sorted(params_dict.items()))
        
        if call_command is None:
            call_command = self.cycle_name
        
        if self.cached_params == params_str:
            # Одинаковый цикл - только вызов
            if call_command:
                self.context.write(call_command)
            self.call_count += 1
            return False
        else:
            # Новый цикл - полное определение
            params_formatted = self._format_params(params_dict)
            self.context.write(f"{self.cycle_name}({params_formatted})")
            self.cached_params = params_str
            self.call_count += 1
            return True
    
    def _format_params(self, params_dict):
        """
        Форматирование параметров для вывода
        
        Args:
            params_dict: dict с параметрами
        
        Returns:
            str: Отформатированная строка параметров
        """
        parts = []
        for key, value in params_dict.items():
            if isinstance(value, float):
                # Форматирование чисел с плавающей точкой
                formatted_value = self.context.format(value, "F3")
            elif isinstance(value, str):
                # Строки в кавычках
                formatted_value = f'"{value}"'
            else:
                # Целые числа и другие типы
                formatted_value = str(value)
            
            parts.append(f"{key}={formatted_value}")
        
        return ", ".join(parts)
    
    def reset(self):
        """
        Сброс кэша (для M30, новой операции или смены цикла)
        """
        self.cached_params = None
        self.call_count = 0
    
    def get_stats(self):
        """
        Получить статистику использования кэша
        
        Returns:
            dict: Статистика (количество вызовов, имя цикла)
        """
        return {
            'cycle_name': self.cycle_name,
            'call_count': self.call_count,
            'is_cached': self.cached_params is not None
        }


# === Вспомогательные функции для конкретных циклов ===

def get_cycle800_params(context, command):
    """
    Извлечение параметров для CYCLE800 из APT команды
    
    Args:
        context: Контекст постпроцессора
        command: APT команда
    
    Returns:
        dict: Параметры для CYCLE800
    """
    return {
        'MODE': command.getNumeric(0, 1),
        'TABLE': command.getString(1, "TABLE"),
        'X': command.getNumeric(2, 0.0),
        'Y': command.getNumeric(3, 0.0),
        'Z': command.getNumeric(4, 0.0),
        'A': command.getNumeric(5, 0.0),
        'B': command.getNumeric(6, 0.0),
        'C': command.getNumeric(7, 0.0)
    }


def get_cycle81_params(context, command):
    """
    Извлечение параметров для CYCLE81 (сверление) из APT команды
    
    Args:
        context: Контекст постпроцессора
        command: APT команда
    
    Returns:
        dict: Параметры для CYCLE81
    """
    return {
        'RTP': command.getNumeric(0, 0.0),    # Plane возврата
        'RFP': command.getNumeric(1, 0.0),    # Plane отсчёта
        'SDIS': command.getNumeric(2, 0.0),   # Безопасная дистанция
        'DP': command.getNumeric(3, 0.0),     # Глубина (абсолютная)
        'DPR': command.getNumeric(4, 0.0)     # Глубина (относительная)
    }


def get_cycle83_params(context, command):
    """
    Извлечение параметров для CYCLE83 (глубокое сверление) из APT команды
    
    Args:
        context: Контекст постпроцессора
        command: APT команда
    
    Returns:
        dict: Параметры для CYCLE83
    """
    return {
        'RTP': command.getNumeric(0, 0.0),    # Plane возврата
        'RFP': command.getNumeric(1, 0.0),    # Plane отсчёта
        'SDIS': command.getNumeric(2, 0.0),   # Безопасная дистанция
        'DP': command.getNumeric(3, 0.0),     # Глубина
        'DPR': command.getNumeric(4, 0.0),    # Относительная глубина
        'FDEP': command.getNumeric(5, 0.0),   # Первое углубление
        'FDPR': command.getNumeric(6, 0.0),   # Углубление
        'DAM': command.getNumeric(7, 0.0),    # Величина разрушения
        'DTS': command.getNumeric(8, 0.0),    # Время ожидания
        'FRF': command.getNumeric(9, 0.0),    # Коэффициент подачи
        'VARI': command.getNumeric(10, 0.0)   # Тип цикла
    }


def create_cycle_cache(context, cycle_type):
    """
    Создание или получение кэша для указанного типа цикла
    
    Args:
        context: Контекст постпроцессора
        cycle_type: Тип цикла ("CYCLE800", "CYCLE81", "CYCLE83" и т.д.)
    
    Returns:
        CycleCache: Объект кэша
    """
    cache_key = f"{cycle_type}_CACHE"
    cache = context.globalVars.Get(cache_key, None)
    
    if cache is None:
        cache = CycleCache(context, cycle_type)
        context.globalVars.Set(cache_key, cache)
    
    return cache
