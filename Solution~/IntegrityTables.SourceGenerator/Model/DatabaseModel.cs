using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class DatabaseModel
{
    public List<TableModel> Tables;

    public INamedTypeSymbol DatabaseSymbol;
    
    public readonly Dictionary<INamedTypeSymbol, TableModel> TableMap = new(SymbolEqualityComparer.Default);
    
    public readonly List<ManyToManyModel> ManyToManyModels = new();

    public string FileName(string category, string part)
    {
        var namespaceName = DatabaseSymbol.ContainingNamespace.IsGlobalNamespace ? "Global" : DatabaseSymbol.ContainingNamespace.Name;
        var parts = namespaceName.Split('.');
        var ns = string.Join(".", parts.Reverse());
        return $"{part}.{category}.{DatabaseSymbol.Name}.{ns}";
    }

    public bool GenerateForUnity = false;
    
    public List<string> OnTablesCreatedMethods = new();

    public ILookup<string, TableModel> Groups;

    public readonly List<ServiceModel> ServiceModels = new();

    public readonly List<SystemModel> SystemModels = new();

    public string TypeName
    {
        get
        {
            if (DatabaseSymbol.ContainingType != null)
            {
                var containingTypeChain = new List<string>();
                var currentType = DatabaseSymbol.ContainingType;

                while (currentType != null)
                {
                    containingTypeChain.Add(currentType.Name);
                    currentType = currentType.ContainingType;
                }

                containingTypeChain.Reverse();
                var containingTypePath = string.Join(".", containingTypeChain);
                return $"{containingTypePath}.{DatabaseSymbol.Name}";
            }

            return DatabaseSymbol.Name;
        }
    }

    public string QualifiedTypeName => $"{NameSpace}{(string.IsNullOrEmpty(NameSpace)?string.Empty:".")}{TypeName}";

    public string NameSpace
    {
        get
        {
            var sb = new StringBuilder();
            var ns = new Stack<string>();
            var currentNamespace = DatabaseSymbol.ContainingNamespace;
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
}