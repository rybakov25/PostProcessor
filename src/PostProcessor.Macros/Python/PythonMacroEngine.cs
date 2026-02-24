using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PostProcessor.Core.Context;
using PostProcessor.Core.Models;
using PostProcessor.Macros.Interfaces;
using Python.Runtime;

namespace PostProcessor.Macros.Python;

/// <summary>
/// Движок для выполнения макросов на Python
/// Поддерживает базовые и специфичные для станка макросы
/// </summary>
public class PythonMacroEngine : IMacroEngine
{
    private readonly Dictionary<string, PyObject> _macroRegistry = new();
    private readonly string _machineName;
    private readonly string[] _macroPaths;
    private bool _isInitialized;
    private bool _pythonLoaded;
    private readonly string _pythonDllPath;

    public PythonMacroEngine(string machineName, params string[] macroPaths) : this(null, machineName, macroPaths)
    {
    }
    
    public PythonMacroEngine(string? pythonDllPath, string machineName, params string[] macroPaths)
    {
        _machineName = machineName;
        _macroPaths = macroPaths;
        _pythonDllPath = pythonDllPath ?? FindPythonDll();
    }
    
    /// <summary>
    /// Поиск Python DLL в системных путях
    /// </summary>
    private static string FindPythonDll()
    {
        var possiblePaths = new[]
        {
            @"C:\Python311\python311.dll",
            @"C:\Python3119\python311.dll",
            @"C:\Python310\python310.dll",
            @"C:\Python39\python39.dll",
            @"C:\Python38\python38.dll"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var dir in pathEnv.Split(';'))
            {
                var dllPath = Path.Combine(dir.Trim(), "python311.dll");
                if (File.Exists(dllPath))
                    return dllPath;
            }
        }

        return null;
    }

    public void RegisterLoader(IMacroLoader loader)
    {
        // Python макросы не используют IMacroLoader
    }

    public IEnumerable<IMacro> FindMacros(string commandName)
    {
        return Enumerable.Empty<IMacro>();
    }

    /// <summary>
    /// Инициализация Python runtime и загрузка макросов
    /// </summary>
    public async Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await Task.Run(() =>
        {
            try
            {
                if (!string.IsNullOrEmpty(_pythonDllPath) && File.Exists(_pythonDllPath))
                {
                    Runtime.PythonDLL = _pythonDllPath;
                    Console.WriteLine($"[Python] Using Python DLL: {_pythonDllPath}");
                }
                else
                {
                    Console.WriteLine("[Python] Python DLL not found, trying system default...");
                }

                if (!PythonEngine.IsInitialized)
                {
                    PythonEngine.Initialize();
                    PythonEngine.BeginAllowThreads();
                    _pythonLoaded = true;
                    Console.WriteLine("[Python] Runtime initialized");
                    Console.WriteLine($"[Python] Python version: {PythonEngine.Version}");
                    Console.WriteLine($"[Python] Python path: {Runtime.PythonDLL}");
                }

                using (Py.GIL())
                {
                    var sys = Py.Import("sys");
                    var sysPath = sys.GetAttr("path");

                    // Добавляем пути к макросам
                    foreach (var path in _macroPaths.Concat(paths))
                    {
                        if (Directory.Exists(path))
                        {
                            sysPath.InvokeMethod("append", new PyString(path));
                            Console.WriteLine($"[Python] Added path: {path}");
                        }
                    }
                }

                // Загружаем макросы с приоритетами
                LoadAllMacros();

                _isInitialized = true;
                Console.WriteLine($"[Python] Loaded {_macroRegistry.Count} macros");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Python] Initialization error: {ex.Message}");
                Console.WriteLine($"[Python] Stack trace: {ex.StackTrace}");
                Console.WriteLine("[Python] Python macros will be disabled");
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Загрузка макросов с приоритетами:
    /// 1. user/ - пользовательские (highest priority)
    /// 2. user/{machine}/ - пользовательские для станка
    /// 3. {machine}/ - специфичные для контроллера (siemens, fanuc, etc.)
    /// 4. base/ - базовые (lowest priority)
    /// </summary>
    private void LoadAllMacros()
    {
        var loadedMacros = new HashSet<string>();

        // Приоритет 1 (highest): Загружаем пользовательские (переопределяют все)
        LoadMacrosFromDirectory("user", loadedMacros);
        
        // Приоритет 2: Загружаем пользовательские для конкретного станка
        if (!string.IsNullOrEmpty(_machineName))
        {
            LoadMacrosFromDirectory(Path.Combine("user", _machineName), loadedMacros);
        }

        // Приоритет 3: Загружаем специфичные для контроллера (siemens, fanuc, heidenhain, haas)
        // _machineName может быть "siemens", "fanuc", etc.
        if (!string.IsNullOrEmpty(_machineName) && _machineName != "mmill")
        {
            LoadMacrosFromDirectory(_machineName, loadedMacros);
        }

        // Приоритет 4 (lowest): Загружаем базовые макросы (переопределяются всеми)
        LoadMacrosFromDirectory("base", loadedMacros);
    }
    
    /// <summary>
    /// Загрузка макросов из директории с отслеживанием уже загруженных
    /// </summary>
    private void LoadMacrosFromDirectory(string dirName, HashSet<string> loadedMacros)
    {
        var baseDir = AppContext.BaseDirectory;
        var macroPath = Path.Combine(baseDir, "macros", "python", dirName);
        
        // Пробуем альтернативные пути
        if (!Directory.Exists(macroPath))
        {
            macroPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "macros", "python", dirName));
        }
        
        if (!Directory.Exists(macroPath))
        {
            macroPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "macros", "python", dirName));
        }
        
        if (!Directory.Exists(macroPath))
        {
            return;
        }
        
        Console.WriteLine($"[Python] Loading macros from: {macroPath}");
        
        var pythonFiles = Directory.GetFiles(macroPath, "*.py", SearchOption.TopDirectoryOnly);
        
        foreach (var file in pythonFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            
            // Пропускаем если уже загружен (более высокий приоритет)
            if (loadedMacros.Contains(fileName.ToLowerInvariant()))
            {
                continue;
            }
            
            try
            {
                LoadMacroFromFile(file);
                loadedMacros.Add(fileName.ToLowerInvariant());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Python] Error loading {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Загрузка макроса из файла
    /// </summary>
    private void LoadMacroFromFile(string filePath)
    {
        using (Py.GIL())
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var directory = Path.GetDirectoryName(filePath);

            using (var sys = Py.Import("sys"))
            {
                using var sysPath = sys.GetAttr("path");

                // Добавляем директорию с макросом в sys.path
                sysPath.InvokeMethod("append", new PyString(directory!));
            }

            using var module = Py.Import(fileName);

            if (module.HasAttr("execute"))
            {
                var executeFunc = module.GetAttr("execute");
                // Сохраняем ссылку с увеличением счётчика ссылок
                _macroRegistry[fileName.ToLowerInvariant()] = executeFunc;
                Console.WriteLine($"[Python] Loaded macro: {fileName}");
                // executeFunc не Dispose() потому что сохраняем в реестре
            }
            else
            {
                Console.WriteLine($"[Python] Warning: No 'execute' function in {fileName}");
            }
        }
    }

    /// <summary>
    /// Выполнение макроса
    /// </summary>
    public async Task ExecuteAsync(PostContext context, APTCommand command, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            return;
        }

        var macroName = command.MajorWord?.ToLowerInvariant();
        if (string.IsNullOrEmpty(macroName))
            return;

        if (!_macroRegistry.TryGetValue(macroName, out var macroFunc))
        {
            return;
        }

        try
        {
            using (Py.GIL())
            {
                // Создаём Python-обёртки
                var pythonContext = new PythonPostContext(context);
                var pythonCommand = new PythonAptCommand(command);
                using var pyContext = pythonContext.ToPython();
                using var pyCommand = pythonCommand.ToPython();

                macroFunc.Invoke(pyContext, pyCommand);
                // pyContext и pyCommand освободятся автоматически через using
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Python] Error executing macro '{macroName}': {ex.Message}");
        }
    }

    public bool HasMacro(string commandName)
    {
        return _macroRegistry.ContainsKey(commandName.ToLowerInvariant());
    }

    public int GetMacroCount() => _macroRegistry.Count;

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            using (Py.GIL())
            {
                foreach (var macro in _macroRegistry.Values)
                {
                    macro.Dispose();
                }
                _macroRegistry.Clear();
            }

            if (_pythonLoaded)
            {
                PythonEngine.Shutdown();
                _pythonLoaded = false;
            }

            _isInitialized = false;
        });
    }
}
