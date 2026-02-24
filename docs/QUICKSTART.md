# 🚀 Быстрый старт: Python макросы за 10 минут

> **Для кого:** Новички без опыта программирования постпроцессоров
> **Время:** 10 минут
> **Результат:** Работающий макрос для вашего станка

---

## 📋 Что вы узнаете

1. ✅ Как устроен постпроцессор
2. ✅ Как написать первый макрос
3. ✅ Как запустить и проверить
4. ✅ Готовые шаблоны для использования

---

## Шаг 1: Понимаем структуру (2 минуты)

### Что такое постпроцессор?

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   CAM-система   │     │   Постпроцессор  │     │    Станок ЧПУ   │
│   (CATIA, NX)   │────▶│ + Python макросы │────▶│    (Siemens,    │
│                 │     │                  │     │    Fanuc...)    │
└─────────────────┘     └──────────────────┘     └─────────────────┘
      APT-файл                G-код                   Обработка
```

### Структура проекта

```
PostProcessor/
├── macros/
│   └── python/
│       ├── base/      # Базовые макросы (не трогать)
│       ├── mmill/     # Макросы для вашего станка
│       └── user/      # Ваши макросы (здесь работаем)
├── configs/
│   └── machines/      # Конфигурации станков
└── docs/              # Документация
```

---

## Шаг 2: Пишем первый макрос (3 минуты)

### Создайте файл

Создайте файл `macros/python/user/my_first_macro.py`:

```python
# -*- coding: ascii -*-
# Мой первый макрос

def execute(context, command):
    """Приветственный макрос"""
    context.comment("=== Привет от Python макроса! ===")
    context.write("G0 X0 Y0 Z50")
```

### Разбираем по строкам

| Строка | Что делает |
|--------|------------|
| `# -*- coding: ascii -*-` | Кодировка файла (обязательно) |
| `def execute(context, command):` | Функция макроса (обязательно) |
| `context.comment(...)` | Вывод комментария в скобках |
| `context.write(...)` | Вывод G-кода |

### Запомните 2 команды

```python
context.write("G01 X100")     # Вывести G-код
context.comment("Текст")      # Вывести комментарий
```

---

## Шаг 3: Запускаем (2 минуты)

### Подготовка тестового APT-файла

Создайте файл `test.apt`:

```apt
PARTNO/TEST_PART
GOTO/0, 0, 0
GOTO/100, 50, 10
SPINDL/ON, CLW, 1600
FEDRAT/500
FINI
```

### Запуск постпроцессора

Откройте командную строку в папке проекта:

```bash
dotnet run -- -i test.apt -o output.nc -c siemens
```

### Проверяем результат

Откройте `output.nc`:

```nc
(=== Привет от Python макроса! ===)
N1 G0 X0. Y0. Z50.
(Program continues...)
```

🎉 **Готово!** Вы написали и запустили первый макрос!

---

## Шаг 4: Продвинутые возможности (5 минут)

### StateCache — кэш состояний

Кэширование LAST_* переменных для модального вывода:

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

### CycleCache — кэширование циклов

Автоматический выбор: полное определение или вызов:

```python
# -*- coding: ascii -*-
def execute(context, command):
    params = {
        'MODE': 1,
        'X': 100.0,
        'Y': 200.0,
        'Z': 50.0
    }
    
    # Умный вывод
    context.cycleWriteIfDifferent("CYCLE800", params)
```

**Результат:**
```nc
; Первый вызов (полное определение)
CYCLE800(MODE=1, X=100.000, Y=200.000, Z=50.000)

; Второй вызов (те же параметры - только вызов)
CYCLE800()
```

### NumericNCWord — форматирование из конфига

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

### TextNCWord — комментарии со стилем

```python
# -*- coding: ascii -*-
def execute(context, command):
    # Автоматически использует стиль из конфига
    context.comment("Начало операции")
    
    # Siemens: (Начало операции)
    # Haas: ; Начало операции
```

---

## Шаг 5: Модифицируем макрос (3 минуты)

### Добавляем чтение параметров

Изменим макрос для обработки координат из APT:

```python
# -*- coding: ascii -*-
# GOTO с координатами

def execute(context, command):
    """Обработка GOTO с координатами"""

    # Проверяем наличие параметров
    if not command.numeric or len(command.numeric) == 0:
        return

    # Получаем координаты
    x = command.numeric[0]
    y = command.numeric[1] if len(command.numeric) > 1 else 0
    z = command.numeric[2] if len(command.numeric) > 2 else 0

    # Обновляем регистры
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    # Выводим G-код
    context.write(f"G1 X{x:.3f} Y{y:.3f} Z{z:.3f}")
```

### Тестируем

**APT:**
```apt
GOTO/100, 50, 10
```

**Вывод:**
```nc
N1 G1 X100.000 Y50.000 Z10.000
```

---

## 📚 Готовые шаблоны макросов

Копируйте эти шаблоны в `macros/python/user/` и модифицируйте под ваши нужды.

### Шаблон 1: Базовый GOTO

```python
# -*- coding: ascii -*-
# GOTO - Линейное перемещение

def execute(context, command):
    """
    APT: GOTO/X, Y, Z

    Вывод: G1 X... Y... Z...
    """
    if not command.numeric:
        return

    x = command.numeric[0] if len(command.numeric) > 0 else context.registers.x
    y = command.numeric[1] if len(command.numeric) > 1 else context.registers.y
    z = command.numeric[2] if len(command.numeric) > 2 else context.registers.z

    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    context.write(f"G1 X{x:.3f} Y{y:.3f} Z{z:.3f}")
```

---

### Шаблон 2: Управление шпинделем

```python
# -*- coding: ascii -*-
# SPINDL - Управление шпинделем

def execute(context, command):
    """
    APT: SPINDL/ON, CLW, 1600
         SPINDL/OFF

    Вывод: M3 S... / M5
    """
    # Получаем обороты
    rpm = command.numeric[0] if command.numeric else 0
    context.registers.s = rpm

    # Определяем состояние
    state = "OFF"
    if command.minorWords:
        for word in command.minorWords:
            w = word.upper()
            if w in ["ON", "CLW", "CLOCKWISE"]:
                state = "CW"
            elif w in ["CCLW", "CCW"]:
                state = "CCW"
            elif w == "OFF":
                state = "OFF"

    # Вывод команд
    if state == "CW":
        context.write("M3")
        if rpm > 0:
            context.write(f"S{int(rpm)}")
    elif state == "CCW":
        context.write("M4")
        if rpm > 0:
            context.write(f"S{int(rpm)}")
    else:
        context.write("M5")
```

---

### Шаблон 3: Управление охлаждением

```python
# -*- coding: ascii -*-
# COOLNT - Управление охлаждением

def execute(context, command):
    """
    APT: COOLNT/ON
         COOLNT/FLOOD
         COOLNT/MIST
         COOLNT/OFF

    Вывод: M8 / M7 / M9
    """
    state = "FLOOD"  # По умолчанию

    if command.minorWords:
        for word in command.minorWords:
            w = word.upper()
            if w in ["ON", "FLOOD"]:
                state = "FLOOD"
            elif w == "MIST":
                state = "MIST"
            elif w == "OFF":
                state = "OFF"

    if state == "FLOOD":
        context.write("M8")
    elif state == "MIST":
        context.write("M7")
    else:
        context.write("M9")
```

---

### Шаблон 4: Управление подачей (модальное) с StateCache

```python
# -*- coding: ascii -*-
# FEDRAT - Подача (модальная)

def execute(context, command):
    """
    APT: FEDRAT/500

    Вывод: F... (только при изменении)
    """
    if not command.numeric:
        return

    feed = command.numeric[0]
    
    # Проверка изменения через StateCache
    if context.cacheHasChanged("LAST_FEED", feed):
        context.registers.f = feed
        context.writeBlock()
        context.cacheSet("LAST_FEED", feed)
```

---

### Шаблон 5: Быстрое перемещение

```python
# -*- coding: ascii -*-
# RAPID - Быстрое перемещение

def execute(context, command):
    """
    APT: RAPID/X, Y, Z

    Вывод: G0 X... Y... Z...
    """
    # Устанавливаем тип движения RAPID
    context.system.MOTION = "RAPID"
    context.currentMotionType = "RAPID"

    if not command.numeric:
        return

    x = command.numeric[0] if len(command.numeric) > 0 else context.registers.x
    y = command.numeric[1] if len(command.numeric) > 1 else context.registers.y
    z = command.numeric[2] if len(command.numeric) > 2 else context.registers.z

    context.registers.x = x
    context.registers.y = y
    context.registers.z = z

    context.write(f"G0 X{x:.3f} Y{y:.3f} Z{z:.3f}")
```

---

### Шаблон 6: Смена инструмента

```python
# -*- coding: ascii -*-
# LOADTL - Смена инструмента

def execute(context, command):
    """
    APT: LOADTL/5

    Вывод: T5 M6
    """
    if not command.numeric:
        context.warning("LOADTL требует номер инструмента")
        return

    new_tool = int(command.numeric[0])

    # Проверка на тот же инструмент
    if context.globalVars.TOOL == new_tool:
        return

    context.registers.t = new_tool
    context.globalVars.TOOL = new_tool

    context.write(f"T{new_tool}")
    context.write("M6")
```

---

### Шаблон 7: Инициализация программы

```python
# -*- coding: ascii -*-
# INIT - Начало программы

def execute(context, command):
    """
    APT: PARTNO/NAME

    Вывод: Заголовок, начальные G-коды
    """
    # Инициализация счетчиков
    context.globalVars.SetInt("BLOCK_NUMBER", 1)
    context.globalVars.SetInt("BLOCK_INCREMENT", 2)
    context.globalVars.SetDouble("LAST_FEED", 0.0)

    # Заголовок
    context.comment(f"Program: {command.getString(0, 'UNKNOWN')}")
    context.comment(f"Date: {context.config.getParameterString('dateTime', 'N/A')}")

    # Начальные команды
    context.write("G54 G40 G90 G94 G17")
    context.write("G0 Z100.")
```

---

### Шаблон 8: Завершение программы

```python
# -*- coding: ascii -*-
# FINI - Конец программы

def execute(context, command):
    """
    APT: FINI

    Вывод: Отвод, M5, M9, M30
    """
    # Отвод по Z
    context.write("G0 Z100.")

    # Выключение шпинделя
    context.write("M5")

    # Выключение охлаждения
    context.write("M9")

    # Конец программы
    context.write("M30")
```

---

### Шаблон 9: Цикл с CycleCache

```python
# -*- coding: ascii -*-
# CYCLE800 - Поворотная ось (с кэшированием)

def execute(context, command):
    """
    APT: CYCLE800/MODE, X, Y, Z, ...

    Вывод: CYCLE800(...) с автоматическим кэшированием
    """
    params = {
        'MODE': command.getNumeric(0, 1),
        'X': command.getNumeric(1, 0.0),
        'Y': command.getNumeric(2, 0.0),
        'Z': command.getNumeric(3, 0.0)
    }
    
    # Умный вывод с кэшированием
    context.cycleWriteIfDifferent("CYCLE800", params)
```

---

### Шаблон 10: NumericNCWord с форматированием

```python
# -*- coding: ascii -*-
# AXIS - Перемещение с форматированием из конфига

def execute(context, command):
    """
    APT: AXIS/X, Y, Z

    Вывод: X... Y... Z... с форматом из конфига
    """
    if not command.numeric:
        return

    x = command.getNumeric(0, 0)
    y = command.getNumeric(1, 0)
    z = command.getNumeric(2, 0)
    
    # Установка значений с форматированием из конфига
    context.setNumericValue('X', x)
    context.setNumericValue('Y', y)
    context.setNumericValue('Z', z)
    
    # Получение отформатированных строк
    xStr = context.getFormattedValue('X')  # "X100.500"
    yStr = context.getFormattedValue('Y')  # "Y200.750"
    zStr = context.getFormattedValue('Z')  # "Z50.250"
    
    context.writeBlock()
```

---

## 🔧 Полезные команды API

### Вывод G-кода

```python
context.write("G01 X100")              # С номером блока: N10 G01 X100
context.write("G01 X100", True)        # Без номера блока: G01 X100
context.comment("Начало обработки")    # (Начало обработки)
context.warning("Предупреждение!")     # (WARNING: Предупреждение!)
context.writeln()                       # Пустая строка
```

### Чтение параметров команды

```python
# Числовые параметры
if command.numeric and len(command.numeric) > 0:
    x = command.numeric[0]

# С default значением
x = command.getNumeric(0, 0.0)
y = command.getNumeric(1, context.registers.y)

# Ключевые слова
if command.hasMinorWord("on"):
    context.write("M3")

# Перебор ключевых слов
if command.minorWords:
    for word in command.minorWords:
        if word.upper() == "CLW":
            ...
```

### Работа с регистрами

```python
# Чтение
x = context.registers.x
feed = context.registers.f

# Запись
context.registers.x = 100.5
context.registers.f = 500.0
context.registers.s = 2000
context.registers.t = 5
```

### Глобальные переменные

```python
# Чтение
last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
counter = context.globalVars.GetInt("COUNTER", 0)

# Запись
context.globalVars.SetDouble("LAST_FEED", 500.0)
context.globalVars.SetInt("COUNTER", 10)
```

### StateCache — кэш состояний (v1.1.0)

```python
# Проверка изменения
if context.cacheHasChanged("LAST_FEED", feed):
    context.writeBlock()
    context.cacheSet("LAST_FEED", feed)

# Сброс кэша
context.cacheReset("LAST_FEED")
```

### CycleCache — кэширование циклов (v1.1.0)

```python
# Умный вывод цикла
params = {'X': 100.0, 'Y': 200.0}
context.cycleWriteIfDifferent("CYCLE800", params)

# Принудительный полный вывод
context.cycleForceWrite("CYCLE800", params)

# Очистка кэша цикла
context.cycleCacheClear("CYCLE800")
```

### NumericNCWord — форматирование (v1.1.0)

```python
# Установка значения
context.setNumericValue('X', 100.5)

# Получение отформатированной строки
xStr = context.getFormattedValue('X')  # "X100.500"

# Запись в блок
context.writeBlock()
```

### TextNCWord — стилизованный текст (v1.1.0)

```python
# Комментарий (стиль из конфига)
context.comment("Начало операции")

# Примечание
context.note("Примечание")

# Предупреждение
context.warning("Внимание!")
```

---

## 🐛 Отладка

### Вывод отладочной информации

```python
def execute(context, command):
    context.comment(f"DEBUG: {command.majorWord}")
    context.comment(f"DEBUG: numeric={command.numeric}")
    context.comment(f"DEBUG: minorWords={command.minorWords}")
```

### Запуск с отладкой

```bash
dotnet run -- -i test.apt -o output.nc -c siemens --debug
```

---

## 📝 Чек-лист перед запуском

- [ ] Файл макроса в папке `macros/python/user/`
- [ ] Файл называется `*.py`
- [ ] Есть функция `def execute(context, command):`
- [ ] Кодировка `# -*- coding: ascii -*-` в начале
- [ ] Проверка наличия параметров перед использованием
- [ ] Обновление регистров после изменения координат

---

## 🎓 Следующие шаги

### Изучите документацию

- [PYTHON_MACROS_GUIDE.md](PYTHON_MACROS_GUIDE.md) — полное руководство по макросам
- [CONFIGURATION_GUIDE.md](CONFIGURATION_GUIDE.md) — настройка конфигов
- [ARCHITECTURE.md](ARCHITECTURE.md) — архитектура постпроцессора

### Продвинутые темы

- StateCache для модального вывода
- CycleCache для кэширования циклов
- NumericNCWord для форматирования
- TextNCWord для комментариев

### Примеры макросов

Изучите готовые макросы в `macros/python/base/`:
- `goto.py` — линейные перемещения
- `spindl.py` — управление шпинделем
- `coolnt.py` — охлаждение
- `cycle81.py` — циклы сверления

---

## ❓ Частые вопросы

### Q: Макрос не работает, что делать?

**A:** Проверьте:
1. Файл в правильной папке?
2. Есть функция `execute`?
3. Нет синтаксических ошибок?

### Q: Как узнать какие параметры у команды?

**A:** Добавьте отладочный вывод:
```python
context.comment(f"DEBUG: numeric={command.numeric}")
context.comment(f"DEBUG: minorWords={command.minorWords}")
```

### Q: Можно ли использовать библиотеки Python?

**A:** Да, стандартные библиотеки работают:
```python
import math

angle = math.degrees(math.atan2(j, k))
```

### Q: Как сделать макрос только для моего станка?

**A:** Положите макрос в `macros/python/mmill/` вместо `user/`.

---

## 🎯 Упражнения для практики

### Упражнение 1: Модифицируйте GOTO

Добавьте проверку безопасности по Z:

```python
if z < 5:
    context.warning(f"Z={z} ниже безопасной высоты!")
    z = 5
```

### Упражнение 2: Добавьте мягкий старт шпинделя

```python
if rpm > 1000:
    context.write("G04 P0.5")  # Пауза перед стартом
```

### Упражнение 3: Создайте макрос для паузы

```python
# DWELL MACRO
# APT: DWELL/2.5
# Вывод: G04 P2.5
```

---

**Удачи в написании макросов! 🚀**
