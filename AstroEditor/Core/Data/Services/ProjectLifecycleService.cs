// AstroEditor.Core/Data/Services/ProjectLifecycleService.cs
// Сервис управления жизненным циклом проекта

using AstroEditor.Core.Common.Logging;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Реализация сервиса жизненного цикла проекта.
/// </summary>
public class ProjectLifecycleService : IProjectLifecycleService
{
    private readonly ILogger _logger;
    private readonly ProjectState _state;
    private readonly IProjectStorage _storage;
    private readonly IProgramService _programService;

    public string ProjectFolder => _storage.ProjectFolder;
    public bool HasUnsavedChanges => _storage.HasUnsavedChanges;
    
    public event Action? OnProjectChanged;

    public ProjectLifecycleService(
        ProjectState state,
        IProjectStorage storage,
        IProgramService programService,
        ILogger? logger = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _programService = programService ?? throw new ArgumentNullException(nameof(programService));
        _logger = logger ?? Log.For<ProjectLifecycleService>();
        
        // Подписка на события
        if (_storage is ProjectStorageService pss)
            pss.OnProjectChanged += RaiseProjectChanged;
        if (_programService is ProgramService ps)
            ps.OnProgramsChanged += RaiseProjectChanged;
    }

    public void InitializeNew(string projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
            throw new ArgumentException("Project folder cannot be empty", nameof(projectFolder));

        try
        {
            _logger.LogInformation("Initializing new project: {Folder}", projectFolder);
            _storage.InitializeNew(projectFolder);
            _logger.LogInformation("Project initialized successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to project folder: {Folder}", projectFolder);
            throw new ProjectServiceException($"Access denied to folder: {projectFolder}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while initializing project: {Folder}", projectFolder);
            throw new ProjectServiceException($"IO error initializing project: {projectFolder}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing project: {Folder}", projectFolder);
            throw new ProjectServiceException($"Failed to initialize project: {projectFolder}", ex);
        }
    }

    public void Open(string projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
            throw new ArgumentException("Project folder cannot be empty", nameof(projectFolder));

        try
        {
            _logger.LogInformation("Opening project: {Folder}", projectFolder);
            _storage.Open(projectFolder);
            _logger.LogInformation("Project opened successfully");
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Project folder not found: {Folder}", projectFolder);
            throw new ProjectServiceException($"Project folder not found: {projectFolder}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to project folder: {Folder}", projectFolder);
            throw new ProjectServiceException($"Access denied to folder: {projectFolder}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while opening project: {Folder}", projectFolder);
            throw new ProjectServiceException($"IO error opening project: {projectFolder}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error opening project: {Folder}", projectFolder);
            throw new ProjectServiceException($"Failed to open project: {projectFolder}", ex);
        }
    }

    public void SaveAll()
    {
        try
        {
            _logger.LogInformation("Saving all project data");
            _storage.SaveAll();
            _logger.LogInformation("Project saved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while saving project");
            throw new ProjectServiceException("Access denied while saving project", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while saving project");
            throw new ProjectServiceException("IO error while saving project", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving project");
            throw new ProjectServiceException("Failed to save project", ex);
        }
    }

    public void SaveProgram(string programName)
    {
        if (string.IsNullOrWhiteSpace(programName))
            throw new ArgumentException("Program name cannot be empty", nameof(programName));

        try
        {
            _logger.LogDebug("Saving program: {ProgramName}", programName);
            _storage.SaveProgram(programName);
            _logger.LogDebug("Program saved successfully: {ProgramName}", programName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid program name: {ProgramName}", programName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save program: {ProgramName}", programName);
            throw new ProjectServiceException($"Failed to save program: {programName}", ex);
        }
    }

    private void RaiseProjectChanged()
    {
        try
        {
            OnProjectChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising OnProjectChanged event");
            // Не выбрасываем — событие необязательное
        }
    }
}

/// <summary>
/// Исключение сервиса управления проектами.
/// </summary>
public class ProjectServiceException : Exception
{
    public ProjectServiceException(string message) : base(message) { }
    public ProjectServiceException(string message, Exception inner) : base(message, inner) { }
}
