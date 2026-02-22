# 🔄 Интеграция СПРУТ и IMSpost

> **Архитектура постпроцессора** объединяет лучшие решения из СПРУТ CAM и IMSpost

---

## 📋 Оглавление

1. [Обзор архитектуры](#обзор-архитектуры)
2. [СПРУТ SDK → Наши решения](#спрут-sdk--наши-решения)
3. [IMSpost → Наши решения](#imspost--наши-решения)
4. [Интегрированные компоненты](#интегрированные-компоненты)
5. [Примеры использования](#примеры-использования)

---

## Обзор архитектуры

```
┌─────────────────────────────────────────────────────────┐
│                    PostProcessor v1.1                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────┐         ┌──────────────────┐      │
│  │   СПРУТ SDK      │         │    IMSpost       │      │
│  │  (DotnetPost)    │         │   (hlpfiles)     │      │
│  ├──────────────────┤         ├──────────────────┤      │
│  │ TPostprocessor   │         │ *.def macros     │      │
│  │ TTextNCFile      │         │ init.def         │      │
│  │ NCBlock          │         │ goto.def         │      │
│  │ NCWord           │         │ spindl.def       │      │
│  │ Register         │         │ coolnt.def       │      │
│  └────────┬─────────┘         └────────┬─────────┘      │
│           │                            │                │
│           └──────────┬─────────────────┘                │
│                      │                                  │
│              ┌───────▼────────┐                         │
│              │  PostProcessor │                         │
│              │   Core (C#)    │                         │
│              ├────────────────┤                         │
│              │ NCWord.cs      │                         │
│              │ BlockWriter.cs │                         │
│              │ Register.cs    │                         │
│              │ FormatSpec.cs  │                         │
│              └────────────────┘                         │
│                      │                                  │
│              ┌───────▼────────┐                         │
│              │ Python Macros  │                         │
│              ├────────────────┤                         │
│              │ base/ (18)     │                         │
│              │  - goto.py     │                         │
│              │  - spindl.py   │                         │
│              │  - coolnt.py   │                         │
│              │  - fedrat.py   │                         │
│              │  - rapid.py    │                         │
│              │  - delay.py    │ ← Новый                 │
│              │  - seqno.py    │ ← Новый                 │
│              │  - cutcom.py   │ ← Новый                 │
│              │  - from.py     │ ← Новый                 │
│              │  - gohome.py   │ ← Новый                 │
│              │  - wplane.py   │ ← Новый                 │
│              │  - cycle81.py  │ ← Новый                 │
│              │  - cycle83.py  │ ← Новый                 │
│              │  - subprog.py  │ ← Новый                 │
│              │ siemens/ (18)  │ ← Контроллер-специф.    │
│              └────────────────┘                         │
│                                                         │
│              **Итого: 36 макросов**                     │
│              (18 base + 18 siemens)                     │
└─────────────────────────────────────────────────────────┘
```

---

## СПРУТ SDK → Наши решения

### 1. **TPostprocessor → PythonMacroEngine**

| СПРУТ | Наш проект | Статус |
|-------|------------|--------|
| `TPostprocessor` (базовый класс) | `PythonMacroEngine` (интерпретатор) | ✅ Своя реализация |
| `OnGoto()`, `OnCircle()` | `goto.py`, `arc.py` (макросы) | ✅ Гибче |
| `CLDProject`, `ICLDCommand` | `APTCommand`, `StreamingAPTLexer` | ✅ Проще |

### 2. **TTextNCFile → PostContext + BlockWriter**

| СПРУТ | Наш проект | Статус |
|-------|------------|--------|
| `TTextNCFile` (выходной файл) | `PostContext` + `TextWriter` | ✅ |
| `NCBlock` (формирование блоков) | `BlockWriter.cs` | ✅ Интегрировано |
| `NCWord`, `NumericNCWord` | `NCWord.cs`, `Register.cs` | ✅ Наследование |

### 3. **NCBlock → BlockWriter**

**СПРУТ:**
```csharp
NCBlock Block;
Block.Out();  // Вывод с модальностью
```

**Наш проект:**
```csharp
BlockWriter BlockWriter;
BlockWriter.WriteBlock();  // Вывод с модальностью
```

**Преимущества:**
- ✅ Автоматическая модальность
- ✅ Гибкое управление (Hide/Show)
- ✅ Настройка разделителей

### 4. **NumericNCWord → Register + FormatSpec**

**СПРУТ:**
```csharp
NumericNCWord X = new("X{-####!0##}", 0);
```

**Наш проект:**
```csharp
Register X = new("X", 0.0, true, "F4.3");
// Или с FormatSpec:
FormatSpec spec = FormatSpec.Parse("X{-####!0##}");
```

**Преимущества:**
- ✅ Простой формат "F4.3" по умолчанию
- ✅ Поддержка сложных форматов через `FormatSpec`
- ✅ Культурно-независимое форматирование

---

## IMSpost → Наши решения

### 1. **init.def → init.py**

| IMSpost | Наш проект | Статус |
|---------|------------|--------|
| `GLOBAL.LASTCYCLE`, `GLOBAL.TOOLCNT` | `context.globalVars.*` | ✅ |
| `SYSTEM.SPINDLE_NAME`, `SYSTEM.MOTION` | `context.system.*` | ✅ |
| `REGISTER.[S].VALUE` | `context.registers.s` | ✅ |

### 2. **use1set.def → use1set.py**

**IMSpost:**
```
USE1SET/LINEAR,POSITION,CLW,CCLW
```

**Наш проект:**
```python
# use1set.py автоматически вызывается
USE1SET/LINEAR,POSITION  # Установить модальность
```

**Интеграция с BlockWriter:**
```python
def execute(context, command):
    # Добавление модальности
    _add_modality(context, 'LINEAR', 'POSITION')
    
    # Применение к BlockWriter
    _apply_to_blockwriter(context, 'LINEAR', 'POSITION')
```

### 3. **force.def → force.py**

**IMSpost:**
```
FORCE/MINUS,AAXIS,5AXIS
```

**Наш проект:**
```python
# force.py
FORCE/MINUS,AAXIS,5AXIS  # Принудительно минус для оси A
```

**Интеграция:**
```python
def execute(context, command):
    # Формирование условия
    condition = f'MACHINE.{axis}.ABSOLUTE{direction}{value}'
    
    # Обновление GLOBAL и SYSTEM
    context.globalVars.FORCE_WAY = condition
    context.system.FORCE_WAY = condition
```

### 4. **rtcp.def → rtcp.py**

**IMSpost:**
```
RTCP/ON   → OUTPUT(MODE.RTCP.ON)
RTCP/OFF  → OUTPUT(MODE.RTCP.OFF)
```

**Наш проект:**
```python
# rtcp.py
RTCP/ON   → context.write("RTCPON")
RTCP/OFF  → context.write("RTCPOF")
```

**Интеграция с BlockWriter:**
```python
def _force_registers(context):
    # Принудительный вывод всех осей
    for axis in ['X', 'Y', 'Z', 'A', 'B', 'C']:
        reg = _get_register(context, axis)
        reg.ForceChanged()
    
    context.writeBlock()  # Вывод через BlockWriter
```

### 5. **seqno.def → seqno.py**

**IMSpost:**
```
SEQNO/ON
SEQNO/OFF
SEQNO/START,100
SEQNO/INCR,5
```

**Наш проект:**
```python
# seqno.py
SEQNO/ON           → context.BlockWriter.BlockNumberingEnabled = True
SEQNO/OFF          → context.BlockWriter.BlockNumberingEnabled = False
SEQNO/START,100    → context.globalVars.BLOCK_NUMBER = 100
SEQNO/INCR,5       → context.globalVars.BLOCK_INCREMENT = 5
```

### 6. **delay.def → delay.py**

**IMSpost:**
```
DELAY/2.5      → G04 X2.5
DELAY/REV,10   → G04 P(10*60/RPM)
```

**Наш проект:**
```python
# delay.py
DELAY/2.5      → context.write("G04 X2.500")
DELAY/REV,10   → Конвертация в секунды → G04
```

---

## Интегрированные компоненты

### 1. **BlockWriter + USE1SET**

```python
# use1set.py устанавливает модальность
USE1SET/LINEAR,POSITION

# BlockWriter использует модальность
context.registers.x = 100.5
context.registers.y = 200.3
context.writeBlock()  # Выводит только изменённые
```

### 2. **BlockWriter + RTCP**

```python
# rtcp.py принудительно выводит все оси
RTCP/ON

# _force_registers() обновляет все регистры
for axis in ['X', 'Y', 'Z', 'A', 'B', 'C']:
    reg.ForceChanged()

context.writeBlock()  # Вывод всех осей
```

### 3. **FormatSpec + IMSpost форматы**

```python
# IMSpost style форматы
FormatSpec.Parse("X{-####!0##}")  # Знак только минус, точка всегда

# Простые форматы
Register("X", format="F4.3")  # 4 цифры до точки, 3 после
```

### 4. **GLOBAL/SYSTEM + context**

| IMSpost | Наш проект |
|---------|------------|
| `GLOBAL.SPINDLE_RPM` | `context.globalVars.SPINDLE_RPM` |
| `SYSTEM.MOTION` | `context.system.MOTION` |
| `REGISTER.[S].VALUE` | `context.registers.s.Value` |
| `MODE.COOLNT` | `context.machine.coolant` |

---

## Примеры использования

### Пример 1: Инициализация с настройками IMSpost

```python
# init.py
def execute(context, command):
    # IMSpost-style инициализация
    context.globalVars.LASTCYCLE = 'DRILL'
    context.globalVars.TOOLCNT = 0
    context.globalVars.SPINDLE_DEF = 'CLW'
    context.globalVars.COOLANT_DEF = 'FLOOD'
    
    # Настройка BlockWriter
    context.setBlockNumbering(start=1, increment=2, enabled=True)
    
    # Вывод списка инструментов (опционально)
    if context.config.get("printToolListAtStart", False):
        _print_tool_list(context)
```

### Пример 2: Управление модальностью через USE1SET

```python
# use1set.py
def execute(context, command):
    # Установка модальности для LINEAR
    USE1SET/LINEAR,POSITION
    
    # Применение к BlockWriter
    context.registers.x = 100.5
    context.registers.y = 200.3
    context.writeBlock()  # Выводит только изменённые
```

### Пример 3: RTCP с принудительным выводом

```python
# rtcp.py
def execute(context, command):
    RTCP/ON
    
    # Вывод команды
    context.write("RTCPON")
    
    # Принудительный вывод всех осей
    _force_registers(context)
    
    # Обновление системных переменных
    context.system.COORD_RTCP = 1
```

### Пример 4: FORCE для управления направлением

```python
# force.py
def execute(context, command):
    FORCE/MINUS,AAXIS,5AXIS
    
    # Формирование условия
    condition = f'MACHINE.A.ABSOLUTE<0'
    
    # Обновление переменных
    context.globalVars.STRATEGY_BEST_SOL_5X = condition
    context.system.FORCE_WAY = condition
```

---

## 📊 Сравнительная таблица

| Функция | СПРУТ | IMSpost | Наш проект | Статус |
|---------|-------|---------|------------|--------|
| **Базовый класс** | `TPostprocessor` | `*.def` | Python макросы | ✅ |
| **Выходной файл** | `TTextNCFile` | `OUTPUT()` | `BlockWriter` | ✅ |
| **Модальность** | `NCBlock` | `MODE.MODAL` | `BlockWriter` | ✅ |
| **Регистры** | `NCWord` | `REGISTER` | `Register` | ✅ |
| **Форматы** | `{-####!0##}` | `F4.3` | `FormatSpec` | ✅ |
| **Циклы** | `CycleState` | `CYCLE_*` | `cycle_cache.py` | ✅ |
| **RTCP** | `fiveAxis` | `RTCP/ON` | `rtcp.py` | ✅ |
| **FORCE** | ❌ | `FORCE/*` | `force.py` | ✅ |
| **USE1SET** | ❌ | `USE1SET/*` | `use1set.py` | ✅ |
| **SEQNO** | ❌ | `SEQNO/*` | `seqno.py` | ✅ |

---

## 🎯 Преимущества интеграции

1. **Гибкость Python** + **Надёжность C#**
2. **Модульность IMSpost** + **Архитектура СПРУТ**
3. **BlockWriter** автоматически управляет модальностью
4. **FormatSpec** поддерживает оба стиля форматирования
5. **GLOBAL/SYSTEM** переменные совместимы с IMSpost
6. **Макросы** легко портируются из IMSpost

---

<div align="center">

**PostProcessor v1.1** — Лучшее из СПРУТ и IMSpost

[Начать работу](../README.md) • [Документация](../docs/)

</div>
