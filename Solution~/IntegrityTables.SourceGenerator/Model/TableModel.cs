using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class TableModel
{
    public INamedTypeSymbol TableSymbol;

    public string GroupName;

    private string _typeName = null;

    public string TypeName
    {
        get
        {
            if (_typeName == null) _typeName = FindTypeName(TableSymbol);
            return _typeName;
        }
    }

    public string FieldName => $"_{FacadeName}";
    
    public string FacadeName => $"{TypeName}Table";
    
    public List<FieldModel> Fields;

    public DatabaseModel DatabaseModel;

    public List<TriggerModel> Triggers;

    public Dictionary<string, List<FieldModel>> UniqueIndexes;

    public List<FieldModel> Dependencies;

    public bool IsManyToMany = false;

    public readonly List<string> DefaultDataMethods = new();
    
    public readonly List<string> ConfigureEnumMethods = new();
    
    public readonly List<string> ConstraintMethods = new();
    
    public readonly List<(IMethodSymbol method, AttributeData attribute)> QueryMethods = new();
    
    public bool IsBlittable => Fields.All(i => i.IsBlittable);

    public bool RequiresIsBlittable;

    public bool GenerateViewModel;
    
    public INamedTypeSymbol GenerateEnum;

    public bool IsComponent;
    
    public int Capacity = 1024;
    public List<ValidatorModel> ValidatorModels = new();


    internal static string FindTypeName(INamedTypeSymbol tableSymbol)
    {
        if (tableSymbol.ContainingType != null)
        {
            var containingTypeChain = new List<string>();
            var currentType = tableSymbol.ContainingType;

            while (currentType != null)
            {
                containingTypeChain.Add(currentType.Name);
                currentType = currentType.ContainingType;
            }

            containingTypeChain.Reverse();
            var containingTypePath = string.Join(".", containingTypeChain);
            return $"{containingTypePath}.{tableSymbol.Name}";
        }

        return tableSymbol.Name;
    }
    
    public string FullyQualifiedTypeName
    {
        get
        {
            return $"{FullyQualifiedNameSpace}{TypeName}";
        }
    }

    public string QualifiedTypeName
    {
        get
        {
            return $"{QualifiedNameSpace}{TypeName}";
        }
    }

    // Full namespace of this symbol
    public string NameSpace
    {
        get
        {
            var sb = new StringBuilder();
            var ns = new Stack<string>();
            var currentNamespace = TableSymbol.ContainingNamespace;
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

    public string QualifiedNameSpace
    {
        get
        {
            if (TableSymbol.ContainingNamespace.IsGlobalNamespace)
                return "";
            var tableNameSpace = TableSymbol.ContainingNamespace.Name;
            var modelNameSpace = DatabaseModel.DatabaseSymbol.ContainingNamespace.Name;
            if (tableNameSpace == modelNameSpace)
            {
                return "";
            }

            if (tableNameSpace.StartsWith(modelNameSpace))
            {
                return $"{tableNameSpace.Substring(modelNameSpace.Length + 1)}.";
            }
            return $"{tableNameSpace}.";
            
        }
    }

    private string FullyQualifiedNameSpace
    {
        get
        {
            if (TableSymbol.ContainingNamespace.IsGlobalNamespace)
                return "";
            return $"{TableSymbol.ContainingNamespace.Name}.";
        }
    }

    public FieldModel this[string name]
    {
        get => Fields.First(i => i.Name == name);
    }
}