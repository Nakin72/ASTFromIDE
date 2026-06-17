using System;
using System.Collections;
using System.Collections.Generic;

namespace AstroEditor.Core.v3.Types
{
    // Категории базовых данных
    public enum AccessLevel
    {
        Core,
        Vendor,
        Programmer,
        User
    }
    public enum DataTypeFamily
    {
        Any,
        Numeric,
        Text,
        Temporal,
        Logical,
        Collection, // Для списков
        Structure   // Для комплексных структур / пользовательских типов
    }

    // Объект созданного типа данных


    // Универсальный контейнер переменной
    public class CoreDataContainer
    {
        private object? _value;
        public CoreDataType DataType { get; init; }

        public object? Value
        {
            get => _value;
            set
            {
                ValidateValue(value);
                _value = value;
            }
        }

        public CoreDataContainer(CoreDataType dataType, object? value = null)
        {
            DataType = dataType;

            // Авто-инициализация пустых объектов для структур и списков, если передан null
            if (value == null)
            {
                if (DataType.Family == DataTypeFamily.Structure) value = new CoreStruct();
                else if (DataType.Family == DataTypeFamily.Collection) value = new CoreDataList(DataType.ElementType ?? CoreDataType.Any);
            }

            Value = value;
        }

        private void ValidateValue(object? value)
        {
            if (value == null)
            {
                if (DataType != CoreDataType.Any && DataType.Family != DataTypeFamily.Text)
                    throw new ArgumentNullException(nameof(value), $"Тип {DataType.Name} не поддерживает null.");
                return;
            }

            if (DataType == CoreDataType.Any) return;

            // Проверка базового системного типа C#
            if (DataType.TargetSystemType != null && value.GetType() != DataType.TargetSystemType)
            {
                throw new InvalidCastException($"Ожидается системный класс '{DataType.TargetSystemType.Name}', передан '{value.GetType().Name}'.");
            }

            // [ИСПРАВЛЕНО]: Проверка типа элементов для списков
            if (DataType.Family == DataTypeFamily.Collection && value is CoreDataList list)
            {
                // Если у контейнера базовый тип "List", его ElementType равен null. 
                // В таком случае мы трактуем его как Any (разрешено всё).
                var expectedElement = DataType.ElementType ?? CoreDataType.Any;

                // Если ожидается конкретный тип, проверяем на строгое соответствие
                if (expectedElement != CoreDataType.Any && list.AllowedType != expectedElement)
                {
                    throw new ArgumentException($"Тип списка '{DataType.Name}' ожидает элементы типа '{expectedElement.Name}', но передан список элементов '{list.AllowedType.Name}'.");
                }
            }
        }
    }

    public class CoreDataType
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; }
        public DataTypeFamily Family { get; }
        public Type? TargetSystemType { get; }
        public CoreDataType? ElementType { get; init; }

        // Ссылка на базовый тип, от которого унаследован этот тип
        public CoreDataType? BaseType { get; }

        internal CoreDataType(string name, AccessLevel accessLevel, DataTypeFamily family, Type? targetSystemType = null, CoreDataType? baseType = null)
        {
            Name = name;
            AccessLevel = accessLevel;
            Family = family;
            TargetSystemType = targetSystemType;
            BaseType = baseType;
        }

        // Проверка: можно ли этот тип данных присвоить в переменную целевого типа targetType
        public bool IsAssignableTo(CoreDataType targetType)
        {
            if (targetType == CoreDataType.Any) return true;

            CoreDataType? current = this;
            while (current != null)
            {
                if (current == targetType) return true;
                current = current.BaseType; // Идем вверх по цепочке наследования
            }
            return false;
        }

        // Пример обновления статического конструктора (базовые типы ни от кого не наследуются, baseType = null)
        // Системные типы "из коробки"
        public static readonly CoreDataType Any = new("Any", AccessLevel.User, DataTypeFamily.Any);
        public static readonly CoreDataType Boolean = new("Boolean", AccessLevel.User, DataTypeFamily.Logical, typeof(bool));
        public static readonly CoreDataType SignedByte = new("Signed Byte", AccessLevel.User, DataTypeFamily.Numeric, typeof(sbyte));
        public static readonly CoreDataType Byte = new("Byte", AccessLevel.User, DataTypeFamily.Numeric, typeof(byte));
        public static readonly CoreDataType Short = new("Short", AccessLevel.User, DataTypeFamily.Numeric, typeof(short));
        public static readonly CoreDataType UnsignedShort = new("Unsigned Short", AccessLevel.User, DataTypeFamily.Numeric, typeof(ushort));
        public static readonly CoreDataType Integer = new("Integer", AccessLevel.User, DataTypeFamily.Numeric, typeof(int));
        public static readonly CoreDataType UnsignedInteger = new("Unsigned Integer", AccessLevel.User, DataTypeFamily.Numeric, typeof(uint));
        public static readonly CoreDataType Long = new("Long", AccessLevel.User, DataTypeFamily.Numeric, typeof(long));
        public static readonly CoreDataType UnsignedLong = new("Unsigned Long", AccessLevel.User, DataTypeFamily.Numeric, typeof(ulong));
        public static readonly CoreDataType Double = new("Double", AccessLevel.User, DataTypeFamily.Numeric, typeof(double));
        public static readonly CoreDataType Decimal = new("Decimal", AccessLevel.User, DataTypeFamily.Numeric, typeof(decimal));
        public static readonly CoreDataType DateTime = new("DateTime", AccessLevel.User, DataTypeFamily.Temporal, typeof(DateTime));
        public static readonly CoreDataType String = new("String", AccessLevel.User, DataTypeFamily.Text, typeof(string));

        // Системный базовый тип для любого списка и любой структуры
        public static readonly CoreDataType GenericList = new("List", AccessLevel.User, DataTypeFamily.Collection, typeof(CoreDataList));
        public static readonly CoreDataType GenericStruct = new("Struct", AccessLevel.User, DataTypeFamily.Structure, typeof(CoreStruct));
        // ... остальные системные типы

    }
    // Реестр созданных типов
    public static class TypeRegistry
    {
        private static readonly Dictionary<string, CoreDataType> _types = new(StringComparer.OrdinalIgnoreCase);

        static TypeRegistry()
        {
            RegisterType(CoreDataType.Any);
            RegisterType(CoreDataType.Boolean);
            RegisterType(CoreDataType.SignedByte);
            RegisterType(CoreDataType.Byte);
            RegisterType(CoreDataType.Short);
            RegisterType(CoreDataType.UnsignedShort);
            RegisterType(CoreDataType.Integer);
            RegisterType(CoreDataType.UnsignedInteger);
            RegisterType(CoreDataType.Long);
            RegisterType(CoreDataType.UnsignedLong);
            RegisterType(CoreDataType.Double);
            RegisterType(CoreDataType.Decimal);
            RegisterType(CoreDataType.DateTime);
            RegisterType(CoreDataType.String);
            RegisterType(CoreDataType.GenericList);
            RegisterType(CoreDataType.GenericStruct);
        }

        private static void RegisterType(CoreDataType type) => _types[type.Name] = type;

        // Создание нового пользовательского типа (структуры)
        public static CoreDataType RegisterUserStructType(string name)
        {
            if (_types.ContainsKey(name))
                throw new ArgumentException($"Тип '{name}' уже существует.");

            var newType = new CoreDataType(name, AccessLevel.User, DataTypeFamily.Structure, typeof(CoreStruct));
            _types[name] = newType;
            return newType;
        }

        // Создание нового типа-списка (например, "IntList" или "Matrix2D")
        public static CoreDataType RegisterListType(string name, CoreDataType elementType)
        {
            if (_types.ContainsKey(name))
                throw new ArgumentException($"Тип '{name}' уже существует.");

            var newType = new CoreDataType(name, AccessLevel.User, DataTypeFamily.Collection, typeof(CoreDataList))
            {
                ElementType = elementType
            };
            _types[name] = newType;
            return newType;
        }

        public static CoreDataType GetType(string name) => _types[name];
        public static CoreDataType RegisterUserDerivedType(string name, CoreDataType baseType)
        {
            if (_types.ContainsKey(name))
                throw new ArgumentException($"Тип '{name}' уже существует.");

            // Наследуем семейство и системный тип C# от базового типа
            var newType = new CoreDataType(name, AccessLevel.User, baseType.Family, baseType.TargetSystemType, baseType);
            _types[name] = newType;
            return newType;
        }
        internal static bool UnregisterType(string name)
        {
            return _types.Remove(name);
        }
    }

 
    // Список без дженериков
    public class CoreDataList : IEnumerable<CoreDataContainer>
    {
        private readonly List<CoreDataContainer> _items = new();
        public CoreDataType AllowedType { get; init; }
        public int? MaxCapacity { get; init; }

        public CoreDataList(CoreDataType allowedType, int? maxCapacity = null)
        {
            AllowedType = allowedType;
            MaxCapacity = maxCapacity;
        }

        public void Add(CoreDataContainer item)
        {
            if (MaxCapacity.HasValue && _items.Count >= MaxCapacity.Value)
                throw new InvalidOperationException("Превышена вместимость списка.");

            if (AllowedType != CoreDataType.Any && item.DataType != AllowedType)
                throw new ArgumentException($"Список ожидает '{AllowedType.Name}', а передан контейнер '{item.DataType.Name}'.");

            _items.Add(item);
        }
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Индекс вне диапазона списка.");
            _items.RemoveAt(index);
        }

        // Удаление конкретного контейнера из списка
        public bool Remove(CoreDataContainer item)
        {
            return _items.Remove(item);
        }

        // Полная очистка списка
        public void Clear()
        {
            _items.Clear();
        }
        public CoreDataContainer this[int index] => _items[index];
        public int Count => _items.Count;
        public IEnumerator<CoreDataContainer> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    // Структура без дженериков
    public class CoreStruct
    {
        private readonly Dictionary<string, CoreDataContainer> _fields = new(StringComparer.OrdinalIgnoreCase);
        public bool IsLocked { get; set; } = false;


        public CoreDataContainer this[string name]
        {
            get => _fields.TryGetValue(name, out var container) ? container : throw new KeyNotFoundException($"Поле '{name}' отсутствует.");
            set => _fields[name] = !IsLocked ? value : throw new InvalidOperationException("Структура заблокирована.");
        }

        public void AddField(string name, CoreDataContainer container)
        {
            if (IsLocked) throw new InvalidOperationException("Структура заблокирована.");
            _fields.Add(name, container);
        }
        public bool RemoveField(string name)
        {
            if (IsLocked)
                throw new InvalidOperationException("Нельзя удалять поля: структура заблокирована.");

            return _fields.Remove(name);
        }

        // Полная очистка всех полей структуры
        public void ClearFields()
        {
            if (IsLocked)
                throw new InvalidOperationException("Нельзя очистить структуру: она заблокирована.");

            _fields.Clear();
        }
        public IEnumerable<KeyValuePair<string, CoreDataContainer>> GetFields()
        {
            return _fields;
        }

        // // Метод для очистки поля или пометки его невалидным (для UI редактора)
        // public bool RemoveField(string name)
        // {
        //     if (IsLocked) throw new InvalidOperationException("Структура заблокирована.");
        //     return _fields.Remove(name);
        // }
    }
}
