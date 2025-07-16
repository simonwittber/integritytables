using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class ServiceModel
{
    public INamedTypeSymbol ServiceSymbol;
    private string _typeName = null;

    public string TypeName
    {
        get
        {
            if (_typeName == null) _typeName = TableModel.FindTypeName(ServiceSymbol);
            return _typeName;
        }
    }
    
    public string QualifiedTypeName
    {
        get
        {
            if (!ServiceSymbol.ContainingNamespace.IsGlobalNamespace)
                return ServiceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return ServiceSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
    }

    // Full namespace of this symbol
    public string NameSpace
    {
        get
        {
            var sb = new StringBuilder();
            var ns = new Stack<string>();
            var currentNamespace = ServiceSymbol.ContainingNamespace;
            while (currentNamespace != null && !currentNamespace.IsGlobalNamespace)
            {
                ns.Push(currentNamespace.Name);
                currentNamespace = currentNamespace.ContainingNamespace;
            }

            while (ns.Count > 0)
            {
                sb.Append(ns.Pop());
                if (ns.Count > 0)
                    sb.Append(".");
            }

            return sb.ToString();
        }
    }

    public List<TriggerModel> Triggers = new();
    public DatabaseModel DatabaseModel;
}