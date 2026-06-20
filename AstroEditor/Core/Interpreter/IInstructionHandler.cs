// AstroEditor.Core/Interpreter/IInstructionHandler.cs
// Интерфейс для обработчиков инструкций (опционально, для внешних расширений)

using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Interpreter;

public interface IInstructionHandler
{
    /// <summary>
    /// Возвращает словарь обработчиков: FormId -> Action<Instruction>
    /// </summary>
    Dictionary<string, Action<Instruction>> GetHandlers();
}
