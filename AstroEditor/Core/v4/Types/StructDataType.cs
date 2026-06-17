using AstroEditor.Core.v4.Common;

namespace AstroEditor.Core.v4.Types;

public class StructDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Struct;
    public StructDataType() => Fields = new List<StructField>();
}