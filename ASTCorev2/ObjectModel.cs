// //using AstroEditor.Core.CoreTypes;
// using AstroEditor.Core.SystemTypes;


// namespace AstroEditor.Core.ObjectModel
// {

//     abstract class AstNode
//     {
//         public abstract string ToTUIstring();

//     }
//     public class Slot
//     {
//         public required string TypeAlias { get; init; }
//         abstract public SystemContainer? Variable
//         {
//             get; set
//             {
//                 if (value is null)
//                 {
//                     value = new SystemStringContainer("...", "placeholder");
//                 }
//             }
//         }
//     }
//     public class Expression //Inserts in field
//     {

//     }
//     public class Field //where placed variables, expressions and constants
//     {
//         public Slot Slot { get; set; }
//         public required string requiredType { get; init; }
//         public string toTUIstring()
//         {
//             return Slot.Variable.Name.ToString();
//         }
//     }
    
// public enum VariableScopeType
// {
//     Literal,    // Константа, вбитая руками прямо в строку
//     Local,      // Локальная переменная текущей программы
//     Global      // Глобальная переменная
// }

// public class FormFieldSlot
// {
//     // Имя слота в форме (например, "Speed", "TargetPos", "Condition")
//     public string SlotName { get; init; }

//     // Ограничение типа (например, "Number" или "MyCustomSpeed"). 
//     // "Any" — если поле принимает что угодно.
//     public string ExpectedType { get; init; } = "Any";

//     // Откуда поле берет данные прямо сейчас
//     public VariableScopeType SourceType { get; set; } = VariableScopeType.Literal;

//     // Ключ для поиска. 
//     // Если Literal -> здесь хранится само значение в виде строки/числа.
//     // Если Local или Global -> здесь хранится ИМЯ переменной из соответствующей таблицы.
//     public object RawValue { get; set; }

//     // Кэшированная ссылка на контейнер для "прямого эфира" (живое отображение)
//     // Заполняется перед выполнением программы или при выборе переменной в UI
//     public SystemContainer? LinkedContainer { get; set; }
// }

// }