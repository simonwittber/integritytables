using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class ValidatorModel
{
    public TableModel TableModel;
    public IMethodSymbol MethodSymbol;
    public FieldModel FieldModel;
}