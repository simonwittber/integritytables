using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public static partial class ModelBuilder
{
    private static void BuildTriggers(SourceProductionContext context, DatabaseModel model)
    {
        foreach (var tableModel in model.Tables)
            tableModel.Triggers = FindTriggers(context, model, tableModel);
    }

    private static List<TriggerModel> FindTriggers(SourceProductionContext context, DatabaseModel model, TableModel tableModel)
    {
        var methodNames = (List<string>) ["BeforeAdd", "AfterAdd", "BeforeRemove", "AfterRemove"];
        var triggers = new List<TriggerModel>();

        // find a public static method with the name and the correct parameters, model.TypeName, and ref tableModel.TypeName + "Row"
        foreach (var member in tableModel.TableSymbol.GetMembers())
        {
            if (!(member is IMethodSymbol methodSymbol)) continue;
            if (methodSymbol.Name == "BeforeAdd")
            {
                if (AssertMethodIsPublic(context, methodSymbol)) continue;
                if (AssertMethodIsStatic(context, methodSymbol)) continue;
                var parameterCount = tableModel.Fields.Count + 1;
                if (AssertMethodHasParameterCount(context, methodSymbol, parameterCount)) continue;
                if (AssertParameterType(context, model, methodSymbol, 0, model.QualifiedTypeName)) continue;
                foreach(var field in tableModel.Fields)
                {
                    var requiredType = field.TypeName;
                    if(field.IsReference)
                        requiredType = "int";
                    if (AssertParameterType(context, model, methodSymbol, field.Index + 1, requiredType)) continue;
                    if (AssertParameterName(context, model, methodSymbol, field.Index + 1, field.Name)) continue;
                }
                AddTriggerModel(tableModel, methodSymbol, triggers);
            }

            if (methodSymbol.Name == "AfterAdd")
            {
                if (AssertMethodIsPublic(context, methodSymbol)) continue;
                if (AssertMethodIsStatic(context, methodSymbol)) continue;
                if (AssertMethodHasParameterCount(context, methodSymbol, 2)) continue;
                if (AssertParameterType(context, model, methodSymbol, 0, model.QualifiedTypeName)) continue;
                if (AssertParameterType(context, model, methodSymbol, 1, tableModel.TypeName + "Row")) continue;
                AddTriggerModel(tableModel, methodSymbol, triggers);
            }

            if (methodSymbol.Name == "BeforeRemove")
            {
                if (AssertMethodIsPublic(context, methodSymbol)) continue;
                if (AssertMethodIsStatic(context, methodSymbol)) continue;
                if (AssertMethodHasParameterCount(context, methodSymbol, 2)) continue;
                if (AssertParameterType(context, model, methodSymbol, 0, model.QualifiedTypeName)) continue;
                if (AssertParameterType(context, model, methodSymbol, 1, tableModel.TypeName + "Row")) continue;
                AddTriggerModel(tableModel, methodSymbol, triggers);
            }
            
            if (methodSymbol.Name == "AfterRemove")
            {
                if (AssertMethodIsPublic(context, methodSymbol)) continue;
                if (AssertMethodIsStatic(context, methodSymbol)) continue;
                if (AssertMethodHasParameterCount(context, methodSymbol, 2)) continue;
                if (AssertParameterType(context, model, methodSymbol, 0, model.QualifiedTypeName)) continue;
                if (AssertParameterType(context, model, methodSymbol, 1, "int")) continue;
                AddTriggerModel(tableModel, methodSymbol, triggers);
            }
        }

        return triggers;
    }

    private static void AddTriggerModel(TableModel tableModel, IMethodSymbol methodSymbol, List<TriggerModel> triggers)
    {
        var triggerModel = new TriggerModel
        {
            TableModel = tableModel,
            Method = methodSymbol,
            MethodName = methodSymbol.Name,
        };
        triggers.Add(triggerModel);
    }

    private static bool AssertParameterType(SourceProductionContext context, DatabaseModel model, IMethodSymbol methodSymbol, int index, string typeName)
    {
        if (methodSymbol.Parameters[index].Type.ToDisplayString() != typeName)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("IT0003",
                    $"Trigger Convention Error",
                    $"Trigger method '{methodSymbol.Name}' Parameter #{index} must be '{typeName}', not {methodSymbol.Parameters[index].Type.ToDisplayString()}", "IntegrityTables", DiagnosticSeverity.Error, true),
                methodSymbol.Locations.FirstOrDefault()));
            return true;
        }

        return false;
    }
    
    private static bool AssertParameterName(SourceProductionContext context, DatabaseModel model, IMethodSymbol methodSymbol, int index, string parameterName)
    {
        if (methodSymbol.Parameters[index].Name != parameterName)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("IT0003",
                    $"Trigger Convention Error",
                    $"Trigger method '{methodSymbol.Name}' Parameter #{index} must be '{parameterName}', not {methodSymbol.Parameters[index].Name}", "IntegrityTables", DiagnosticSeverity.Error, true),
                methodSymbol.Locations.FirstOrDefault()));
            return true;
        }

        return false;
    }

    private static bool AssertMethodHasParameterCount(SourceProductionContext context, IMethodSymbol methodSymbol, int count)
    {
        if (methodSymbol.Parameters.Length != count)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("IT0002", "Trigger Convention Error", $"Trigger method '{methodSymbol.Name}' must have {count} parameters", "IntegrityTables", DiagnosticSeverity.Error, true),
                methodSymbol.Locations.FirstOrDefault()));
            return true;
        }

        return false;
    }

    private static bool AssertMethodIsStatic(SourceProductionContext context, IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("IT0001", "Trigger method must be static", "Trigger method '{0}' must be static", "IntegrityTables", DiagnosticSeverity.Error, true),
                methodSymbol.Locations.FirstOrDefault(), methodSymbol.Name));
            return true;
        }

        return false;
    }
    
    private static bool AssertMethodIsPublic(SourceProductionContext context, IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("IT0001", "Trigger method must be public", "Trigger method '{0}' must be static", "IntegrityTables", DiagnosticSeverity.Error, true),
                methodSymbol.Locations.FirstOrDefault(), methodSymbol.Name));
            return true;
        }

        return false;
    }
}