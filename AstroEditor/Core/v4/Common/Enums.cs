namespace AstroEditor.Core.v4.Common;

public enum DataTypeCategory { Core, System, Vendor, User }
public enum DataTypeKind { Primitive, Struct, Array, Alias }
public enum BuiltInPrimitive
{
    SByte, Byte, Short, UShort, Int, UInt, Long, ULong,
    Float, Double, Decimal,
    Bool, Char, String
}
public enum FormAccessLevel { Core, System, Programmer, User }
public enum ArgumentDirection { In, Out, Ref }
public enum FieldValueType { Constant, Variable, Expression, Enum, FunctionCall, Label }