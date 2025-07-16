using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public static partial class ModelBuilder
{
    private static void BuildTriggers(SourceProductionContext context, DatabaseModel model)
    {
        foreach (var tableModel in model.Tables)
            tableModel.Triggers = FindTriggers(context, model, model.DatabaseSymbol, tableModel.TableSymbol, requiredRowType: tableModel.TableSymbol, requireStatic: true, requirePublic:true);
        foreach (var serviceModel in model.ServiceModels)
            serviceModel.Triggers = FindTriggers(context, model, null, serviceModel.ServiceSymbol, requiredRowType: null, requireStatic: false, requirePublic: false);
    }

    private static List<TriggerModel> FindTriggers(SourceProductionContext context, DatabaseModel model, INamedTypeSymbol requiredDatabaseSymbol, INamedTypeSymbol symbol, INamedTypeSymbol requiredRowType, bool requireStatic, bool requirePublic)
    {
        var triggerAttributes = new ValueTuple<string, RefKind[], string>[]
        {
            ("BeforeAddAttribute", [RefKind.Ref], "BeforeAdd"),
            ("AfterAddAttribute", [RefKind.In], "AfterAdd"),
            ("BeforeRemoveAttribute", [RefKind.In], "BeforeRemove"),
            ("AfterRemoveAttribute", [RefKind.In], "AfterRemove"),
            ("BeforeUpdateAttribute", [RefKind.In, RefKind.Ref], "BeforeUpdate"),
            ("AfterUpdateAttribute", [RefKind.In, RefKind.In], "AfterUpdate"),
            ("BeforeFieldUpdateAttribute", [RefKind.In, RefKind.Ref], "BeforeUpdate"),
            ("AfterFieldUpdateAttribute", [RefKind.In, RefKind.In], "AfterUpdate"),
        };
        var triggers = new List<TriggerModel>();
        {
            foreach (var (attributeName, refKinds, eventName) in triggerAttributes)
            {
                var methods = GetTriggerMethods(context, requiredDatabaseSymbol, symbol, attributeName, refKinds, requiredRowType, requireStatic, requirePublic);
                foreach (var ma in methods)
                {
                    var method = ma.method;
                    // is it a field trigger?
                    var isFieldTrigger = ma.attribute.AttributeClass?.Name is "BeforeFieldUpdateAttribute" or "AfterFieldUpdateAttribute";
                    var argument = ma.attribute.ConstructorArguments.FirstOrDefault();
                    var firstParameterIndex = requiredDatabaseSymbol == null ? 0 : 1;
                    // we can assume safely param 1 exists here.
                    var parameterSymbol = method.Parameters[firstParameterIndex].Type as INamedTypeSymbol;
                    var tableTypeSymbol = parameterSymbol?.TypeArguments[0] as INamedTypeSymbol;
                    if (!model.TableMap.TryGetValue(tableTypeSymbol!, out var tableModel))
                    {
                        ReportConventionError(context, method, $"First parameter is {tableTypeSymbol}, must be one of ({string.Join(", ", model.TableMap.Keys)})");
                        continue;
                    }

                    if (isFieldTrigger && argument is {Kind: TypedConstantKind.Primitive, Value: string name})
                    {
                        // add to trigger model
                        triggers.Add(new TriggerModel() {TableModel = tableModel, IsFieldTrigger = true, Method = method, AttributeName = attributeName, RefKinds = refKinds, FieldName = name, EventName = eventName});
                    }
                    else
                        triggers.Add(new TriggerModel() {TableModel = tableModel, IsFieldTrigger = false, Method = method, AttributeName = attributeName, RefKinds = refKinds, FieldName = null, EventName = eventName});
                }
            }
        }
        return triggers;
    }

    private static List<(IMethodSymbol method, AttributeData attribute)> GetTriggerMethods(SourceProductionContext context, INamedTypeSymbol databaseSymbol, INamedTypeSymbol namedTypeSymbol, string attributeName, RefKind[] refKinds, INamedTypeSymbol requiredRowType, bool requireStatic, bool requirePublic)
    {
        var methods = namedTypeSymbol.GetMembers()
            .Where(m => m.Kind == SymbolKind.Method)
            .Select(m => (method: (IMethodSymbol) m, attribute: m.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == $"{Namespace}.{attributeName}")))
            .ToList();

        bool HasCorrectSignature(IMethodSymbol method)
        {
            if (requireStatic && !method.IsStatic)
            {
                ReportConventionError(context, method, "must be static");
                return false;
            }
            
            if (requirePublic && method.DeclaredAccessibility != Accessibility.Public)
            {
                ReportConventionError(context, method, "must be public");
                return false;
            }

            var parameters = method.Parameters;
            var databaseParameterCount = databaseSymbol == null ? 0 : 1;
            var paramCount = databaseParameterCount + refKinds.Length;
            if (parameters.Length != paramCount)
            {
                ReportConventionError(context, method, $"must have {paramCount} parameters");
                return false;
            }

            if (databaseSymbol != null)
            {
                var databaseParameter = parameters[0];
                if (!(databaseParameter.Type is INamedTypeSymbol parameter0Type && parameter0Type.Name == databaseSymbol.Name))
                {
                    ReportConventionError(context, databaseParameter, $"First parameter must be of type {databaseSymbol.Name}");
                    return false;
                }
            }

            foreach (var (refSymbol, kind) in parameters.Skip(databaseParameterCount).Zip(refKinds, (a, b) => (a, b)))
            {
                if (refSymbol.RefKind != kind)
                {
                    ReportConventionError(context, refSymbol, "must be " + kind.ToString().ToLower());
                    return false;
                }

                if (refSymbol.Type is not INamedTypeSymbol paramNamedTypeSymbol || paramNamedTypeSymbol.Name != "Row" || paramNamedTypeSymbol.TypeArguments.Length != 1)
                {
                    ReportConventionError(context, refSymbol, "must be of type Row<T>");
                    return false;
                }

                if (requiredRowType != null)
                {
                    if (!paramNamedTypeSymbol.TypeArguments[0].Equals(requiredRowType, SymbolEqualityComparer.Default))
                    {
                        var location = refSymbol.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax().GetLocation();
                        ReportConventionError(context, location, paramNamedTypeSymbol, $"type argument must be of type {requiredRowType.Name}");
                        return false;
                    }
                }
            }

            return true;
        }

        foreach (var ma in methods.ToArray())
        {
            if (ma.attribute == null)
            {
                methods.Remove(ma);
                continue;
            }

            if (!HasCorrectSignature(ma.method))
            {
                methods.Remove(ma);
            }
        }

        return methods;
    }
}