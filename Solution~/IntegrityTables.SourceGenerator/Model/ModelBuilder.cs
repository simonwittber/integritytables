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



    public static DatabaseModel Build(SourceProductionContext context, INamedTypeSymbol databaseClass, ImmutableArray<INamedTypeSymbol> allTableStructs, ImmutableArray<INamedTypeSymbol> allServiceClasses)
    {
        var model = new DatabaseModel
        {
            DatabaseSymbol = databaseClass,
            Tables = [],
            OnTablesCreatedMethods = []
        };
        
        //see if databaseClass has [GenerateDatabase] attribute with GenerateForUnity = true
        var generateDatabaseAttribute = databaseClass.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.GenerateDatabaseAttribute");
        if (generateDatabaseAttribute != null)
        {
            var generateForUnityArgument = generateDatabaseAttribute.NamedArguments
                .FirstOrDefault(a => a.Key == "GenerateForUnity");
            if (generateForUnityArgument.Value is {Kind: TypedConstantKind.Primitive, Value: bool and true})
            {
                model.GenerateForUnity = true;
            }
        }

        var tableStructs = FilterForForDatabaseType(allTableStructs, databaseClass, $"{Namespace}.{TableAttributeName}").ToImmutableArray();
        foreach (var tableStruct in tableStructs)
        {
            var tableModel = BuildTableModel(context, model, tableStruct);
            model.Tables.Add(tableModel);
        }
        
        var serviceClasses = FilterForForDatabaseType(allServiceClasses, databaseClass, $"{Namespace}.{ServiceAttributeName}").ToImmutableArray();
        foreach (var serviceClass in serviceClasses)
        {
            var serviceModel = BuildServiceModel(context, model, serviceClass);
            if(serviceModel != null)
                model.ServiceModels.Add(serviceModel);
        }
        
        CollectOnTablesCreatedMethods(context, databaseClass, model);
        BuildTableFieldModels(context, model);
        BuildValidatorModels(context, model);
        BuildTriggers(context, model);
        BuildUniqueIndexes(model);
        BuildDependencyMap(model);
        BuildManyToMany(model);
        BuildGroups(model);
        ValidateTableModels(context, model);
        return model;
    }

    private static ServiceModel BuildServiceModel(SourceProductionContext context, DatabaseModel model, INamedTypeSymbol serviceClass)
    {
        var serviceModel = new ServiceModel()
        {
            DatabaseModel = model,
            ServiceSymbol = serviceClass
        };
        var attributes = serviceClass.GetAttributes();
        attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.{ServiceAttributeName}");
        
        return serviceModel;
    }

    private static void BuildGroups(DatabaseModel model)
    {
        model.Groups = model.Tables.ToLookup(tableModel => tableModel.GroupName??"Global");
    }

    private static void CollectOnTablesCreatedMethods(SourceProductionContext context, INamedTypeSymbol databaseClass, DatabaseModel model)
    {
        // collect OnTablesCreated decorated methods
        var onTablesCreatedMethods = databaseClass.GetMembers()
            .Where(m => m.Kind == SymbolKind.Method)
            .Select(m => (method: (IMethodSymbol) m, attribute: m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.OnTablesCreatedAttribute")))
            .Where(ma => ma.attribute != null);

        foreach (var (method, _) in onTablesCreatedMethods)
        {
            if(method.Parameters.Length > 0) 
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must not have parameters"
                ));
                continue;
            }
            if (method.ReturnType.Name != "Void")
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must return void"
                ));
                continue;
            }
            model.OnTablesCreatedMethods.Add(method.Name);
        }
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
                .FirstOrDefault(a =>
                {
                    return a.AttributeClass?.ToDisplayString() == attributeName;
                });
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