using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class TriggerModel
{
    public IMethodSymbol Method;
    public TableModel TableModel;
    public string MethodName { get; set; }
}