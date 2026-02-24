# 🚀 PostProcessor v1.1.0

**Дата релиза:** 24 февраля 2026 г.

Крупное обновление постпроцессора с добавлением кэширования состояний, умной модальности и расширенной поддержкой оборудования.

---

## ✨ Что нового

### 🔹 StateCache — кэш состояний

Кэширование модальных переменных для оптимизации вывода G-кода:

- **LAST_FEED** — последняя подача
- **LAST_TOOL** — последний инструмент
- **LAST_CS** — последняя система координат
- **LAST_SPINDLE** — последние обороты шпинделя

**Пример использования:**
```python
# Проверка изменения подачи перед выводом
last_feed = context.cacheGet("LAST_FEED", 0.0)
if last_feed != context.registers.f:
    context.write(f"F{context.registers.f:.1f}")
    context.cacheSet("LAST_FEED", context.registers.f)
```

**Результат:**
```nc
N1 G1 X100. Y200. F500.0  ; Выведено (изменение)
N2 X150. Y250.            ; F не выведено (не изменилось)
N3 X200. Y300. F800.0     ; Выведено (изменение)
```

---

### 🔹 CycleCache — кэширование циклов

Автоматический выбор между полным определением цикла и вызовом:

**Пример:**
```python
params = {
    "MODE": 1,
    "TABLE": "TABLE1",
    "X": 100.0,
    "Y": 200.0,
    "Z": 50.0
}

context.cycleWriteIfDifferent("CYCLE800", params)
```

**Вывод:**
```nc
; Первый вызов (полное определение)
CYCLE800(MODE=1, TABLE="TABLE1", X=100.000, Y=200.000, Z=50.000)

; Второй вызов (те же параметры - только вызов)
CYCLE800()

; Третий вызов (новые параметры - полное определение)
CYCLE800(MODE=1, TABLE="TABLE1", X=150.000, Y=250.000, Z=60.000)
```

---

### 🔹 BlockWriter — умный формирователь блоков

Автоматическая модальная проверка и вывод только изменённых регистров:

**Пример:**
```python
# Установка значений
context.registers.x = 100.5
context.registers.y = 200.3

# Запись блока (выведет только изменённые)
context.writeBlock()  # Вывод: N10 X100.500 Y200.300

# Изменение только X
context.registers.x = 150.0
context.writeBlock()  # Вывод: N20 X150.000
```

---

### 🔹 Форматирование из JSON-конфигов

Все параметры форматирования теперь в конфигурации контроллера:

**Пример конфига (siemens/840d.json):**
```json
{
  "formatting": {
    "blockNumber": {
      "enabled": true,
      "prefix": "N",
      "increment": 10,
      "start": 10
    },
    "comments": {
      "type": "parentheses",
      "transliterate": true,
      "maxLength": 128
    },
    "coordinates": {
      "decimals": 3,
      "trailingZeros": false
    },
    "feedrate": {
      "decimals": 1,
      "prefix": "F"
    },
    "spindleSpeed": {
      "decimals": 0,
      "prefix": "S"
    }
  }
}
```

---

### 🔹 NumericNCWord

Числовые NC-слова с форматированием из конфига:

**Пример:**
```python
# Автоматическое форматирование из конфига
context.setNumericValue('X', 100.5)  # Вывод: X100.500
context.setNumericValue('F', 500.0)  # Вывод: F500.0
context.setNumericValue('S', 1200)   # Вывод: S1200
```

**Паттерны формата:**
- `F4.3` — 4 знака всего, 3 после запятой (X100.500)
- `F3.1` — 3 знака всего, 1 после запятой (F500.0)
- `F0` — целое число (S1200)

---

### 🔹 TextNCWord

Текстовые NC-слова для комментариев со стилем:

**Пример:**
```python
# Стиль берётся из конфига контроллера
context.comment("Начало обработки")
context.comment("Привет мир")  # Транслитерация кириллицы
```

**Вывод:**
```nc
; Siemens (parentheses)
(Начало обработки)
(Privet mir)

; Haas (semicolon)
; Начало обработки
; Privet mir
```

---

### 🔹 SequenceNCWord

Нумерация блоков (N10, N20, N30...) из конфига:

**Пример конфига:**
```json
{
  "formatting": {
    "blockNumber": {
      "enabled": true,
      "prefix": "N",
      "increment": 10,
      "start": 10
    }
  }
}
```

**Вывод:**
```nc
N10 G0 X0. Y0. Z50.
N20 G1 X100. F500.
N30 Y200.
```

---

## 🛠️ Поддерживаемое оборудование

### Контроллеры (4)

| Контроллер | Версия | Макросы | Статус |
|------------|--------|---------|--------|
| **Siemens** | 840D / 840D sl | 18 | ✅ Полная поддержка |
| **Fanuc** | 31i / 32i | 11 | ✅ Полная поддержка |
| **Heidenhain** | TNC 640 / TNC 620 | 9 | ✅ Полная поддержка |
| **Haas** | NGC / Next Gen | 9 | ✅ Полная поддержка |

### Профили станков (7)

| Станок | Тип | Контроллер | Оси |
|--------|-----|------------|-----|
| **mmill** | Фрезерный | Любой | 3 оси |
| **DMG Mori DMU 50** | 5-осевой | Siemens | 5 осей |
| **DMG Mori NLX 2500** | Токарный | Fanuc | 2 оси |
| **Haas VF-2** | Фрезерный | Haas | 3 оси |
| **TOS KURIM FSQ100** | Горизонтальный | Siemens | 4 оси |
| **Romi GL250** | Токарный | Haas | 2 оси |
| **DMG MillTap** | Скоростной | Siemens | 3 оси |

---

## 📦 Установка

### Требования

- **.NET 8.0 SDK** — для сборки
- **Python 3.8-3.12** — для макросов (не 3.13!)
- **CAM-система** — CATIA, NX, Mastercam, Fusion 360

### Из исходного кода

```bash
# Клонирование
git clone https://github.com/rybakov25/PostProcessor.git
cd PostProcessor

# Сборка
dotnet build

# Запуск
dotnet run --project src/PostProcessor.CLI/PostProcessor.CLI.csproj \
  -- -i input.apt -o output.nc -c siemens -m mmill
```

### Готовые бинарники

Скачайте с [страницы релизов](https://github.com/rybakov25/PostProcessor/releases):

- **Windows**: `PostProcessor-1.1.0-win-x64.zip`
- **Linux**: `PostProcessor-1.1.0-linux-x64.zip`

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

    # Вывод G-кода (автоматическая модальность)
    context.writeBlock()
```

### Пример: GOTO с StateCache

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
        context.writeBlock()  # G0 X.. Y.. Z..
    else:
        context.writeBlock()  # G1 X.. Y.. Z..

        # Модальная подача с использованием StateCache
        if context.registers.f > 0:
            last_feed = context.cacheGet("LAST_FEED", 0.0)
            if last_feed != context.registers.f:
                context.write(f"F{context.registers.f:.1f}")
                context.cacheSet("LAST_FEED", context.registers.f)
```

### Пример: CYCLE800 с CycleCache

```python
# -*- coding: ascii -*-
def execute(context, command):
    # Сбор параметров цикла
    params = {
        "MODE": command.numeric[0] if len(command.numeric) > 0 else 1,
        "TABLE": command.string if command.string else "TABLE1",
        "X": context.registers.x,
        "Y": context.registers.y,
        "Z": context.registers.z,
        "A": context.registers.a,
        "B": context.registers.b,
        "C": context.registers.c
    }

    # Автоматический выбор: полное определение или вызов
    context.cycleWriteIfDifferent("CYCLE800", params)
```

---

## 🧪 Тестирование

### Unit-тесты (169)

| Категория | Тестов | Статус |
|-----------|--------|--------|
| **Register** | 12 | ✅ |
| **BlockWriter** | 16 | ✅ |
| **StateCache** | 18 | ✅ |
| **CycleCache** | 18 | ✅ |
| **NumericNCWord** | 20 | ✅ |
| **SequenceNCWord** | 16 | ✅ |
| **TextNCWord** | 20 | ✅ |
| **ArcMacro** | 12 | ✅ |
| **PlaneMacro** | 4 | ✅ |
| **SubprogMacro** | 4 | ✅ |
| **PostContext** | 8 | ✅ |
| **AptLexer** | 7 | ✅ |
| **Integration** | 8 | ✅ |

**Итого:** 169 тестов ✅

### Запуск тестов

```bash
dotnet test src/PostProcessor.Tests/PostProcessor.Tests.csproj
```

---

## 📊 Статистика релиза

| Метрика | Значение | Изменение |
|---------|----------|-----------|
| **Строк кода C#** | ~10,000 | +4,000 |
| **Python макросы** | 65 | +24 |
| **Unit-тесты** | 169 | +136 |
| **Документация** | ~8,000 строк | +3,000 |
| **Конфигурации** | 4 контроллера + 7 профилей | +1 контроллер |
| **Новых файлов C#** | 9 | NCWord, BlockWriter, StateCache... |

---

## 🔧 Изменения в API

### Новые методы PostContext

```csharp
// StateCache
public T cacheGet<T>(string key, T defaultValue)
public void cacheSet<T>(string key, T value)
public bool cacheContains(string key)
public void cacheRemove(string key)

// CycleCache
public void cycleWriteIfDifferent(string cycleName, Dictionary<string, object> parameters)

// Форматирование
public void setNumericValue(string address, double value)
public string getFormattedValue(string address)
public void comment(string text)  // С учётом стиля из конфига
```

### Новые методы PythonPostContext

```python
# StateCache
context.cacheGet(key, default_value)
context.cacheSet(key, value)
context.cacheContains(key)
context.cacheRemove(key)

# CycleCache
context.cycleWriteIfDifferent(cycle_name, parameters)

# Форматирование
context.setNumericValue(address, value)
context.getFormattedValue(address)
context.comment(text)  # С учётом стиля из конфига
```

---

## 🐛 Исправления

- ✅ Исправлена модальность регистров (A, B, C)
- ✅ Корректная транслитерация кириллицы в комментариях
- ✅ Обработка дуг >180° (автоматический переход на IJK)
- ✅ Форматирование чисел с учётом конфига контроллера

---

## ⚠️ Известные ограничения

- ⚠️ **Python 3.13 не поддерживается** (pythonnet limitation)
- ⚠️ **Токарные циклы G71-G76** — в разработке
- ⚠️ **Mill-Turn поддержка** — планируется в v1.2.0

---

## 📋 План развития

### v1.2.0 (Q3 2026)

- [ ] Mill-Turn поддержка (приводной инструмент)
- [ ] Токарные циклы G71-G76 для Fanuc
- [ ] Дополнительные профили (Mazak, Okuma)
- [ ] Расширенное тестовое покрытие (>90%)

### v1.3.0 (Q4 2026)

- [ ] Графический интерфейс (GUI)
- [ ] Визуализация траекторий
- [ ] Поддержка 5-осевой симультанной обработки

---

## 🤝 Вклад в проект

1. Fork репозитория
2. Создайте ветку (`git checkout -b feature/amazing-feature`)
3. Закоммитьте изменения (`git commit -m 'Add amazing feature'`)
4. Отправьте в ветку (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

📖 **Подробнее:** [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 📄 Лицензия

MIT License — см. файл [LICENSE](LICENSE)

---

## 📞 Контакты

- **Репозиторий:** https://github.com/rybakov25/PostProcessor
- **Issues:** https://github.com/rybakov25/PostProcessor/issues
- **Документация:** https://github.com/rybakov25/PostProcessor/tree/master/docs

---

## 🙏 Благодарности

- **IMSpost** — за вдохновение архитектурой
- **pythonnet** — за Python интеграцию
- **xUnit** — за фреймворк тестирования

---

**Full Changelog:** https://github.com/rybakov25/PostProcessor/compare/v1.0.0...v1.1.0
