using AstroEditor.Core.Common;

namespace AstroEditor.Core.Types;

public class ArrayDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Array;
    public ArrayDataType() { }
}