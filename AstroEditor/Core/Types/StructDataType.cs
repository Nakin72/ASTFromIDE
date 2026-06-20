using AstroEditor.Core.Common;

namespace AstroEditor.Core.Types;

public class StructDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Struct;
    public StructDataType() => Fields = new List<StructField>();
}