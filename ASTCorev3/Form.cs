



namespace AstroEditor.Core.v3.Form
{

    interface Field
    {
        public object value { get; }

    }

    class BaseField
    {
        public object Value { get; set; }
        public string toTUIString()
        {
            return Value.ToString();
        }
    }

    class DropdownField
    {
       // public IList List{get;set;}
    }

}