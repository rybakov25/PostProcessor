# Руководство по конфигурации PostProcessor

> **Полное руководство по настройке контроллеров и станков через JSON-конфиги**

---

## 📋 Оглавление

1. [Структура конфигов](#структура-конфигов)
2. [Конфигурация контроллера](#конфигурация-контроллера)
3. [Конфигурация станка](#конфигурация-станка)
4. [Параметры форматирования](#параметры-форматирования)
5. [Примеры конфигов](#примеры-конфигов)

---

## Структура конфигов

### Директории

```
configs/
├── controllers/           # Конфигурации контроллеров
│   ├── siemens/
│   │   └── 840d.json
│   ├── fanuc/
│   │   ├── 31i.json
│   │   └── 32i.json
│   ├── heidenhain/
│   │   └── tnc640.json
│   └── haas/
│       └── ngc.json
└── machines/             # Профили станков
    ├── default.json
    ├── haas_vf2.json
    ├── dmg_mori_dmu50_5axis.json
    └── ...
```

### Типы конфигов

| Тип | Назначение | Пример |
|-----|------------|--------|
| **Контроллер** | Настройки ЧПУ (G/M-коды, форматы) | `siemens/840d.json` |
| **Станок** | Профиль конкретного станка | `haas_vf2.json` |

---

## Конфигурация контроллера

### Базовая структура

```json
{
  "$schema": "../controller-schema.json",
  "name": "Siemens Sinumerik 840D sl",
  "machineType": "Milling",
  "version": "1.0",
  
  "output": {...},
  "formatting": {...},
  "gcode": {...},
  "mcode": {...},
  "cycles": {...},
  "fiveAxis": {...},
  "templates": {...},
  "safety": {...}
}
```

### output — настройки вывода

```json
"output": {
  "extension": ".mpf",           // Расширение файла
  "encoding": "UTF-8",           // Кодировка
  "lineEnding": "LF"             // Концовка строки (LF/CRLF)
}
```

### formatting — параметры форматирования

#### blockNumber — нумерация блоков

```json
"blockNumber": {
  "enabled": true,               // Включить нумерацию
  "prefix": "N",                 // Префикс (N)
  "increment": 10,               // Шаг (10 → N10, N20, N30)
  "start": 10                    // Начальный номер
}
```

**Примеры:**
- `N10, N20, N30...` (increment=10)
- `N1, N2, N3...` (increment=1)
- `O100, O101, O102...` (prefix="O")

#### comments — стиль комментариев

```json
"comments": {
  "type": "parentheses",         // parentheses | semicolon | both
  "prefix": "(",                 // Префикс для parentheses
  "suffix": ")",                 // Суффикс для parentheses
  "semicolonPrefix": ";",        // Префикс для semicolon
  "maxLength": 128,              // Макс. длина (0 = без ограничений)
  "transliterate": false,        // Транслитерация кириллицы
  "allowSpecialCharacters": true // Разрешить спецсимволы
}
```

**Стили:**

| Стиль | type | Результат |
|-------|------|-----------|
| **Parentheses** | `"parentheses"` | `(Comment text)` |
| **Semicolon** | `"semicolon"` | `; Comment text` |
| **Both** | `"both"` | `(Comment text) ; Comment text` |

**Транслитерация:**

```json
"transliterate": true
```

```python
context.comment("Привет")  # → (Privet)
```

#### coordinates — форматирование координат

```json
"coordinates": {
  "decimals": 3,               // Знаков после запятой
  "leadingZeros": true,        // Ведущие нули (0100.500)
  "trailingZeros": false,      // Хвостовые нули (100.5)
  "decimalPoint": true         // Десятичная точка всегда
}
```

**Примеры:**

| decimals | leadingZeros | trailingZeros | Результат |
|----------|--------------|---------------|-----------|
| 3 | true | false | `0100.500` |
| 3 | false | false | `100.500` |
| 3 | false | true | `100.5` |

#### feedrate — форматирование подачи

```json
"feedrate": {
  "decimals": 1,               // Знаков после запятой
  "prefix": "F"                // Префикс
}
```

**Пример:** `F500.0`

#### spindleSpeed — форматирование шпинделя

```json
"spindleSpeed": {
  "decimals": 0,               // Знаков после запятой
  "prefix": "S"                // Префикс
}
```

**Пример:** `S1200`

### gcode — G-коды

```json
"gcode": {
  "rapid": "G0",               // Быстрое перемещение
  "linear": "G1",              // Линейная интерполяция
  "circularCW": "G2",          // Дуга по часовой
  "circularCCW": "G3",         // Дуга против часовой
  "planeXY": "G17",            // Плоскость XY
  "planeZX": "G18",            // Плоскость ZX
  "planeYZ": "G19",            // Плоскость YZ
  "absolute": "G90",           // Абсолютные координаты
  "incremental": "G91"         // Относительные координаты
}
```

### mcode — M-коды

```json
"mcode": {
  "programEnd": "M30",         // Конец программы
  "spindleCW": "M3",           // Шпиндель по часовой
  "spindleCCW": "M4",          // Шпиндель против часовой
  "spindleStop": "M5",         // Шпиндель стоп
  "coolantOn": "M8",           // Охлаждение вкл
  "coolantOff": "M9",          // Охлаждение выкл
  "toolChange": "M6"           // Смена инструмента
}
```

### cycles — циклы сверления

```json
"cycles": {
  "drilling": "CYCLE81",       // Сверление
  "deepDrilling": "CYCLE83",   // Глубокое сверление
  "tapping": "CYCLE84",        // Нарезание резьбы
  "boring": "CYCLE86"          // Растачивание
}
```

### fiveAxis — 5-осевая обработка

```json
"fiveAxis": {
  "enabled": true,             // Включить 5 осей
  "tcpEnabled": true,          // RTCP включён
  "tcpOn": "RTCPON",           // Включить RTCP
  "tcpOff": "RTCPOF",          // Выключить RTCP
  "transformation": "TRAORI",  // Трансформация
  "transformationOff": "TRAFOOF",
  "cycle800": {
    "enabled": true,
    "format": "CYCLE800({mode},\"{table}\",{rotation},...)"
  }
}
```

### templates — шаблоны программы

```json
"templates": {
  "enabled": true,
  "header": [
    ";==================================================",
    "; PostProcessor v1.0 for {name} ;)",
    "; Input: {inputFile} ;)",
    "; Generated: {dateTime} ;)",
    ";==================================================)"
  ],
  "footer": [
    "M5",
    "M9",
    "M30"
  ]
}
```

**Переменные:**
- `{name}` — имя контроллера
- `{machine}` — профиль станка
- `{inputFile}` — входной файл
- `{dateTime}` — дата и время

### safety — параметры безопасности

```json
"safety": {
  "retractPlane": 50.0,        // Плоскость отвода
  "clearanceHeight": 100.0,    // Безопасная высота
  "approachDistance": 5.0,     // Расстояние подхода
  "maxFeedRate": 10000.0,      // Макс. подача
  "maxRapidRate": 20000.0      // Макс. быстрая
}
```

---

## Конфигурация станка

### Базовая структура

```json
{
  "$schema": "machine-profile-schema.json",
  "name": "Haas VF-2",
  "machineProfile": "vf2_01",
  "machineType": "Milling",
  "version": "NGC",
  
  "axes": {...},
  "limits": {...},
  "registerFormats": {...},
  "functionCodes": {...},
  "workCoordinateSystems": {...},
  "drillingCycles": {...},
  "safety": {...},
  "multiAxis": {...},
  "templates": {...},
  "customParameters": {...},
  "customGCodes": {...},
  "customMCodes": {...},
  "axisLimits": {...},
  "macros": {...}
}
```

### axes — оси станка

```json
"axes": {
  "linear": ["X", "Y", "Z"],   // Линейные оси
  "rotary": ["B", "C"],        // Вращательные оси
  "primary": "X",              // Основная ось
  "secondary": "Y",            // Вторая ось
  "tertiary": "Z"              // Третья ось
}
```

### limits — лимиты осей

```json
"limits": {
  "X": { "min": -50, "max": 510 },
  "Y": { "min": -50, "max": 410 },
  "Z": { "min": -40, "max": 410 },
  "A": { "min": -120, "max": 120 },
  "B": { "min": 0, "max": 360 },
  "C": { "min": 0, "max": 360 }
}
```

### registerFormats — форматы регистров

```json
"registerFormats": {
  "X": {
    "address": "X",
    "format": "F4.3",          // Формат (F4.3 = 4 цифры, 3 после точки)
    "isModal": true,           // Модальное
    "minValue": -1000,         // Мин. значение
    "maxValue": 1000           // Макс. значение
  },
  "F": {
    "address": "F",
    "format": "F3.1",
    "isModal": false,
    "minValue": 1,
    "maxValue": 10000
  },
  "S": {
    "address": "S",
    "format": "F0",
    "isModal": false,
    "minValue": 10,
    "maxValue": 12000
  }
}
```

### functionCodes — коды функций

```json
"functionCodes": {
  "rapid": {
    "code": "G00",
    "group": "MOTION",
    "isModal": true,
    "description": "Rapid positioning"
  },
  "spindle_cw": {
    "code": "M03",
    "group": "SPINDLE",
    "isModal": true,
    "description": "Spindle ON CW"
  }
}
```

### workCoordinateSystems — рабочие системы координат

```json
"workCoordinateSystems": [
  { "number": 1, "code": "G54", "xOffset": 0.0, "yOffset": 0.0, "zOffset": 0.0 },
  { "number": 2, "code": "G55", "xOffset": 0.0, "yOffset": 0.0, "zOffset": 0.0 },
  { "number": 3, "code": "G56", "xOffset": 0.0, "yOffset": 0.0, "zOffset": 0.0 }
]
```

### multiAxis — 5-осевые параметры

```json
"multiAxis": {
  "enableRtcp": true,          // Включить RTCP
  "maxA": 120.0,               // Макс. угол A
  "minA": -120.0,              // Мин. угол A
  "maxB": 360.0,               // Макс. угол B
  "minB": 0.0,                 // Мин. угол B
  "strategy": "cartesian"      // Стратегия (cartesian/tcp)
}
```

### customParameters — пользовательские параметры

```json
"customParameters": {
  "useHighSpeedMachining": true,
  "highSpeedCode": "G05.1Q1",
  "enableLookAhead": true,
  "defaultWorkOffset": "G54",
  "toolChangePosition": "G91 G28 Z0"
}
```

### customGCodes / customMCodes — пользовательские коды

```json
"customGCodes": {
  "workOffset": "G54",
  "tcpOn": "TRAORI",
  "tcpOff": "TRAFOOF"
},
"customMCodes": {
  "toolClamp": "M10",
  "toolUnclamp": "M11",
  "palletChange": "M60"
}
```

### axisLimits — ограничения осей

```json
"axisLimits": {
  "XMin": -50,
  "XMax": 510,
  "YMin": -50,
  "YMax": 410,
  "ZMin": -40,
  "ZMax": 410
}
```

### macros — пути к макросам

```json
"macros": {
  "init": "haas/init.py",
  "fini": "haas/fini.py",
  "goto": "haas/goto.py",
  "rapid": "haas/rapid.py",
  "toolChange": "haas/loadtl.py"
}
```

---

## Примеры конфигов

### Siemens 840D (фрезерный)

```json
{
  "name": "Siemens Sinumerik 840D sl",
  "formatting": {
    "blockNumber": {
      "enabled": true,
      "prefix": "N",
      "increment": 10,
      "start": 10
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 128
    },
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true,
      "trailingZeros": false
    }
  },
  "gcode": {
    "rapid": "G0",
    "linear": "G1"
  },
  "mcode": {
    "spindleCW": "M3",
    "coolantOn": "M8"
  },
  "cycles": {
    "drilling": "CYCLE81",
    "deepDrilling": "CYCLE83"
  }
}
```

### Fanuc 31i (фрезерный)

```json
{
  "name": "Fanuc 31i",
  "formatting": {
    "blockNumber": {
      "enabled": true,
      "prefix": "N",
      "increment": 1,
      "start": 1
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 64
    },
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true
    }
  },
  "gcode": {
    "rapid": "G00",
    "linear": "G01"
  },
  "registerFormats": {
    "X": { "format": "F4.3", "minValue": -1000, "maxValue": 1000 },
    "F": { "format": "F3.1", "minValue": 1, "maxValue": 10000 }
  }
}
```

### Haas NGC (фрезерный)

```json
{
  "name": "Haas NGC",
  "formatting": {
    "blockNumber": {
      "enabled": false
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 128
    },
    "coordinates": {
      "decimals": 3,
      "leadingZeros": false,
      "trailingZeros": false
    }
  },
  "customParameters": {
    "useHighSpeedMachining": true,
    "highSpeedCode": "G05.1Q1"
  }
}
```

### Heidenhain TNC640 (уникальный синтаксис)

```json
{
  "name": "Heidenhain TNC 640",
  "formatting": {
    "blockNumber": {
      "enabled": false
    },
    "comments": {
      "type": "semicolon",
      "semicolonPrefix": ";"
    },
    "coordinates": {
      "decimals": 3,
      "leadingZeros": false
    }
  },
  "functionCodes": {
    "rapid": { "code": "L", "description": "Heidenhain syntax" },
    "linear": { "code": "L", "description": "Heidenhain syntax" }
  }
}
```

---

## Использование в Python-макросах

### Получение параметров из конфига

```python
# -*- coding: ascii -*-
def execute(context, command):
    # Получение параметров из конфига
    name = context.config.name
    machine = context.config.machineProfile
    
    # Параметры безопасности
    maxFeed = context.config.safety.maxFeedRate
    retract = context.config.safety.retractPlane
    
    # 5-осевые параметры
    enableRtcp = context.config.multiAxis.enableRtcp
    maxA = context.config.multiAxis.maxA
    
    # Пользовательские параметры
    useHighSpeed = context.config.getParameter("useHighSpeedMachining", False)
    highSpeedCode = context.config.getParameter("highSpeedCode", "G05.1Q1")
    
    # M-коды из конфига
    m3 = context.config.mcode.spindleCW
    m8 = context.config.mcode.coolantOn
```

### Форматирование из конфига

```python
# -*- coding: ascii -*-
def execute(context, command):
    # Установка значения с форматированием из конфига
    context.setNumericValue('X', 100.5)
    
    # Получение отформатированной строки
    xStr = context.getFormattedValue('X')  # "X100.500" (из конфига)
    
    # Комментарий со стилем из конфига
    context.comment("Начало операции")
    # Siemens: (Начало операции)
    # Haas: ; Начало операции
```

---

## См. также

- [PYTHON_MACROS_GUIDE.md](PYTHON_MACROS_GUIDE.md) — руководство по Python-макросам
- [CUSTOMIZATION_GUIDE.md](CUSTOMIZATION_GUIDE.md) — руководство по настройке
- [SUPPORTED_EQUIPMENT.md](SUPPORTED_EQUIPMENT.md) — поддерживаемое оборудование
