using System;
using System.Collections.Concurrent; // ВАЖНО: для потокобезопасного списка HashSet
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AstroEditor.Core.CoreTypes;

namespace AstroEditor.Core.SystemTypes
{
    public interface I_SystemDataContainer : I_DataContainer
    {
        public string Type { get; }
    }

    public abstract class SystemContainer : BaseContainer, I_SystemDataContainer
    {
        // ГЛОБАЛЬНЫЙ РЕЕСТР: Сюда автоматически попадают все рантайм-типы
        // ConcurrentDictionary гарантирует, что движок не "упадет" при многопоточной работе
        private static readonly ConcurrentDictionary<string, byte> _customTypesRegistry = new();

        // ПУБЛИЧНЫЙ СПИСОК: Отсюда ваш UI или фабрика могут прочитать все кастомные типы
        public static IEnumerable<string> CustomTypes => _customTypesRegistry.Keys;

        public virtual string Type { get; init; } = "Any";

        [SetsRequiredMembers]
        protected SystemContainer(object data, string name, string systemType, bool isCustom = false)
                : base(data, name, isCustom)
        {
            Type = systemType; 

            // АВТОРЕГИСТРАЦИЯ: Если контейнер помечен как кастомный, 
            // его имя типа автоматически заносится в глобальный реестр
            if (isCustom && !string.IsNullOrWhiteSpace(systemType))
            {
                _customTypesRegistry.TryAdd(systemType, 0);
            }
        }

        // Метод для ручного удаления типа из реестра (например, при удалении шаблона пользователем)
        public static void UnregisterCustomType(string systemType)
        {
            _customTypesRegistry.TryRemove(systemType, out _);
        }

        // Метод для полной очистки списка кастомных типов (например, при смене сцены или проекта)
        public static void ClearCustomTypes()
        {
            _customTypesRegistry.Clear();
        }
    }

    public class SystemNumberContainer : SystemContainer
    {
        protected override int CoreTypeNum { get; init; } = 1; 

        [SetsRequiredMembers]
        public SystemNumberContainer(object data, string name, string systemType = "Number", bool isCustom = false)
            : base(data, name, systemType, isCustom)
        {
            if (data is int or double or float or decimal or long or short or byte)
            {
                Data = data;
            }
            else
            {
                Data = 0;
            }
        }
    }

    public class SystemNumberListContainer : SystemContainer
    {
        protected override int CoreTypeNum { get; init; } = 2; 

        [SetsRequiredMembers]
        public SystemNumberListContainer(object data, string name, string systemType = "NumberList", bool isCustom = false)
            : base(data, name, systemType, isCustom)
        {
            bool isNumberList = data is List<int> or List<double> or List<float> or List<decimal> or List<long> or List<short> or List<byte>;

            if (isNumberList)
            {
                Data = data;
            }
            else if (data is Array array)
            {
                var list = new List<object>();
                foreach (var item in array) { list.Add(item); }
                Data = list;
            }
            else
            {
                Data = new List<object>(); 
            }
        }
    }

    public class SystemStringContainer : SystemContainer
    {
        protected override int CoreTypeNum { get; init; } = 3; 

        [SetsRequiredMembers]
        public SystemStringContainer(object data, string name, string systemType = "String", bool isCustom = false)
            : base(data, name, systemType, isCustom)
        {
            Data = data?.ToString() ?? string.Empty;
        }
    }

    public class SystemStringListContainer : SystemContainer
    {
        protected override int CoreTypeNum { get; init; } = 4; 

        [SetsRequiredMembers]
        public SystemStringListContainer(object data, string name, string systemType = "StringList", bool isCustom = false)
            : base(data, name, systemType, isCustom) 
        {
            if (data is List<string> sArray)
            {
                Data = sArray;
            }
            else if (data is string[])
            {
                Data = new List<string>(data as string[]);
            }
            else
            {
                Data = new List<string>();
            }
        }
    }

    public class SystemBoolContainer : SystemContainer
    {
        protected override int CoreTypeNum { get; init; } = 5; 

        [SetsRequiredMembers]
        public SystemBoolContainer(object data, string name, string systemType = "Boolean", bool isCustom = false)
            : base(data, name, systemType, isCustom)
        {
            Data = data is bool b ? b : false;
        }
    }

    public class SystemBoolListContainer : SystemContainer
    {
        protected override int CoreTypeNum { get; init; } = 6; 

        [SetsRequiredMembers]
        public SystemBoolListContainer(object data, string name, string systemType = "BooleanList", bool isCustom = false)
            : base(data, name, systemType, isCustom) 
        {
            if (data is bool[])
            {
                Data = new List<bool>(data as bool[]);
            }
            else if (data is List<bool>)
            {
                Data = data;
            }
            else
            {
                Data = new List<bool>();
            }
        }
    }

    public class SystemStructContainer : SystemContainer
    {
        protected override int CoreTypeNum { get; init; } = 7; 

        [SetsRequiredMembers]
        public SystemStructContainer(object data, string name, string systemType = "Structure", bool isCustom = false)
                    : base(null, name, systemType, isCustom) 
        {
            if (data is OrderedDictionary<string, SystemContainer> readyDict)
            {
                Data = readyDict;
            }
            else if (data is IEnumerable<SystemContainer> collection)
            {
                var dict = new OrderedDictionary<string, SystemContainer>();
                foreach (var container in collection)
                {
                    if (container != null)
                    {
                        dict[container.Name] = container;
                    }
                }
                Data = dict;
            }
            else
            {
                Data = new OrderedDictionary<string, SystemContainer>();
            }
        }

        public SystemContainer this[string key]
        {
            get
            {
                if (Data is OrderedDictionary<string, SystemContainer> dict && dict.TryGetValue(key, out var container))
                {
                    return container;
                }
                return null;
            }
            set
            {
                if (Data is OrderedDictionary<string, SystemContainer> dict && value != null)
                {
                    dict[key] = value;
                }
            }
        }

        public void Add(SystemContainer container)
        {
            if (Data is OrderedDictionary<string, SystemContainer> dict && container != null)
            {
                dict[container.Name] = container;
            }
        }
    }
}
