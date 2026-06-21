// AstroEditor.Core/Data/Services/TypeService.cs
// Сервис типов и форм

using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Common;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Types;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Сервис типов и форм — регистрация и управление.
/// </summary>
public class TypeService : ITypeService
{
    private readonly ILogger _logger;
    private readonly ProjectState _state;

    public DataTypeRegistry TypeRegistry => _state.TypeRegistry;
    public FormRegistry FormRegistry => _state.FormRegistry;

    public TypeService(ProjectState state, ILogger? logger = null)
    {
        _state = state;
        _logger = logger ?? Log.For<TypeService>();
    }

    public void RegisterType(DataType type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        
        _state.TypeRegistry.RegisterType(type);
        _logger.LogDebug("Type registered: {Id} ({Name})", type.Id, type.Name);
    }

    public void RegisterForm(FormDefinition form)
    {
        if (form == null)
            throw new ArgumentNullException(nameof(form));
        
        _state.FormRegistry.RegisterForm(form);
        _logger.LogDebug("Form registered: {Id}", form.Id);
    }

    public DataType? GetTypeById(string id)
    {
        return _state.TypeRegistry.GetTypeById(id);
    }

    public FormDefinition? GetFormById(string id)
    {
        return _state.FormRegistry.GetFormById(id);
    }

    public void InitializePrimitives()
    {
        _logger.LogInformation("Initializing primitive types");
        
        var primitives = new (string Id, string Name, BuiltInPrimitive Prim)[]
        {
            ("sbyte", "SBYTE", BuiltInPrimitive.SByte),
            ("byte", "BYTE", BuiltInPrimitive.Byte),
            ("short", "SHORT", BuiltInPrimitive.Short),
            ("ushort", "USHORT", BuiltInPrimitive.UShort),
            ("int", "INT", BuiltInPrimitive.Int),
            ("uint", "UINT", BuiltInPrimitive.UInt),
            ("long", "LONG", BuiltInPrimitive.Long),
            ("ulong", "ULONG", BuiltInPrimitive.ULong),
            ("float", "FLOAT", BuiltInPrimitive.Float),
            ("double", "DOUBLE", BuiltInPrimitive.Double),
            ("decimal", "DECIMAL", BuiltInPrimitive.Decimal),
            ("bool", "BOOL", BuiltInPrimitive.Bool),
            ("char", "CHAR", BuiltInPrimitive.Char),
            ("string", "STRING", BuiltInPrimitive.String)
        };

        foreach (var (id, name, prim) in primitives)
        {
            if (_state.TypeRegistry.GetTypeById(id) == null)
            {
                var type = new PrimitiveDataType
                {
                    Id = id,
                    Name = name,
                    Primitive = prim,
                    Category = DataTypeCategory.Core
                };
                RegisterType(type);
            }
        }
        
        // Системные типы-псевдонимы
        RegisterAliasIfMissing("real", "REAL", "double", DataTypeCategory.System);
        RegisterAliasIfMissing("position", "POSITION", null, DataTypeCategory.System, fields: new List<StructField>
        {
            new() { Name = "X", TypeId = "double" },
            new() { Name = "Y", TypeId = "double" },
            new() { Name = "Z", TypeId = "double" },
            new() { Name = "A", TypeId = "double" },
            new() { Name = "B", TypeId = "double" },
            new() { Name = "C", TypeId = "double" }
        });

        _state.TypeRegistry.ResolveReferences();
    }

    public void InitializeBuiltinForms()
    {
        _logger.LogInformation("Initializing builtin forms");
        
        _state.FormRegistry.Clear();
        
        // Управляющие структуры
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAssignmentForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateWhileForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndWhileForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateForForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndForForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateForEachForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndForEachForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateIfForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateElseForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndIfForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateSwitchForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateCaseForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateDefaultForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndSwitchForm());
        
        // Вызовы и возвраты
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateCallForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateReturnForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateLabelForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateJumpLblForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateJumpIfForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateBreakForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateContinueForm());

        // Аварии
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmRaiseForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmClearForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmAckForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmClearAllForm());

        // Прерывания
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateInterruptDeclareForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateInterruptOnForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateInterruptOffForm());

        // Таймеры
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerDeclareForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerOnForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerOffForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerResetForm());

        // WAIT
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateWaitForm());

        // Исключения
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTryForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateCatchForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateFinallyForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndTryForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateThrowForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateRethrowForm());
    }
    
    private void RegisterAliasIfMissing(string id, string name, string? baseTypeId, DataTypeCategory category, List<StructField>? fields = null)
    {
        if (_state.TypeRegistry.GetTypeById(id) != null) return;

        if (fields != null)
        {
            var structType = new StructDataType
            {
                Id = id,
                Name = name,
                Category = category,
                Fields = fields
            };
            RegisterType(structType);
        }
        else if (baseTypeId != null)
        {
            var alias = new AliasDataType
            {
                Id = id,
                Name = name,
                Category = category,
                BaseTypeId = baseTypeId
            };
            RegisterType(alias);
        }
    }
}
