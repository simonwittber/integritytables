using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class TriggerModel
{
    public IMethodSymbol Method;
    public string AttributeName;
    public RefKind[] RefKinds;
    public string EventName;
    public string FieldName;
    public bool IsFieldTrigger;
    public TableModel TableModel;
}