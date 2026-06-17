using AstroEditor.Core.v4.Common;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace AstroEditor.Core.v4.Types;

public class PrimitiveDataType : DataType
{
    [JsonPropertyName("primitive")] public BuiltInPrimitive Primitive { get; set; }
    public override DataTypeKind Kind => DataTypeKind.Primitive;

    public PrimitiveDataType() => Category = DataTypeCategory.Core;

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
}