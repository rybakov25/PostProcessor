# GitHub Actions для PostProcessor

Этот проект использует GitHub Actions для автоматизации CI/CD процессов.

## 📋 Workflow файлы

### 1. CI - Build and Test ([ci.yml](workflows/ci.yml))

**Триггеры:**
- Push в ветки `master` или `develop`
- Pull Request в ветки `master` или `develop`

**Задачи:**
- ✅ Сборка на Windows и Ubuntu (matrix strategy)
- ✅ Запуск 33 unit-тестов
- ✅ Публикация результатов тестов (TRX формат)
- ✅ Публикация отчётов о покрытии кода

**Переменные окружения:**
```yaml
DOTNET_VERSION: '8.0.x'
PYTHON_VERSION: '3.11'
CONFIGURATION: 'Release'
```

---

### 2. Release - Create Package ([release.yml](workflows/release.yml))

**Триггеры:**
- Push тега версии (формат: `v1.0.0`, `v2.1.3`, и т.д.)

**Задачи:**
- ✅ Сборка и тестирование
- ✅ Публикация CLI приложения (win-x64, linux-x64)
- ✅ Создание ZIP архивов
- ✅ Создание GitHub Release с артефактами

**Использование:**
```bash
# Создать релиз
git tag v1.0.0
git push origin v1.0.0

# GitHub Actions автоматически:
# 1. Соберёт проект
# 2. Запустит тесты
# 3. Создаст исполняемые файлы
# 4. Опубликует релиз на GitHub
```

---

### 3. Code Quality ([code-quality.yml](workflows/code-quality.yml))

**Триггеры:**
- Push в ветки `master` или `develop`
- Pull Request в ветки `master` или `develop`

**Задачи:**
- ✅ dotnet format (проверка стиля кода)
- ✅ Roslynator analyzer (статический анализ)
- ✅ Проверка структуры документации
- ✅ Markdown link check (проверка ссылок)

---

## 🚀 Быстрый старт

### 1. Создать удалённый репозиторий

```bash
# На GitHub создайте новый репозиторий
# Затем добавьте его как remote:
git remote add origin https://github.com/yourusername/PostProcessor.git
```

### 2. Настроить Python для GitHub Actions

GitHub Actions автоматически установит Python 3.11. Для локальной разработки:

```bash
# Убедитесь, что Python установлен
python --version  # Должен быть 3.11.x

# Проверьте путь к Python DLL
# Windows: C:\Python311\python311.dll
```

### 3. Push в репозиторий

```bash
git push -u origin master
```

### 4. Включить GitHub Actions

1. Перейдите на вкладку **Actions** вашего репозитория
2. Нажмите **"I understand my workflows, go ahead and enable them"**
3. Workflows запустятся автоматически при следующем push

---

## 📊 Мониторинг

### Просмотр результатов сборок

1. **GitHub → Actions tab** — все запуски workflows
2. **GitHub → Pull Requests** — статус проверок для PR
3. **GitHub → Releases** — опубликованные релизы

### Артефакты

После успешной сборки артефакты доступны в течение 7 дней:
- `test-results-{os}.zip` — результаты тестов (TRX)
- `coverage-{os}.zip` — отчёты о покрытии кода

---

## 🔧 Настройка

### Изменить версию .NET

Отредактируйте `.github/workflows/ci.yml`:
```yaml
env:
  DOTNET_VERSION: '9.0.x'  # Изменить на нужную версию
```

### Добавить платформу

Добавьте ОС в matrix strategy:
```yaml
strategy:
  matrix:
    os: [windows-latest, ubuntu-latest, macos-latest]
```

### Изменить версию Python

```yaml
env:
  PYTHON_VERSION: '3.12'  # Изменить на нужную версию
```

---

## 🏷️ Создание релиза

### Семантическое версионирование

Используйте формат [SemVer](https://semver.org/):

```
v{MAJOR}.{MINOR}.{PATCH}

Примеры:
v1.0.0  - Первый стабильный релиз
v1.1.0  - Новая функциональность (backwards compatible)
v1.1.1  - Исправление бага
v2.0.0  - Breaking changes
```

### Команда для создания релиза

```bash
# Убедитесь, что все изменения закоммичены
git status

# Создайте тег
git tag v1.0.0

# Отправьте тег
git push origin v1.0.0

# GitHub Actions автоматически создаст релиз!
```

---

## ⚠️ Troubleshooting

### Ошибка: "Python DLL not found"

**Решение:**
1. Убедитесь, что Python 3.11 установлен
2. Проверьте путь в `PythonMacroEngine.cs`:
   ```csharp
   var possiblePaths = new[]
   {
       @"C:\Python311\python311.dll",
       @"C:\Python3119\python311.dll",
       // ...
   };
   ```

### Ошибка: "dotnet format failed"

**Решение:**
```bash
# Отформатируйте код локально
dotnet format PostProcessor.sln

# Закоммитьте изменения
git add .
git commit -m "style: Apply code formatting"
git push
```

### Ошибка: "Tests failed on Linux"

**Причина:** Различия в line endings (CRLF vs LF)

**Решение:**
1. Проверьте `.gitattributes` — все source файлы должны использовать LF
2. Конвертируйте файлы:
   ```bash
   # Linux/Mac
   find . -name "*.cs" -exec sed -i 's/\r$//' {} \;
   find . -name "*.py" -exec sed -i 's/\r$//' {} \;
   ```

---

## 📚 Дополнительные ресурсы

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET GitHub Actions](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)
- [Python GitHub Actions](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-python)
- [Semantic Versioning](https://semver.org/)
- [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
