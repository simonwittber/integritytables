using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public static partial class ModelBuilder
{
    private const string Namespace = "IntegrityTables";
    private const string TableAttributeName = "GenerateTableAttribute";
    private const string ServiceAttributeName = "GenerateServiceAttribute";
    private const string SystemAttributeName = "GenerateSystemAttribute";

    public static DatabaseModel Build(SourceProductionContext context, Compilation compilation, INamedTypeSymbol databaseClass, ImmutableArray<INamedTypeSymbol> allTableStructs, ImmutableArray<INamedTypeSymbol> allSystemClasses)
    {
        var model = new DatabaseModel
        {
            DatabaseSymbol = databaseClass,
            Tables = [],
        };

        //see if databaseClass has [GenerateDatabase] attribute with GenerateForUnity = true
        var generateDatabaseAttribute = databaseClass.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.GenerateDatabaseAttribute");

        var tableStructs = FilterForForDatabaseType(allTableStructs, databaseClass, $"{Namespace}.{TableAttributeName}").ToImmutableArray();
        foreach (var tableStruct in tableStructs)
        {
            var tableModel = BuildTableModel(context, model, tableStruct);
            model.Tables.Add(tableModel);
        }

        var systemClasses = FilterForForDatabaseType(allSystemClasses, databaseClass, $"{Namespace}.{SystemAttributeName}").ToImmutableArray();
        foreach (var systemClass in systemClasses)
        {
            var systemModel = BuildSystemModel(context, compilation, model, systemClass);
            if (systemModel != null)
                model.SystemModels.Add(systemModel);
        }

        BuildTableFieldModels(context, model);
        BuildTriggers(context, model);
        BuildUniqueIndexes(model);
        BuildDependencyMap(model);
        BuildManyToMany(model);
        BuildGroups(model);
        ValidateTableModels(context, model);
        return model;
    }

    private static SystemModel BuildSystemModel(SourceProductionContext context, Compilation compilation, DatabaseModel model, INamedTypeSymbol systemClass)
    {
        var systemModel = new SystemModel()
        {
            SystemSymbol = systemClass
        };
        
        // Find the Execute method and analyze its parameters
        var executeMethod = systemClass.GetMembers("Execute")
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (executeMethod != null)
        {
            // get all parameters of the Execute method
            // make sure they are tableModel.RowTypeName
            foreach (var parameter in executeMethod.Parameters)
            {
                var isValid = false;
                if (parameter.Type is INamedTypeSymbol nts)
                {
                    var tableModel = model.TableMap.Values.FirstOrDefault(t => t.RowTypeName == nts.Name);
                    if (tableModel != null)
                    {
                        systemModel.Parameters.Add((parameter.Name, tableModel));
                        systemModel.IsRaw = false;
                    }
                    else
                    {
                        ReportConventionError(context, parameter, $"System.Execute method parameter must be a table model type, not {parameter.Type.Name}");
                    }
                }
            }
        }

        return systemModel;
    }

    private static void BuildGroups(DatabaseModel model)
    {
        model.Groups = model.Tables.ToLookup(tableModel => tableModel.GroupName ?? "Global");
    }

    private static void BuildDependencyMap(DatabaseModel model)
    {
        foreach (var table in model.Tables)
        {
            table.Dependencies = new List<FieldModel>();
        }

        foreach (var table in model.Tables)
        {
            foreach (var field in table.Fields)
            {
                if (!field.IsReference) continue;
                if (model.TableMap.TryGetValue(field.ReferencedTableModel.TableSymbol, out var referencedTableModel))
                    referencedTableModel.Dependencies.Add(field);
            }
        }
    }

    private static void BuildUniqueIndexes(DatabaseModel model)
    {
        foreach (var table in model.Tables)
        {
            table.UniqueIndexes = new Dictionary<string, List<FieldModel>>();

            foreach (var fieldModel in table.Fields.Where(fieldModel => fieldModel.IsUnique))
            {
                if (!table.UniqueIndexes.TryGetValue(fieldModel.UniqueIndexName, out var names))
                    table.UniqueIndexes[fieldModel.UniqueIndexName] = names = [];
                names.Add(fieldModel);
            }
        }
    }


    private static IEnumerable<INamedTypeSymbol> FilterForForDatabaseType(ImmutableArray<INamedTypeSymbol> declarations, ISymbol databaseClass, string attributeName)
    {
        foreach (var declaration in declarations)
        {
            var attribute = declaration.GetAttributes()
                .FirstOrDefault(a => { return a.AttributeClass?.ToDisplayString() == attributeName; });
            if (attribute != null)
            {
                // get name from constructor argument
                var nameArgument = attribute.ConstructorArguments.FirstOrDefault();
                if (nameArgument is {Kind: TypedConstantKind.Type, Value: ITypeSymbol symbol})
                {
                    if (symbol.Equals(databaseClass, SymbolEqualityComparer.Default))
                    {
                        yield return declaration;
                    }
                }
            }
        }
    }
}