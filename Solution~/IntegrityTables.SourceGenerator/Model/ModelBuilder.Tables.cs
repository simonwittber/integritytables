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

        return tableModel;
    }

    private static void BuildManyToMany(DatabaseModel model)
    {
        foreach (var tableModel in model.Tables)
        {
            if (tableModel.Fields.Count != 2) continue;
            var areReferenceFields = tableModel.Fields[0].IsReference && tableModel.Fields[1].IsReference;
            var areUniqueFields = tableModel.Fields[0].IsUnique && tableModel.Fields[1].IsUnique;
            var areSameIndex = tableModel.Fields[0].UniqueIndexName == tableModel.Fields[1].UniqueIndexName;
            var isManyToMany = areReferenceFields && areUniqueFields && areSameIndex;
            var isSymmetricJunction = tableModel.Fields[0].ReferencedTableModel == tableModel.Fields[1].ReferencedTableModel &&
                                      tableModel.Fields[0].CollectionName == tableModel.Fields[1].CollectionName;

            if (isManyToMany)
            {
                tableModel.IsManyToMany = true;
                model.ManyToManyModels.Add(new ManyToManyModel()
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
        foreach (var tableModel in model.Tables)
        {
            CollectFields(context, model, tableModel);
            ValidateFields(context, model, tableModel);
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

            foreach (var specialName in (string[]) ["id", "_version", "_index", "data"])
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

            // Check if field is a Reference<T> type
            if (field.Type is INamedTypeSymbol namedType)
            {
                // Check for Reference<T>
                if (namedType.IsGenericType && namedType.ConstructedFrom.ToDisplayString() == "IntegrityTables.Reference<T>")
                {
                    var referencedType = namedType.TypeArguments[0] as INamedTypeSymbol;
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
                        
                        fieldModel.IsReference = true;

                        // does field have [RequiredReference] attribute?
                        foreach (var attribute in field.GetAttributes())
                        {
                            if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.RequiredReferenceAttribute")
                            {
                                fieldModel.IsNotNull = true; // RequiredReference means it cannot be null
                            }

                            if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.PropertyNameAttribute")
                            {
                                // get PropertyName from constructor argument
                                if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string propertyName)
                                {
                                    fieldModel.PropertyName = propertyName;
                                }
                            }

                            if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.CollectionNameAttribute")
                            {
                                // get CollectionName from constructor argument
                                if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string propertyName)
                                {
                                    fieldModel.CollectionName = propertyName;
                                }
                            }
                        }


                        if (tableModel.IsComponent && fieldModel.ReferencedTableModel.IsComponent)
                        {
                            fieldModel.IsComponentReference = true;
                        }
                    }
                }
            }

            foreach (var attribute in field.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.HotFieldAttribute")
                {
                    fieldModel.IsHotField = true;
                }

                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.ComputedAttribute")
                {
                    fieldModel.IsComputed = true;
                }

                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.ImmutableAttribute")
                {
                    fieldModel.IsImmutable = true;
                }

                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.IgnoreForEqualityAttribute")
                {
                    fieldModel.IgnoreForEquality = true;
                }

                // AfterUpdate and BeforeUpdate attributes
                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.AfterUpdateAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string methodName)
                    {
                        var member = tableModel.TableSymbol.GetMembers().FirstOrDefault(i => i.Name == methodName);
                        if (member is IMethodSymbol methodSymbol)
                        {
                            var invalid  = AssertMethodIsPublic(context, methodSymbol);
                            invalid |= AssertMethodIsStatic(context, methodSymbol);
                            invalid |= AssertMethodHasParameterCount(context, methodSymbol, 3);
                            if(invalid) continue;
                            invalid |= AssertParameterType(context, model, methodSymbol, 0, model.QualifiedTypeName);
                            invalid |= AssertParameterType(context, model, methodSymbol, 1, tableModel.TypeName+"Row");
                            invalid |= AssertParameterType(context, model, methodSymbol, 2, fieldModel.TypeName);
                            invalid |= AssertParameterName(context, model, methodSymbol, 2, "oldValue");
                            if(!invalid)
                                fieldModel.AfterUpdateMethod = methodSymbol;
                        }
                    }
                }
                
                if (attribute.AttributeClass?.ToDisplayString() == $"{Namespace}.BeforeUpdateAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string methodName)
                    {
                        var member = tableModel.TableSymbol.GetMembers().FirstOrDefault(i => i.Name == methodName);
                        if (member is IMethodSymbol methodSymbol)
                        {
                            var invalid  = AssertMethodIsPublic(context, methodSymbol);
                            invalid |= AssertMethodIsStatic(context, methodSymbol);
                            invalid |= AssertMethodHasParameterCount(context, methodSymbol, 3);
                            if(invalid) continue;
                            invalid |= AssertParameterType(context, model, methodSymbol, 0, model.QualifiedTypeName);
                            invalid |= AssertParameterType(context, model, methodSymbol, 1, tableModel.TypeName+"Row");
                            invalid |= AssertParameterType(context, model, methodSymbol, 2, fieldModel.TypeName);
                            invalid |= AssertParameterName(context, model, methodSymbol, 2, "newValue");
                            if(!invalid)
                                fieldModel.BeforeUpdateMethod = methodSymbol;
                        }
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
            fieldModel.Index = tableModel.Fields.Count; 
            
            tableModel.Fields.Add(fieldModel);
        }
    }
}