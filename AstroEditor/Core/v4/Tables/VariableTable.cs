using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Variables;
using AstroEditor.Core.v4.Types;
namespace AstroEditor.Core.v4.Tables
{
    /// <summary>
    /// Таблица переменных (глобальная или локальная)
    /// </summary>
    public class VariableTable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("variables")]
        public List<Variable> Variables { get; set; } = new();

        [JsonPropertyName("isGlobal")]
        public bool IsGlobal { get; set; } = true; // по умолчанию глобальная

        public void AddVariable(Variable variable) => Variables.Add(variable);

        public Variable? FindVariable(string name) => Variables.FirstOrDefault(v => v.Name == name);

        public bool RemoveVariable(string name)
        {
            var found = FindVariable(name);
            if (found != null)
            {
                Variables.Remove(found);
                return true;
            }
            return false;
        }

        public void Clear() => Variables.Clear();

        /// <summary>
        /// После десериализации восстановить ссылки на типы по TypeId
        /// </summary>
        public void ResolveReferences(DataTypeRegistry registry)
        {
            foreach (var variable in Variables)
            {
                variable.Type = registry.GetTypeById(variable.TypeId);
            }
        }
    }

}