using AstroEditor.Core.v4.Common;

namespace AstroEditor.Core.v4.Types;

public class AliasDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Alias;
    public AliasDataType() { }
}