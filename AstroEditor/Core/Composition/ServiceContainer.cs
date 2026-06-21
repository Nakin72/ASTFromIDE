// AstroEditor.Core/Composition/ServiceContainer.cs
// Композиционный корень — настройка DI контейнера

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AstroEditor.Core.Data;
using AstroEditor.Core.Data.Services;
using AstroEditor.Core.Binding;
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Expressions;

namespace AstroEditor.Core.Composition;

/// <summary>
/// Конфигуратор сервисов приложения.
/// </summary>
public static class ServiceContainer
{
    /// <summary>
    /// Создать и настроить DI контейнер.
    /// </summary>
    public static IServiceProvider ConfigureServices(
        Action<ServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        
        // Логирование
        services.AddLogging(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole();
        });
        
        // Сервисы данных (Singleton)
        services.AddSingleton<ProjectState>();
        services.AddSingleton<IProjectStorage, ProjectStorageService>();
        services.AddSingleton<IProgramService, ProgramService>();
        services.AddSingleton<ITypeService, TypeService>();
        services.AddSingleton<IRuntimeService, RuntimeService>();
        
        // Сервис привязок (создаётся с GlobalTables из ProjectState)
        services.AddSingleton<IBindingService>(sp =>
        {
            var state = sp.GetRequiredService<ProjectState>();
            var logger = sp.GetRequiredService<ILogger<ThreadSafeBindingManager>>();
            return new ThreadSafeBindingManager(state.GlobalTables, logger);
        });
        
        // Кэш выражений
        services.AddSingleton<IExpressionCache, ExpressionCache>();
        
        // Сервис жизненного цикла проекта
        services.AddSingleton<IProjectLifecycleService, ProjectLifecycleService>();
        
        // Сервис управления плагинами
        services.AddSingleton<IPluginHostingService, PluginHostingService>();
        
        // Фабрики
        services.AddSingleton<IInterpreterFactory>(sp =>
        {
            var state = sp.GetRequiredService<ProjectState>();
            return new InterpreterFactory(
                state.TypeRegistry,
                state.FormRegistry,
                state.GlobalTables,
                state.Programs,
                state.Functions,
                null
            );
        });
        
        services.AddSingleton<ISchedulerFactory>(sp =>
        {
            var runtimeService = sp.GetRequiredService<IRuntimeService>();
            var bindingService = sp.GetRequiredService<IBindingService>();
            return new SchedulerFactory(
                runtimeService.Alarms,
                runtimeService.Interrupts,
                runtimeService.Timers,
                bindingService
            );
        });
        
        // Сервис интерпретаторов
        services.AddSingleton<IInterpreterHostingService, InterpreterHostingService>();
        
        // Unit of Work (P2-2)
        services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();
        
        // ProjectManager (координатор)
        services.AddSingleton<ProjectManager>(sp =>
        {
            var state = sp.GetRequiredService<ProjectState>();
            var lifecycleService = sp.GetRequiredService<IProjectLifecycleService>();
            var pluginHostingService = sp.GetRequiredService<IPluginHostingService>();
            var interpreterHostingService = sp.GetRequiredService<IInterpreterHostingService>();
            var typeService = sp.GetRequiredService<ITypeService>();
            var runtimeService = sp.GetRequiredService<IRuntimeService>();
            var bindingService = sp.GetRequiredService<IBindingService>();
            var programService = sp.GetRequiredService<IProgramService>();
            
            return new ProjectManager(
                state,
                lifecycleService,
                pluginHostingService,
                interpreterHostingService,
                typeService,
                runtimeService,
                bindingService,
                programService
            );
        });
        
        // PluginManager (создаётся при инициализации проекта)
        services.AddTransient<PluginManager>(sp =>
        {
            var pm = sp.GetRequiredService<ProjectManager>();
            // PluginManager создаётся внутри ProjectManager.InitializeNew()
            throw new InvalidOperationException("PluginManager must be created via ProjectManager");
        });
        
        // Вызов пользовательской конфигурации
        configure?.Invoke(services);
        
        return services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Получить сервис из контейнера.
    /// </summary>
    public static T GetService<T>(this IServiceProvider provider) where T : notnull
    {
        return provider.GetRequiredService<T>();
    }
    
    /// <summary>
    /// Получить сервис или null.
    /// </summary>
    public static T? GetServiceOrNull<T>(this IServiceProvider provider) where T : class
    {
        return provider.GetService<T>();
    }
}
