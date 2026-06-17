// using AstroEditor.Core.v4.Types;
// using AstroEditor.Core.v4.Variables;
// using AstroEditor.Core.v4.Tables;
// using System.Text.Json;

// var registry = new DataTypeRegistry();

// // 1. Регистрируем все примитивы "ядра"
// var intType = PrimitiveDataType.Int();
// intType.Id = "int"; // фиксированный ID для удобства
// registry.RegisterType(intType);

// var doubleType = PrimitiveDataType.Double();
// doubleType.Id = "double";
// registry.RegisterType(doubleType);

// var boolType = PrimitiveDataType.Bool();
// boolType.Id = "bool";
// registry.RegisterType(boolType);

// var stringType = PrimitiveDataType.String();
// stringType.Id = "string";
// registry.RegisterType(stringType);

// // 2. Создаём системный тип REAL как псевдоним на double (или можно создать отдельный системный тип)
// var realType = new AliasDataType
// {
//     Name = "REAL",
//     Category = DataTypeCategory.System,
//     BaseTypeId = "double"
// };
// registry.RegisterType(realType);

// // 3. Создаём структуру POSITION
// var posType = new StructDataType
// {
//     Name = "POSITION",
//     Category = DataTypeCategory.System,
//     Fields = new List<StructField>
//     {
//         new StructField { Name = "X", TypeId = "double" },
//         new StructField { Name = "Y", TypeId = "double" },
//         new StructField { Name = "Z", TypeId = "double" },
//         new StructField { Name = "A", TypeId = "double" }, // ориентация
//         new StructField { Name = "B", TypeId = "double" },
//         new StructField { Name = "C", TypeId = "double" }
//     }
// };
// registry.RegisterType(posType);

// // 4. Создаём глобальную таблицу
// var globalTable = new VariableTable
// {
//     Name = "GlobalRegisters",
//     IsGlobal = true
// };

// // 5. Добавляем переменные
// globalTable.AddVariable(new Variable("Speed", realType, 150.0));
// globalTable.AddVariable(new Variable("Home", posType, new Dictionary<string, object>
// {
//     ["X"] = 0.0,
//     ["Y"] = 0.0,
//     ["Z"] = 0.0,
//     ["A"] = 0.0,
//     ["B"] = 0.0,
//     ["C"] = 0.0
// }));
// globalTable.AddVariable(new Variable("Counter", intType, 0));
// globalTable.AddVariable(new Variable("IsRunning", boolType, false));

// // 6. Сериализация в JSON
// var options = new JsonSerializerOptions
// {
//     WriteIndented = true,
//     Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
// };
// var json = JsonSerializer.Serialize(globalTable, options);
// Console.WriteLine("=== Глобальная таблица ===");
// Console.WriteLine(json);

// // 7. Десериализация обратно
// var restoredTable = JsonSerializer.Deserialize<VariableTable>(json, options);
// // Восстанавливаем ссылки на типы
// restoredTable?.ResolveReferences(registry);
// Console.WriteLine($"\nВосстановлено: {restoredTable?.Name}, переменных: {restoredTable?.Variables.Count}");