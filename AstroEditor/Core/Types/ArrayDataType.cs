using AstroEditor.Core.Common;

namespace AstroEditor.Core.Types;

public class ArrayDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Array;
    
    /// <summary>
    /// Тип элементов массива
    /// </summary>
    public string ElementTypeId { get; set; } = "any";
    
    public ArrayDataType() { }
}