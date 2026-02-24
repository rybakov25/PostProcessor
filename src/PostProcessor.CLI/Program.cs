using PostProcessor.APT.Parser;
using PostProcessor.Core.Config;
using PostProcessor.Core.Config.Models;
using PostProcessor.Core.Context;
using PostProcessor.Macros.Engine;
using PostProcessor.Macros.Python;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.Text;

namespace PostProcessor.CLI;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        var inputOption = new Option<string>(["--input", "-i"], "Input APT/CL file path")
        {
            IsRequired = true
        };

        var outputOption = new Option<string>(["--output", "-o"], "Output NC file path")
        {
            IsRequired = true
        };

        var controllerOption = new Option<string>(["--controller", "-c"],
            getDefaultValue: () => "siemens",
            description: "Controller type (fanuc, siemens, heidenhain)");

        var machineOption = new Option<string>(["--machine", "-m"],
            getDefaultValue: () => "",
            description: "Machine type (mmill, fsq100, etc.) - loads machine-specific macros");

        var configOption = new Option<string?>(["--config", "-cfg"],
            "Custom controller config path (overrides --controller)");

        var macroPathOption = new Option<string[]>(["--macro-path", "-mp"],
            "Additional macro paths (semicolon-separated)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var debugOption = new Option<bool>(["--debug", "-d"],
            getDefaultValue: () => false,
            description: "Enable debug output");

        var validateOnlyOption = new Option<bool>(["--validate-only", "-v"],
            getDefaultValue: () => false,
            description: "Validate APT syntax only (no G-code generation)");

        var rootCommand = new RootCommand("PostProcessor v1.1 - APT/CL to G-code converter for CNC machines")
        {
            inputOption,
            outputOption,
            controllerOption,
            machineOption,
            configOption,
            macroPathOption,
            debugOption,
            validateOnlyOption
        };

        rootCommand.SetHandler(
            ExecuteAsync,
            inputOption,
            outputOption,
            controllerOption,
            machineOption,
            configOption,
            macroPathOption,
            debugOption,
            validateOnlyOption
        );

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> ExecuteAsync(
        string input,
        string output,
        string controller,
        string machine,
        string? configPath,
        string[] macroPaths,
        bool debug,
        bool validateOnly)
    {
        try
        {
            Console.WriteLine("PostProcessor v1.1 - APT/CL to G-code Converter");
            Console.WriteLine($"Controller: {controller.ToUpperInvariant()}");
            if (!string.IsNullOrEmpty(machine))
            {
                Console.WriteLine($"Machine: {machine.ToUpperInvariant()}");
            }
            Console.WriteLine($"Input: {input}");
            Console.WriteLine($"Output: {output}");
            Console.WriteLine();

            // ����������� ������� ����������
            var baseDir = AppContext.BaseDirectory;
            var solutionDir = FindSolutionDirectory(baseDir) ?? baseDir;

            if (debug)
            {
                Console.WriteLine($"[DEBUG] Base directory: {baseDir}");
                Console.WriteLine($"[DEBUG] Solution directory: {solutionDir}");
                Console.WriteLine();
            }

            // �������� ������������� �������� �����
            var inputFullPath = Path.IsPathRooted(input) ? input : Path.GetFullPath(input, solutionDir);
            if (!File.Exists(inputFullPath))
            {
                Console.Error.WriteLine($"\nInput file not found: {inputFullPath}");
                return 1;
            }

            // �������� ������������ �����������
            ControllerConfig config;
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                var configFullPath = Path.IsPathRooted(configPath)
                    ? configPath
                    : Path.GetFullPath(configPath, solutionDir);

                if (!File.Exists(configFullPath))
                    throw new FileNotFoundException($"Config file not found: {configFullPath}");

                config = ConfigLoader.Load(configFullPath);
                Console.WriteLine($"Loaded custom config: {Path.GetFileName(configFullPath)}");
            }
            else
            {
                // �������������� ����� ������������ �� ����� �����������
                var searchPaths = new[]
                {
                    Path.Combine(solutionDir, "configs", "controllers", controller)
                };

                string? foundPath = null;
                foreach (var searchPath in searchPaths)
                {
                    if (debug) Console.WriteLine($"[DEBUG] Searching config in: {searchPath}");

                    if (Directory.Exists(searchPath))
                    {
                        var jsonFiles = Directory.GetFiles(searchPath, "*.json", SearchOption.TopDirectoryOnly);
                        if (jsonFiles.Length > 0)
                        {
                            foundPath = jsonFiles[0];
                            break;
                        }
                    }
                }

                if (foundPath == null)
                {
                    var searchedLocations = string.Join("\n  ", searchPaths.Select(p => p.Replace(solutionDir, "{solution}")));
                    Console.Error.WriteLine($"\nError: Controller config not found for '{controller}'");
                    Console.Error.WriteLine($"Searched in:");
                    Console.Error.WriteLine($"  {searchedLocations}");
                    Console.Error.WriteLine($"\nPlease create config at:");
                    Console.Error.WriteLine($"  {Path.Combine(solutionDir, $"configs/controllers/{controller}/default.json")}");
                    return 1;
                }

                config = ConfigLoader.Load(foundPath);
                Console.WriteLine($"Loaded config: {controller} ({Path.GetFileName(foundPath)})");
            }

            // ����������� ����� �������� ��������
            var defaultMacroPaths = new List<string>
            {
                Path.Combine(solutionDir, "macros", "system"),
                Path.Combine(solutionDir, "macros", controller)
            };

            if (macroPaths.Length > 0)
            {
                foreach (var path in macroPaths)
                {
                    var fullPath = Path.IsPathRooted(path) ? path : Path.GetFullPath(path, solutionDir);
                    defaultMacroPaths.Add(fullPath);
                }
            }

            // Python �������
            var pythonMacroPaths = new List<string>
            {
                Path.Combine(solutionDir, "macros", "python"),
                Path.Combine(baseDir, "macros", "python")
            };
            pythonMacroPaths.AddRange(macroPaths.Select(p => Path.IsPathRooted(p) ? p : Path.GetFullPath(p, solutionDir)));

            // ���������� ������������ �����
            var validMacroPaths = pythonMacroPaths
                .Distinct()
                .Where(p => Directory.Exists(p))
                .ToList();

            if (validMacroPaths.Count == 0)
            {
                Console.Error.WriteLine("\nWarning: No Python macro directories found");
                Console.Error.WriteLine($"Expected: {Path.Combine(solutionDir, "macros", "python")}");
                Console.Error.WriteLine("PostProcessor will run without macros (header/footer only)");
            }

            Console.WriteLine($"Python macro paths ({validMacroPaths.Count}):");
            foreach (var path in validMacroPaths)
                Console.WriteLine($"  {path.Replace(baseDir, "{bin}").Replace(solutionDir, "{solution}")}");

            var pythonEngine = new PostProcessor.Macros.Python.PythonMacroEngine(machine, pythonMacroPaths.ToArray());

            Console.WriteLine("\nLoading Python macros...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // �������� Python ��������
            await pythonEngine.LoadAsync(validMacroPaths).ConfigureAwait(false);

            stopwatch.Stop();

            var totalMacros = pythonEngine.GetMacroCount();
            Console.WriteLine($"Loaded {totalMacros} Python macros in {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine();

            // ��������� ���������� ��� ��������� ����
            if (validateOnly)
            {
                Console.WriteLine("Validating APT syntax (no G-code generation)...");
                var validationPassed = await ValidateSyntaxAsync(inputFullPath).ConfigureAwait(false);

                if (validationPassed)
                {
                    Console.WriteLine("\nSyntax validation passed");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine("\nSyntax validation failed");
                    return 1;
                }
            }

            // ��������� G-����
            Console.WriteLine("Generating G-code...");
            stopwatch.Restart();

            await using var writer = new StreamWriter(output, false, Encoding.UTF8);
            
            // Вывод header из конфигурации контроллера
            if (config.HeaderFooterEnabled)
            {
                foreach (var line in config.Header)
                {
                    var processedLine = line
                        .Replace("{name}", config.Name)
                        .Replace("{machine}", config.MachineProfile ?? "Unknown")
                        .Replace("{inputFile}", Path.GetFileName(input))
                        .Replace("{dateTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    
                    await writer.WriteLineAsync(processedLine);
                }
                await writer.WriteLineAsync();
            }
            else
            {
                // Header по умолчанию если не указан в конфиге
                await writer.WriteLineAsync("(==================================================)");
                await writer.WriteLineAsync($"(; PostProcessor v1.1 for {config.Name} ;)");
                await writer.WriteLineAsync($"(; Input: {Path.GetFileName(input)} ;)");
                await writer.WriteLineAsync($"(; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ;)");
                await writer.WriteLineAsync("(==================================================)");
                await writer.WriteLineAsync();
            }

            var context = new PostContext(writer)
            {
                Config = config
            };

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.WriteLine("\nCancellation requested... finishing current operation");
            };

            try
            {
                await APT.Parser.APTParser.ParseWithMacrosAsync(
                    inputFullPath,
                    context,
                    pythonEngine,
                    cancellationTokenSource.Token
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nProcessing cancelled by user");
                await writer.WriteLineAsync("(PROCESSING CANCELLED BY USER)");
                return 1;
            }
            finally
            {
                // Output footer from controller config
                if (config.HeaderFooterEnabled)
                {
                    await writer.WriteLineAsync();
                    foreach (var line in config.Footer)
                    {
                        await writer.WriteLineAsync(line);
                    }
                }
                else
                {
                    // Default footer if not specified in config
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("(==================================================)");
                    await writer.WriteLineAsync("( END OF PROGRAM )");
                    await writer.WriteLineAsync("(==================================================)");
                }
            }

            stopwatch.Stop();

            // ���������� ���������
            var stats = context.GetStatistics();
            Console.WriteLine();
            Console.WriteLine("  G-code generation completed successfully");
            Console.WriteLine($"  Output file: {output}");
            Console.WriteLine($"  Size: {new FileInfo(output).Length / 1024} KB");
            Console.WriteLine($"  Commands processed: {stats.CommandCount}");
            Console.WriteLine($"  Motion blocks: {stats.MotionCount}");
            Console.WriteLine($"  Tool changes: {stats.ToolChanges}");
            Console.WriteLine($"  Processing time: {stopwatch.ElapsedMilliseconds} ms");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nFatal error: {ex.Message}");
            if (debug)
                Console.Error.WriteLine($"Stack trace:\n{ex.StackTrace}");
            return 1;
        }
    }

    private static async Task<bool> ValidateSyntaxAsync(string inputPath)
    {
        try
        {
            int commandCount = 0;
            await using var lexer = new PostProcessor.APT.Lexer.StreamingAPTLexer(inputPath);

            await foreach (var command in lexer.ParseStreamAsync().ConfigureAwait(false))
            {
                commandCount++;

                // ������� ��������� ������
                if (string.IsNullOrWhiteSpace(command.MajorWord))
                {
                    Console.Error.WriteLine($"Line {command.LineNumber}: Empty major word");
                    return false;
                }

                // �������� ��������� ��� ��������
                if (command.MajorWord is "goto" or "rapid" && command.NumericValues.Count < 2)
                {
                    Console.Error.WriteLine($"Line {command.LineNumber}: {command.MajorWord.ToUpperInvariant()} requires at least 2 coordinates");
                    return false;
                }
            }

            Console.WriteLine($"Validated {commandCount} commands");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Validation error: {ex.Message}");
            return false;
        }
    }

    private static string? FindSolutionDirectory(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);
        while (currentDir != null)
        {
            if (currentDir.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any())
                return currentDir.FullName;
            currentDir = currentDir.Parent;
        }
        return null;
    }
}


