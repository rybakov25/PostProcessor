# PostProcessor

> **Универсальный постпроцессор для обработки APT/CL файлов с поддержкой Python макросов**
>
> Поддерживает **любое оборудование**: 3-осевые и 5-осевые фрезерные станки, токарные, многозадачные обрабатывающие центры с различными ЧПУ (Siemens, Fanuc, Heidenhain, Haas и др.)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Python](https://img.shields.io/badge/Python-3.8--3.12-3776AB?logo=python)](https://www.python.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 📋 Оглавление

- [Возможности](#возможности)
- [Быстрый старт](#быстрый-старт)
- [Документация](#документация)
- [Примеры использования](#примеры-использования)
- [Структура проекта](#структура-проекта)
- [Настройка](#настройка)
- [Создание макросов](#создание-макросов)
- [Требования](#требования)

---

## Возможности

| Возможность | Описание |
|-------------|----------|
| 🌍 **Универсальность** | Поддержка любого оборудования через конфигурации и макросы |
| 🧩 **Модульная архитектура** | Расширяйте функциональность через Python макросы без перекомпиляции |
| ⚙️ **Гибкая настройка** | Поддержка различных контроллеров (Siemens, Fanuc, Heidenhain, Haas, Fagor) |
| 🏭 **Профили станков** | Уникальные настройки для каждого станка: фрезерные, токарные, многозадачные |
| 🐍 **Python макросы** | Создавайте логику обработки на Python с полным доступом к API |
| 🔄 **5-осевая обработка** | Поддержка многоосевых станков с кинематикой, RTCP, CYCLE800 |
| 🛡️ **Безопасность** | Проверка ограничений станка и параметров безопасности |
| 📝 **Модальные команды** | Оптимизированный вывод G-кода |
| 🔧 **Кастомизация** | Переопределение G/M-кодов через конфигурацию |
| 📦 **Готовые шаблоны** | Базовые макросы для быстрого старта |

---

## Быстрый старт

### 1. Установка и сборка

```bash
# Клонируйте репозиторий
git clone <repository-url>
cd PostProcessor

# Сборка проекта
dotnet build
```

### 2. Базовое использование

```bash
# Обработка APT-файла
dotnet run -- -i input.apt -o output.nc -c siemens

# С профилем станка
dotnet run -- -i input.apt -o output.nc -c siemens --machine-profile mmill

# С отладочной информацией
dotnet run -- -i input.apt -o output.nc -c siemens --debug
```

### 3. Первый макрос за 5 минут

Создайте файл `macros/python/user/hello.py`:

```python
# -*- coding: ascii -*-
def execute(context, command):
    context.comment("=== Привет от Python макроса! ===")
    context.write("G0 X0 Y0 Z50")
```

Запустите постпроцессор и проверьте результат в `output.nc`:

```nc
(=== Привет от Python макроса! ===)
N1 G0 X0. Y0. Z50.
```

📖 **Подробное руководство:** [QUICKSTART.md](docs/QUICKSTART.md)

---

## Документация

| Документ | Описание |
|----------|----------|
| [🚀 Быстрый старт](docs/QUICKSTART.md) | Напишите первый макрос за 10 минут |
| [📚 Руководство по макросам](docs/PYTHON_MACROS_GUIDE.md) | Полное API, примеры, отладка |
| [🏗️ Архитектура](docs/ARCHITECTURE.md) | Устройство постпроцессора для разработчиков |
| [⚙️ Настройка](docs/CUSTOMIZATION_GUIDE.md) | Конфигурация контроллеров и станков |
| [🌍 Поддерживаемое оборудование](docs/SUPPORTED_EQUIPMENT.md) | Список оборудования и профили |
| [🔄 Переход с IMSpost](docs/IMSPOST_TO_PYTHON_GUIDE.md) | Миграция макросов IMSpost на Python |

---

## Примеры использования

### 3-осевой фрезерный станок (Siemens 840D)

```bash
dotnet run -- -i part.apt -o part.nc \
  -c siemens \
  --machine-profile dmu50_3axis
```

### 5-осевой обрабатывающий центр (Siemens 840D sl)

```bash
dotnet run -- -i impeller.apt -o impeller.nc \
  -c siemens \
  --machine-profile dmu50_5axis \
  --macro-path macros/python/5axis
```

### Токарный станок (Fanuc 31i)

```bash
dotnet run -- -i shaft.apt -o shaft.nc \
  -c fanuc \
  --machine-profile nlx2500_01
```

### Многозадачный станок (Heidenhain TNC640)

```bash
dotnet run -- -i complex.apt -o complex.nc \
  -c heidenhain \
  --machine-profile nt6600
```

### Валидация APT-файла

```bash
dotnet run -- -i part.apt -o /dev/null \
  -c siemens \
  --validate-only
```

### Добавление поддержки нового контроллера

1. Создайте конфигурацию `configs/controllers/your_controller/base.json`:
```json
{
  "name": "Your Controller",
  "machineType": "Milling",
  "registerFormats": { ... },
  "functionCodes": { ... }
}
```

2. Добавьте базовые макросы в `macros/python/base/`

3. Используйте:
```bash
dotnet run -- -i part.apt -o part.nc -c your_controller
```

---

## Структура проекта

```
PostProcessor/
├── src/
│   ├── PostProcessor.CLI/      # Интерфейс командной строки
│   ├── PostProcessor.Core/     # Ядро постпроцессора
│   ├── PostProcessor.Macros/   # Движок Python макросов
│   └── PostProcessor.APT/      # Парсер APT/CL файлов
│
├── macros/
│   └── python/
│       ├── base/               # Базовые макросы (универсальные)
│       ├── {machine}/          # Макросы для конкретного станка
│       └── user/               # Ваши макросы (приоритет)
│           └── {machine}/      # Переопределение для станка
│
├── configs/
│   ├── controllers/            # Конфигурации контроллеров
│   │   ├── siemens/            # Siemens 840D, 840D sl
│   │   ├── fanuc/              # Fanuc 31i, 32i, 35i
│   │   ├── heidenhain/         # Heidenhain TNC 640, 620
│   │   ├── haas/               # Haas CNC
│   │   └── {your_controller}/  # Добавьте свой
│   └── machines/               # Профили станков
│       ├── dmu50_3axis.json    # 3-осевой DMG Mori
│       ├── dmu50_5axis.json    # 5-осевой DMG Mori
│       ├── nlx2500_01.json     # Токарный Mori Seiki
│       └── {your_machine}.json # Добавьте свой
│
├── docs/                       # Документация
└── templates/                  # Шаблоны для создания
```

**Принцип приоритета макросов:**
```
user/{machine}/{macro}.py  →Highest priority (your overrides)
{machine}/{macro}.py       → Medium priority (machine-specific)
base/{macro}.py            → Lowest priority (universal base)
```

---

## Настройка

### Параметры командной строки

| Параметр | Короткий | Описание | Default |
|----------|----------|----------|---------|
| `--input` | `-i` | Входной APT файл | Обязательно |
| `--output` | `-o` | Выходной NC файл | Обязательно |
| `--controller` | `-c` | Тип контроллера | `siemens` |
| `--config` | `-cfg` | Путь к конфигурации | Авто |
| `--machine-profile` | `-mp` | Профиль станка | Нет |
| `--macro-path` | `-m` | Доп. путь к макросам | `macros/python` |
| `--debug` | `-d` | Режим отладки | `false` |
| `--validate-only` | `-v` | Только валидация | `false` |

### Конфигурация контроллера

Пример `configs/controllers/siemens/840d.json`:

```json
{
  "name": "Siemens 840D",
  "machineType": "Milling",
  "registerFormats": {
    "X": { "address": "X", "format": "F4.3", "isModal": true },
    "F": { "address": "F", "format": "F3.1", "isModal": false }
  },
  "functionCodes": {
    "rapid": { "code": "G00" },
    "linear": { "code": "G01" },
    "spindle_cw": { "code": "M03" }
  },
  "safety": {
    "clearancePlane": 100.0,
    "retractPlane": 5.0,
    "maxFeedRate": 10000.0
  },
  "customParameters": {
    "useCustomFeature": true
  }
}
```

### Профиль станка

Пример `configs/machines/mmill.json`:

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
  "fiveAxis": {
    "enableRtcp": true,
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

## Создание макросов

### Базовый шаблон макроса

```python
# -*- coding: ascii -*-
# MACRO_NAME - Описание

def execute(context, command):
    """
    Документация макроса
    
    Args:
        context: Объект контекста постпроцессора
        command: Объект APT-команды
    """
    # Проверка параметров
    if not command.numeric:
        return
    
    # Получение значений
    x = command.numeric[0]
    
    # Обновление регистров
    context.registers.x = x
    
    # Вывод G-кода
    context.write(f"G01 X{x:.3f}")
```

### Примеры макросов

#### GOTO — линейное перемещение

```python
def execute(context, command):
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

#### SPINDL — управление шпинделем

```python
def execute(context, command):
    rpm = command.numeric[0] if command.numeric else 0
    context.registers.s = rpm
    
    state = "OFF"
    if command.minorWords:
        for word in command.minorWords:
            if word.upper() in ["ON", "CLW"]:
                state = "CW"
            elif word.upper() in ["CCLW", "CCW"]:
                state = "CCW"
            elif word.upper() == "OFF":
                state = "OFF"
    
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

#### FEDRAT — подача (модальная)

```python
def execute(context, command):
    if not command.numeric:
        return
    
    feed = command.numeric[0]
    context.registers.f = feed
    
    # Проверка на изменение (модальность)
    last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
    if last_feed == feed:
        return
    
    context.globalVars.SetDouble("LAST_FEED", feed)
    context.write(f"F{feed:.1f}")
```

📖 **Больше примеров:** [PYTHON_MACROS_GUIDE.md](docs/PYTHON_MACROS_GUIDE.md)

---

## API макросов

### Объект context

| Объект | Описание |
|--------|----------|
| `context.registers` | Регистры станка (X, Y, Z, F, S, T) |
| `context.config` | Конфигурация контроллера |
| `context.system` | Системные переменные |
| `context.globalVars` | Глобальные переменные |

**Методы вывода:**

```python
context.write("G01 X100")           # Вывод G-кода
context.comment("Текст")            # Комментарий
context.warning("Предупреждение")   # Предупреждение
```

### Объект command

```python
command.majorWord          # "goto", "spindl"
command.numeric            # [100.0, 50.0, 10.0]
command.minorWords         # ["on", "clw"]
command.hasMinorWord("on") # Проверка ключевого слова
command.getNumeric(0, 0.0) # Число с default
```

---

## Требования

| Компонент | Версия | Примечание |
|-----------|--------|------------|
| .NET SDK | 8.0+ | Обязательно для сборки |
| Python | 3.8-3.12 | Для макросов (pythonnet не поддерживает 3.13+) |
| CAM-система | Любая | Генерирующая APT/CL файлы |

### Поддерживаемые контроллеры

Постпроцессор поддерживает **любой контроллер** через систему конфигураций. Готовые конфигурации:

- ✅ **Siemens** 840D / 840D sl
- ✅ **Fanuc** 31i / 32i / 35i / Series 30
- ✅ **Heidenhain** TNC 640 / TNC 620 / iTNC 530
- ✅ **Haas** NGC / Next Generation Control
- ✅ **Fagor** CNC 8055 / 8065
- ✅ **Mitsubishi** M70 / E70
- ✅ **Okuma** OSP-P300 / OSP-L300
- ✅ **Другие** — создайте свою конфигурацию (JSON + макросы)

**Добавление нового контроллера:**
1. Создайте `configs/controllers/{controller}/` с JSON-конфигурацией
2. Добавьте базовые макросы в `macros/python/base/` или `macros/python/{controller}/`
3. Используйте через `-c {controller}`

---

## Поддерживаемые APT-команды

Базовые команды (универсальные):

| Команда | Описание | Макрос |
|---------|----------|--------|
| `PARTNO` | Номер детали / начало программы | base/partno.py |
| `GOTO` | Линейное перемещение | base/goto.py |
| `RAPID` | Быстрое перемещение | base/rapid.py |
| `SPINDL` | Управление шпинделем | base/spindl.py |
| `COOLNT` | Управление охлаждением | base/coolnt.py |
| `FEDRAT` | Управление подачей | base/fedrat.py |
| `LOADTL` | Смена инструмента | base/loadtl.py |
| `REMARK` | Комментарий | base/remark.py |
| `FINI` | Конец программы | base/fini.py |

5-осевые команды:

| Команда | Описание | Макрос |
|---------|----------|--------|
| `RTCP` | Вкл/выкл RTCP (TCPM) | base/rtcp.py |
| `CYCLE800` | Поворотная плоскость (Siemens) | siemens/cycle800.py |
| `AXELIM` | Ограничение осей | base/axelim.py |
| `5AXMOVE` | 5-осевое перемещение | base/5axmove.py |

Токарные команды:

| Команда | Описание | Макрос |
|---------|----------|--------|
| `TURRET` | Смена револьверной головки | lathe/turret.py |
| `CHUCK` | Управление патроном | lathe/chuck.py |
| `TAILSTK` | Управление пинолью | lathe/tailstk.py |

**Не нашли нужную команду?** Создайте свой макрос в `macros/python/{controller}/` или `user/{machine}/`.

---

## Лицензия

MIT License — см. файл [LICENSE](LICENSE)

---

## Вклад в проект

1. Fork репозитория
2. Создайте ветку (`git checkout -b feature/amazing-feature`)
3. Закоммитьте изменения (`git commit -m 'Add amazing feature'`)
4. Отправьте в ветку (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

---

## Контакты

Для вопросов и предложений создавайте [Issues](../../issues) в репозитории.
