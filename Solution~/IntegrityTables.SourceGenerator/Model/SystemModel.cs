using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class SystemModel
{
    public INamedTypeSymbol SystemSymbol;
    private string _typeName = null;

    public string TypeName
    {
        get
        {
            if (_typeName == null) _typeName = TableModel.FindTypeName(SystemSymbol);
            return _typeName;
        }
    }
    
    public string QualifiedTypeName
    {
        get
        {
            if (!SystemSymbol.ContainingNamespace.IsGlobalNamespace)
                return SystemSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return SystemSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
    }

    public string NameSpace
    {
        get
        {
            if (SystemSymbol.ContainingNamespace.IsGlobalNamespace)
                return "";
            return SystemSymbol.ContainingNamespace.ToDisplayString();
        }
    }

    public List<(INamedTypeSymbol Type, bool isList)> ReadDependencies = new();
    public List<INamedTypeSymbol> WriteDependencies = new();
}
