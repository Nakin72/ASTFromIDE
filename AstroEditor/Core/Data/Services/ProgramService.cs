// AstroEditor.Core/Data/Services/ProgramService.cs
// Сервис управления программами

using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Programs;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Сервис управления программами — создание, добавление, удаление.
/// </summary>
public class ProgramService : IProgramService
{
    private readonly ILogger _logger;
    private readonly ProjectState _state;

    public IReadOnlyDictionary<string, AstroProgram> Programs => _state.Programs.AsReadOnly();
    public event Action? OnProgramsChanged;

    public ProgramService(ProjectState state, ILogger? logger = null)
    {
        _state = state;
        _logger = logger ?? Log.For<ProgramService>();
    }

    public void AddProgram(AstroProgram program)
    {
        if (program == null)
            throw new ArgumentNullException(nameof(program));
        
        if (_state.Programs.ContainsKey(program.Name))
        {
            _logger.LogWarning("Program {Name} already exists, updating", program.Name);
        }
        
        _state.Programs[program.Name] = program;
        _logger.LogInformation("Program added/updated: {Name}", program.Name);
        OnProgramsChanged?.Invoke();
    }

    public bool RemoveProgram(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        
        if (_state.Programs.Remove(name))
        {
            _logger.LogInformation("Program removed: {Name}", name);
            OnProgramsChanged?.Invoke();
            return true;
        }
        
        _logger.LogWarning("Program not found for removal: {Name}", name);
        return false;
    }

    public AstroProgram? GetProgram(string name)
    {
        return _state.Programs.TryGetValue(name, out var program) ? program : null;
    }

    public AstroProgram CreateProgram(string name, string author = "", string description = "")
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Program name cannot be empty", nameof(name));
        
        var program = new AstroProgram
        {
            Name = name,
            Author = author,
            Description = description,
            Version = "1.0"
        };
        
        _logger.LogDebug("Program created: {Name}", name);
        return program;
    }
}
