# Руководство по написанию Python макросов

> **Полное руководство для начинающих** — от первого макроса до продвинутых техник

## 📋 Оглавление

1. [Введение](#введение)
2. [Быстрый старт (5 минут)](#быстрый-старт-5-минут)
3. [Основы Python для макросов](#основы-python-для-макросов)
4. [API макросов](#api-макросов)
5. [Примеры макросов](#примеры-макросов)
6. [Продвинутые темы](#продвинутые-темы)
7. [Отладка](#отладка)
8. [Справочник](#справочник)
9. [Частые ошибки](#частые-ошибки)

---

## Введение

### Что такое макросы?

**Макросы** — это Python-скрипты, которые обрабатывают APT-команды из CAM-систем и преобразуют их в G-код для вашего станка с ЧПУ.

```
APT-файл (из CAM)  →  Постпроцессор  →  Python макросы  →  G-код (для станка)
```

### Зачем они нужны?

| Задача | Решение через макросы |
|--------|----------------------|
| Изменить формат G-кода | Редактируете Python-файл |
| Добавить проверку безопасности | Добавляете условие в макрос |
| Поддержать 5-осевую обработку | Пишете логику конвертации IJK→ABC |
| Кастомизировать смену инструмента | Модифицируете макрос LOADTL |

### Как работает постпроцессор?

```
1. Чтение APT-файла
   ↓
2. Парсинг команд (GOTO, SPINDL, COOLNT...)
   ↓
3. Поиск соответствующего макроса (.py файл)
   ↓
4. Выполнение функции execute(context, command)
   ↓
5. Вывод G-кода через context.write()
```

---

## Быстрый старт (5 минут)

### Шаг 1: Найдите папку с макросами

```
PostProcessor/
└── macros/
    └── python/
        ├── base/      # Базовые макросы (для всех станков)
        └── mmill/     # Макросы для вашего станка
```

### Шаг 2: Создайте первый макрос

Создайте файл `macros/python/user/hello.py`:

```python
# -*- coding: ascii -*-
# Мой первый макрос

def execute(context, command):
    """Приветственный макрос"""
    context.comment("=== Привет от Python макроса! ===")
    context.write("G0 X0 Y0 Z50")
```

### Шаг 3: Запустите постпроцессор

```bash
dotnet run -- -i input.apt -o output.nc -c siemens
```

### Шаг 4: Проверьте результат

В выходном файле `output.nc` появится:

```nc
(=== Привет от Python макроса! ===)
N1 G0 X0. Y0. Z50.
```

🎉 **Поздравляем!** Вы написали свой первый макрос!

---

## Основы Python для макросов

### Переменные и типы данных

```python
# Числа (int, float)
x = 100.5          # координата
feed = 500         # подача мм/мин
tool_number = 5    # номер инструмента

# Строки (str)
gcode = "G01"
comment = "Начало обработки"

# Логические значения (bool)
is_rapid = True
has_tool = False

# Списки (list)
coordinates = [100.0, 50.0, 10.0]
words = ["ON", "CLW"]
```

### Функции

```python
# Объявление функции
def my_function(param1, param2):
    result = param1 + param2
    return result

# Вызов функции
value = my_function(10, 20)  # value = 30
```

### Условия (if/elif/else)

```python
# Простое условие
if feed > 1000:
    context.warning("Подача слишком высокая!")

# Если-иначе
if spindle_state == "ON":
    context.write("M3")
else:
    context.write("M5")

# Несколько условий
if motion_type == "LINEAR":
    gcode = "G1"
elif motion_type == "RAPID":
    gcode = "G0"
else:
    gcode = "G1"  # по умолчанию
```

### Циклы (for/while)

```python
# Перебор списка
for word in command.minorWords:
    if word.upper() == "ON":
        context.write("M3")

# Цикл с диапазоном
for i in range(5):
    context.comment(f"Итерация {i}")

# Цикл while
count = 0
while count < 10:
    count += 1
```

---

## API макросов

### Структура функции макроса

Каждый макрос должен содержать функцию `execute`:

```python
def execute(context, command):
    """
    Документация макроса
    
    Args:
        context: Объект контекста постпроцессора
        command: Объект APT-команды
    """
    # Ваша логика здесь
    pass
```

---

### Объект `context` — главный инструмент макроса

#### Методы вывода G-кода

| Метод | Описание | Пример |
|-------|----------|--------|
| `write(line)` | Вывести строку G-кода | `context.write("G01 X100")` |
| `writeln(line)` | Вывести строку без номера блока | `context.writeln("")` |
| `comment(text)` | Вывести комментарий в скобках | `context.comment("Начало")` |
| `warning(text)` | Вывести предупреждение | `context.warning("Z太低!")` |

**Примеры:**

```python
# Вывод G-кода с номером блока
context.write("G01 X100. Y50. Z10.")
# Вывод: N10 G01 X100. Y50. Z10.

# Вывод без номера блока
context.write("G01 X100.", suppress_block=True)
# Вывод: G01 X100.

# Комментарий
context.comment("Начало обработки")
# Вывод: (Начало обработки)

# Предупреждение
context.warning("Z=5 ниже безопасной высоты!")
# Вывод: (WARNING: Z=5 ниже безопасной высоты!)
```

---

#### Объект `context.registers` — регистры станка

Регистры хранят текущие значения координат и параметров:

| Регистр | Тип | Описание |
|---------|-----|----------|
| `x`, `y`, `z` | float | Координаты линейных осей |
| `a`, `b`, `c` | float | Координаты поворотных осей |
| `f` | float | Подача (мм/мин) |
| `s` | float | Обороты шпинделя (об/мин) |
| `t` | int | Номер инструмента |
| `i`, `j`, `k` | float | Вектор направления (для 5-оси) |

**Чтение и запись:**

```python
# Чтение текущего значения
current_x = context.registers.x
current_feed = context.registers.f

# Запись нового значения
context.registers.x = 100.5
context.registers.y = 50.0
context.registers.z = 10.0
context.registers.f = 500.0
context.registers.s = 2000
context.registers.t = 5
```

---

#### Объект `context.config` — конфигурация контроллера

```python
# Основная информация
name = context.config.name              # "Siemens 840D"
profile = context.config.machineProfile # "mmill_01"

# Параметры безопасности
safe_z = context.config.safety.retractPlane      # 5.0
max_z = context.config.safety.clearancePlane     # 100.0
max_feed = context.config.safety.maxFeedRate     # 10000.0
max_rpm = context.config.safety.maxSpindleSpeed  # 12000.0

# 5-осевая обработка
enable_5axis = context.config.multiAxis.enableRtcp  # True/False
max_a = context.config.multiAxis.maxA               # 120.0
min_a = context.config.multiAxis.minA               # -120.0
max_b = context.config.multiAxis.maxB               # 360.0
```

**M-коды из конфигурации:**

```python
# Доступ через context.config.mcode
m3 = context.config.mcode.spindleCW      # "M3"
m5 = context.config.mcode.spindleStop    # "M5"
m8 = context.config.mcode.coolantOn      # "M8"
m6 = context.config.mcode.toolChange     # "M6"
m30 = context.config.mcode.programEnd    # "M30"
```

---

#### Объект `context.system` — системные переменные (SYSTEM.*)

Системные переменные хранят состояние постпроцессора:

```python
# Тип движения
motion = context.system.MOTION  # "LINEAR", "RAPID", "CYCLE"
context.system.MOTION = "RAPID"

# Имя регистра шпинделя
spindle_name = context.system.SPINDLE_NAME  # "S"

# Тип технологии
tech_type = context.system.TECHNOLOGY_TYPE  # "MILLING", "TURNING", "ALL"

# Длина инструмента
tool_length = context.system.TOOL_LENGTH

# Тип дуги
circle_type = context.system.CIRCTYPE

# LINTOL (линейный допуск)
lintol = context.system.LINTOL
lintol_linear = context.system.LINTOL_LINEAR
lintol_rotary = context.system.LINTOL_ROTARY
```

**Методы для работы с переменными:**

```python
# Получить значение
value = context.system.GetInt("BLOCK_NUMBER", 1)
value = context.system.GetDouble("FEED_PROG", 100.0)
value = context.system.GetBool("TOOLCHNG", False)
value = context.system["CUSTOM_VAR"]

# Установить значение
context.system.SetInt("BLOCK_NUMBER", 10)
context.system.SetDouble("FEED_PROG", 500.0)
context.system.SetBool("TOOLCHNG", True)
context.system["CUSTOM_VAR"] = "value"
```

---

#### Объект `context.globalVars` — глобальные переменные (GLOBAL.*)

Глобальные переменные сохраняют состояние между вызовами макросов:

```python
# Переменные шпинделя
spindle_def = context.globalVars.SPINDLE_DEF      # "CLW", "CCLW", "OFF"
spindle_rpm = context.globalVars.SPINDLE_RPM      # 1600.0
spindle_block = context.globalVars.SPINDLE_BLOCK  # 0 или 1

# Переменные инструмента
tool = context.globalVars.TOOL                    # текущий номер
ftool = context.globalVars.FTOOL                  # первый инструмент
hval = context.globalVars.HVAL                    # номер корректора
toolchng = context.globalVars.TOOLCHNG            # флаг смены

# Переменные охлаждения
coolant_def = context.globalVars.COOLANT_DEF      # "FLOOD", "MIST", "OFF"
coolant_block = context.globalVars.COOLANT_BLOCK

# Переменные подачи
feed_block = context.globalVars.FEED_BLOCK
feed_prog = context.globalVars.FEED_PROG
feedmode = context.globalVars.FEEDMODE            # "FPM", "FPR", "FIT"

# Переменные движения
linear_type = context.globalVars.LINEAR_TYPE
rapid_type = context.globalVars.RAPID_TYPE

# Стратегии
strategy_rtcp = context.globalVars.STRATEGY_RTCP
strategy_3x_milling = context.globalVars.STRATEGY_3X_MILLING
```

**Методы для работы с переменными:**

```python
# Получить значение
last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
block_num = context.globalVars.GetInt("BLOCK_NUMBER", 1)

# Установить значение
context.globalVars.SetDouble("LAST_FEED", 500.0)
context.globalVars.SetInt("BLOCK_NUMBER", 10)
context.globalVars.SetBool("TOOLCHNG", True)
```

---

### Объект `command` — APT-команда

#### Свойства команды

| Свойство | Тип | Описание | Пример |
|----------|-----|----------|--------|
| `majorWord` | str | Основное слово команды | `"goto"`, `"spindl"` |
| `lineNumber` | int | Номер строки в APT | `42` |
| `numeric` | list[float] | Числовые параметры | `[100.0, 50.0, 10.0]` |
| `strings` | list[str] | Строковые параметры | `["PART_NAME"]` |
| `minorWords` | list[str] | Ключевые слова | `["on", "clw"]` |

**Примеры APT-команд:**

```
GOTO/100, 50, 10
├── majorWord: "goto"
└── numeric: [100.0, 50.0, 10.0]

SPINDL/ON, CLW, 1600
├── majorWord: "spindl"
├── minorWords: ["on", "clw"]
└── numeric: [1600.0]

COOLNT/FLOOD
├── majorWord: "coolnt"
└── minorWords: ["flood"]

LOADTL/5, ADJUST, 1, MILL
├── majorWord: "loadtl"
├── numeric: [5.0, 1.0]
└── minorWords: ["adjust", "mill"]
```

#### Методы команды

```python
# Проверка наличия ключевого слова
if command.hasMinorWord("on"):
    context.write("M3")

if command.hasMinorWord("off"):
    context.write("M5")

# Получение значения с default
x = command.getNumeric(0, 0.0)  # Первый параметр или 0.0
y = command.getNumeric(1, context.registers.y)  # Второй или текущий
name = command.getString(0, "DEFAULT")  # Первая строка или "DEFAULT"

# Проверка наличия параметров
if command.numeric and len(command.numeric) > 0:
    x = command.numeric[0]

if command.minorWords:
    for word in command.minorWords:
        # обработка ключевых слов
```

---

## Примеры макросов

### Пример 1: GOTO — линейное перемещение

**APT:** `GOTO/100, 50, 10`

**Макрос (`base/goto.py`):**

```python
# -*- coding: ascii -*-
# GOTO MACRO - Linear Motion

def execute(context, command):
    """
    Process GOTO linear motion command
    
    APT format: GOTO/X, Y, Z [, I, J, K]
    - X, Y, Z — координаты
    - I, J, K — вектор направления (для 5-оси)
    """
    
    # Проверка наличия координат
    if not command.numeric or len(command.numeric) == 0:
        return
    
    # Получение координат
    x = command.numeric[0] if len(command.numeric) > 0 else context.registers.x
    y = command.numeric[1] if len(command.numeric) > 1 else context.registers.y
    z = command.numeric[2] if len(command.numeric) > 2 else context.registers.z
    
    # Обновление регистров
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z
    
    # Определение типа движения
    motion_type = context.system.MOTION
    is_rapid = (motion_type == "RAPID" or context.currentMotionType == "RAPID")
    
    # Формирование строки G-кода
    if is_rapid:
        gcode = "G0"
        # Сброс типа движения после RAPID
        context.system.MOTION = "LINEAR"
        context.currentMotionType = "LINEAR"
    else:
        gcode = "G1"
    
    # Вывод движения
    line = f"{gcode} X{format_num(x)}"
    if len(command.numeric) > 1:
        line += f" Y{format_num(y)}"
    if len(command.numeric) > 2:
        line += f" Z{format_num(z)}"
    
    context.write(line)
    
    # Вывод подачи (только если изменилась — модально)
    if context.registers.f > 0:
        last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
        if last_feed != context.registers.f:
            context.write(f"F{context.registers.f:.1f}")
            context.globalVars.SetDouble("LAST_FEED", context.registers.f)


def format_num(value):
    """Форматирование числа без лишних нулей"""
    rounded = round(value, 3)
    formatted = str(rounded).rstrip('0').rstrip('.')
    if '.' not in formatted:
        formatted += '.'
    return formatted
```

**Вывод:**
```nc
N10 G1 X100. Y50. Z10.
N12 F500.0
```

---

### Пример 2: RAPID — быстрое перемещение

**APT:** `RAPID/200, 100, 50`

**Макрос (`base/rapid.py`):**

```python
# -*- coding: ascii -*-
# RAPID MACRO - Rapid Positioning

def execute(context, command):
    """
    Process RAPID positioning command
    
    Устанавливает SYSTEM.MOTION = RAPID для следующего перемещения
    """
    
    # Установка типа движения RAPID
    context.system.MOTION = "RAPID"
    context.currentMotionType = "RAPID"
    
    # Если есть координаты — выводим G0 сразу
    if command.numeric and len(command.numeric) > 0:
        x = command.numeric[0] if len(command.numeric) > 0 else context.registers.x
        y = command.numeric[1] if len(command.numeric) > 1 else context.registers.y
        z = command.numeric[2] if len(command.numeric) > 2 else context.registers.z
        
        context.registers.x = x
        context.registers.y = y
        context.registers.z = z
        
        line = f"G0 X{format_num(x)}"
        if len(command.numeric) > 1:
            line += f" Y{format_num(y)}"
        if len(command.numeric) > 2:
            line += f" Z{format_num(z)}"
        
        context.write(line)


def format_num(value):
    rounded = round(value, 3)
    formatted = str(rounded).rstrip('0').rstrip('.')
    if '.' not in formatted:
        formatted += '.'
    return formatted
```

**Вывод:**
```nc
N10 G0 X200. Y100. Z50.
```

---

### Пример 3: SPINDL — управление шпинделем

**APT:** `SPINDL/ON, CLW, 1600`

**Макрос (`base/spindl.py`):**

```python
# -*- coding: ascii -*-
# SPINDL MACRO - Spindle Control

def execute(context, command):
    """
    Process SPINDL spindle control command
    
    APT Examples:
      SPINDL/ON, CLW, 1600    — включить по часовой, 1600 об/мин
      SPINDL/OFF              — выключить
      SPINDL/1200             — установить 1200 об/мин
    """
    
    # Установка оборотов из числовых параметров
    if command.numeric and len(command.numeric) > 0:
        context.globalVars.SPINDLE_RPM = command.numeric[0]
    
    context.registers.s = context.globalVars.SPINDLE_RPM
    
    # Определение состояния шпинделя
    spindle_state = context.globalVars.SPINDLE_DEF
    
    # Обработка ключевых слов
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            
            if word_upper in ["ON", "CLW", "CLOCKWISE"]:
                spindle_state = "CLW"
                context.globalVars.SPINDLE_DEF = "CLW"
                
            elif word_upper in ["CCLW", "CCW", "COUNTER-CLOCKWISE"]:
                spindle_state = "CCLW"
                context.globalVars.SPINDLE_DEF = "CCLW"
                
            elif word_upper == "ORIENT":
                spindle_state = "ORIENT"
                
            elif word_upper == "OFF":
                spindle_state = "OFF"
    
    # Вывод команд в зависимости от состояния
    if spindle_state == "CLW":
        context.write("M3")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write(f"S{int(context.globalVars.SPINDLE_RPM)}")
            
    elif spindle_state == "CCLW":
        context.write("M4")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write(f"S{int(context.globalVars.SPINDLE_RPM)}")
            
    elif spindle_state == "ORIENT":
        context.write("M19")
        
    else:  # OFF
        context.write("M5")
```

**Вывод:**
```nc
N10 M3
N12 S1600
```

---

### Пример 4: FEDRAT — управление подачей

**APT:** `FEDRAT/500`

**Макрос (`base/fedrat.py`):**

```python
# -*- coding: ascii -*-
# FEDRAT MACRO - Feed Rate (MODAL)

def execute(context, command):
    """
    Process FEDRAT feed rate command
    
    Подача МОДАЛЬНА — выводится только при изменении
    """
    
    # Проверка наличия параметров
    if not command.numeric or len(command.numeric) == 0:
        return
    
    feed = command.numeric[0]
    
    # Обновление регистра
    context.registers.f = feed
    
    # Проверка на изменение (модальность)
    last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
    if last_feed == feed:
        return  # Та же подача — не выводим
    
    # Подача изменилась — выводим и запоминаем
    context.globalVars.SetDouble("LAST_FEED", feed)
    context.write(f"F{round(feed, 1)}")
```

**Вывод:**
```nc
N10 F500.0
```

---

### Пример 5: COOLNT — управление охлаждением

**APT:** `COOLNT/FLOOD`

**Макрос (`base/coolnt.py`):**

```python
# -*- coding: ascii -*-
# COOLNT MACRO - Coolant Control

def execute(context, command):
    """
    Process COOLNT coolant control command
    
    APT Examples:
      COOLNT/ON       — включить охлаждение
      COOLNT/FLOOD    — включить жидкостное
      COOLNT/MIST     — включить туман
      COOLNT/OFF      — выключить
    """
    
    coolant_state = context.globalVars.COOLANT_DEF
    
    # Обработка ключевых слов
    if command.minorWords:
        for word in command.minorWords:
            word_upper = word.upper()
            
            if word_upper in ["ON", "FLOOD"]:
                coolant_state = "FLOOD"
                context.globalVars.COOLANT_DEF = "FLOOD"
                
            elif word_upper == "MIST":
                coolant_state = "MIST"
                context.globalVars.COOLANT_DEF = "MIST"
                
            elif word_upper == "OFF":
                coolant_state = "OFF"
                context.globalVars.COOLANT_DEF = "OFF"
    
    # Вывод команд
    if coolant_state == "FLOOD":
        context.write("M8")
    elif coolant_state == "MIST":
        context.write("M7")
    else:  # OFF
        context.write("M9")
```

**Вывод:**
```nc
N10 M8
```

---

### Пример 6: LOADTL — смена инструмента (для MMILL)

**APT:** `LOADTL/5, ADJUST, 1, MILL`

**Макрос (`mmill/loadtl.py`):**

```python
# -*- coding: ascii -*-
# MMILL LOADTL MACRO - Tool Change

_block_number = 70  # Начальный номер блока

def execute(context, command):
    """
    Process LOADTL for MMILL
    
    Добавляет специфичные команды:
    - RTCPON после смены
    - M101H0 (зажим головы)
    - G0 B0 (поворот оси B)
    """
    
    global _block_number
    
    # Проверка на одинаковый инструмент
    if context.globalVars.TOOLCHG_IGNORE_SAME:
        new_tool = int(command.numeric[0]) if command.numeric else 0
        if context.globalVars.TOOL == new_tool:
            return  # Тот же инструмент — пропускаем
    
    # Получение номера инструмента
    if command.numeric:
        context.globalVars.TOOL = int(command.numeric[0])
    
    context.globalVars.HVAL = 1
    
    # Получение скорости шпинделя
    spindle_speed = command.numeric[1] if len(command.numeric) > 1 else 1600
    context.registers.s = spindle_speed
    
    # Вывод команд смены инструмента
    context.write(f"N{_block_number} T{context.globalVars.TOOL}")
    _block_number += 10
    
    context.write(f"N{_block_number} D1")
    _block_number += 10
    
    context.write(f"N{_block_number} M6")
    _block_number += 10
    
    # Специфичные команды MMILL
    context.write(f"N{_block_number} G0 B0")
    _block_number += 10
    
    context.write(f"N{_block_number} M101H0")
    _block_number += 10
    
    context.write(f"N{_block_number} RTCPON")
    _block_number += 10
    
    # Включение шпинделя
    context.write(f"N{_block_number} S{int(spindle_speed)} M3")
    _block_number += 10
    
    # Установка флагов
    context.globalVars.TOOLCHNG = 1
    context.globalVars.FTOOL = context.globalVars.TOOL
```

**Вывод:**
```nc
N70 T5
N80 D1
N90 M6
N100 G0 B0
N110 M101H0
N120 RTCPON
N130 S1600 M3
```

---

### Пример 7: INIT — инициализация программы

**APT:** `PARTNO/DETAIL_NAME`

**Макрос (`mmill/init.py`):**

```python
# MMILL INIT MACRO

def execute(context, command):
    """
    Инициализация программы для MMILL
    
    Выводит:
    - Заголовок программы
    - Начальные G-коды
    - CYCLE800 для 5-оси
    """
    
    # Инициализация нумерации блоков
    context.globalVars.SetInt("BLOCK_NUMBER", 1)
    context.globalVars.SetInt("BLOCK_INCREMENT", 2)
    
    # Инициализация модальности подачи
    context.globalVars.SetDouble("LAST_FEED", 0.0)
    
    # Заголовок программы
    header = context.config.header
    if header and header.enabled:
        for line in header.lines:
            context.write(line.format(
                company="Company Name",
                machine="Machine Name",
                inputFile=context.config.getParameterString("inputFile", "unknown"),
                dateTime=context.config.getParameterString("dateTime", "unknown")
            ), suppress_block=True)
    
    # Начальные блоки
    context.write("G54 G40 G90 G94 CUT2DF G17")
    context.write("TRANS")
    context.write("RTCPOF")
    
    # CYCLE800 для 5-оси
    cycle = context.machine.config.fiveAxis.cycle800
    params = cycle.parameters
    context.write('CYCLE800({},"{}",{},{},{},{},{},{},{},{},{},{},{},{},{},{})'.format(
        params['mode'], params['table'], params['rotation'], params['plane'],
        params.get('x', 0), params.get('y', 0), params.get('z', 0),
        params.get('a', 0), params.get('b', 0), params.get('c', 0),
        params.get('dx', 0), params.get('dy', 0), params.get('dz', 0),
        params['direction'], params['feed'], params['maxFeed']
    ))
    
    context.write("G64 SOFT FFWON")
    context.write(context.machine.config.head.clampCommand + "; TCB6 HEAD")
```

**Вывод:**
```nc
(Program header...)
N1 G54 G40 G90 G94 CUT2DF G17
N2 TRANS
N3 RTCPOF
N4 CYCLE800(...)
N5 G64 SOFT FFWON
N6 M101; TCB6 HEAD
```

---

### Пример 8: FINI — завершение программы

**APT:** `FINI`

**Макрос (`mmill/fini.py`):**

```python
# -*- coding: ascii -*-
# MMILL FINI MACRO - End of Program

def execute(context, command):
    """
    Завершение программы для MMILL
    
    Выводит:
    - Отвод по Z
    - Выключение шпинделя и охлаждения
    - Выключение RTCP
    - Конец программы M30
    """
    
    # Отвод по Z
    context.write("G0 Z100.")
    
    # Выключение шпинделя
    context.write("M5")
    
    # Выключение охлаждения
    context.write("M9")
    
    # Выключение RTCP
    context.write("RTCPOF")
    
    # Конец программы
    context.write("M30")
```

**Вывод:**
```nc
N100 G0 Z100.
N102 M5
N104 M9
N106 RTCPOF
N108 M30
```

---

## Продвинутые темы

### Модальные команды

**Модальность** означает, что команда действует до отмены или изменения.

```python
# Пример модальной подачи
def execute(context, command):
    feed = command.numeric[0]
    
    # Проверяем, изменилась ли подача
    last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
    
    if last_feed == feed:
        return  # Не выводим — уже активна
    
    # Выводим только при изменении
    context.globalVars.SetDouble("LAST_FEED", feed)
    context.write(f"F{feed:.1f}")
```

**Модальные G-коды:**
- G0/G1 — тип движения
- G17/G18/G19 — плоскость дуги
- G90/G91 — абсолютные/относительные координаты
- G54-G59 — рабочие системы координат

---

### 5-осевая обработка (IJK → ABC)

Для 5-осевых станков вектор направления (I,J,K) конвертируется в углы поворота (A,B,C).

```python
import math

def ijk_to_abc(i, j, k):
    """
    Конвертация IJK вектора в ABC углы
    
    Для Siemens 840D:
    - A = вращение вокруг X
    - B = вращение вокруг Y
    """
    # A угол (вокруг X)
    a = math.degrees(math.atan2(j, k))
    
    # B угол (вокруг Y)
    b = math.degrees(math.atan2(i, math.sqrt(j*j + k*k)))
    
    # Нормализация к 0-360
    if a < 0:
        a += 360
    if b < 0:
        b += 360
    
    return round(a, 3), round(b, 3), 0.0


# Использование в макросе GOTO
def execute(context, command):
    # Получение IJK
    i = command.numeric[3] if len(command.numeric) > 3 else None
    j = command.numeric[4] if len(command.numeric) > 4 else None
    k = command.numeric[5] if len(command.numeric) > 5 else None
    
    if i is not None and j is not None and k is not None:
        # Конвертация в ABC
        a, b, c = ijk_to_abc(i, j, k)
        
        context.registers.a = a
        context.registers.b = b
        
        # Вывод с поворотными осями
        context.write(f"G1 X{x:.3f} Y{y:.3f} Z{z:.3f} A{a:.3f} B{b:.3f}")
```

---

### Работа с конфигурацией

**Чтение пользовательских параметров:**

```python
# Из JSON конфигурации станка
use_soft_start = context.config.getParameterBool("softStart", False)
feed_override = context.config.getParameterDouble("feedOverride", 100.0)
custom_code = context.config.getParameterString("footerCode", "M30")

# Использование в макросе
if use_soft_start:
    context.write("G04 P1.0")  # Пауза 1 секунда
```

**Пример конфигурации (`configs/machines/my_machine.json`):**

```json
{
  "name": "My Custom Machine",
  "machineProfile": "custom_01",
  "customParameters": {
    "softStart": true,
    "feedOverride": 120.0,
    "useCustomFeature": true,
    "footerCode": "M30"
  },
  "customGCodes": {
    "rapidOverride": "G00.1"
  },
  "customMCodes": {
    "toolClamp": "M10",
    "toolUnclamp": "M11"
  }
}
```

---

### Глобальные переменные

**Создание и использование:**

```python
# Установка значения
context.globalVars.SetDouble("CUSTOM_FEED", 500.0)
context.globalVars.SetInt("CUSTOM_COUNTER", 10)
context.globalVars.SetBool("CUSTOM_FLAG", True)
context.globalVars["CUSTOM_STRING"] = "value"

# Чтение значения
feed = context.globalVars.GetDouble("CUSTOM_FEED", 0.0)
counter = context.globalVars.GetInt("CUSTOM_COUNTER", 0)
flag = context.globalVars.GetBool("CUSTOM_FLAG", False)
value = context.globalVars["CUSTOM_STRING"]
```

---

## Отладка

### Вывод отладочной информации

```python
def execute(context, command):
    # Вывод информации о команде
    context.comment(f"DEBUG: majorWord={command.majorWord}")
    context.comment(f"DEBUG: numeric={command.numeric}")
    context.comment(f"DEBUG: minorWords={command.minorWords}")
    context.comment(f"DEBUG: lineNumber={command.lineNumber}")
    
    # Вывод текущих регистров
    context.comment(f"DEBUG: X={context.registers.x}")
    context.comment(f"DEBUG: F={context.registers.f}")
    
    # Вывод системных переменных
    context.comment(f"DEBUG: MOTION={context.system.MOTION}")
```

### Запуск с debug-флагом

```bash
dotnet run -- -i input.apt -o output.nc -c siemens --debug
```

### Чтение логов постпроцессора

Логи выводятся в консоль при запуске с флагом `--debug`.

---

## Справочник

### Все доступные команды APT

| Команда | Описание | Макрос |
|---------|----------|--------|
| `GOTO` | Линейное перемещение | `base/goto.py` |
| `RAPID` | Быстрое перемещение | `base/rapid.py` |
| `SPINDL` | Управление шпинделем | `base/spindl.py` |
| `COOLNT` | Управление охлаждением | `base/coolnt.py` |
| `FEDRAT` | Управление подачей | `base/fedrat.py` |
| `LOADTL` | Смена инструмента | `mmill/loadtl.py` |
| `PARTNO` | Начало программы | `mmill/init.py` |
| `FINI` | Конец программы | `mmill/fini.py` |
| `RTCP` | Вкл/выкл RTCP | `mmill/rtcp.py` |
| `ROTATE` | Поворот стола | `mmill/rotabl.py` |

---

### Все параметры context

| Параметр | Тип | Описание |
|----------|-----|----------|
| `context.registers` | PythonRegisters | Регистры станка |
| `context.config` | PythonConfig | Конфигурация контроллера |
| `context.machine` | PythonMachineState | Состояние машины |
| `context.system` | PythonSystemVariables | Системные переменные |
| `context.globalVars` | PythonGlobalVariables | Глобальные переменные |
| `context.currentFeed` | float | Текущая подача |
| `context.currentMotionType` | str | Текущий тип движения |

---

### Все параметры command

| Параметр | Тип | Описание |
|----------|-----|----------|
| `command.majorWord` | str | Основное слово команды |
| `command.lineNumber` | int | Номер строки в APT |
| `command.numeric` | list[float] | Числовые параметры |
| `command.strings` | list[str] | Строковые параметры |
| `command.minorWords` | list[str] | Ключевые слова |

| Метод | Описание |
|-------|----------|
| `hasMinorWord(word)` | Проверка ключевого слова |
| `getNumeric(index, default)` | Получить число с default |
| `getString(index, default)` | Получить строку с default |

---

### Форматы чисел

```python
# Округление
rounded = context.round(3.14159, 2)  # 3.14

# Форматирование
formatted = context.format(100.5, "F2")  # "100.50"
formatted = context.format(100.5, "F3")  # "100.500"

# Своё форматирование
def format_num(value, decimals=3):
    rounded = round(value, decimals)
    formatted = str(rounded).rstrip('0').rstrip('.')
    if '.' not in formatted:
        formatted += '.'
    return formatted
```

---

## Частые ошибки

### Ошибка 1: Макрос не загружается

**Симптомы:**
- G-код не генерируется
- Нет ошибок в консоли

**Причины:**
1. Файл не в папке `macros/python/`
2. Нет функции `execute(context, command)`
3. Ошибка синтаксиса Python

**Решение:**
```bash
# Проверьте структуру
macros/python/
└── your_macro.py  # Должен быть здесь

# Проверьте функцию
def execute(context, command):  # Должна быть такая функция
    pass
```

---

### Ошибка 2: IndexError при доступе к параметрам

**Симптомы:**
```
IndexError: list index out of range
```

**Причина:** Доступ к несуществующему элементу списка

**Неправильно:**
```python
x = command.numeric[0]  # Ошибка если список пустой
```

**Правильно:**
```python
if command.numeric and len(command.numeric) > 0:
    x = command.numeric[0]
else:
    x = context.registers.x  # Значение по умолчанию
```

Или используйте метод с default:
```python
x = command.getNumeric(0, context.registers.x)
```

---

### Ошибка 3: NoneType error

**Симптомы:**
```
AttributeError: 'NoneType' object has no attribute...
```

**Причина:** Попытка доступа к свойству None

**Неправильно:**
```python
for word in command.minorWords:  # minorWords может быть None
    if word.upper() == "ON":
        ...
```

**Правильно:**
```python
if command.minorWords:  # Проверка на None
    for word in command.minorWords:
        if word.upper() == "ON":
            ...
```

---

### Ошибка 4: Не обновляются регистры

**Симптомы:**
- Координаты не сохраняются между командами
- Следующие макросы используют старые значения

**Решение:** Всегда обновляйте регистры после изменения:
```python
# Получение новых координат
x = command.numeric[0]
y = command.numeric[1]
z = command.numeric[2]

# Обновление регистров
context.registers.x = x
context.registers.y = y
context.registers.z = z
```

---

### Ошибка 5: Дублирование G-кода

**Симптомы:**
- Одинаковые строки выводятся несколько раз

**Причина:** Отсутствие проверки модальности

**Решение:**
```python
# Для подачи
last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
if last_feed == feed:
    return  # Не выводим

context.globalVars.SetDouble("LAST_FEED", feed)
context.write(f"F{feed:.1f}")

# Для типа движения
if context.system.MOTION == "LINEAR":
    # Уже линейное движение — не выводим G1
    pass
```

---

### Ошибка 6: Неправильная конвертация IJK→ABC

**Симптомы:**
- Поворотные оси двигаются неправильно
- Станок выдает ошибку

**Причины:**
1. Неправильная формула конвертации
2. Не учтена кинематика станка

**Решение:** Используйте проверенную функцию:
```python
import math

def ijk_to_abc(i, j, k):
    a = math.degrees(math.atan2(j, k))
    b = math.degrees(math.atan2(i, math.sqrt(j*j + k*k)))
    
    # Нормализация
    if a < 0:
        a += 360
    if b < 0:
        b += 360
    
    return round(a, 3), round(b, 3), 0.0
```

---

### Ошибка 7: Python не найден

**Симптомы:**
```
Error: Python runtime not found
```

**Причины:**
1. Python не установлен
2. Установлена неподдерживаемая версия (Python 3.13+)

**Решение:**
1. Установите Python 3.8-3.12
2. Или используйте встроенный Python runtime

---

## Советы и лучшие практики

### 1. Всегда проверяйте наличие параметров

```python
# Плохо
x = command.numeric[0]

# Хорошо
if command.numeric and len(command.numeric) > 0:
    x = command.numeric[0]
```

### 2. Используйте значения по умолчанию

```python
x = command.getNumeric(0, context.registers.x)
name = command.getString(0, "DEFAULT")
```

### 3. Обновляйте регистры

```python
context.registers.x = x
context.registers.y = y
context.registers.z = z
```

### 4. Проверяйте безопасность

```python
if z < context.config.safety.retractPlane:
    context.warning(f"Z={z:.3f} ниже безопасной высоты!")
    z = context.config.safety.retractPlane
```

### 5. Комментируйте сложный код

```python
# Конвертация IJK в ABC для 5-оси
# Используем atan2 для правильного определения квадранта
a = math.degrees(math.atan2(j, k))
```

### 6. Тестируйте на простых примерах

```
# Простой тестовый APT
PARTNO/TEST
GOTO/0, 0, 0
GOTO/100, 50, 10
SPINDL/ON, CLW, 1600
FEDRAT/500
FINI
```

### 7. Ведите журнал изменений

```python
# -*- coding: ascii -*-
# GOTO MACRO - Linear Motion
# Version: 1.2
# Changes:
#   1.2 - Добавлена поддержка 5-оси
#   1.1 - Исправлена модальность подачи
#   1.0 - Начальная версия
```

---

## Дополнительные ресурсы

- [ARCHITECTURE.md](ARCHITECTURE.md) — Архитектура постпроцессора
- [CUSTOMIZATION_GUIDE.md](CUSTOMIZATION_GUIDE.md) — Руководство по настройке
- [QUICKSTART.md](QUICKSTART.md) — Быстрый старт для новичков
