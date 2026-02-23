# Архитектура постпроцессора

> **Для разработчиков** — подробное описание архитектуры, компонентов и взаимодействия

---

## 📋 Оглавление

1. [Обзор архитектуры](#обзор-архитектуры)
2. [Структура проекта](#структура-проекта)
3. [Компоненты системы](#компоненты-системы)
4. [StateCache и CycleCache (v1.1.0)](#statecache-и-cyclecache-v110)
5. [NumericNCWord и TextNCWord (v1.1.0)](#numericncword-и-textncword-v110)
6. [Загрузка и приоритеты макросов](#загрузка-и-приоритеты-макросов)
7. [Поток выполнения](#поток-выполнения)
8. [API для макросов](#api-для-макросов)
9. [Конфигурация](#конфигурация)
10. [Расширение функциональности](#расширение-функциональности)

---

## Обзор архитектуры

### Высокоуровневая схема

```
┌─────────────────────────────────────────────────────────────────┐
│                        CAM-система                              │
│                    (CATIA, NX, Mastercam)                       │
└─────────────────────────────┬───────────────────────────────────┘
                              │ APT/CL файл
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     PostProcessor                               │
│            УНИВЕРСАЛЬНЫЙ ПОСТПРОЦЕССОР                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   APT       │  │   Macros    │  │      Config             │  │
│  │   Parser    │─▶│   Engine    │─▶│      Manager            │  │
│  └─────────────┘  └──────┬──────┘  └─────────────────────────┘  │
│                          │                                      │
│                          │ Python макросы                       │
│                          ▼                                      │
│                  ┌───────────────┐                              │
│                  │ Python Runtime│                              │
│                  │  (pythonnet)  │                              │
│                  └───────┬───────┘                              │
└──────────────────────────┼──────────────────────────────────────┘
                           │ G-код
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Любое оборудование с ЧПУ                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Фрезерные  │  │   Токарные  │  │   Многозадачные         │  │
│  │  3-5 осей   │  │   2-3 оси   │  │   Mill-Turn             │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Siemens    │  │    Fanuc    │  │   Heidenhain            │  │
│  │  840D/sl    │  │  31i/32i    │  │   TNC 640/620           │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Ключевые принципы

| Принцип | Описание |
|---------|----------|
| **Универсальность** | Поддержка любого оборудования через конфигурации и макросы |
| **Модульность** | Каждый компонент независим и заменяем |
| **Расширяемость** | Новые функции через макросы без изменения ядра |
| **Конфигурируемость** | Все параметры в JSON-файлах |
| **Безопасность** | Проверка ограничений станка перед выводом |

---

## Структура проекта

```
PostProcessor/
│
├── src/
│   ├── PostProcessor.CLI/           # Интерфейс командной строки
│   │   ├── Program.cs               # Точка входа
│   │   └── CommandLineOptions.cs    # Параметры CLI
│   │
│   ├── PostProcessor.Core/          # Ядро постпроцессора
│   │   ├── Context/
│   │   │   ├── PostContext.cs       # Контекст обработки
│   │   │   ├── Register.cs          # Регистры (X, Y, Z, F, S, T)
│   │   │   ├── MachineState.cs      # Состояние станка
│   │   │   ├── ToolInfo.cs          # Информация об инструменте
│   │   │   │
│   │   │   ├── StateCache.cs        # NEW v1.1.0: Кэш состояний LAST_*
│   │   │   ├── CycleCache.cs        # NEW v1.1.0: Кэш параметров циклов
│   │   │   ├── NumericNCWord.cs     # NEW v1.1.0: Числовые NC-слова
│   │   │   ├── SequenceNCWord.cs    # NEW v1.1.0: Нумерация блоков
│   │   │   ├── TextNCWord.cs        # NEW v1.1.0: Текстовые NC-слова
│   │   │   └── BlockWriter.cs       # NEW v1.1.0: Запись блоков
│   │   │
│   │   ├── Config/
│   │   │   ├── ControllerConfig.cs  # Конфигурация контроллера
│   │   │   └── Models/              # Модели конфигурации
│   │   │
│   │   ├── Models/
│   │   │   └── APTCommand.cs        # Модель APT-команды
│   │   │
│   │   └── Interfaces/              # Интерфейсы
│   │
│   ├── PostProcessor.Macros/        # Движок макросов
│   │   ├── Python/
│   │   │   ├── PythonMacroEngine.cs # Загрузчик Python
│   │   │   ├── PythonPostContext.cs # Python-обёрка контекста (v1.1.0 UPDATED)
│   │   │   └── PythonAptCommand.cs  # Python-обёртка команды
│   │   │
│   │   ├── Engine/
│   │   │   └── MacroLoader.cs       # Загрузчик макросов
│   │   │
│   │   ├── Attributes/
│   │   │   └── MacroNameAttribute.cs # Атрибуты макросов
│   │   │
│   │   └── BuiltInMacros/           # Встроенные макросы
│   │
│   └── PostProcessor.APT/           # Парсер APT-файлов
│       └── AptParser.cs             # Парсер APT/CL
│
├── macros/
│   └── python/
│       ├── base/                    # Базовые макросы (универсальные)
│       │   ├── goto.py              # Линейное перемещение
│       │   ├── rapid.py             # Быстрое перемещение
│       │   ├── spindl.py            # Управление шпинделем
│       │   ├── coolnt.py            # Управление охлаждением
│       │   ├── fedrat.py            # Управление подачей
│       │   └── fini.py              # Завершение программы
│       │
│       ├── fanuc/                   # Fanuc 31i/32i/35i
│       │   ├── init.py              # Инициализация
│       │   ├── partno.py            # Начало программы
│       │   ├── fini.py              # Завершение
│       │   ├── goto.py              # Линейное перемещение
│       │   ├── rapid.py             # Быстрое перемещение
│       │   ├── spindl.py            # Шпиндель
│       │   ├── coolnt.py            # Охлаждение
│       │   ├── fedrat.py            # Подача
│       │   ├── loadtl.py            # Смена инструмента
│       │   └── lathe_*.py           # Токарные макросы
│       │
│       ├── heidenhain/              # Heidenhain TNC640/620
│       │   ├── init.py              # Инициализация
│       │   ├── partno.py            # Начало программы (BEGIN PGM)
│       │   ├── fini.py              # Завершение (END PGM)
│       │   ├── goto.py              # Перемещение (L X+100 FMAX)
│       │   ├── rapid.py             # Быстрое перемещение
│       │   ├── spindl.py            # Шпиндель
│       │   ├── coolnt.py            # Охлаждение
│       │   ├── fedrat.py            # Подача
│       │   └── loadtl.py            # Смена инструмента (TOOL CALL)
│       │
│       ├── haas/                    # Haas NGC
│       │   ├── init.py              # Инициализация
│       │   ├── partno.py            # Начало программы (%)
│       │   ├── fini.py              # Завершение
│       │   ├── goto.py              # Линейное перемещение
│       │   ├── rapid.py             # Быстрое перемещение
│       │   ├── spindl.py            # Шпиндель
│       │   ├── coolnt.py            # Охлаждение
│       │   ├── fedrat.py            # Подача
│       │   └── loadtl.py            # Смена инструмента
│       │
│       ├── lathe/                   # Токарные макросы (универсальные)
│       │   ├── turret.py            # Револьверная головка (T0101)
│       │   ├── chuck.py             # Патрон (M10/M11)
│       │   └── tailstk.py           # Пиноль (M20/M21)
│       │
│       ├── mmill/                   # Макросы для станка MMILL
│       │   ├── init.py              # Инициализация программы
│       │   ├── loadtl.py            # Смена инструмента
│       │   ├── rtcp.py              # Управление RTCP
│       │   ├── rotabl.py            # Поворот стола
│       │   └── fini.py              # Завершение (специфичное)
│       │
│       └── user/                    # Пользовательские макросы
│           └── {machine}/           # Переопределения для станка
│
├── configs/
│   ├── controllers/                 # Конфигурации контроллеров
│   │   ├── siemens/
│   │   │   └── 840d.json            # Siemens 840D
│   │   ├── fanuc/
│   │   │   └── 31i.json             # Fanuc 31i
│   │   └── heidenhain/
│   │       └── tnc640.json          # Heidenhain TNC640
│   │
│   └── machines/                    # Профили станков
│       ├── mmill.json               # Mecof MMILL
│       ├── dmu50.json               # DMG Mori DMU50
│       └── ...
│
├── docs/
│   ├── ARCHITECTURE.md              # Этот файл
│   ├── PYTHON_MACROS_GUIDE.md       # Руководство по макросам
│   ├── QUICKSTART.md                # Быстрый старт
│   ├── CUSTOMIZATION_GUIDE.md       # Руководство по настройке
│   ├── IMSPOST_TO_PYTHON_GUIDE.md   # Переход с IMSpost
│   ├── PROJECT_STRUCTURE.md         # Структура проекта
│   └── instruction.txt              # Справочник IMSpost
│
├── README.md                        # Главная страница
├── PostProcessor.sln                # Solution файл
└── NuGet.config                     # NuGet пакеты
```

---

## Компоненты системы

### 1. PostProcessor.CLI

**Назначение:** Интерфейс командной строки

**Основные файлы:**
- `Program.cs` — точка входа, парсинг аргументов
- `CommandLineOptions.cs` — определение параметров

**Параметры командной строки:**

| Параметр | Короткий | Описание | Default |
|----------|----------|----------|---------|
| `--input` | `-i` | Входной APT файл | Обязательно |
| `--output` | `-o` | Выходной NC файл | Обязательно |
| `--controller` | `-c` | Тип контроллера | `siemens` |
| `--config` | `-cfg` | Путь к конфигурации | Авто |
| `--machine-profile` | `-mp` | Профиль станка | Нет |
| `--macro-path` | `-m` | Доп. путь к макросам | `macros/python` |
| `--debug` | `-d` | Режим отладки | `false` |

**Пример использования:**
```bash
dotnet run -- -i part.apt -o part.nc -c siemens --machine-profile mmill --debug
```

---

### 2. PostProcessor.Core

**Назначение:** Ядро постпроцессора, контекст обработки

#### PostContext

Главный класс, хранящий состояние обработки:

```csharp
public class PostContext
{
    public RegisterSet Registers { get; }      // Регистры
    public MachineState Machine { get; }       // Состояние станка
    public ControllerConfig Config { get; }    // Конфигурация
    public StreamWriter Output { get; }        // Вывод

    // Кэширование (v1.1.0)
    public StateCache StateCache { get; }      // Кэш модальных состояний
    public CycleCache CycleCache { get; }      // Кэш параметров циклов

    // Форматирование (v1.1.0)
    public NumericNCWord NumericWords { get; } // Числовые NC-слова
    public TextNCWord TextWords { get; }       // Текстовые NC-слова

    // Системные переменные
    public void SetSystemVariable(string name, object value);
    public T GetSystemVariable<T>(string name, T defaultValue);
}
```

#### RegisterSet

Набор регистров станка:

```csharp
public class RegisterSet
{
    public Register X { get; }  // Координата X
    public Register Y { get; }  // Координата Y
    public Register Z { get; }  // Координата Z
    public Register A { get; }  // Ось A
    public Register B { get; }  // Ось B
    public Register C { get; }  // Ось C
    public Register F { get; }  // Подача
    public Register S { get; }  // Шпиндель
    public Register T { get; }  // Инструмент
}
```

#### MachineState

Текущее состояние станка:

```csharp
public class MachineState
{
    public ToolInfo CurrentTool { get; set; }     // Текущий инструмент
    public SpindleDirection SpindleState { get; set; }  // Шпиндель
    public CoolantMode CoolantState { get; set; }       // Охлаждение
    public int ActiveCoordinateSystem { get; set; }     // G54-G59
}
```

---

### 3. PostProcessor.Macros

**Назначение:** Движок выполнения макросов

#### PythonMacroEngine

Загружает и выполняет Python макросы:

```csharp
public class PythonMacroEngine
{
    // Загрузка макросов из директории
    public void LoadMacros(string path);

    // Выполнение макроса для команды
    public void ExecuteMacro(string macroName, PostContext context, APTCommand command);

    // Проверка наличия макроса
    public bool HasMacro(string name);
}
```

#### PythonPostContext

Python-обёртка для PostContext:

```python
# Доступ из Python
context.registers.x = 100.5
context.config.safety.retractPlane
context.system.MOTION = "RAPID"
context.globalVars.TOOL = 5

# v1.1.0: Новые методы кэширования
context.cacheGet("LAST_FEED", 0.0)
context.cacheSet("LAST_FEED", 500.0)
context.cacheHasChanged("LAST_FEED", 500.0)

# v1.1.0: Циклы
context.cycleWriteIfDifferent("CYCLE800", params)

# v1.1.0: Форматирование
context.setNumericValue('X', 100.5)
context.getFormattedValue('X')
```

#### MacroLoader

Загружает макросы с учётом приоритетов:

```
1. System макросы (встроенные)
2. Controller макросы (из конфига)
3. Base макросы (универсальные)
4. Machine макросы (специфичные)
5. User макросы (пользовательские)
```

---

### 4. PostProcessor.APT

**Назначение:** Парсинг APT/CL файлов

#### AptParser

Парсит APT-файл в команды:

```csharp
public class AptParser
{
    public async IAsyncEnumerable<APTCommand> ParseAsync(string filePath);
}
```

**Формат APT-команд:**

```
GOTO/100, 50, 10
├── MajorWord: "goto"
└── Numeric: [100.0, 50.0, 10.0]

SPINDL/ON, CLW, 1600
├── MajorWord: "spindl"
├── MinorWords: ["on", "clw"]
└── Numeric: [1600.0]

LOADTL/5, ADJUST, 1, MILL
├── MajorWord: "loadtl"
├── Numeric: [5.0, 1.0]
└── MinorWords: ["adjust", "mill"]
```

---

## StateCache и CycleCache (v1.1.0)

### StateCache — кэш состояний

StateCache предоставляет кэширование переменных для модального вывода.

**Принцип работы:**
```python
# Проверка изменения
if context.cacheHasChanged("LAST_FEED", 500.0):
    context.registers.f = 500.0
    context.writeBlock()
    context.cacheSet("LAST_FEED", 500.0)
```

**Методы:**

| Метод | Описание | Пример |
|-------|----------|--------|
| `cacheGet(key, default)` | Получить значение из кэша | `context.cacheGet("LAST_FEED", 0.0)` |
| `cacheSet(key, value)` | Установить значение в кэш | `context.cacheSet("LAST_FEED", 500.0)` |
| `cacheHasChanged(key, value)` | Проверить изменение значения | `context.cacheHasChanged("LAST_FEED", 500.0)` |
| `cacheReset(key)` | Сбросить значение в кэше | `context.cacheReset("LAST_FEED")` |

**Примеры использования:**

```python
# Кэширование подачи
def execute(context, command):
    feed = command.getNumeric(0, 0.0)
    if context.cacheHasChanged("LAST_FEED", feed):
        context.registers.f = feed
        context.writeBlock()
        context.cacheSet("LAST_FEED", feed)

# Кэширование инструмента
def execute(context, command):
    tool = command.getNumeric(0, 0)
    if context.cacheHasChanged("LAST_TOOL", tool):
        context.write(f"T{tool}")
        context.cacheSet("LAST_TOOL", tool)
```

---

### CycleCache — кэширование циклов

CycleCache автоматически определяет: полное определение цикла или только вызов.

**Принцип работы:**
```python
params = {'MODE': 1, 'X': 100.0, 'Y': 200.0}
context.cycleWriteIfDifferent("CYCLE800", params)
```

**Результат:**
```nc
; Первый вызов (полное определение)
CYCLE800(MODE=1, X=100.000, Y=200.000)

; Второй вызов (те же параметры - только вызов)
CYCLE800()
```

**Методы:**

| Метод | Описание | Пример |
|-------|----------|--------|
| `cycleWriteIfDifferent(name, params)` | Записать цикл если параметры отличаются | `context.cycleWriteIfDifferent("CYCLE800", params)` |
| `cycleReset(name)` | Сбросить кэш цикла | `context.cycleReset("CYCLE800")` |
| `cycleGetCache(name)` | Получить кэш цикла | `context.cycleGetCache("CYCLE800")` |

**Примеры использования:**

```python
# Цикл G81 (сверление)
def execute(context, command):
    params = {
        'R': command.getNumeric(1, 5.0),
        'Z': command.getNumeric(2, -50.0),
        'F': command.getNumeric(3, 100.0)
    }
    context.cycleWriteIfDifferent("G81", params)

# Цикл CYCLE800 (поворотная плоскость)
def execute(context, command):
    params = {
        'MODE': 1,
        'TABLE': 'TABLE',
        'ROTATION': 'ROTATION',
        'A': context.registers.a.value,
        'B': context.registers.b.value
    }
    context.cycleWriteIfDifferent("CYCLE800", params)
```

---

## NumericNCWord и TextNCWord (v1.1.0)

### NumericNCWord — форматирование из конфига

NumericNCWord предоставляет форматирование числовых значений из JSON-конфига.

**Пример:**
```python
context.setNumericValue('X', 100.5)
xStr = context.getFormattedValue('X')  # "X100.500" (из конфига)
```

**Конфигурация:**
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
      "leadingZeros": false,
      "trailingZeros": true
    },
    "spindle": {
      "decimals": 0,
      "leadingZeros": false,
      "trailingZeros": false
    }
  }
}
```

**Методы:**

| Метод | Описание | Пример |
|-------|----------|--------|
| `setNumericValue(address, value)` | Установить числовое значение | `context.setNumericValue('X', 100.5)` |
| `getFormattedValue(address)` | Получить отформатированное значение | `context.getFormattedValue('X')` |
| `setFeedRate(value)` | Установить подачу | `context.setFeedRate(500.0)` |
| `setSpindleSpeed(value)` | Установить скорость шпинделя | `context.setSpindleSpeed(12000)` |

**Примеры использования:**

```python
# Форматирование координат
def execute(context, command):
    x = command.getNumeric(0, 0.0)
    y = command.getNumeric(1, 0.0)
    z = command.getNumeric(2, 0.0)
    
    context.setNumericValue('X', x)
    context.setNumericValue('Y', y)
    context.setNumericValue('Z', z)
    
    context.writeBlock()

# Форматирование подачи
def execute(context, command):
    feed = command.getNumeric(0, 100.0)
    context.setFeedRate(feed)
    if context.cacheHasChanged("LAST_FEED", feed):
        context.writeBlock()
        context.cacheSet("LAST_FEED", feed)
```

---

### TextNCWord — комментарии со стилем

TextNCWord предоставляет комментарии со стилем из конфига.

**Пример:**
```python
context.comment("Начало операции")
# Siemens: (Начало операции)
# Haas: ; Начало операции
```

**Конфигурация:**
```json
{
  "formatting": {
    "comments": {
      "type": "parentheses",
      "maxLength": 128,
      "transliterate": false,
      "prefix": "",
      "suffix": ""
    }
  }
}
```

**Типы комментариев:**

| Тип | Формат | Пример |
|-----|--------|--------|
| `parentheses` | `(текст)` | `(Начало операции)` |
| `semicolon` | `; текст` | `; Начало операции` |
| `both` | `(текст) ; текст` | `(Начало операции) ; Начало операции` |

**Методы:**

| Метод | Описание | Пример |
|-------|----------|--------|
| `comment(text)` | Записать комментарий | `context.comment("Операция 1")` |
| `writeComment(text, force)` | Записать комментарий с опциями | `context.writeComment("Текст", True)` |

**Примеры использования:**

```python
# Комментарии в программе
def execute(context, command):
    context.comment("=== НАЧАЛО ПРОГРАММЫ ===")
    context.writeBlock()
    
    context.comment("Смена инструмента")
    context.write("T1 M6")
    
    context.comment("Быстрый подход")
    context.write("G0 X100 Y200")
```

---

## Загрузка и приоритеты макросов

### Приоритеты макросов

| Приоритет | Тип | Путь | Описание |
|-----------|-----|------|----------|
| 1000 | System | Встроенные | Системные макросы ядра |
| 2000 | Controller | configs/controllers/ | Макросы контроллера |
| 3000 | Base | macros/python/base/ | Базовые универсальные |
| 4000 | Machine | macros/python/{machine}/ | Специфичные для станка |
| 5000 | User | macros/python/user/ | Пользовательские |

**Важно:** Макросы с более высоким приоритетом переопределяют макросы с низким.

### Порядок загрузки

```
1. Загрузка ядра
   └── Загрузка системных макросов (приоритет 1000)

2. Загрузка конфигурации контроллера
   └── Загрузка макросов контроллера (приоритет 2000)

3. Загрузка базовых макросов
   └── macros/python/base/*.py (приоритет 3000)

4. Загрузка макросов станка
   └── macros/python/{machine}/*.py (приоритет 4000)

5. Загрузка пользовательских макросов
   └── macros/python/user/*.py (приоритет 5000)
```

### Пример переопределения

**Базовый макрос** (`base/goto.py`):
```python
def execute(context, command):
    context.write("G1 X...")  # Стандартный G1
```

**Пользовательский макрос** (`user/goto.py`):
```python
def execute(context, command):
    context.write("G1.1 X...")  # Переопределённый G1.1
```

**Результат:** Будет использован пользовательский макрос (приоритет 5000 > 3000).

---

## Поток выполнения

### Последовательность обработки

```
1. Инициализация
   ├── Загрузка конфигурации контроллера
   ├── Загрузка профиля станка
   └── Загрузка макросов по приоритетам

2. Парсинг APT-файла
   └── Чтение команд по одной

3. Обработка каждой команды
   ├── Поиск макроса по имени команды
   ├── Создание Python-обёрток (context, command)
   ├── Выполнение макроса
   └── Вывод G-кода

4. Завершение
   └── Закрытие файла, вывод статистики
```

### Диаграмма последовательности

```
CLI          Core          Macros         Python
 │            │              │              │
 │─Parse args─│              │              │
 │            │              │              │
 │            │─Load config──│              │
 │            │              │              │
 │            │─Load macros──│              │
 │            │              │              │
 │            │              │─Load .py────▶│
 │            │              │              │
 │            │              │              │
 │─Parse APT──▶               │              │
 │            │              │              │
 │            │─For each command            │
 │            │  │                          │
 │            │  ├─Find macro               │
 │            │  │                          │
 │            │  ├─Create wrappers─────────▶│
 │            │  │                          │
 │            │  ├─Execute macro───────────▶│
 │            │  │                          │
 │            │  ◀─Write G-code─────────────│
 │            │              │              │
 │            │              │              │
 │─Write NC───│              │              │
```

### Схема архитектуры с кэшами (v1.1.0)

```
┌─────────────────────────────────────────────────────────────┐
│                    PythonPostContext                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Python-обёртка для PostContext                       │  │
│  │                                                       │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  │  │
│  │  │ StateCache  │  │ CycleCache  │  │ NumericNCWord │  │  │
│  │  │ ─────────── │  │ ─────────── │  │ ───────────── │  │  │
│  │  │ cacheGet()  │  │ cycleWrite  │  │ setNumeric()  │  │  │
│  │  │ cacheSet()  │  │ cycleReset  │  │ getFormatted()│  │  │
│  │  │ cacheHasCh. │  │ cycleGet    │  │               │  │  │
│  │  │ cacheReset()│  │             │  │               │  │  │
│  │  └─────────────┘  └─────────────┘  └───────────────┘  │  │
│  │                                                       │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  │  │
│  │  │ TextNCWord  │  │ BlockWriter │  │  Registers    │  │  │
│  │  │ ─────────── │  │ ─────────── │  │ ───────────── │  │  │
│  │  │ comment()   │  │ writeBlock()│  │ X, Y, Z, F, S │  │  │
│  │  │ writeComm.  │  │ write()     │  │ A, B, C, T    │  │  │
│  │  └─────────────┘  └─────────────┘  └───────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ делегирует
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       PostContext                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ StateCache  │  │ CycleCache  │  │  MachineState       │  │
│  │ (C# core)   │  │ (C# core)   │  │  (C# core)          │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## API для макросов

### Объект context

| Объект | Описание |
|--------|----------|
| `context.registers` | Регистры станка (X, Y, Z, F, S, T) |
| `context.config` | Конфигурация контроллера |
| `context.machine` | Состояние станка |
| `context.system` | Системные переменные (SYSTEM.*) |
| `context.globalVars` | Глобальные переменные (GLOBAL.*) |
| `context.cache` | Кэш состояний (v1.1.0) |
| `context.cycleCache` | Кэш циклов (v1.1.0) |

**Методы вывода:**

```python
context.write("G01 X100")           # Вывод с номером блока
context.write("G01 X100", True)     # Без номера блока
context.comment("Текст")            # Комментарий в скобках
context.warning("Предупреждение")   # Предупреждение
context.writeln()                   # Пустая строка
```

**Методы кэширования (v1.1.0):**

| Метод | Описание | Пример |
|-------|----------|--------|
| `cacheGet(key, default)` | Получить значение из кэша | `context.cacheGet("LAST_FEED", 0.0)` |
| `cacheSet(key, value)` | Установить значение в кэш | `context.cacheSet("LAST_FEED", 500.0)` |
| `cacheHasChanged(key, value)` | Проверить изменение значения | `context.cacheHasChanged("LAST_FEED", 500.0)` |
| `cacheReset(key)` | Сбросить значение в кэше | `context.cacheReset("LAST_FEED")` |

**Методы циклов (v1.1.0):**

| Метод | Описание | Пример |
|-------|----------|--------|
| `cycleWriteIfDifferent(name, params)` | Записать цикл если параметры отличаются | `context.cycleWriteIfDifferent("CYCLE800", params)` |
| `cycleReset(name)` | Сбросить кэш цикла | `context.cycleReset("CYCLE800")` |
| `cycleGetCache(name)` | Получить кэш цикла | `context.cycleGetCache("CYCLE800")` |

**Методы форматирования (v1.1.0):**

| Метод | Описание | Пример |
|-------|----------|--------|
| `setNumericValue(address, value)` | Установить числовое значение | `context.setNumericValue('X', 100.5)` |
| `getFormattedValue(address)` | Получить отформатированное значение | `context.getFormattedValue('X')` |
| `setFeedRate(value)` | Установить подачу | `context.setFeedRate(500.0)` |
| `setSpindleSpeed(value)` | Установить скорость шпинделя | `context.setSpindleSpeed(12000)` |

---

### Объект command

| Свойство | Тип | Описание |
|----------|-----|----------|
| `majorWord` | str | Имя команды (goto, spindl) |
| `lineNumber` | int | Номер строки в APT |
| `numeric` | list[float] | Числовые параметры |
| `strings` | list[str] | Строковые параметры |
| `minorWords` | list[str] | Ключевые слова |

**Методы:**

```python
command.hasMinorWord("on")           # Проверка ключевого слова
command.getNumeric(0, 0.0)           # Число с default
command.getString(0, "DEFAULT")      # Строка с default
```

---

## Конфигурация

### Структура конфигурации контроллера

```json
{
  "name": "Siemens 840D",
  "machineType": "Milling",
  "version": "1.0",

  "registerFormats": {
    "X": { "address": "X", "format": "F4.3", "isModal": true },
    "F": { "address": "F", "format": "F3.1", "isModal": false }
  },

  "functionCodes": {
    "rapid": { "code": "G00", "group": "MOTION" },
    "linear": { "code": "G01", "group": "MOTION" },
    "spindle_cw": { "code": "M03", "group": "SPINDLE" }
  },

  "safety": {
    "clearancePlane": 100.0,
    "retractPlane": 5.0,
    "maxFeedRate": 10000.0,
    "maxSpindleSpeed": 12000.0
  },

  "multiAxis": {
    "enableRtcp": true,
    "maxA": 120.0,
    "minA": -120.0,
    "maxB": 360.0,
    "minB": 0.0
  },

  "formatting": {
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true,
      "trailingZeros": false
    },
    "feedrate": {
      "decimals": 1,
      "leadingZeros": false,
      "trailingZeros": true
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 128,
      "transliterate": false
    }
  },

  "customParameters": {
    "useCustomFeature": true,
    "feedOverride": 120.0
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

### Структура профиля станка

```json
{
  "name": "Mecof MMILL",
  "machineProfile": "mmill_01",

  "axisLimits": {
    "XMin": 0,
    "XMax": 2000,
    "YMin": 0,
    "YMax": 1000,
    "ZMin": -500,
    "ZMax": 500
  },

  "head": {
    "type": "TCB6",
    "clampCommand": "M101"
  },

  "fiveAxis": {
    "cycle800": {
      "parameters": {
        "mode": 1,
        "table": "TABLE",
        "rotation": "ROTATION"
      }
    },
    "rtcp": {
      "on": "RTCPON",
      "off": "RTCPOF"
    }
  },

  "customParameters": {
    "softStart": true,
    "toolChangeHeight": 200.0
  }
}
```

---

## Расширение функциональности

### Добавление поддержки нового оборудования

Постпроцессор поддерживает **любое оборудование** через систему конфигураций и макросов.

#### Шаг 1: Добавьте контроллер

1. Создайте директорию `configs/controllers/{controller_name}/`
2. Создайте базовую конфигурацию `base.json`:

```json
{
  "name": "Your Controller Name",
  "machineType": "Milling",
  "description": "Custom controller for your machine",
  "registerFormats": {
    "X": { "address": "X", "format": "F4.3", "isModal": true },
    "Y": { "address": "Y", "format": "F4.3", "isModal": true },
    "Z": { "address": "Z", "format": "F4.3", "isModal": true },
    "F": { "address": "F", "format": "F3.1", "isModal": false },
    "S": { "address": "S", "format": "F0.0", "isModal": false }
  },
  "functionCodes": {
    "rapid": { "code": "G00" },
    "linear": { "code": "G01" },
    "spindle_cw": { "code": "M03" },
    "spindle_ccw": { "code": "M04" },
    "spindle_stop": { "code": "M05" },
    "coolant_on": { "code": "M08" },
    "coolant_off": { "code": "M09" }
  },
  "safety": {
    "clearancePlane": 100.0,
    "retractPlane": 5.0,
    "maxFeedRate": 10000.0
  },
  "formatting": {
    "coordinates": {
      "decimals": 3,
      "leadingZeros": true,
      "trailingZeros": false
    },
    "comments": {
      "type": "parentheses",
      "maxLength": 128
    }
  }
}
```

#### Шаг 2: Добавьте базовые макросы

Создайте макросы в `macros/python/base/`:

```
macros/python/base/
├── partno.py    # Начало программы
├── goto.py      # Линейное перемещение
├── rapid.py     # Быстрое перемещение
├── spindl.py    # Шпиндель
├── coolnt.py    # Охлаждение
├── fedrat.py    # Подача
├── loadtl.py    # Смена инструмента
└── fini.py      # Конец программы
```

#### Шаг 3: Добавьте профиль станка (опционально)

Создайте `configs/machines/{machine_name}.json`:

```json
{
  "name": "Your Machine Name",
  "machineProfile": "your_machine_01",
  "axisLimits": {
    "XMin": 0,
    "XMax": 1000,
    "YMin": 0,
    "YMax": 500,
    "ZMin": -200,
    "ZMax": 300
  },
  "fiveAxis": {
    "enableRtcp": true,
    "rtcp": {
      "on": "TCPM",
      "off": "TCPOF"
    }
  }
}
```

#### Шаг 4: Используйте

```bash
dotnet run -- -i part.apt -o part.nc -c your_controller --machine-profile your_machine_01
```

---

### Добавление нового макроса

**Шаг 1:** Создайте файл в `macros/python/user/`:

```python
# -*- coding: ascii -*-
# CUSTOM MACRO - Описание

def execute(context, command):
    """Документация макроса"""
    # Ваша логика
    context.write("G01 X100")
```

**Шаг 2:** Протестируйте:

```bash
dotnet run -- -i test.apt -o output.nc -c siemens
```

---

### Добавление новой конфигурации

**Шаг 1:** Скопируйте существующую конфигурацию:

```bash
cp configs/controllers/siemens/840d.json configs/controllers/siemens/custom.json
```

**Шаг 2:** Отредактируйте под ваши нужды.

**Шаг 3:** Используйте при запуске:

```bash
dotnet run -- -i part.apt -o part.nc --config configs/controllers/siemens/custom.json
```

---

### Добавление профиля станка

**Шаг 1:** Создайте файл `configs/machines/my_machine.json`:

```json
{
  "name": "My Machine",
  "machineProfile": "my_machine_01",
  "axisLimits": {
    "XMin": 0,
    "XMax": 500
  }
}
```

**Шаг 2:** Используйте при запуске:

```bash
dotnet run -- -i part.apt -o part.nc -c siemens --machine-profile my_machine_01
```

---

## Справочник

### Поддерживаемые APT-команды

| Команда | Описание | Макрос по умолчанию |
|---------|----------|---------------------|
| `PARTNO` | Начало программы | `init.py` |
| `GOTO` | Линейное перемещение | `goto.py` |
| `RAPID` | Быстрое перемещение | `rapid.py` |
| `SPINDL` | Управление шпинделем | `spindl.py` |
| `COOLNT` | Управление охлаждением | `coolnt.py` |
| `FEDRAT` | Управление подачей | `fedrat.py` |
| `LOADTL` | Смена инструмента | `loadtl.py` |
| `RTCP` | Вкл/выкл RTCP | `rtcp.py` |
| `FINI` | Конец программы | `fini.py` |

---

### Системные переменные

| Переменная | Описание | Default |
|------------|----------|---------|
| `MOTION` | Тип движения | "LINEAR" |
| `SPINDLE_NAME` | Имя регистра шпинделя | "S" |
| `TECHNOLOGY_TYPE` | Тип технологии | "ALL" |
| `TOOL_LENGTH` | Длина инструмента | 0.0 |
| `CIRCTYPE` | Тип дуги | 0 |
| `LINTOL` | Линейный допуск | 0 |
| `BLOCK_NUMBER` | Номер блока | 1 |
| `BLOCK_INCREMENT` | Шаг нумерации | 2 |

---

### Глобальные переменные

| Переменная | Описание | Default |
|------------|----------|---------|
| `SPINDLE_DEF` | Состояние шпинделя | "CLW" |
| `SPINDLE_RPM` | Обороты шпинделя | 100.0 |
| `TOOL` | Текущий инструмент | 0 |
| `FTOOL` | Первый инструмент | -1 |
| `COOLANT_DEF` | Состояние охлаждения | "FLOOD" |
| `FEEDMODE` | Режим подачи | "FPM" |
| `LAST_FEED` | Последняя подача | 0.0 |
| `STRATEGY_RTCP` | Стратегия RTCP | 1 |

---

## Дополнительные ресурсы

- [PYTHON_MACROS_GUIDE.md](PYTHON_MACROS_GUIDE.md) — Полное руководство по макросам
- [QUICKSTART.md](QUICKSTART.md) — Быстрый старт
- [CUSTOMIZATION_GUIDE.md](CUSTOMIZATION_GUIDE.md) — Руководство по настройке
