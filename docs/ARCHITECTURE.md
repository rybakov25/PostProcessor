# Архитектура постпроцессора

> **Для разработчиков** — подробное описание архитектуры, компонентов и взаимодействия

---

## 📋 Оглавление

1. [Обзор архитектуры](#обзор-архитектуры)
2. [Структура проекта](#структура-проекта)
3. [Компоненты системы](#компоненты-системы)
4. [Загрузка и приоритеты макросов](#загрузка-и-приоритеты-макросов)
5. [Поток выполнения](#поток-выполнения)
6. [API для макросов](#api-для-макросов)
7. [Конфигурация](#конфигурация)
8. [Расширение функциональности](#расширение-функциональности)

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
│   │   │   └── ToolInfo.cs          # Информация об инструменте
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
│   │   │   ├── PythonPostContext.cs # Python-обёрка контекста
│   │   │   └── PythonAptCommand.cs  # Python-обёрка команды
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

**Методы вывода:**

```python
context.write("G01 X100")           # Вывод с номером блока
context.write("G01 X100", True)     # Без номера блока
context.comment("Текст")            # Комментарий в скобках
context.warning("Предупреждение")   # Предупреждение
context.writeln()                   # Пустая строка
```

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
