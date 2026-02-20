# Руководство по переписыванию макросов IMSpost на Python

## Обзор

Этот документ описывает процесс переписывания макросов IMSpost (.def файлы) на Python для постпроцессора Siemens 840D.

## Соответствие IMSpost → Python

### Таблица соответствий

| IMSpost элемент    | Python эквивалент                     | Описание                 |
| ------------------ | ------------------------------------- | ------------------------ |
| `CLDATAN.1`        | `command.numeric[0]`                  | Первое числовое значение |
| `CLDATAN.2`        | `command.numeric[1]`                  | Второе числовое значение |
| `CLDATAM`          | `command.minorWords`                  | Ключевые слова команды   |
| `SYSTEM.MOTION`    | `context.motion_type`                 | Тип движения             |
| `REGISTER.X.VALUE` | `context.registers.x`                 | Регистр X                |
| `REGISTER.Y.VALUE` | `context.registers.y`                 | Регистр Y                |
| `REGISTER.Z.VALUE` | `context.registers.z`                 | Регистр Z                |
| `REGISTER.F.VALUE` | `context.registers.f`                 | Регистр подачи           |
| `REGISTER.S.VALUE` | `context.registers.s`                 | Регистр шпинделя         |
| `REGISTER.T.VALUE` | `context.registers.t`                 | Регистр инструмента      |
| `MODE.SPINDLE`     | `context.spindle_mode`                | Режим шпинделя           |
| `MODE.COOLNT`      | `context.coolant_mode`                | Режим охлаждения         |
| `OUTPUT(...)`      | `context.write(...)`                  | Вывод G-кода             |
| `CALL(MACRO/)`     | Вызов функции Python                  | Вызов макроса            |
| `CASE/ENDCASE`     | `if/elif/else`                        | Условные конструкции     |
| `WHILE/ENDWHILE`   | `while`                               | Циклы                    |
| `GLOBAL.VARIABLE`  | `setattr(context, 'variable', value)` | Глобальные переменные    |
| `SOLUTION(...)`    | `_solve_kinematics()`                 | Решение кинематики       |

---

## Структура Python макроса

### Базовый шаблон

```python
# -*- coding: ascii -*-
# ============================================================================
# MACRO NAME - DESCRIPTION (Siemens 840D)
# ============================================================================
# Переписано с IMSpost <macro_name>.def
# Краткое описание функциональности
# ============================================================================

def execute(context, command):
    """
    Process <MACRO> command
    
    IMSpost logic:
    - Описание основной логики из IMSpost
    - Ключевые условия и проверки
    - Вызываемые макросы
    
    APT Example:
      <MACRO>/parameters
    """
    # 1. Проверка входных параметров
    if not command.numeric or len(command.numeric) == 0:
        return
    
    # 2. Получение значений из команды
    value = command.numeric[0]
    
    # 3. Проверка условий (аналог IF в IMSpost)
    if some_condition:
        _process_special_case(context, command)
        return
    
    # 4. Обновление регистров
    context.registers.x = value
    
    # 5. Вывод G-кода
    context.write("G01 X" + str(value))
```

---

## Переписанные макросы

### P0 - Критические макросы

#### 1. goto.def → base/goto.py

**Назначение:** Обработка линейных перемещений GOTO

**IMSpost логика:**
```
IF(SYSTEM.MOTION = 'CYCLE')
   CALL(CYCLMOTN/)
   BREAK
ENDIF

IF ((SYSTEM.LINTOL) AND (SYSTEM.MOTION = 'LINEAR'))
   SOLUTION(LINTOL/...,"CORD")
   ...
ENDIF

CASE (SYSTEM.MOTION)
   'LINEAR': OUTPUT(MODE.MOTION.LINEAR,NEWLIN)
   'RAPID': OUTPUT(MODE.MOTION.POSITION,NEWLIN)
   'SEQUENCE': ...
ENDCASE
```

**Python реализация:**
```python
def execute(context, command):
    # Проверка на цикл
    motion_type = getattr(context, 'motion_type', 'LINEAR')
    
    if motion_type == 'CYCLE':
        _process_cycle_motion(context, command)
        return
    
    # LINTOL обработка
    lintol_enabled = getattr(context, 'lintol_enabled', False)
    
    if lintol_enabled and motion_type == 'LINEAR':
        lintol_output = _process_lintol(context, command)
    
    # Решение кинематики
    solution = _solve_kinematics(context, command)
    
    # Вывод в зависимости от типа движения
    if motion_type == 'LINEAR':
        _output_linear(context, solution, lintol_output)
    elif motion_type == 'RAPID':
        _output_rapid(context, solution)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\base\goto.py`

---

#### 2. rapid.def → base/rapid.py

**Назначение:** Быстрые перемещения

**IMSpost логика:**
```
IF(GLOBAL.TLCHNG=0)
   SYSTEM.MOTION = GLOBAL.RAPID_TYPE
ENDIF

IF (GLOBAL.RAPID_RESTORE_FEED)
   CALL(USE1SET/SYSTEM.FEEDRATE_NAME,...)
ENDIF
```

**Python реализация:**
```python
def execute(context, command):
    # Проверка на смену инструмента
    tool_change_active = getattr(context, 'tool_change_active', False)
    
    if not tool_change_active:
        # Устанавливаем тип движения как RAPID
        pass
    
    # Восстановление подачи
    restore_feed = getattr(context, 'rapid_restore_feed', False)
    
    if restore_feed:
        _restore_feed_modality(context)
    
    # Вывод быстрого перемещения
    _output_rapid_move(context, x, y, z)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\base\rapid.py`

---

#### 3. spindl.def → base/spindl.py

**Назначение:** Управление шпинделем

**IMSpost логика:**
```
IF (CLDATAN.0)
   GLOBAL.SPINDLE_RPM = CLDATAN.1
   SYSTEM.SPIN = CLDATAN.1
ENDIF

CASE (CLDATAM)
   'CLW':   SPINDLE = "CLW", GLOBAL.SPINDLE_DEF = 'CLW'
   'CCLW':  SPINDLE = "CCLW", GLOBAL.SPINDLE_DEF = 'CCLW'
   'OFF':   SPINDLE = "OFF"
   'ON':    SPINDLE = GLOBAL.SPINDLE_DEF
   'MAXRPM': IF (CLDATAN.RIGHT.MAXRPM.0) → SYSTEM.MAX_CSS = ...
ENDCASE

IF ((GLOBAL.SPINDLE_BLOCK = 1) AND ...)
   CALL(USE1SET/...)
ELSE
   OUTPUT(MODE.MODAL.SPINDLE,MODE.MODAL.SPEED,NEWLIN)
ENDIF
```

**Python реализация:**
```python
def execute(context, command):
    # Установка оборотов
    if command.numeric and len(command.numeric) > 0:
        spindle_rpm = command.numeric[0]
        context.registers.s = spindle_rpm
    
    # Определение состояния
    spindle_state = _get_spindle_state(context, command)
    
    # Проверка на блокировку вывода
    spindle_block = getattr(context, 'spindle_block', False)
    
    if spindle_block and spindle_state != 'off':
        _set_spindle_modality(context, spindle_state, spindle_rpm)
    else:
        _output_spindle_command(context, spindle_state, spindle_rpm)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\base\spindl.py`

---

#### 4. coolnt.def → base/coolnt.py

**Назначение:** Управление охлаждением

**IMSpost логика:**
```
CASE (CLDATAM)
   'MIST':  COOLANT = "MIST", GLOBAL.COOLANT_DEF = 'MIST'
   'FLOOD': COOLANT = "FLOOD", GLOBAL.COOLANT_DEF = 'FLOOD'
   'THRU':  COOLANT = "THRU", GLOBAL.COOLANT_DEF = 'THRU'
   'OFF':   COOLANT = "OFF"
   'ON':    COOLANT = GLOBAL.COOLANT_DEF
ENDCASE

IF ((GLOBAL.TOOLCHG_COOLON) AND (GLOBAL.TLCHNG) AND (MODE.COOLNT<>"OFF"))
   BREAK
ENDIF

IF ((GLOBAL.COOLANT_BLOCK = 1) AND (MODE.COOLNT<>"OFF"))
   CALL(USE1SET/...)
ELSE
   OUTPUT(MODE.MODAL.COOLNT,NEWLIN)
ENDIF
```

**Python реализация:**
```python
def execute(context, command):
    # Определение состояния
    coolant_state = _get_coolant_state(context, command)
    
    # Проверка на смену инструмента
    toolchg_coolon = getattr(context, 'toolchg_coolon', False)
    tool_change_active = getattr(context, 'tool_change_active', False)
    
    if toolchg_coolon and tool_change_active and coolant_state != 'off':
        return
    
    # Вывод команды
    coolant_block = getattr(context, 'coolant_block', False)
    
    if coolant_block and coolant_state != 'off':
        _set_coolant_modality(context, coolant_state)
    else:
        _output_coolant_command(context, coolant_state)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\base\coolnt.py`

---

#### 5. fedrat.def → base/fedrat.py

**Назначение:** Управление подачей

**IMSpost логика:**
```
GLOBAL.FEED_PROG = CLDATAN.1
SYSTEM.FEED = CLDATAN.1
REGISTER.[SYSTEM.FEEDRATE_NAME].VALUE = CLDATAN.1

CASE (CL)
   'FPM':   GLOBAL.FEEDMODE = "FPM", установка модальности
   'FPR':   GLOBAL.FEEDMODE = "FPR", конвертация в мм/мин
   'IPM':   GLOBAL.FEEDMODE = "FPM", inch/min
   'IPR':   GLOBAL.FEEDMODE = "FPR", inch/rev
   'MMPM':  GLOBAL.FEEDMODE = "FPM", mm/min
   'MMPR':  GLOBAL.FEEDMODE = "FPR", mm/rev
   'INVTIM':GLOBAL.FEEDMODE = "FIT", inverse time
ENDCASE

IF (GLOBAL.FEED_BLOCK = 1)
   CALL(USE1SET/...)
ELSE
   OUTPUT(MODE.MODAL.FEED,NEWLIN)
ENDIF
```

**Python реализация:**
```python
def execute(context, command):
    # Установка значения подачи
    feed_value = command.numeric[0]
    context.registers.f = feed_value
    
    # Определение режима
    feed_mode = _get_feed_mode(context, command)
    
    # Обработка по типу режима
    if feed_mode == 'FPM':
        new_feed_mode = _process_fpm(context, feed_value)
    elif feed_mode == 'FPR':
        new_feed_mode = _process_fpr(context, feed_value)
    # ... другие режимы
    
    # Вывод подачи
    feed_block = getattr(context, 'feed_block', False)
    
    if feed_block:
        _set_feed_modality(context, new_feed_mode)
    else:
        _output_feed_command(context, feed_value, new_feed_mode)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\base\fedrat.py`

---

### P1 - Макросы для MMILL

#### 6. loadtl.def → mmill/loadtl.py

**Назначение:** Смена инструмента

**IMSpost логика:**
```
IF (GLOBAL.CHANNEL_TOOL = "TURRET")
   CALL(TURRET/)
   BREAK
ENDIF

CALL(RTCP/OFF)
CALL(WPLANE/OFF)

CASE(CLDATAM)
   "OSETNO": GLOBAL.HVAL = CLDATAN.RIGHT.OSETNO.1
   "ADJUST": GLOBAL.HVAL = CLDATAN.RIGHT.ADJUST
   "LENGTH": SYSTEM.TOOL_LENGTH = CLDATAN.RIGHT.LENGTH.1
   "MILL":   SYSTEM.TECHNOLOGY_TYPE = "MILLING"
   "TURN":   SYSTEM.TECHNOLOGY_TYPE = "TURNING"
ENDCASE

IF(GLOBAL.TOOLCHG_SEQUENCE)
   GLOBAL.TOOL = GLOBAL.TOOL + 1
ELSE
   GLOBAL.TOOL = CLDATAN.1
ENDIF

OUTPUT(T-command, M6, positioning, RTCPON)
```

**Python реализация:**
```python
def execute(context, command):
    global _block_number
    
    # Проверка на револьверную головку
    channel_tool = getattr(context, 'channel_tool', '')
    if channel_tool == "TURRET":
        _process_turret_change(context, command)
        return
    
    # Выключение RTCP и WPLANE
    _disable_rtcp_and_wplane(context)
    
    # Обработка параметров
    hval = 1
    if command.minorWords:
        for i, word in enumerate(command.minorWords):
            if word.upper() == 'OSETNO':
                hval = int(command.minorWords[i + 1])
            elif word.upper() == 'MILL':
                setattr(context, 'technology_type', 'MILLING')
    
    # Определение номера инструмента
    if command.numeric:
        new_tool = int(command.numeric[0])
    
    # Вывод команд смены
    context.write("N" + str(_block_number) + " T" + str(new_tool))
    context.write("N" + str(_block_number) + " M6")
    context.write("N" + str(_block_number) + " RTCPON")
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\mmill\loadtl.py`

---

#### 7. init.def → mmill/init.py

**Назначение:** Инициализация глобальных переменных и программы

**IMSpost логика:**
```
GLOBAL.LASTCYCLE = 'DRILL'
GLOBAL.FEEDMODE = "FPM"
GLOBAL.STRATEGY_RTCP = 1
GLOBAL.STRATEGY_3X_MILLING = 2
GLOBAL.WPLANE_ONOFF = 1
GLOBAL.TOOLCHG_TREG = "T"
GLOBAL.TOOLCHG_LREG = "D"
...
SYSTEM.MOTION = "LINEAR"
GLOBAL.LINEAR_TYPE = "LINEAR"
GLOBAL.RAPID_TYPE = "RAPID_BREAK"
...
```

**Python реализация:**
```python
def execute(context, command):
    global _block_number
    _block_number = 10
    
    # Инициализация глобальных переменных
    setattr(context, 'lastcycle', 'DRILL')
    setattr(context, 'feedmode', 'FPM')
    setattr(context, 'strategy_rtcp', 1)
    setattr(context, 'strategy_3x_milling', 2)
    setattr(context, 'motion_type', 'LINEAR')
    setattr(context, 'tool', 0)
    setattr(context, 'ftool', -1)
    ...
    
    # Вывод заголовка программы
    header = context.config.header
    for line in header.lines:
        context.write(line)
    
    # Начальные блоки
    context.write("N10 G54 G40 G90 G94 CUT2DF G17")
    context.write("N20 TRANS")
    context.write("N30 RTCPOF")
    context.write("N40 CYCLE800(...)")
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\mmill\init.py`

---

#### 8. rtcp.def → mmill/rtcp.py

**Назначение:** Управление RTCP (TCP)

**IMSpost логика:**
```
CASE(CLDATAM)
   'ON': IF(MODE.WPLANE="ON") → CALL(WPLANE/OFF)
         OUTPUT(MODE.RTCP.ON,NEWLIN)
         IF(GLOBAL.STRATEGY_RTCP=1) → SYSTEM.COORD_RTCP = 1
         SYSTEM.CIRCTYPE = 10
         
   'OFF': IF(MODE.RTCP<>"OFF") → OUTPUT(MODE.RTCP.OFF,NEWLIN)
          SYSTEM.CIRCTYPE = GLOBAL.CIRCTYPE_SAV
          SYSTEM.COORD_RTCP = 0
ENDCASE

Force registers after RTCP modification
```

**Python реализация:**
```python
def execute(context, command):
    # Определение команды
    state = 'off'
    if command.minorWords:
        word = command.minorWords[0].upper()
        if word == 'ON':
            state = 'on'
    
    if state == 'on':
        _process_rtcp_on(context)
    else:
        _process_rtcp_off(context)

def _process_rtcp_on(context):
    # Проверка на WPLANE
    wplane_mode = getattr(context, 'wplane_mode', 'OFF')
    if wplane_mode == 'ON':
        _call_wplane_off(context)
    
    # Вывод команды включения
    context.write(context.config.fiveAxis.rtcp.on)
    
    # Установка флагов
    strategy_rtcp = getattr(context, 'strategy_rtcp', 1)
    if strategy_rtcp == 1:
        setattr(context, 'coord_rtcp', 1)
    
    setattr(context, 'circtype', 10)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\mmill\rtcp.py`

---

#### 9. rotabl.def → mmill/rotabl.py

**Назначение:** Поворот стола

**IMSpost логика:**
```
STR = GETAPT()
SPLIT("/",STR,MAJOR,CLD)
CLD = "ROTATE/" + CLD
CALL(CLD)
```

**Python реализация:**
```python
def execute(context, command):
    _process_rotate(context, command)

def _process_rotate(context, command):
    # Получение параметров
    angle = command.numeric[0] if command.numeric else 0
    axis = command.minorWords[0] if command.minorWords else 'Z'
    
    # Определение типа поворота
    rotation_type = _get_rotation_type(context, command)
    
    if rotation_type == 'simple':
        _process_simple_rotation(context, angle, axis)
    elif rotation_type == 'discrete':
        _process_discrete_rotation(context, angle, axis)
    elif rotation_type == 'shortest':
        _process_shortest_rotation(context, angle, axis)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\mmill\rotabl.py`

---

#### 10. fini_cfg.def → mmill/fini.py

**Назначение:** Завершение программы

**IMSpost логика:**
```
IF ((GLOBAL.TOOLCHG_REFIRST) AND (GLOBAL.FTOOL >= 0))
   REGISTER.[GLOBAL.TOOLCHG_TREG].VALUE = GLOBAL.FTOOL
   OUTPUT(MODE.MACHINE.TOOLCHG, NEWLIN)
ENDIF

CALL(WPLANE/OFF)
CALL(RTCP/OFF)
CALL(POLAR/OFF)

OUTPUT('M30', NEWLIN)

IF ((GLOBAL.SUBMAX) AND (GLOBAL.SUB_MOVE))
   CALL(SUBMOVE/...)
ENDIF
```

**Python реализация:**
```python
def execute(context, command):
    # Восстановление первого инструмента
    toolchg_refirst = getattr(context, 'toolchg_refirst', False)
    ftool = getattr(context, 'ftool', -1)
    
    if toolchg_refirst and ftool >= 0:
        context.registers.t = ftool
        context.write("T" + str(ftool))
    
    # Выключение WPLANE, RTCP, POLAR
    _disable_wplane(context)
    context.write(context.config.fiveAxis.rtcp.off)
    _disable_polar(context)
    
    # Конец программы
    footer = context.config.footer
    for line in footer.lines:
        context.write(line)
```

**Файл:** `C:\Users\rybak\source\repos\PostProcessor\macros\python\mmill\fini.py`

---

## Статус макросов

| Приоритет | IMSpost макрос | Python файл | Статус |
|-----------|----------------|-------------|--------|
| P0 | goto.def | base/goto.py | ✅ Готов |
| P0 | rapid.def | base/rapid.py | ✅ Готов |
| P0 | spindl.def | base/spindl.py | ✅ Готов |
| P0 | coolnt.def | base/coolnt.py | ✅ Готов |
| P0 | fedrat.def | base/fedrat.py | ✅ Готов |
| P1 | loadtl.def | mmill/loadtl.py | ✅ Готов |
| P1 | init.def | mmill/init.py | ✅ Готов |
| P1 | rtcp.def | mmill/rtcp.py | ✅ Готов |
| P1 | rotabl.def | mmill/rotabl.py | ✅ Готов |
| P2 | fini_cfg.def | mmill/fini.py | ✅ Готов |

---

## Тестовые примеры

### Пример 1: Линейное перемещение

**APT:**
```
GOTO/100, 50, 10
```

**Ожидаемый вывод:**
```
N10 G1 X100. Y50. Z10.
```

### Пример 2: Управление шпинделем

**APT:**
```
SPINDL/ON,CLW,1600
```

**Ожидаемый вывод:**
```
N20 M3
N30 S1600
```

### Пример 3: Смена инструмента

**APT:**
```
LOADTL/5,ADJUST,1,SPINDL,2000,MILL
```

**Ожидаемый вывод:**
```
N70 T5
N80 M6
N90 G0 B0
N100 M101H0
N110 RTCPON
N120 S2000 M3
```

### Пример 4: RTCP

**APT:**
```
RTCP/ON
...обработка...
RTCP/OFF
```

**Ожидаемый вывод:**
```
N10 RTCPON
...обработка...
N20 RTCPOF
```

---

## Рекомендации по разработке

1. **Сохраняйте логику IMSpost** - все условия, циклы, проверки должны быть перенесены
2. **Используйте контекст** - `context.registers`, `context.config`, `context.machine`
3. **Глобальные переменные** - используйте `setattr(context, 'name', value)`
4. **Проверка параметров** - всегда проверяйте наличие `command.numeric` и `command.minorWords`
5. **Форматирование** - используйте `context.config.formatting` для форматирования чисел
6. **Конфигурация** - G/M-коды берите из `context.config.gcode` и `context.config.mcode`

---

## Отладка

Для отладки макросов используйте:

```python
def execute(context, command):
    context.comment(f"DEBUG: majorWord={command.majorWord}")
    context.comment(f"DEBUG: numeric={command.numeric}")
    context.comment(f"DEBUG: minorWords={command.minorWords}")
```

Запуск с debug-флагом:
```bash
dotnet run -- -i input.apt -o output.nc -c siemens --debug
```
