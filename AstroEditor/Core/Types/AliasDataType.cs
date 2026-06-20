using AstroEditor.Core.Common;

namespace AstroEditor.Core.Types;

public class AliasDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Alias;
    public AliasDataType() { }
}