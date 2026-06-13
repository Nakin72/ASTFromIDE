//using AstroEditor.Core.CoreTypes;
using AstroEditor.Core.SystemTypes;


namespace AstroEditor.Core.ObjectModel
{

    abstract class AstNode
    {
        public abstract string ToTUIstring();

    }
    public class Slot
    {
        public required string TypeAlias { get; init; }
        public SystemContainer? Variable
        {
            get; set
            {
                if (value is null)
                {
                    value = new SystemStringContainer("...", "placeholder");
                }
            }
        }
    }
    public class Field //where placed variables, expressions and constants
    {
        public Slot Slot { get; set; }
        public required string requiredType { get; init; }
        public string toTUIstring()
        {
            return Slot.Variable.Name.ToString();
        }
    }
    public class Expression //Inserts in field
    {

    }

    public class Form //Built from fields and expressions
    {
        public List<Field>? FormContainer { get; set; } = null;

        int progress;
        bool isExecuted = false;

        void ExecuteForm()
        {

        }

    }
}