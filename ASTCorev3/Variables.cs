using System;
using System.Collections;
using System.Collections.Generic;
using AstroEditor.Core.v3.Types;

namespace AstroEditor.Core.v3.Variables
{
    // 1. Область видимости переменной
    public enum VariableScope
    {
        Local,  // Доступна внутри текущей программы
        Global  // Доступна между всеми программами системы
    }

    // 2. Объект Переменной
    public class CoreVariable
    {
        public string Name { get; }
        public VariableScope Scope { get; }
        public CoreDataContainer Container { get; set; }

        // Свойство для быстрого доступа к значению: myVar.Value = 10;
        public object? Value
        {
            get => Container.Value;
            set => Container.Value = value;
        }

        public CoreVariable(string name, VariableScope scope, CoreDataContainer container)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя переменной не может быть пустым.");

            Name = name;
            Scope = scope;
            Container = container;
        }
        internal void UpdateContainerTypeInternal(CoreDataType newType)
        {
            // Создаем новый контейнер с новым типом, но сохраняем текущее значение
            var newContainer = new CoreDataContainer(newType, this.Value);
            // Заменяем контейнер в переменной (свойство Container должно иметь приватный сеттер: public CoreDataContainer Container { get; private set; })
            this.Container = newContainer;
        }
    }

    // 3. Таблица переменных для ОДНОГО конкретного типа данных
    public class VariableTypeTable
    {
        public CoreDataType TableType { get; init; }
        private readonly Dictionary<string, CoreVariable> _variables = new(StringComparer.OrdinalIgnoreCase);

        public VariableTypeTable(CoreDataType tableType) => TableType = tableType;

        public void Add(CoreVariable variable)
        {
            if (_variables.ContainsKey(variable.Name))
                throw new ArgumentException($"Переменная '{variable.Name}' уже существует в таблице '{TableType.Name}'.");
            _variables.Add(variable.Name, variable);
        }

        // Удаление конкретной переменной по имени
        public bool Remove(string name) => _variables.Remove(name);

        public bool TryGet(string name, out CoreVariable? variable) => _variables.TryGetValue(name, out variable);

        public IEnumerable<CoreVariable> GetAll() => _variables.Values;

        public void Clear() => _variables.Clear();
    }

    // 4. Менеджер таблиц переменных (Глобальный хаб)


    public class VariableTableManager
    {
        private readonly Dictionary<CoreDataType, VariableTypeTable> _globalTables = new();
        private readonly Dictionary<CoreDataType, VariableTypeTable> _localTables = new();

        public CoreVariable DeclareVariable(string name, CoreDataType dataType, object? initialValue, VariableScope scope, AccessLevel creatorAccess)
        {
            if (creatorAccess < dataType.AccessLevel)
                throw new UnauthorizedAccessException($"Недостаточно прав для создания типа '{dataType.Name}'.");

            var targetPool = (scope == VariableScope.Global) ? _globalTables : _localTables;

            if (!targetPool.TryGetValue(dataType, out var typeTable))
            {
                typeTable = new VariableTypeTable(dataType);
                targetPool.Add(dataType, typeTable);
            }

            var container = new CoreDataContainer(dataType, initialValue);
            var variable = new CoreVariable(name, scope, container);

            typeTable.Add(variable);
            return variable;
        }

        // --- УДАЛЕНИЕ ПЕРЕМЕННЫХ ---

        // Удалить конкретную переменную по имени из определенной области видимости
        public bool DeleteVariable(string name, VariableScope scope)
        {
            var targetPool = (scope == VariableScope.Global) ? _globalTables : _localTables;

            // Ищем, в таблице какого именно типа лежит эта переменная
            foreach (var table in targetPool.Values)
            {
                if (table.Remove(name))
                {
                    return true; // Успешно удалено, прерываем поиск
                }
            }
            return false; // Переменная не найдена
        }

        // Полная очистка всей области видимости (например, при закрытии локальной программы)
        public void ClearStorage(VariableScope scope)
        {
            var targetPool = (scope == VariableScope.Global) ? _globalTables : _localTables;
            foreach (var table in targetPool.Values)
            {
                table.Clear();
            }
            targetPool.Clear(); // Удаляем сами таблицы для экономии памяти
        }


        // --- СОВМЕСТИМОСТЬ И ПОЛИМОРФНЫЙ ПОИСК ---

        // Возвращает ВСЕ переменные, которые совместимы с целевым типом (включая наследников)
        // Дублирования нет: мы просто объединяем ссылки из разных таблиц
        public List<CoreVariable> GetCompatibleVariables(CoreDataType targetType, VariableScope scope)
        {
            var targetPool = (scope == VariableScope.Global) ? _globalTables : _localTables;
            var result = new List<CoreVariable>();

            foreach (var table in targetPool.Values)
            {
                // Метод IsAssignableTo (который мы написали ранее) проверяет цепочку наследования.
                // Если тип таблицы (например, PlayerHP) унаследован от targetType (например, Integer),
                // то все переменные из этой таблицы подходят!
                if (table.TableType.IsAssignableTo(targetType))
                {
                    result.AddRange(table.GetAll());
                }
            }

            return result;
        }

        // Возвращает список существующих таблиц, отсортированных по иерархии наследования:
        // Сначала идут базовые системные типы, затем типы первого уровня наследования, затем глубже.
        public List<VariableTypeTable> GetTablesSortedByHierarchy(VariableScope scope)
        {
            var targetPool = (scope == VariableScope.Global) ? _globalTables : _localTables;

            return targetPool.Values
                .OrderBy(table => GetInheritanceDepth(table.TableType))
                .ThenBy(table => table.TableType.Name) // Если глубина одинаковая — сортируем по алфавиту
                .ToList();
        }

        // Вспомогательный метод вычисления глубины наследования типа
        private int GetInheritanceDepth(CoreDataType type)
        {
            int depth = 0;
            CoreDataType? current = type.BaseType;
            while (current != null)
            {
                depth++;
                current = current.BaseType;
            }
            return depth; // 0 для базовых (Integer), 1 для прямых наследников (HP) и т.д.
        }
        public void RemoveDataTypeWithMigration(CoreDataType typeToRemove)
        {
            // 1. Системные типы удалять категорически запрещено
            if (typeToRemove.AccessLevel == 0)
                throw new InvalidOperationException($"Критическая ошибка: невозможно удалить системный тип данных '{typeToRemove.Name}'.");

            // 2. Проверяем, используется ли этот тип в сложных связях (внутри других контейнеров)
            // Ищем по всем глобальным и локальным таблицам
            var allTables = _globalTables.Values.Concat(_localTables.Values);

            foreach (var table in allTables)
            {
                // Проверяем: не является ли типToRemove типом элементов в каком-то списке?
                if (table.TableType.Family == DataTypeFamily.Collection && table.TableType.ElementType == typeToRemove)
                {
                    throw new InvalidOperationException(
                        $"Защита системы: Невозможно удалить тип '{typeToRemove.Name}', " +
                        $"так как он используется как тип элементов в списке '{table.TableType.Name}'.");
                }

                // Проверяем внутренности переменных (вдруг тип используется внутри полей структур или списков)
                foreach (var variable in table.GetAll())
                {
                    if (IsTypeUsedInContainer(variable.Container, typeToRemove))
                    {
                        throw new InvalidOperationException(
                            $"Защита системы: Невозможно удалить тип '{typeToRemove.Name}', " +
                            $"так как он все еще используется в переменной '{variable.Name}' или глубоко внутри её полей/элементов.");
                    }
                }
            }
            // 3. Если тип относится к структурам или спискам, и проверки выше прошли (переменных нет) — просто удаляем
            if (typeToRemove.Family == DataTypeFamily.Structure || typeToRemove.Family == DataTypeFamily.Collection)
            {
                // Удаляем пустые таблицы этого типа, если они были созданы в рантайме
                _globalTables.Remove(typeToRemove);
                _localTables.Remove(typeToRemove);
                TypeRegistry.UnregisterType(typeToRemove.Name);
                return;
            }

            // 4. МИГРАЦИЯ ДЛЯ ПРОСТЫХ ТИПОВ (Numeric, Text, Temporal, Logical)
            // Если тип простой, у него гарантированно есть BaseType (например, Integer для PlayerHP)
            CoreDataType fallbackType = typeToRemove.BaseType ?? CoreDataType.Any;

            MigrateTableData(_globalTables, typeToRemove, fallbackType);
            MigrateTableData(_localTables, typeToRemove, fallbackType);

            // 5. Окончательно удаляем тип из реестра типов
            TypeRegistry.UnregisterType(typeToRemove.Name);
        }
        private void MigrateTableData(Dictionary<CoreDataType, VariableTypeTable> pool, CoreDataType oldType, CoreDataType newType)
        {
            if (!pool.TryGetValue(oldType, out var oldTable)) return;

            // Если таблицы для родительского типа еще нет в пуле — создаем её
            if (!pool.TryGetValue(newType, out var newTable))
            {
                newTable = new VariableTypeTable(newType);
                pool.Add(newType, newTable);
            }

            // Переносим каждую переменную
            foreach (var variable in oldTable.GetAll().ToList())
            {
                // Изменяем тип самого контейнера данных на родительский
                // Используем рефлексию или внутренний метод, так как DataType у нас { get; init; }
                // Для чистоты кода без рефлексии, добавим внутренний метод миграции типа в CoreDataContainer,
                // но в данном примере просто пересоздадим ссылку, если бы свойство позволяло.
                // Чтобы не менять { get; init }, обновим тип контейнера через хак или сделаем поле изменяемым для системы:

                variable.UpdateContainerTypeInternal(newType);

                // Добавляем в родительскую таблицу
                newTable.Add(variable);
            }

            // Удаляем старую таблицу типа из пула
            pool.Remove(oldType);
        }
        // Рекурсивный поиск: используется ли тип внутри контейнера (для проверок вложенности)
        // private bool IsTypeUsedInContainer(CoreDataContainer container, CoreDataType targetType)
        // {
        //     if (container.DataType == targetType) return true;

        //     if (container.Value is CoreStruct cStruct)
        //     {
        //         // Мы не можем легко прочитать приватный словарь _fields структуры, 
        //         // поэтому для полноценной проверки в класс CoreStruct нужно будет добавить метод-итератор по контейнерам полей.
        //         // Заглушка для демонстрации идеи:
        //         // foreach(var fieldContainer in cStruct.GetContainers()) if(IsTypeUsedInContainer(fieldContainer, targetType)) return true;
        //     }
        //     else if (container.Value is CoreDataList cList)
        //     {
        //         foreach (var itemContainer in cList)
        //         {
        //             if (IsTypeUsedInContainer(itemContainer, targetType)) return true;
        //         }
        //     }

        //     return false;
        // }
        // РЕКУРСИВНЫЙ ПОИСК: Проверяет, используется ли тип где-либо внутри контейнера
        public bool IsTypeUsedInContainer(CoreDataContainer container, CoreDataType targetType)
        {
            // 1. Прямая проверка типа самого контейнера
            if (container.DataType == targetType) return true;

            // 2. Если внутри контейнера лежит структура — рекурсивно проверяем все её поля
            if (container.Value is CoreStruct cStruct)
            {
                foreach (var field in cStruct.GetFields())
                {
                    if (IsTypeUsedInContainer(field.Value, targetType))
                    {
                        return true;
                    }
                }
            }
            // 3. Если внутри контейнера лежит список — рекурсивно проверяем все его элементы
            else if (container.Value is CoreDataList cList)
            {
                foreach (var itemContainer in cList)
                {
                    if (IsTypeUsedInContainer(itemContainer, targetType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        // МЕТОД ДЛЯ UI: Найти все переменные, которые "сломаются" (будут помечены красным) после удаления типа
        public List<CoreVariable> FindVariablesUsingType(CoreDataType typeToCheck)
        {
            var affectedVariables = new List<CoreVariable>();
            var allTables = _globalTables.Values.Concat(_localTables.Values);

            foreach (var table in allTables)
            {
                foreach (var variable in table.GetAll())
                {
                    if (IsTypeUsedInContainer(variable.Container, typeToCheck))
                    {
                        affectedVariables.Add(variable);
                    }
                }
            }

            return affectedVariables;
        }
    }
}






