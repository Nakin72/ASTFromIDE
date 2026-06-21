using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace AstroEditor.Core.Forms;

/// <summary>
/// Реестр форм инструкций.
/// ✅ P1-1: Использует ConcurrentDictionary для потокобезопасности без блокировок.
/// </summary>
public class FormRegistry
{
    [JsonIgnore]
    private readonly ConcurrentDictionary<string, FormDefinition> _formsById = new();
    [JsonIgnore]
    private readonly ConcurrentDictionary<string, FormDefinition> _formsByName = new();

    /// <summary>
    /// Сериализуемый список форм. Для десериализации используйте SetAllForms.
    /// </summary>
    [JsonPropertyName("allForms")]
    public List<FormDefinition> AllFormsList
    {
        get
        {
            return _formsById.Values.ToList();
        }
        set
        {
            _formsById.Clear();
            _formsByName.Clear();
            if (value != null)
                foreach (var form in value)
                    RegisterForm(form);
        }
    }

    [JsonIgnore]
    public IReadOnlyCollection<FormDefinition> AllForms
    {
        get
        {
            return _formsById.Values.ToList().AsReadOnly();
        }
    }
    
    public void RegisterForm(FormDefinition form)
    {
        if (string.IsNullOrEmpty(form.Id)) form.Id = Guid.NewGuid().ToString();
        _formsById[form.Id] = form;
        _formsByName[form.Name] = form;
    }

    public FormDefinition? GetFormById(string id)
    {
        return _formsById.GetValueOrDefault(id);
    }
    
    public FormDefinition? GetFormByName(string name)
    {
        return _formsByName.GetValueOrDefault(name);
    }

    public bool RemoveForm(string id)
    {
        if (_formsById.TryGetValue(id, out var form))
        {
            _formsById.TryRemove(id, out _);
            _formsByName.TryRemove(form.Name, out _);
            return true;
        }
        return false;
    }
    
    public void Clear()
    {
        _formsById.Clear();
        _formsByName.Clear();
    }

    public List<FormDefinition> GetFormsByCategory(string category)
    {
        return _formsById.Values.Where(f => f.Category == category).ToList();
    }
    
    public List<FormDefinition> GetControlFlowForms()
    {
        return _formsById.Values.Where(f => f.IsControlFlow).ToList();
    }
}