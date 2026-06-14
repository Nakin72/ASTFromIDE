using System;
using System.Collections.Generic;
using System.Text.Json.Serialization; // Добавить сюда

namespace AstroEditor.Core.CoreTypes
{
    public interface I_DataContainer
    {
        // В интерфейсе оставляем только публичные члены
        public string CoreType { get; }
        public object Data { get; set; }
        public string Name { get; init; }
        public bool IsCustom { get; init; }
    }
    [JsonDerivedType(typeof(CoreNumberContainer), typeDiscriminator: "CoreNum")]
    [JsonDerivedType(typeof(CoreNumberListContainer), typeDiscriminator: "CoreNumList")]
    [JsonDerivedType(typeof(CoreStringContainer), typeDiscriminator: "CoreStr")]
    [JsonDerivedType(typeof(CoreStringListContainer), typeDiscriminator: "CoreStrList")]
    [JsonDerivedType(typeof(CoreBoolContainer), typeDiscriminator: "CoreBool")]
    [JsonDerivedType(typeof(CoreBoolListContainer), typeDiscriminator: "CoreBoolList")]
    [JsonDerivedType(typeof(CoreStructContainer), typeDiscriminator: "CoreStruct")]
    public abstract class BaseContainer : I_DataContainer
    {
        // Единый массив на всё приложение. Строки создаются в памяти ровно ОДИН раз.
        public static readonly string[] CoreTypes = {
            "Any",               // 0
            "CoreNumber",         // 1
            "CoreNumberList",     // 2
            "CoreString",         // 3
            "CoreStringArray",    // 4
            "CoreBoolean",        // 5
            "CoreBooleanArray",   // 6
            "CoreStructure"       // 7
        };

        // Делаем свойство виртуальным с возможностью инициализации (init)
        protected virtual int CoreTypeNum { get; init; } = 0;

        // Вычисляемое свойство: не занимает память в объекте, ссылается на статический массив
        public string CoreType => CoreTypes[CoreTypeNum];

        public required object Data { get; set; }
        public required string Name { get; init; }
        public required bool IsCustom { get; init; } = false;

        // Конструктор больше не принимает строку coretype. Наследники сами знают свой индекс!
        protected BaseContainer(object data, string name, bool isCustom = false)
        {
            Data = data;
            Name = name;
            IsCustom = isCustom;
        }
    }

    public class CoreNumberContainer : BaseContainer
    {
        // Переопределяем индекс для этого класса. Значение "1" зашито на уровне типа.
        protected override int CoreTypeNum { get; init; } = 1;

        public CoreNumberContainer(object data, string name, bool isCustom = false)
            : base(data, name, isCustom)
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

    public class CoreNumberListContainer : BaseContainer
    {
        protected override int CoreTypeNum { get; init; } = 2;

        public CoreNumberListContainer(object data, string name, bool isCustom = false)
            : base(data, name, isCustom)
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

    public class CoreStringContainer : BaseContainer
    {
        protected override int CoreTypeNum { get; init; } = 3;

        public CoreStringContainer(object data, string name, bool isCustom = false)
            : base(data, name, isCustom)
        {
            Data = data?.ToString() ?? string.Empty;
        }
    }

    public class CoreStringListContainer : BaseContainer
    {
        protected override int CoreTypeNum { get; init; } = 4;

        public CoreStringListContainer(object data, string name, bool isCustom = false)
            : base(data, name, isCustom)
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

    public class CoreBoolContainer : BaseContainer
    {
        protected override int CoreTypeNum { get; init; } = 5;

        public CoreBoolContainer(object data, string name, bool isCustom = false)
            : base(data, name, isCustom)
        {
            Data = data is bool b ? b : false;
        }
    }

    public class CoreBoolListContainer : BaseContainer
    {
        protected override int CoreTypeNum { get; init; } = 6;

        public CoreBoolListContainer(object data, string name, bool isCustom = false)
            : base(data, name, isCustom)
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

    public class CoreStructContainer : BaseContainer
    {
        protected override int CoreTypeNum { get; init; } = 7;

        public CoreStructContainer(object data, string name, bool isCustom = false)
                    : base(data, name, isCustom)
        {
            if (data is OrderedDictionary<string, BaseContainer> readyDict)
            {
                Data = readyDict;
            }
            else if (data is IEnumerable<BaseContainer> collection)
            {
                var dict = new OrderedDictionary<string, BaseContainer>();
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
                Data = new OrderedDictionary<string, BaseContainer>();
            }
        }

        public BaseContainer this[string key]
        {
            get
            {
                if (Data is OrderedDictionary<string, BaseContainer> dict && dict.TryGetValue(key, out var container))
                {
                    return container;
                }
                return null;
            }
            set
            {
                if (Data is OrderedDictionary<string, BaseContainer> dict && value != null)
                {
                    dict[key] = value;
                }
            }
        }

        public void Add(BaseContainer container)
        {
            if (Data is OrderedDictionary<string, BaseContainer> dict && container != null)
            {
                dict[container.Name] = container;
            }
        }
    }
}
