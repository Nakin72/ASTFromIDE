using AstroEditor.Core.Common;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace AstroEditor.Core.Types;

public class PrimitiveDataType : DataType
{
    [JsonPropertyName("primitive")] public BuiltInPrimitive Primitive { get; set; }
    public override DataTypeKind Kind => DataTypeKind.Primitive;

    public PrimitiveDataType() => Category = DataTypeCategory.Core;

    /// <summary>Встроенные ограничения для данного примитива (CLI-лимиты).</summary>
    [JsonIgnore]
    public ValueConstraints? BuiltinConstraints => GetConstraintsFor(Primitive);

    /// <summary>CLR-тип, соответствующий примитиву.</summary>
    [JsonIgnore]
    public Type ClrType => GetClrType(Primitive);

    // Фабричные методы
    public static PrimitiveDataType SByte() => new() { Name = "SBYTE", Primitive = BuiltInPrimitive.SByte };
    public static PrimitiveDataType Byte() => new() { Name = "BYTE", Primitive = BuiltInPrimitive.Byte };
    public static PrimitiveDataType Short() => new() { Name = "SHORT", Primitive = BuiltInPrimitive.Short };
    public static PrimitiveDataType UShort() => new() { Name = "USHORT", Primitive = BuiltInPrimitive.UShort };
    public static PrimitiveDataType Int() => new() { Name = "INT", Primitive = BuiltInPrimitive.Int };
    public static PrimitiveDataType UInt() => new() { Name = "UINT", Primitive = BuiltInPrimitive.UInt };
    public static PrimitiveDataType Long() => new() { Name = "LONG", Primitive = BuiltInPrimitive.Long };
    public static PrimitiveDataType ULong() => new() { Name = "ULONG", Primitive = BuiltInPrimitive.ULong };
    public static PrimitiveDataType Float() => new() { Name = "FLOAT", Primitive = BuiltInPrimitive.Float };
    public static PrimitiveDataType Double() => new() { Name = "DOUBLE", Primitive = BuiltInPrimitive.Double };
    public static PrimitiveDataType Decimal() => new() { Name = "DECIMAL", Primitive = BuiltInPrimitive.Decimal };
    public static PrimitiveDataType Bool() => new() { Name = "BOOL", Primitive = BuiltInPrimitive.Bool };
    public static PrimitiveDataType Char() => new() { Name = "CHAR", Primitive = BuiltInPrimitive.Char };
    public static PrimitiveDataType String() => new() { Name = "STRING", Primitive = BuiltInPrimitive.String };

    /// <summary>Возвращает встроенные лимиты для примитива.</summary>
    public static ValueConstraints GetConstraintsFor(BuiltInPrimitive prim) => prim switch
    {
        BuiltInPrimitive.SByte   => new() { Min = sbyte.MinValue, Max = sbyte.MaxValue },
        BuiltInPrimitive.Byte    => new() { Min = byte.MinValue,  Max = byte.MaxValue  },
        BuiltInPrimitive.Short   => new() { Min = short.MinValue, Max = short.MaxValue },
        BuiltInPrimitive.UShort  => new() { Min = ushort.MinValue,Max = ushort.MaxValue},
        BuiltInPrimitive.Int     => new() { Min = int.MinValue,   Max = int.MaxValue   },
        BuiltInPrimitive.UInt    => new() { Min = uint.MinValue,  Max = uint.MaxValue  },
        BuiltInPrimitive.Long    => new() { Min = long.MinValue,  Max = long.MaxValue  },
        BuiltInPrimitive.ULong   => new() { Min = ulong.MinValue, Max = ulong.MaxValue },
        BuiltInPrimitive.Float   => new() { Min = float.MinValue, Max = float.MaxValue },
        BuiltInPrimitive.Double  => new() { Min = double.MinValue,Max = double.MaxValue},
        BuiltInPrimitive.Decimal => new() { Min = (double)decimal.MinValue, Max = (double)decimal.MaxValue },
        BuiltInPrimitive.Bool    => new() { Min = 0, Max = 1, AllowedValues = new List<object> { 0, 1 } },
        BuiltInPrimitive.Char    => new() { Min = char.MinValue,  Max = char.MaxValue  },
        BuiltInPrimitive.String  => new() { MinLength = 0, MaxLength = int.MaxValue },
        _ => new()
    };

    /// <summary>CLR-тип для примитива.</summary>
    public static Type GetClrType(BuiltInPrimitive prim) => prim switch
    {
        BuiltInPrimitive.SByte   => typeof(sbyte),
        BuiltInPrimitive.Byte    => typeof(byte),
        BuiltInPrimitive.Short   => typeof(short),
        BuiltInPrimitive.UShort  => typeof(ushort),
        BuiltInPrimitive.Int     => typeof(int),
        BuiltInPrimitive.UInt    => typeof(uint),
        BuiltInPrimitive.Long    => typeof(long),
        BuiltInPrimitive.ULong   => typeof(ulong),
        BuiltInPrimitive.Float   => typeof(float),
        BuiltInPrimitive.Double  => typeof(double),
        BuiltInPrimitive.Decimal => typeof(decimal),
        BuiltInPrimitive.Bool    => typeof(bool),
        BuiltInPrimitive.Char    => typeof(char),
        BuiltInPrimitive.String  => typeof(string),
        _ => typeof(object)
    };
}