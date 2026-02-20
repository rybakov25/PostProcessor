using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PostProcessor.Core.Context;
using PostProcessor.Core.Models;
using PostProcessor.Macros.Interfaces;

namespace PostProcessor.Macros.Engine;

/// <summary>
/// Объединённый движок макросов (C# + Python)
/// </summary>
public class CompositeMacroEngine : IMacroEngine
{
    private readonly List<IMacroEngine> _engines = new();

    public void AddEngine(IMacroEngine engine)
    {
        _engines.Add(engine);
    }

    public void RegisterLoader(IMacroLoader loader)
    {
        foreach (var engine in _engines.OfType<MacroEngine>())
        {
            engine.RegisterLoader(loader);
        }
    }

    public IEnumerable<IMacro> FindMacros(string commandName)
    {
        var allMacros = new List<IMacro>();
        foreach (var engine in _engines)
        {
            allMacros.AddRange(engine.FindMacros(commandName));
        }
        return allMacros;
    }

    public int GetMacroCount()
    {
        return _engines.Sum(e => e.GetMacroCount());
    }

    public async Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        foreach (var engine in _engines)
        {
            await engine.LoadAsync(paths, cancellationToken);
        }
    }

    public async Task ExecuteAsync(PostContext context, APTCommand command, CancellationToken cancellationToken = default)
    {
        foreach (var engine in _engines)
        {
            await engine.ExecuteAsync(context, command, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var engine in _engines)
        {
            await engine.DisposeAsync();
        }
    }
}
