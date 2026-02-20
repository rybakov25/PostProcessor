# 🚀 PostProcessor

> **Универсальный постпроцессор для CAM-систем с поддержкой Python-макросов**
>
> Генерирует G-код из APT/CL файлов для любого оборудования: 3-5 осевые фрезерные станки, токарные, многозадачные обрабатывающие центры с ЧПУ Siemens, Fanuc, Heidenhain, Haas

[![Release](https://img.shields.io/github/v/release/rybakov25/PostProcessor?label=Release&logo=github)](https://github.com/rybakov25/PostProcessor/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Python](https://img.shields.io/badge/Python-3.8--3.12-3776AB?logo=python)](https://www.python.org/)
[![Build](https://img.shields.io/github/actions/workflow/status/rybakov25/PostProcessor/ci.yml?branch=master&logo=github-actions)](https://github.com/rybakov25/PostProcessor/actions)
[![Tests](https://img.shields.io/github/actions/workflow/status/rybakov25/PostProcessor/ci.yml?branch=master&label=tests&logo=github)](https://github.com/rybakov25/PostProcessor/actions)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 📋 Оглавление

- [Что это?](#что-это)
- [Возможности](#возможности)
- [Быстрый старт](#быстрый-старт)
- [Поддерживаемое оборудование](#поддерживаемое-оборудование)
- [Примеры использования](#примеры-использования)
- [Создание макросов](#создание-макросов)
- [Документация](#документация)
- [Установка](#установка)
- [Требования](#требования)
- [Статус проекта](#статус-проекта)

---

## Что это?

**PostProcessor** — это универсальный постпроцессор, который преобразует управляющие программы из формата APT/CL (из CAM-систем CATIA, NX, Mastercam, Fusion 360) в G-код для конкретных станков с ЧПУ.

### Как это работает?

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   CAM-система   │     │   PostProcessor  │     │    Станок ЧПУ   │
│   (CATIA, NX)   │───▶│ + Python макросы │────▶│    (Siemens,    │
│   APT/CL файл   │     │                  │     │    Fanuc...)    │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

### Ключевая особенность

**Архитектура на Python-макросах** позволяет добавлять поддержку любого оборудования без перекомпиляции основного кода. Просто создайте Python-файл с логикой обработки команд для вашего станка!

---

## ✨ Возможности

| Возможность | Описание |
|-------------|----------|
| 🌍 **Универсальность** | Поддержка любого оборудования через конфигурации и макросы |
| 🧩 **Модульность** | Python макросы без перекомпиляции основного кода |
| ⚙️ **4 контроллера** | Siemens, Fanuc, Heidenhain, Haas — готовые конфигурации |
| 🏭 **7+ профилей** | DMG Mori, Haas, Romi, Mecof — готовые профили станков |
| 🐍 **Python 3.8-3.12** | Полноценный Python для логики постпроцессора |
| 🔄 **3-5 осей** | 3-осевая и 5-осевая обработка (RTCP, CYCLE800) |
| 🛠️ **Токарная** | TURRET, CHUCK, TAILSTK — токарные макросы |
| 🛡️ **Безопасность** | Проверка ограничений станка перед выводом |
| 📝 **Модальность** | Оптимизированный вывод G-кода |
| ✅ **33 теста** | Unit-тесты для ядра и интеграционные тесты |
| 📖 **5000+ строк** | Полная документация на русском |

---

## 🚀 Быстрый старт

### 1. Установка

```bash
# Клонируйте репозиторий
git clone https://github.com/rybakov25/PostProcessor.git
cd PostProcessor

# Сборка
dotnet build
```

### 2. Первый запуск

```bash
# Обработка APT файла
dotnet run --project src/PostProcessor.CLI/PostProcessor.CLI.csproj \
  -- -i input.apt -o output.nc -c siemens -m mmill
```

### 3. Первый макрос за 5 минут

Создайте файл `macros/python/user/hello.py`:

```python
# -*- coding: ascii -*-
def execute(context, command):
    context.comment("=== Привет от Python! ===")
    context.write("G0 X0 Y0 Z50")
```

**Результат в output.nc:**
```nc
(=== Привет от Python! ===)
N1 G0 X0. Y0. Z50.
```

📖 **Подробнее:** [QUICKSTART.md](docs/QUICKSTART.md) — первый макрос за 10 минут

---

## 🛠️ Поддерживаемое оборудование

### Контроллеры (4 готовых)

| Контроллер | Семейства | Макросы | Статус |
|------------|-----------|---------|--------|
| **Siemens** | 840D / 840D sl | 9 базовых + mmill | ✅ |
| **Fanuc** | 31i / 32i / 35i | 11 (фрезерные + токарные) | ✅ |
| **Heidenhain** | TNC 640 / TNC 620 | 9 (уникальный синтаксис) | ✅ |
| **Haas** | NGC / Next Gen | 9 (с % маркером) | ✅ |

### Типы станков

| Тип | Примеры | Поддержка |
|-----|---------|-----------|
| **3-осевые фрезерные** | DMG Mori DMU50, Haas VF-2 | ✅ |
| **5-осевые** | DMG Mori DMU50 5-axis, Haas UMC | ✅ (RTCP, CYCLE800) |
| **Токарные** | Mori Seiki NLX2500, Romi GL250 | ✅ (TURRET, CHUCK, TAILSTK) |
| **Многозадачные** | Mazak Integrex | 🔄 В разработке |

📖 **Полный список:** [SUPPORTED_EQUIPMENT.md](docs/SUPPORTED_EQUIPMENT.md)

---

## 📋 Примеры использования

### 3-осевой фрезерный (Siemens 840D)

```bash
dotnet run -- -i part.apt -o part.nc \
  -c siemens \
  --machine-profile mmill
```

### 5-осевой обрабатывающий центр (Fanuc 31i)

```bash
dotnet run -- -i impeller.apt -o impeller.nc \
  -c fanuc \
  --machine-profile dmg_mori_dmu50_5axis
```

### Токарный станок (Haas NGC)

```bash
dotnet run -- -i shaft.apt -o shaft.nc \
  -c haas \
  --machine-profile romi_gl250
```

### Валидация APT файла

```bash
dotnet run -- -i part.apt -o /dev/null \
  -c siemens \
  --validate-only
```

---

## 🐍 Создание макросов

### Базовый шаблон

```python
# -*- coding: ascii -*-
# MACRO_NAME - Описание

def execute(context, command):
    """
    Обработка APT команды
    
    Args:
        context: Объект контекста постпроцессора
        command: Объект APT команды
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

### Пример: GOTO (линейное перемещение)

```python
# -*- coding: ascii -*-
def execute(context, command):
    if not command.numeric:
        return
    
    x = command.numeric[0] if len(command.numeric) > 0 else 0
    y = command.numeric[1] if len(command.numeric) > 1 else 0
    z = command.numeric[2] if len(command.numeric) > 2 else 0
    
    context.registers.x = x
    context.registers.y = y
    context.registers.z = z
    
    # Проверка на быстрое перемещение
    if context.system.MOTION == 'RAPID':
        context.write(f"G0 X{x:.3f} Y{y:.3f} Z{z:.3f}")
    else:
        context.write(f"G1 X{x:.3f} Y{y:.3f} Z{z:.3f}")
        
        # Модальная подача
        if context.registers.f > 0:
            last_feed = context.globalVars.GetDouble("LAST_FEED", 0.0)
            if last_feed != context.registers.f:
                context.write(f"F{context.registers.f:.1f}")
                context.globalVars.SetDouble("LAST_FEED", context.registers.f)
```

### Пример: SPINDL (шпиндель)

```python
# -*- coding: ascii -*-
def execute(context, command):
    # Получение RPM
    if command.numeric:
        context.globalVars.SPINDLE_RPM = command.numeric[0]
    
    # Обработка ключевых слов
    spindle_state = 'OFF'
    if command.minorWords:
        for word in command.minorWords:
            if word.upper() in ['ON', 'CLW']:
                spindle_state = 'CW'
            elif word.upper() in ['CCLW', 'CCW']:
                spindle_state = 'CCW'
            elif word.upper() == 'OFF':
                spindle_state = 'OFF'
    
    # Вывод M-кода
    if spindle_state == 'CW':
        context.write("M3")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write(f"S{int(context.globalVars.SPINDLE_RPM)}")
    elif spindle_state == 'CCW':
        context.write("M4")
        if context.globalVars.SPINDLE_RPM > 0:
            context.write(f"S{int(context.globalVars.SPINDLE_RPM)}")
    else:
        context.write("M5")
```

📖 **Полное руководство:** [PYTHON_MACROS_GUIDE.md](docs/PYTHON_MACROS_GUIDE.md)

---

## 📚 Документация

| Документ | Описание |
|----------|----------|
| [🚀 QUICKSTART](docs/QUICKSTART.md) | Первый макрос за 10 минут |
| [📚 PYTHON_MACROS_GUIDE](docs/PYTHON_MACROS_GUIDE.md) | Полное API макросов (1400+ строк) |
| [🏗️ ARCHITECTURE](docs/ARCHITECTURE.md) | Архитектура для разработчиков |
| [⚙️ CUSTOMIZATION](docs/CUSTOMIZATION_GUIDE.md) | Настройка контроллеров и станков |
| [🌍 SUPPORTED_EQUIPMENT](docs/SUPPORTED_EQUIPMENT.md) | Поддерживаемое оборудование |
| [🔄 IMSPOST_TO_PYTHON](docs/IMSPOST_TO_PYTHON_GUIDE.md) | Миграция с IMSpost |
| [📊 COMPLETION_REPORT](docs/COMPLETION_REPORT.md) | Статус и план развития |

---

## 📦 Установка

### Из исходного кода

```bash
# Клонирование
git clone https://github.com/rybakov25/PostProcessor.git
cd PostProcessor

# Сборка
dotnet build

# Запуск
dotnet run --project src/PostProcessor.CLI/PostProcessor.CLI.csproj \
  -- -i input.apt -o output.nc -c siemens
```

### Готовые бинарники

Скачайте с [страницы релизов](https://github.com/rybakov25/PostProcessor/releases):
- **Windows**: `PostProcessor-v1.0.0-win-x64.zip`
- **Linux**: `PostProcessor-v1.0.0-linux-x64.zip`

---

## ⚙️ Настройка

### Параметры командной строки

| Параметр | Короткий | Описание | Default |
|----------|----------|----------|---------|
| `--input` | `-i` | Входной APT файл | Обязательно |
| `--output` | `-o` | Выходной NC файл | Обязательно |
| `--controller` | `-c` | Тип контроллера | `siemens` |
| `--machine-profile` | `-mp` | Профиль станка | Нет |
| `--debug` | `-d` | Режим отладки | `false` |

### Конфигурация контроллера

Пример `configs/controllers/siemens/840d.json`:

```json
{
  "name": "Siemens Sinumerik 840D sl",
  "machineType": "Milling",
  "formatting": {
    "coordinates": {
      "decimals": 3,
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
  "fiveAxis": {
    "tcpOn": "RTCPON",
    "tcpOff": "RTCPOF"
  }
}
```

---

## 🔧 Требования

| Компонент | Версия | Примечание |
|-----------|--------|------------|
| **.NET SDK** | 8.0+ | Для сборки |
| **Python** | 3.8-3.12 | Для макросов |
| **CAM-система** | Любая | Генерирующая APT/CL |

### Поддерживаемые CAM-системы

- ✅ CATIA
- ✅ Siemens NX
- ✅ Mastercam
- ✅ Fusion 360
- ✅ SolidCAM
- ✅ HyperMill
- ✅ Другие (с экспортом в APT/CL)

---

## 📊 Статус проекта

### Текущая версия: **v1.0.0**

| Метрика | Значение |
|---------|----------|
| **Строк кода** | 14,925 |
| **C# файлы** | 50+ |
| **Python макросы** | 41 |
| **Unit-тесты** | 33 ✅ |
| **Документация** | 5,000+ строк |
| **Конфигурации** | 5 контроллеров + 7 профилей |

### Готовность к производству

✅ **Готов для:**
- 3-5 осевых фрезерных станков
- Токарных станков (базовая обработка)
- Станков с Siemens, Fanuc, Heidenhain, Haas

⚠️ **В разработке:**
- Токарные циклы G71-G76
- Mill-Turn поддержка
- Расширенные профили (Mazak, Okuma)

---

## 🤝 Вклад в проект

1. Fork репозитория
2. Создайте ветку (`git checkout -b feature/amazing-feature`)
3. Закоммитьте изменения (`git commit -m 'Add amazing feature'`)
4. Отправьте в ветку (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

📖 **Подробнее:** [CONTRIBUTING.md](CONTRIBUTING.md) (в разработке)

---

## 📄 Лицензия

MIT License — см. файл [LICENSE](LICENSE)

---

## 📞 Контакты

- **Репозиторий:** https://github.com/rybakov25/PostProcessor
- **Issues:** https://github.com/rybakov25/PostProcessor/issues
- **Releases:** https://github.com/rybakov25/PostProcessor/releases
- **Actions:** https://github.com/rybakov25/PostProcessor/actions

---

## 🙏 Благодарности

- **IMSpost** — за вдохновение архитектурой
- **pythonnet** — за Python интеграцию
- **xUnit** — за фреймворк тестирования
- **ПАО "Корпорация ВСМПО-АВИСМА" и Солдатову Вячеславу, ведущему инженеру-программисту станков с ЧПУ за вдохновение на разработку**

---

<div align="center">

**PostProcessor** — Универсальный постпроцессор для CNC

[Начать работу](#быстрый-старт) • [Документация](#документация) • [Примеры](#примеры-использования)

</div>
