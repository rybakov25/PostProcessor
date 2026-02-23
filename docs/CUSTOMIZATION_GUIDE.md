# Руководство по настройке постпроцессора

> **Полное руководство по кастомизации** — конфигурации, профили станков, пользовательские макросы
>
> **Универсальный постпроцессор** — поддерживает любое оборудование через систему конфигураций и макросов

---

## 📋 Оглавление

1. [Обзор](#обзор)
2. [Структура проекта](#структура-проекта)
3. [Создание конфигурации контроллера](#создание-конфигурации-контроллера)
4. [Создание профиля станка](#создание-профиля-станка)
5. [Создание Python макросов](#создание-python-макросов)
6. [Использование StateCache в макросах](#использование-statecache-в-макросах)
7. [Использование CycleCache](#использование-cyclecache)
8. [Форматирование через NumericNCWord](#форматирование-через-numericncword)
9. [Стиль комментариев через TextNCWord](#стиль-комментариев-через-textncword)
10. [Использование пользовательских параметров](#использование-пользовательских-параметров)
11. [Примеры настроек](#примеры-настроек)
12. [Отладка](#отладка)

---

## Обзор (v1.1.0)

Постпроцессор поддерживает **гибкую настройку** для любого оборудования через:

| Компонент | Описание | Формат |
|-----------|----------|--------|
| **Конфигурации контроллеров** | Параметры стоек ЧПУ (Siemens, Fanuc, Heidenhain...) | JSON |
| **Профили станков** | Специфика конкретных станков | JSON |
| **Python макросы** | Логика обработки APT-команд | Python |
| **StateCache** | NEW: Кэш состояний LAST_* | C# + Python |
| **CycleCache** | NEW: Кэширование циклов | C# + Python |
| **NumericNCWord** | NEW: Форматирование из конфига | C# + Python |
| **TextNCWord** | NEW: Стиль комментариев | C# + Python |

**Принцип приоритета макросов:**
```
user/{machine}/{macro}.py  → Highest priority (ваши переопределения)
{machine}/{macro}.py       → Medium priority (специфичные для станка)
base/{macro}.py            → Lowest priority (базовые универсальные)
```

---

## Структура проекта

```
PostProcessor/
├── configs/
│   ├── controllers/           # Конфигурации контроллеров
│   │   ├── siemens/           # Siemens 840D, 840D sl
│   │   ├── fanuc/             # Fanuc 31i, 32i, 35i
│   │   ├── heidenhain/        # Heidenhain TNC 640, 620
│   │   ├── haas/              # Haas NGC
│   │   └── {your_controller}/ # Ваш контроллер
│   │       └── base.json
│   │
│   └── machines/              # Профили станков
│       ├── dmu50_3axis.json   # DMG Mori 3-осевой
│       ├── dmu50_5axis.json   # DMG Mori 5-осевой
│       ├── nlx2500_01.json    # Токарный Mori Seiki
│       └── {your_machine}.json # Ваш станок
│
├── macros/
│   └── python/
│       ├── base/              # Базовые макросы (универсальные)
│       ├── {controller}/      # Макросы для контроллера
│       ├── {machine}/         # Макросы для станка
│       └── user/              # Ваши макросы (приоритет)
│           └── {machine}/     # Переопределения для станка
│
└── docs/
    ├── CUSTOMIZATION_GUIDE.md # Этот файл
    ├── PYTHON_MACROS_GUIDE.md # Руководство по макросам
    ├── QUICKSTART.md          # Быстрый старт
    └── ARCHITECTURE.md        # Архитектура
```

---

## Создание конфигурации контроллера

### Шаг 1: Скопируйте существующую конфигурацию

```bash
# Создайте копию для модификации
cp configs/controllers/siemens/840d.json configs/controllers/siemens/custom.json
```

### Шаг 2: Отредактируйте конфигурацию

**Полная структура JSON:**

```json
{
  "name": "Siemens 840D Custom",
  "machineType": "Milling",
  "version": "1.0",
  
  "registerFormats": {
    "X": {
      "address": "X",
      "format": "F4.3",
      "isModal": true,
      "suppressLeadingZero": false,
      "suppressTrailingZero": true
    },
    "Y": { "address": "Y", "format": "F4.3", "isModal": true },
    "Z": { "address": "Z", "format": "F4.3", "isModal": true },
    "A": { "address": "A", "format": "F3.3", "isModal": true },
    "B": { "address": "B", "format": "F3.3", "isModal": true },
    "C": { "address": "C", "format": "F3.3", "isModal": true },
    "F": { "address": "F", "format": "F3.1", "isModal": false },
    "S": { "address": "S", "format": "F0", "isModal": false },
    "T": { "address": "T", "format": "F0", "isModal": false }
  },
  
  "functionCodes": {
    "rapid": { "code": "G00", "group": "MOTION", "isModal": true },
    "linear": { "code": "G01", "group": "MOTION", "isModal": true },
    "cw_arc": { "code": "G02", "group": "MOTION", "isModal": true },
    "ccw_arc": { "code": "G03", "group": "MOTION", "isModal": true },
    "dwell": { "code": "G04", "group": "MOTION" },
    "exact_stop": { "code": "G09", "group": "MOTION" },
    "plane_xy": { "code": "G17", "group": "PLANE" },
    "plane_xz": { "code": "G18", "group": "PLANE" },
    "plane_yz": { "code": "G19", "group": "PLANE" },
    "units_mm": { "code": "G21", "group": "UNITS" },
    "units_inch": { "code": "G20", "group": "UNITS" },
    "absolute": { "code": "G90", "group": "DISTANCE" },
    "incremental": { "code": "G91", "group": "DISTANCE" },
    "spindle_cw": { "code": "M03", "group": "SPINDLE" },
    "spindle_ccw": { "code": "M04", "group": "SPINDLE" },
    "spindle_stop": { "code": "M05", "group": "SPINDLE" },
    "coolant_flood": { "code": "M08", "group": "COOLANT" },
    "coolant_mist": { "code": "M07", "group": "COOLANT" },
    "coolant_off": { "code": "M09", "group": "COOLANT" },
    "tool_change": { "code": "M06", "group": "TOOL" }
  },
  
  "workCoordinateSystems": {
    "G54": { "offset": 1 },
    "G55": { "offset": 2 },
    "G56": { "offset": 3 },
    "G57": { "offset": 4 },
    "G58": { "offset": 5 },
    "G59": { "offset": 6 }
  },
  
  "drillingCycles": {
    "drill": { "code": "CYCLE81", "parameters": ["RTP", "RFP", "SDIS", "DP", "DPR"] },
    "peck": { "code": "CYCLE83", "parameters": ["RTP", "RFP", "SDIS", "DP", "DPR", "FDEP", "FDPR", "DAM", "DTS", "FRF", "VARI"] },
    "ream": { "code": "CYCLE85", "parameters": ["RTP", "RFP", "SDIS", "DP", "DPR", "DTB", "SDR", "FDEP", "FDPR", "DAM", "DTS", "FRF", "VARI"] }
  },
  
  "safety": {
    "clearancePlane": 100.0,
    "retractPlane": 5.0,
    "maxFeedRate": 10000.0,
    "maxSpindleSpeed": 12000.0,
    "rapidOverride": 0.5,
    "checkAxisLimits": true,
    "checkFeedRate": true,
    "checkSpindleSpeed": true
  },
  
  "multiAxis": {
    "enableRtcp": true,
    "strategy": "cartesian",
    "maxA": 120.0,
    "minA": -120.0,
    "maxB": 360.0,
    "minB": 0.0,
    "maxC": 360.0,
    "minC": 0.0,
    "rtcp": {
      "on": "RTCPON",
      "off": "RTCPOF"
    },
    "cycle800": {
      "enabled": true,
      "parameters": {
        "mode": 1,
        "table": "TABLE",
        "rotation": "ROTATION",
        "plane": "Z",
        "direction": 1,
        "feed": 5000,
        "maxFeed": 10000
      }
    }
  },
  
  "header": {
    "enabled": true,
    "lines": [
      "; Program: {inputFile}",
      "; Machine: {machine}",
      "; Date: {dateTime}",
      "; Company: {company}"
    ]
  },
  
  "footer": {
    "enabled": true,
    "lines": [
      "M30"
    ]
  },
  
  "customParameters": {
    "useCustomFeature": true,
    "feedOverride": 100.0,
    "softStart": false,
    "toolChangeHeight": 200.0
  },
  
  "customGCodes": {
    "rapidOverride": "G00.1",
    "workOffset": "G54.1P"
  },
  
  "customMCodes": {
    "toolClamp": "M10",
    "toolUnclamp": "M11",
    "palletChange": "M60"
  },
  
  "axisLimits": {
    "XMin": 0,
    "XMax": 2000,
    "YMin": 0,
    "YMax": 1000,
    "ZMin": -500,
    "ZMax": 500,
    "AMin": -120,
    "AMax": 120,
    "BMin": 0,
    "BMax": 360
  },
  
  "blockNumbering": {
    "enabled": true,
    "start": 1,
    "increment": 2,
    "prefix": "N"
  }
}
```

### Описание разделов

#### registerFormats

Форматирование регистров (координат и параметров):

| Поле | Описание | Пример |
|------|----------|--------|
| `address` | Адрес регистра | "X", "F", "S" |
| `format` | Формат числа | "F4.3" (4 цифры, 3 после точки) |
| `isModal` | Модальность | true/false |

#### functionCodes

G-коды и M-коды контроллера:

```json
"functionCodes": {
  "rapid": { "code": "G00" },
  "linear": { "code": "G01" },
  "spindle_cw": { "code": "M03" }
}
```

#### safety

Параметры безопасности:

| Параметр | Описание | Default |
|----------|----------|---------|
| `clearancePlane` | Плоскость безопасности | 100.0 |
| `retractPlane` | Плоскость отвода | 5.0 |
| `maxFeedRate` | Максимальная подача | 10000.0 |
| `maxSpindleSpeed` | Макс. обороты шпинделя | 12000.0 |

#### multiAxis

Параметры 5-осевой обработки:

| Параметр | Описание |
|----------|----------|
| `enableRtcp` | Включить RTCP (TCP) |
| `strategy` | Стратегия (cartesian, joint) |
| `maxA`, `minA` | Ограничения оси A |
| `maxB`, `minB` | Ограничения оси B |

#### customParameters

Пользовательские параметры для макросов:

```json
"customParameters": {
  "softStart": true,
  "feedOverride": 120.0
}
```

Использование в макросе:

```python
soft_start = context.config.getParameterBool("softStart", False)
feed_override = context.config.getParameterDouble("feedOverride", 100.0)
```

---

## Создание профиля станка

### Шаг 1: Создайте файл профиля

Создайте `configs/machines/my_machine.json`:

```json
{
  "name": "My Custom Machine",
  "machineProfile": "my_machine_01",
  "controller": "siemens/840d",
  
  "axisLimits": {
    "XMin": 0,
    "XMax": 500,
    "YMin": 0,
    "YMax": 400,
    "ZMin": -300,
    "ZMax": 50,
    "AMin": -120,
    "AMax": 120,
    "BMin": 0,
    "BMax": 360
  },
  
  "head": {
    "type": "TCB6",
    "clampCommand": "M101",
    "unclampCommand": "M102"
  },
  
  "table": {
    "type": "rotary",
    "axes": ["B", "C"]
  },
  
  "fiveAxis": {
    "enableRtcp": true,
    "rtcp": {
      "on": "RTCPON",
      "off": "RTCPOF"
    },
    "cycle800": {
      "enabled": true,
      "parameters": {
        "mode": 1,
        "table": "TABLE",
        "rotation": "ROTATION",
        "plane": "Z",
        "x": 0,
        "y": 0,
        "z": 0,
        "a": 0,
        "b": 0,
        "c": 0,
        "dx": 0,
        "dy": 0,
        "dz": 0,
        "direction": 1,
        "feed": 5000,
        "maxFeed": 10000
      }
    }
  },
  
  "toolChanger": {
    "type": "manual",
    "position": {
      "X": 0,
      "Y": 0,
      "Z": 200
    },
    "clampCommand": "M10",
    "unclampCommand": "M11"
  },
  
  "customParameters": {
    "softStart": true,
    "toolChangeHeight": 200.0,
    "enableChipConveyor": false,
    "palletSystem": false
  },
  
  "customGCodes": {
    "safeRapid": "G00.1"
  },
  
  "customMCodes": {
    "toolClamp": "M10",
    "toolUnclamp": "M11",
    "chipConveyorForward": "M301",
    "chipConveyorReverse": "M302"
  }
}
```

### Описание разделов

#### axisLimits

Ограничения перемещения по осям:

```json
"axisLimits": {
  "XMin": 0,
  "XMax": 500,
  ...
}
```

#### head

Параметры шпиндельной головы:

| Параметр | Описание |
|----------|----------|
| `type` | Тип головы (TCB6, VK, VO...) |
| `clampCommand` | Команда зажима |
| `unclampCommand` | Команда разжима |

#### fiveAxis

Параметры 5-осевой обработки:

| Параметр | Описание |
|----------|----------|
| `enableRtcp` | Включить RTCP |
| `rtcp.on/off` | Команды вкл/выкл |
| `cycle800` | Параметры цикла поворота |

#### toolChanger

Параметры смены инструмента:

| Параметр | Описание |
|----------|----------|
| `type` | Тип (manual, automatic, turret) |
| `position` | Позиция смены |
| `clampCommand` | Команда зажима |

---

## Создание Python макросов

### Базовый шаблон

Создайте файл `macros/python/user/my_macro.py`:

```python
# -*- coding: ascii -*-
# MY_MACRO - Описание макроса

def execute(context, command):
    """
    Документация макроса
    
    Args:
        context: Объект контекста
        command: Объект APT-команды
    """
    # Проверка параметров
    if not command.numeric:
        return
    
    # Логика макроса
    value = command.numeric[0]
    context.write(f"G01 X{value:.3f}")
```

### Пример: Кастомный макрос подачи

```python
# -*- coding: ascii -*-
# CUSTOM_FEED - Подача с проверкой

def execute(context, command):
    """
    FEDRAT с проверкой максимального значения
    
    APT: FEDRAT/500
    """
    if not command.numeric:
        return
    
    feed = command.numeric[0]
    
    # Проверка на максимум
    max_feed = context.config.safety.maxFeedRate
    if feed > max_feed:
        context.warning(f"Подача {feed} превышает максимум {max_feed}")
        feed = max_feed
    
    context.registers.f = feed
    
    # Модальность
    last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
    if last_feed == feed:
        return
    
    context.globalVars.SetDouble("LAST_FEED", feed)
    context.write(f"F{feed:.1f}")
```

### Пример: Макрос с использованием пользовательских параметров

```python
# -*- coding: ascii -*-
# SOFT_START_SPINDLE - Мягкий старт шпинделя

def execute(context, command):
    """
    SPINDL с мягким стартом
    
    APT: SPINDL/ON, CLW, 1600
    """
    rpm = command.numeric[0] if command.numeric else 0
    
    # Проверка пользовательского параметра
    soft_start = context.config.getParameterBool("softStart", False)
    
    if soft_start and rpm > 1000:
        # Старт на низких оборотах
        context.write("M3")
        context.write(f"S{int(rpm * 0.5)}")
        context.write("G04 P1.0")  # Пауза 1 сек
        context.write(f"S{int(rpm)}")
    else:
        # Обычный старт
        context.write("M3")
        if rpm > 0:
            context.write(f"S{int(rpm)}")
```

---

## Использование StateCache в макросах

StateCache предоставляет кэширование переменных для модального вывода.

### Пример: модальная подача

```python
# -*- coding: ascii -*-
def execute(context, command):
    feed = command.getNumeric(0, 0)
    
    # Проверка изменения через кэш
    if context.cacheHasChanged("LAST_FEED", feed):
        context.registers.f = feed
        context.writeBlock()
        context.cacheSet("LAST_FEED", feed)
```

### Пример: кэш инструмента

```python
# -*- coding: ascii -*-
def execute(context, command):
    tool_num = command.getNumeric(0, 0)
    
    if context.cacheHasChanged("LAST_TOOL", tool_num):
        context.comment(f"Tool {tool_num}")
        context.registers.t = tool_num
        context.writeBlock()
        context.cacheSet("LAST_TOOL", tool_num)
```

### Методы StateCache

| Метод | Описание |
|-------|----------|
| `cacheGet(key, default)` | Получить значение |
| `cacheSet(key, value)` | Установить значение |
| `cacheHasChanged(key, value)` | Проверить изменение |
| `cacheGetOrSet(key, default)` | Получить или установить |
| `cacheReset(key)` | Сбросить значение |
| `cacheResetAll()` | Сбросить весь кэш |

---

## Использование CycleCache

CycleCache автоматически выбирает: полное определение цикла или только вызов.

### Пример: CYCLE800

```python
# -*- coding: ascii -*-
def execute(context, command):
    params = {
        'MODE': 1,
        'TABLE': 'TABLE1',
        'X': 100.0,
        'Y': 200.0,
        'Z': 50.0,
        'A': 0.0,
        'B': 45.0,
        'C': 0.0
    }
    
    # Умный вывод
    context.cycleWriteIfDifferent("CYCLE800", params)
```

### Результат

```nc
; Первый вызов (полное определение)
CYCLE800(MODE=1, TABLE="TABLE1", X=100.000, Y=200.000, Z=50.000, A=0.000, B=45.000, C=0.000)

; Второй вызов (те же параметры - только вызов)
CYCLE800()
```

### Методы CycleCache

| Метод | Описание |
|-------|----------|
| `cycleWriteIfDifferent(name, params)` | Записать если отличается |
| `cycleReset(name)` | Сбросить кэш |
| `cycleGetCache(name)` | Получить кэш |

---

## Форматирование через NumericNCWord

NumericNCWord предоставляет форматирование из JSON-конфига.

### Пример: установка координаты

```python
# -*- coding: ascii -*-
def execute(context, command):
    x = command.getNumeric(0, 0)
    
    # Установка с форматированием из конфига
    context.setNumericValue('X', x)
    
    # Получение отформатированной строки
    xStr = context.getFormattedValue('X')  # "X100.500"
    
    context.writeBlock()
```

### Конфигурация

```json
{
  "formatting": {
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true,
      "trailingZeros": false
    },
    "feedrate": {
      "decimals": 1,
      "prefix": "F"
    }
  }
}
```

---

## Стиль комментариев через TextNCWord

TextNCWord предоставляет комментарии со стилем из конфига.

### Пример

```python
# -*- coding: ascii -*-
def execute(context, command):
    # Автоматически использует стиль из конфига
    context.comment("Начало операции")
    
    # Siemens: (Начало операции)
    # Haas: ; Начало операции
```

### Конфигурация стиля

```json
{
  "formatting": {
    "comments": {
      "type": "parentheses",  // parentheses | semicolon | both
      "maxLength": 128,
      "transliterate": false
    }
  }
}
```

### Стили

| Стиль | type | Результат |
|-------|------|-----------|
| Parentheses | `"parentheses"` | `(Comment)` |
| Semicolon | `"semicolon"` | `; Comment` |
| Both | `"both"` | `(Comment) ; Comment` |

---

## Использование пользовательских параметров

### В конфигурации

```json
{
  "customParameters": {
    "enableHighSpeedMode": true,
    "maxRapidOverride": 120,
    "toolChangeDelay": 2.5,
    "coolantType": "flood",
    "customMessage": "Hello from config"
  }
}
```

### В макросе

```python
# Булевы значения
high_speed = context.config.getParameterBool("enableHighSpeedMode", False)

# Числовые значения
rapid_override = context.config.getParameterDouble("maxRapidOverride", 100.0)
delay = context.config.getParameterDouble("toolChangeDelay", 1.0)

# Строковые значения
coolant_type = context.config.getParameterString("coolantType", "flood")
message = context.config.getParameterString("customMessage", "")

# Любые значения
value = context.config.getParameter("enableHighSpeedMode", False)
```

---

## Примеры настроек

### Fanuc с расширенными циклами

```json
{
  "name": "Fanuc 31i-B5",
  "customParameters": {
    "useHighSpeedPecking": true,
    "peckDepth": 2.5,
    "retractAmount": 1.0,
    "chipBreak": true
  },
  "customGCodes": {
    "highSpeedPeck": "G73",
    "deepHolePeck": "G83",
    "fineBoring": "G76"
  },
  "customMCodes": {
    "throughSpindleCoolant": "M88",
    "airBlast": "M89"
  }
}
```

### Siemens с 5-осевой обработкой и CycleCache

```json
{
  "name": "Siemens 840D sl",
  "multiAxis": {
    "enableRtcp": true,
    "strategy": "tcp",
    "maxA": 120,
    "minA": -120,
    "maxB": 360,
    "minB": 0
  },
  "customParameters": {
    "enable3DCompensation": true,
    "tiltingAxis": "B",
    "useCycle800": true,
    "safeRetractHeight": 100.0
  },
  "formatting": {
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true,
      "trailingZeros": false
    },
    "feedrate": {
      "decimals": 1,
      "prefix": "F"
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 128,
      "transliterate": false
    }
  }
}
```

**Пример макроса с CycleCache:**

```python
# -*- coding: ascii -*-
# CYCLE800_MACRO - Поворот плоскости с кэшированием

def execute(context, command):
    """
    CYCLE800 с умным кэшированием параметров
    
    APT: 5AXIS/ROTATE, B45, C0
    """
    params = {
        'MODE': context.config.getParameterInt("cycle800Mode", 1),
        'TABLE': context.config.getParameterString("cycle800Table", "TABLE"),
        'B': command.getNumeric(0, 0.0),
        'C': command.getNumeric(1, 0.0)
    }
    
    # Умный вывод: полное определение или только вызов
    context.cycleWriteIfDifferent("CYCLE800", params)
```

### Heidenhain с циклами и StateCache

```json
{
  "name": "Heidenhain TNC640",
  "customParameters": {
    "useCycles": true,
    "cycle200": true,
    "cycle208": true,
    "m128Enabled": true
  },
  "customGCodes": {
    "plane": "PLANE",
    "cycle": "CYCL DEF"
  },
  "formatting": {
    "coordinates": {
      "decimals": 3,
      "leadingZeros": false,
      "trailingZeros": true
    },
    "comments": {
      "type": "semicolon",
      "maxLength": 80
    }
  }
}
```

**Пример макроса с StateCache:**

```python
# -*- coding: ascii -*-
# TOOL_CHANGE_MACRO - Смена инструмента с кэшем

def execute(context, command):
    """
    TURRET с кэшированием последнего инструмента
    
    APT: TURRET/5
    """
    tool_num = command.getNumeric(0, 0)
    
    # Проверка изменения через StateCache
    if context.cacheHasChanged("LAST_TOOL", tool_num):
        context.comment(f"Tool {tool_num}")
        context.registers.t = tool_num
        context.writeBlock()
        
        # Сохранение в кэш
        context.cacheSet("LAST_TOOL", tool_num)
```

### Интеграция с NumericNCWord и TextNCWord

```json
{
  "name": "Custom Controller with Formatting",
  "formatting": {
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true,
      "trailingZeros": false,
      "prefix": ""
    },
    "feedrate": {
      "decimals": 0,
      "prefix": "F"
    },
    "spindle": {
      "decimals": 0,
      "prefix": "S"
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 128,
      "transliterate": true,
      "encodeSpecialChars": true
    }
  }
}
```

**Пример макроса с NumericNCWord:**

```python
# -*- coding: ascii -*-
# LINEAR_MOVE_MACRO - Линейное перемещение с форматированием

def execute(context, command):
    """
    GO/TO с форматированием из конфига
    
    APT: GO/TO, 100.5, 200.3, 50.0
    """
    # Получение координат
    x = command.getNumeric(0, 0.0)
    y = command.getNumeric(1, 0.0)
    z = command.getNumeric(2, 0.0)
    
    # Установка значений с форматированием из конфига
    context.setNumericValue('X', x)
    context.setNumericValue('Y', y)
    context.setNumericValue('Z', z)
    
    # Запись блока с автоматическим форматированием
    context.writeBlock()
```

**Пример макроса с TextNCWord:**

```python
# -*- coding: ascii -*-
# COMMENT_MACRO - Комментарии со стилем

def execute(context, command):
    """
    REMARK с автоматическим стилем из конфига
    
    APT: REMARK/Начало обработки
    """
    text = command.getText(0, "")
    
    # Комментарий автоматически использует стиль из конфига:
    # - Siemens: (Начало обработки)
    # - Haas: ; Начало обработки
    context.comment(text)
    
    context.writeBlock()
```

### Полный пример: 5-осевая обработка с кэшированием

```json
{
  "name": "DMG Mori DMU50 - Siemens 840D",
  "controller": "siemens/840d",
  "machineProfile": "dmu50_5axis",
  
  "formatting": {
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true,
      "trailingZeros": false
    },
    "feedrate": {
      "decimals": 1,
      "prefix": "F"
    },
    "spindle": {
      "decimals": 0,
      "prefix": "S"
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 128
    }
  },
  
  "multiAxis": {
    "enableRtcp": true,
    "strategy": "cartesian",
    "rtcp": {
      "on": "RTCPON",
      "off": "RTCPOF"
    },
    "cycle800": {
      "enabled": true,
      "parameters": {
        "mode": 1,
        "table": "TABLE",
        "rotation": "ROTATION"
      }
    }
  },
  
  "customParameters": {
    "useStateCache": true,
    "useCycleCache": true,
    "safeRetractHeight": 100.0,
    "toolChangeHeight": 200.0
  }
}
```

**Комплексный макрос с StateCache + CycleCache + NumericNCWord:**

```python
# -*- coding: ascii -*-
# FIVE_AXIS_MACRO - 5-осевая обработка с полным кэшированием

def execute(context, command):
    """
    5AXIS/FEDRAT с кэшированием подачи и цикла
    
    APT: 5AXIS/FEDRAT, 5000, B45, C0
    """
    # Получение параметров
    feed = command.getNumeric(0, 5000)
    b_angle = command.getNumeric(1, 0.0)
    c_angle = command.getNumeric(2, 0.0)
    
    # Кэширование подачи через StateCache
    if context.cacheHasChanged("LAST_FEED", feed):
        context.setNumericValue('F', feed)
        context.cacheSet("LAST_FEED", feed)
    
    # Параметры цикла поворота
    cycle_params = {
        'MODE': 1,
        'B': b_angle,
        'C': c_angle
    }
    
    # Умный вывод цикла через CycleCache
    context.cycleWriteIfDifferent("CYCLE800", cycle_params)
    
    # Запись блока с форматированием NumericNCWord
    context.writeBlock()
```

---

## Отладка

### Запуск с debug-флагом

```bash
dotnet run -- -i input.apt -o output.nc -c siemens --debug
```

### Вывод отладочной информации в макросе

```python
def execute(context, command):
    context.comment(f"DEBUG: majorWord={command.majorWord}")
    context.comment(f"DEBUG: numeric={command.numeric}")
    context.comment(f"DEBUG: minorWords={command.minorWords}")
    context.comment(f"DEBUG: registers.x={context.registers.x}")
```

### Проверка конфигурации

```python
def execute(context, command):
    # Проверка параметров конфигурации
    context.comment(f"Controller: {context.config.name}")
    context.comment(f"Machine: {context.config.machineProfile}")
    context.comment(f"Safe Z: {context.config.safety.retractPlane}")
    context.comment(f"Max Feed: {context.config.safety.maxFeedRate}")
```

---

## Дополнительные ресурсы

- [PYTHON_MACROS_GUIDE.md](PYTHON_MACROS_GUIDE.md) — Полное руководство по макросам
- [QUICKSTART.md](QUICKSTART.md) — Быстрый старт
- [ARCHITECTURE.md](ARCHITECTURE.md) — Архитектура постпроцессора
