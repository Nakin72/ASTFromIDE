using System.Collections.ObjectModel;

namespace AstroEditor.Core.v4.Forms;

public class FormRegistry
{
    private readonly Dictionary<string, FormDefinition> _formsById = new();
    private readonly Dictionary<string, FormDefinition> _formsByName = new();

    public IReadOnlyCollection<FormDefinition> AllForms => _formsById.Values.ToList().AsReadOnly();

    public void RegisterForm(FormDefinition form)
    {
        if (string.IsNullOrEmpty(form.Id)) form.Id = Guid.NewGuid().ToString();
        _formsById[form.Id] = form;
        _formsByName[form.Name] = form;
    }

    public FormDefinition? GetFormById(string id) => _formsById.GetValueOrDefault(id);
    public FormDefinition? GetFormByName(string name) => _formsByName.GetValueOrDefault(name);

    public bool RemoveForm(string id)
    {
        if (_formsById.TryGetValue(id, out var form))
        {
            _formsById.Remove(id);
            _formsByName.Remove(form.Name);
            return true;
        }
        return false;
    }
    public void Clear() { _formsById.Clear(); _formsByName.Clear(); }

    public List<FormDefinition> GetFormsByCategory(string category) =>
        _formsById.Values.Where(f => f.Category == category).ToList();
    public List<FormDefinition> GetControlFlowForms() =>
        _formsById.Values.Where(f => f.IsControlFlow).ToList();
}