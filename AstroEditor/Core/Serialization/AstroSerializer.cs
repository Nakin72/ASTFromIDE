// AstroEditor.Core.v4/Serialization/AstroSerializer.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using AstroEditor.Core.Types;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Serialization;

public static class AstroSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void SaveToFile<T>(T obj, string filePath)
    {
        var json = JsonSerializer.Serialize(obj, Options);
        File.WriteAllText(filePath, json);
    }

    public static T? LoadFromFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    // Удобные перегрузки для основных типов
    public static void SaveDataTypeRegistry(DataTypeRegistry registry, string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        SaveToFile(registry, Path.Combine(folderPath, "types.json"));
    }

    public static DataTypeRegistry LoadDataTypeRegistry(string folderPath)
    {
        var file = Path.Combine(folderPath, "types.json");
        DataTypeRegistry registry;
        if (File.Exists(file))
        {
            registry = LoadFromFile<DataTypeRegistry>(file) ?? new DataTypeRegistry();
        }
        else
        {
            registry = new DataTypeRegistry();
        }

        RegisterPrimitivesIfMissing(registry);
        registry.ResolveReferences();
        return registry;
    }

    private static void RegisterPrimitivesIfMissing(DataTypeRegistry registry)
    {
        // 1. Все примитивные типы (ядро) — 14 штук
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
            if (registry.GetTypeById(id) == null)
            {
                var type = new PrimitiveDataType
                {
                    Id = id,
                    Name = name,
                    Primitive = prim,
                    Category = DataTypeCategory.Core
                };
                registry.RegisterType(type);
            }
        }

        // 2. Псевдоним REAL (системный)
        if (registry.GetTypeById("real") == null)
        {
            var realType = new AliasDataType
            {
                Id = "real",
                Name = "REAL",
                Category = DataTypeCategory.System,
                BaseTypeId = "double"
            };
            registry.RegisterType(realType);
        }

        // 3. Структура POSITION (системная)
        if (registry.GetTypeById("position") == null)
        {
            var posType = new StructDataType
            {
                Id = "position",
                Name = "POSITION",
                Category = DataTypeCategory.System,
                Fields = new List<StructField>
            {
                new StructField { Name = "X", TypeId = "double" },
                new StructField { Name = "Y", TypeId = "double" },
                new StructField { Name = "Z", TypeId = "double" }
            }
            };
            registry.RegisterType(posType);
        }
    }
    public static void SaveFormRegistry(FormRegistry registry, string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        SaveToFile(registry, Path.Combine(folderPath, "forms.json"));
    }

    public static FormRegistry LoadFormRegistry(string folderPath)
    {
        var file = Path.Combine(folderPath, "forms.json");
        return LoadFromFile<FormRegistry>(file) ?? new FormRegistry();
    }

    public static void SaveGlobalTables(VariableTableSet tables, string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        SaveToFile(tables, Path.Combine(folderPath, "globals.json"));
    }

    public static VariableTableSet LoadGlobalTables(string folderPath, DataTypeRegistry registry)
    {
        var file = Path.Combine(folderPath, "globals.json");
        var tables = LoadFromFile<VariableTableSet>(file) ?? new VariableTableSet { Name = "GlobalVariables", IsGlobal = true };
        tables.ResolveReferences(registry);
        return tables;
    }

    public static void SaveProgram(AstroProgram program, string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        SaveToFile(program, Path.Combine(folderPath, $"{program.Name}.ast"));
    }

    public static AstroProgram LoadProgram(string folderPath, string programName, DataTypeRegistry registry)
    {
        var file = Path.Combine(folderPath, $"{programName}.ast");
        var program = LoadFromFile<AstroProgram>(file) ?? throw new Exception($"Program {programName} not found");
        program.LocalTables.ResolveReferences(registry);
        program.ReturnType = registry.GetTypeById(program.ReturnTypeId);
        foreach (var arg in program.Arguments)
            arg.Type = registry.GetTypeById(arg.TypeId);
        return program;
    }
}