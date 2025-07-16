using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public static partial class ModelBuilder
{
    private static TableModel BuildTableModel(SourceProductionContext context, DatabaseModel model, INamedTypeSymbol tableStruct)
    {
        var tableModel = new TableModel
        {
            DatabaseModel = model,
            TableSymbol = tableStruct
        };
        model.TableMap.Add(tableModel.TableSymbol, tableModel);
        
        //if model is an IComponent it is a component.
        if (tableStruct.AllInterfaces.Any(i => i.ToDisplayString() == "IntegrityTables.IComponent"))
        {
            tableModel.IsComponent = true;
        }
        
        var tableAttributes = tableStruct.GetAttributes();
        var tableAttribute = tableAttributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.{TableAttributeName}");
        // make sure table also as [Serializable] attribute
        var serializableAttribute = tableAttributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.SerializableAttribute");
        if (serializableAttribute == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                BrokenConvention,
                tableStruct.Locations.FirstOrDefault(),
                tableStruct.Name, "Must also be marked with [Serializable] attribute"
            ));
        }
        
        // get GroupName from constructor argument
        var groupNameArgument = tableAttribute?.NamedArguments.FirstOrDefault(a => a.Key == "GroupName");
        if (groupNameArgument?.Value is {Kind: TypedConstantKind.Primitive, Value: string groupName})
        {
            tableModel.GroupName = groupName;
        }
        
        var blittableArgument = tableAttribute?.NamedArguments.FirstOrDefault(a => a.Key == "Blittable");
        if (blittableArgument?.Value is {Kind: TypedConstantKind.Primitive, Value: bool blittable})
        {
            tableModel.RequiresIsBlittable = blittable;
        }
        
        var viewModelArgument = tableAttribute?.NamedArguments.FirstOrDefault(a => a.Key == "GenerateViewModel");
        if (viewModelArgument?.Value is {Kind: TypedConstantKind.Primitive, Value: bool viewModel})
        {
            tableModel.GenerateViewModel = viewModel;
        }
        
        var enumArgument = tableAttribute?.NamedArguments.FirstOrDefault(a => a.Key == "GenerateEnum");
        if (enumArgument?.Value is {Kind: TypedConstantKind.Type, Value: INamedTypeSymbol generateEnum})
        {
            tableModel.GenerateEnum = generateEnum;
        }
        
        var capacityArgument = tableAttribute?.NamedArguments.FirstOrDefault(a => a.Key == "Capacity");
        if (capacityArgument?.Value is {Kind: TypedConstantKind.Primitive, Value: int capacity})
        {
            tableModel.Capacity = capacity;
        }
        tableModel.Fields = new List<FieldModel>();

        CollectDefaultDataMethods(context, tableStruct, tableModel);
        if (tableModel.GenerateEnum != null) 
            CollectConfigureEnumMethods(context, tableStruct, tableModel);
        CollectConstraintMethods(context, tableStruct, tableModel);
        CollectQueryMethods(context, tableStruct, tableModel);

        return tableModel;
    }

    private static void CollectQueryMethods(SourceProductionContext context, INamedTypeSymbol tableStruct, TableModel tableModel)
    {
        var queryMethods = tableStruct.GetMembers()
            .Where(m => m.Kind == SymbolKind.Method)
            .Select(m => (method: (IMethodSymbol) m, attribute: m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.QueryPropertyAttribute")))
            .Where(ma => ma.attribute != null)
            .ToList();

        foreach (var (method, attribute) in queryMethods)
        {
            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must be static"
                ));
                continue;
            }

            if (method.Parameters.Length != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must have one parameter"
                ));
                continue;
            }
            
            tableModel.QueryMethods.Add((method, attribute));
        }


    }
    
    private static void CollectConfigureEnumMethods(SourceProductionContext context, INamedTypeSymbol tableStruct, TableModel tableModel)
    {
        var configureMethods = tableStruct.GetMembers()
            .Where(m => m.Kind == SymbolKind.Method)
            .Select(m => (method: (IMethodSymbol) m, attribute: m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.ConfigureEnumAttribute")))
            .Where(ma => ma.attribute != null)
            .ToList();
        
        foreach (var (method, _) in configureMethods)
        {
            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must be static"
                ));
                continue;
            }

            if (method.Parameters.Length != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must have one parameter"
                ));
                continue;
            }

            if (method.Parameters[0].Type is INamedTypeSymbol parameterType)
            {
                if (parameterType.Name != "Row" || parameterType.TypeArguments.Length != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, $"must take Row<T> as parameter"
                    ));
                    continue;
                }
                
                // make sure parameter is refKind In
                if (method.Parameters[0].RefKind != RefKind.Ref)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, $"must take Row<T> as 'ref' parameter"
                    ));
                    continue;
                }
                
                if (!SymbolEqualityComparer.Default.Equals(parameterType.TypeArguments[0], tableStruct))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, $"must take Row<{tableStruct.Name}> as parameter"
                    ));
                    continue;
                }
                
            }

            if (!method.ReturnsVoid)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, $"must return void"
                ));
                continue;
            }


            tableModel.ConfigureEnumMethods.Add(method.Name);
        }
    }
    
    private static void CollectDefaultDataMethods(SourceProductionContext context, INamedTypeSymbol tableStruct, TableModel tableModel)
    {
        var defaultDataMethods = tableStruct.GetMembers()
            .Where(m => m.Kind == SymbolKind.Method)
            .Select(m => (method: (IMethodSymbol) m, attribute: m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.DefaultDataAttribute")))
            .Where(ma => ma.attribute != null)
            .ToList();
        foreach (var (method, _) in defaultDataMethods)
        {
            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must be static"
                ));
                continue;
            }

            if (method.Parameters.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must not have parameters"
                ));
                continue;
            }

            // make sure method return array of tableStruct
            var returnTypeOk = false;
            if (method.ReturnType is IArrayTypeSymbol arrayTypeSymbol)
            {
                if (SymbolEqualityComparer.Default.Equals(arrayTypeSymbol.ElementType, tableStruct))
                {
                    returnTypeOk = true;
                }
            }

            if (!returnTypeOk)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, $"must return array of {tableStruct.Name}"
                ));
                continue;
            }


            tableModel.DefaultDataMethods.Add(method.Name);
        }
    }
    
    private static void CollectConstraintMethods(SourceProductionContext context, INamedTypeSymbol tableStruct, TableModel tableModel)
    {
        var methods = tableStruct.GetMembers()
            .Where(m => m.Kind == SymbolKind.Method)
            .Select(m => (method: (IMethodSymbol) m, attribute: m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.CheckConstraintAttribute")))
            .Where(ma => ma.attribute != null)
            .ToList();
        
        foreach (var (method, _) in methods)
        {
            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must be static"
                ));
                continue;
            }
            
            // must be public
            if (!method.DeclaredAccessibility.HasFlag(Accessibility.Public))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must be public"
                ));
                continue;
            }

            if (method.Parameters.Length != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, "must have one parameter 'in Row<T>'"
                ));
                continue;
            }

            // make sure method return bool
            var returnTypeOk = false;
            if (method.ReturnType is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.Name == "Boolean")
                {
                    returnTypeOk = true;
                }
            }
            
            if (!returnTypeOk)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                    method.Name, $"must return bool"
                ));
                continue;
            }
            
            // make sure method parameter is in Row<T>
            if (method.Parameters[0].Type is INamedTypeSymbol parameterType)
            {
                if (parameterType.Name != "Row" || parameterType.TypeArguments.Length != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, $"must take Row<T> as parameter"
                    ));
                    continue;
                }
                
                // make sure parameter is refKind In
                if (method.Parameters[0].RefKind != RefKind.In)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, $"must take Row<T> as 'in' parameter"
                    ));
                    continue;
                }
                
                if (!SymbolEqualityComparer.Default.Equals(parameterType.TypeArguments[0], tableStruct))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, $"must take Row<{tableStruct.Name}> as parameter"
                    ));
                    continue;
                }
                
            }


            tableModel.ConstraintMethods.Add(method.Name);
        }
    }

    private static void BuildManyToMany(DatabaseModel model)
    {
        foreach (var tableModel in model.Tables)
        {
            if(tableModel.Fields.Count != 2) continue;
            var areReferenceFields = tableModel.Fields[0].IsReference && tableModel.Fields[1].IsReference;
            var areUniqueFields = tableModel.Fields[0].IsUnique && tableModel.Fields[1].IsUnique;
            var areSameIndex = tableModel.Fields[0].UniqueIndexName == tableModel.Fields[1].UniqueIndexName;
            var isManyToMany = areReferenceFields && areUniqueFields && areSameIndex;
            var isSymmetricJunction = tableModel.Fields[0].ReferencedTableModel == tableModel.Fields[1].ReferencedTableModel && 
                                tableModel.Fields[0].CollectionName == tableModel.Fields[1].CollectionName;
            
            if (isManyToMany)
            {
                tableModel.IsManyToMany = true;
                model.ManyToManyModels.Add(new  ManyToManyModel()
                {
                    TableModel = tableModel, 
                    IsSymmetricJunction = isSymmetricJunction,
                    Fields = tableModel.Fields.ToArray(),
                });
            }
        }
    }

    private static void BuildTableFieldModels(SourceProductionContext context, DatabaseModel model)
    {
        foreach(var tableModel in model.Tables)
        {
            CollectFields(context, model, tableModel);
            ValidateFields(context, model, tableModel);
        }
    }
    
    private static void BuildValidatorModels(SourceProductionContext context, DatabaseModel model)
    {
        foreach(var tableModel in model.Tables)
        {
            // get all instance methods that are marked with [ValidateAttribute]
            var validateMethods = tableModel.TableSymbol.GetMembers()
                .Where(m => m.Kind == SymbolKind.Method)
                .Select(m => (method: (IMethodSymbol) m, attribute: m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.ValidateAttribute")))
                .Where(ma => ma.attribute != null)
                .ToList();
            foreach (var (method, attribute) in validateMethods)
            {
                var fieldName = attribute.ConstructorArguments[0].Value as string;
                var fieldModel = tableModel.Fields.FirstOrDefault(f => f.Name == fieldName);
                if (fieldModel == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, $"Field '{fieldName}' specified by attribute not found in table '{tableModel.TableSymbol.Name}'"
                    ));
                    continue;
                }
                if(!method.ReturnsVoid)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, "must return void"
                    ));
                    continue;
                }
                if(!method.DeclaredAccessibility.HasFlag(Accessibility.Public))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, "must be public"
                    ));
                    continue;
                }
                if(method.Parameters.Length > 0) 
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, "must not have parameters"
                    ));
                    continue;
                }
                if (method.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BrokenConvention,
                        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                        method.Name, "must not be static"
                    ));
                    continue;
                }
                var validatorModel = new ValidatorModel
                {
                    TableModel = tableModel,
                    MethodSymbol = method,
                    FieldModel = fieldModel
                };
                tableModel.ValidatorModels.Add(validatorModel);
                fieldModel.ValidatorModel = validatorModel;
            }
        }
    }

    private static void ValidateFields(SourceProductionContext context, DatabaseModel model, TableModel tableModel)
    {
        
        if (tableModel.GenerateEnum != null)
        {
            var hasNameField = tableModel.Fields.Any(i => i.Name == "name" && i.IsUnique && i.TypeName == "string");
            if (!hasNameField)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    BrokenConvention,
                    tableModel.TableSymbol.Locations.FirstOrDefault(),
                    tableModel.TableSymbol.Name, $"Table with [GenerateEnum] must have a unique string field named 'name'"
                ));
            }
        }
    }

    private static void CollectFields(SourceProductionContext context, DatabaseModel model, TableModel tableModel)
    {
        foreach (var field in tableModel.TableSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (!field.DeclaredAccessibility.HasFlag(Accessibility.Public))
                continue;

            foreach(var specialName in (string[]) ["id", "_version", "_index", "data"])
            {
                if (field.Name == specialName)
                {
                    ReportConventionError(context, field, $"Field named '{specialName}' is reserved for IntegrityTables and cannot be used in user-defined tables.");
                }
            }
                
            var fieldModel = new FieldModel
            {
                TableModel = tableModel,
                FieldSymbol = field
            };
                
            foreach (var attribute in field.GetAttributes())
            {
                if(attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.HotFieldAttribute")
                {
                    fieldModel.IsHotField = true;
                }
                if(attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.ComputedAttribute")
                {
                    fieldModel.IsComputed = true;
                }
                if(attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.ImmutableAttribute")
                {
                    fieldModel.IsImmutable = true;
                }
                if(attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.IgnoreForEqualityAttribute")
                {
                    fieldModel.IgnoreForEquality = true;
                }
                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.ReferenceAttribute")
                {
                    // make sure field is int
                    if (field.Type.Name != "Int32")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            BrokenConvention,
                            field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                            field.Name, "Field with [Reference] attribute must be of type int"
                        ));
                        continue;
                    }
                    var referencedType = (INamedTypeSymbol) attribute.ConstructorArguments[0].Value;
                    if (referencedType != null)
                    {
                        if (!model.TableMap.TryGetValue(referencedType, out fieldModel.ReferencedTableModel))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                BrokenConvention,
                                field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
                                field.Name, $"Cannot find {referencedType.Name}, is it marked with [GenerateTable]?"
                            ));
                            continue;
                        }

                        foreach (var namedArg in attribute.NamedArguments)
                        {
                            if (namedArg.Key == "PropertyName")
                                fieldModel.PropertyName = namedArg.Value.Value as string;
                            if (namedArg.Key == "CollectionName")
                                fieldModel.CollectionName = namedArg.Value.Value as string;
                            if (namedArg.Key == "NotNull")
                                if(namedArg.Value.Value is bool isNotNull)
                                    fieldModel.IsNotNull = isNotNull;
                            if(namedArg.Key == "CreateIfMissing")
                                if(namedArg.Value.Value is bool createIfMissing)
                                    fieldModel.CreateIfMissing = createIfMissing;
                            if(tableModel.IsComponent && fieldModel.ReferencedTableModel.IsComponent)
                            {
                                fieldModel.IsComponentReference = true;
                            }
                                    
                        }
                        fieldModel.IsReference = true;
                    }
                }

                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.UniqueAttribute")
                {
                    fieldModel.IsUnique = true;
                    var uniqueIndexName = field.Name;
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        var argument = attribute.ConstructorArguments[0];
                        if (argument is {Kind: TypedConstantKind.Primitive, Value: string name})
                            uniqueIndexName = name;
                    }

                    fieldModel.UniqueIndexName = uniqueIndexName;
                }
            }

            if (tableModel.RequiresIsBlittable)
            {
                if (!fieldModel.IsBlittable)
                {
                    ReportConventionError(context, fieldModel.FieldSymbol, "Must be a blittable type when table is marked with [Blittable] attribute");
                }
            }

            tableModel.Fields.Add(fieldModel);
        }
    }
    
    
}