using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public class FieldModel
{
    public IFieldSymbol FieldSymbol;

    public TableModel TableModel;

    public string Name => FieldSymbol.Name;

    public string TypeName => FieldSymbol.Type.ToDisplayString();

    public bool IsReference;

    public TableModel ReferencedTableModel;

    public string PropertyName;

    public string CollectionName;

    public bool IsUnique;

    public string UniqueIndexName;

    public bool IsNotNull;

    public bool IsHotField;
    
    public string CapitalizedName => $"{Name[0].ToString().ToUpper()}{Name.Substring(1)}";

    public bool IsImmutable;

    public string QualifiedTypeName
    {
        get
        {
            var namespaceName = NameSpace;
            var databaseNamespace = TableModel.NameSpace;

            if (namespaceName != databaseNamespace)
                return $"{namespaceName}{(string.IsNullOrEmpty(namespaceName) ? "" : ".")}{TypeName}";
            return TypeName;
        }
    }

    // Full namespace of this symbol
    public string NameSpace
    {
        get
        {
            var sb = new StringBuilder();
            var ns = new Stack<string>();
            var currentNamespace = FieldSymbol.ContainingNamespace;
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

    public bool IsBlittable => IsBlittableType(FieldSymbol.Type);
    public bool IsComputed;
    public bool IgnoreForEquality;
    public bool CreateIfMissing;
    public bool IsComponentReference;
    public FieldModel EntityReferenceField;
    public ValidatorModel ValidatorModel;
    public const string EntityReferenceFieldName = "entityId";

    private bool IsBlittableType(ITypeSymbol type, HashSet<ITypeSymbol> visited = null)
    {
        visited ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        // 1) enums are just their underlying integer
        if (type.TypeKind == TypeKind.Enum)
            return true;

        // 2) primitives and pointers
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
                return true;
        }

        if (type is IPointerTypeSymbol)
            return true;

        // 3) structs
        if (type is INamedTypeSymbol nts && nts.IsValueType)
        {
            // avoid infinite recursion on recursive structs
            if (!visited.Add(nts))
                return true;

            foreach (var field in nts.GetMembers().OfType<IFieldSymbol>())
            {
                if (field.IsStatic)
                    continue;

                // skip fixed buffers / special cases if you want...

                if (!IsBlittableType(field.Type, visited))
                    return false;
            }

            return true;
        }

        // everything else—classes, interfaces, delegates—aren’t blittable
        return false;
    }
}