using AstroEditor.Core.v4.Common;

namespace AstroEditor.Core.v4.Types;

public class ArrayDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Array;
    public ArrayDataType() { }
}