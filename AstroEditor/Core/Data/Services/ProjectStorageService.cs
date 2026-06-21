// AstroEditor.Core/Data/Services/ProjectStorageService.cs
// Реализация сервиса хранения проекта

using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Сервис хранения проекта — сохранение и загрузка.
/// </summary>
public class ProjectStorageService : IProjectStorage
{
    private readonly ILogger _logger;
    private readonly ProjectState _state;
    private string _projectFolder = string.Empty;
    private bool _hasUnsavedChanges;

    public string ProjectFolder => _projectFolder;
    public bool HasUnsavedChanges => _hasUnsavedChanges;
    public event Action? OnProjectChanged;

    public ProjectStorageService(ProjectState state, ILogger? logger = null)
    {
        _state = state;
        _logger = logger ?? Log.For<ProjectStorageService>();
    }

    public void InitializeNew(string projectFolder)
    {
        _projectFolder = projectFolder;
        _logger.LogInformation("Initializing new project: {Folder}", projectFolder);
        
        Directory.CreateDirectory(projectFolder);
        Directory.CreateDirectory(Path.Combine(projectFolder, "Registry"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "Programs"));
        
        _hasUnsavedChanges = true;
        OnProjectChanged?.Invoke();
    }

    public void Open(string projectFolder)
    {
        _projectFolder = projectFolder;
        _logger.LogInformation("Opening project: {Folder}", projectFolder);
        
        var registryFolder = Path.Combine(_projectFolder, "Registry");
        var programsFolder = Path.Combine(_projectFolder, "Programs");
        
        Directory.CreateDirectory(registryFolder);
        Directory.CreateDirectory(programsFolder);

        _state.TypeRegistry = AstroSerializer.LoadDataTypeRegistry(registryFolder);
        _state.FormRegistry = AstroSerializer.LoadFormRegistry(registryFolder);
        _state.GlobalTables = AstroSerializer.LoadGlobalTables(registryFolder, _state.TypeRegistry);
        _state.Programs.Clear();

        // Загружаем все программы из папки
        if (Directory.Exists(programsFolder))
        {
            foreach (var file in Directory.GetFiles(programsFolder, "*.ast"))
            {
                var progName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    var program = AstroSerializer.LoadProgram(programsFolder, progName, _state.TypeRegistry);
                    _state.Programs[progName] = program;
                    _logger.LogDebug("Loaded program: {Name}", progName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load program {Name}", progName);
                }
            }
        }

        _hasUnsavedChanges = false;
        OnProjectChanged?.Invoke();
    }

    public void SaveAll()
    {
        if (string.IsNullOrEmpty(_projectFolder))
            throw new InvalidOperationException("Project folder is not set");

        _logger.LogInformation("Saving all project data...");
        
        var registryFolder = Path.Combine(_projectFolder, "Registry");
        var programsFolder = Path.Combine(_projectFolder, "Programs");
        
        Directory.CreateDirectory(registryFolder);
        Directory.CreateDirectory(programsFolder);

        AstroSerializer.SaveDataTypeRegistry(_state.TypeRegistry, registryFolder);
        AstroSerializer.SaveFormRegistry(_state.FormRegistry, registryFolder);
        AstroSerializer.SaveGlobalTables(_state.GlobalTables, registryFolder);

        foreach (var program in _state.Programs.Values)
        {
            AstroSerializer.SaveProgram(program, programsFolder);
            ExportProgramToText(program, programsFolder);
            ExportProgramToFanucStyle(program, programsFolder);
        }

        _hasUnsavedChanges = false;
        _logger.LogInformation("Project saved successfully");
    }

    /// <summary>
    /// Сохранить весь проект (async).
    /// ✅ P2-2: Для использования в Unit of Work
    /// </summary>
    public async Task SaveAllAsync()
    {
        // В текущей реализации IO операции синхронные
        // Обёртываем в Task.Run для async/await совместимости
        await Task.Run(() => SaveAll());
    }

    public void SaveProgram(string programName)
    {
        if (_state.Programs.TryGetValue(programName, out var program))
        {
            var programsFolder = Path.Combine(_projectFolder, "Programs");
            Directory.CreateDirectory(programsFolder);
            
            AstroSerializer.SaveProgram(program, programsFolder);
            ExportProgramToText(program, programsFolder);
            ExportProgramToFanucStyle(program, programsFolder);
            
            _hasUnsavedChanges = true;
            _logger.LogDebug("Program saved: {Name}", programName);
        }
        else
        {
            _logger.LogWarning("Program not found for save: {Name}", programName);
        }
    }
    
    private void ExportProgramToText(AstroProgram program, string folderPath)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  ПРОГРАММА: {program.Name,-46} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Автор: {program.Author,-48} ║");
            sb.AppendLine($"║  Версия: {program.Version,-47} ║");
            sb.AppendLine($"║  Описание: {program.Description,-45} ║");
            sb.AppendLine($"║  Строк инструкций: {program.Lines.Count,-38} ║");
            sb.AppendLine($"║  Аргументов: {program.Arguments.Count,-42} ║");
            sb.AppendLine($"║  Макс. циклов: {program.MaxCycles,-42} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  ИНСТРУКЦИИ:                                               ║");
            sb.AppendLine("╠════════╦══════════════════════════════════════════════════╣");
            sb.AppendLine("║ № строки║  Форма                                           ║");
            sb.AppendLine("╠════════╬══════════════════════════════════════════════════╣");
            
            foreach (var line in program.Lines)
            {
                var formText = $"{line.FormId}";
                sb.AppendLine($"║ {line.LineNumber,6} ║  {formText,-50}║");
            }
            
            sb.AppendLine("╚════════╩══════════════════════════════════════════════════╝");
            
            var txtPath = Path.Combine(folderPath, $"{program.Name}.txt");
            File.WriteAllText(txtPath, sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to export program to text: {Name}", program.Name);
        }
    }
    
    private void ExportProgramToFanucStyle(AstroProgram program, string folderPath)
    {
        try
        {
            var lsPath = Path.Combine(folderPath, $"{program.Name}.ls");
            FanucStyleExporter.SaveToFile(program, lsPath, _state.TypeRegistry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to export program to FANUC style: {Name}", program.Name);
        }
    }
}
