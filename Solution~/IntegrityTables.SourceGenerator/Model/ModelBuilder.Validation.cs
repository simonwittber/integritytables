using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public static partial class ModelBuilder
{
    private static void ValidateTableModels(SourceProductionContext context, DatabaseModel model)
    {
        foreach (var tableModel in model.Tables)
        {
            CheckEntityFieldExistsInComponentTable(context, tableModel);
            foreach (var fieldModel in tableModel.Fields)
            {
                CheckComponentReferenceFieldsExistInBothTables(context, model, fieldModel, tableModel);
            }
        }
    }

    private static void CheckEntityFieldExistsInComponentTable(SourceProductionContext context, TableModel tableModel)
    {
        // An IComponent table must have a field named entityId.
        if (!tableModel.IsComponent) return;
        var entityField = tableModel.Fields.Find(i => i.Name == FieldModel.EntityReferenceFieldName);
        if (entityField == null || entityField.TypeName != "int")
        {
            tableModel.IsComponent = false;
            context.ReportDiagnostic(Diagnostic.Create(
                BrokenConvention,
                tableModel.TableSymbol.Locations.First(),
                tableModel.TypeName, $"Table must have an int field named '{FieldModel.EntityReferenceFieldName}'"
            ));
        }
    }

    private static void CheckComponentReferenceFieldsExistInBothTables(SourceProductionContext context, DatabaseModel model, FieldModel fieldModel, TableModel tableModel)
    {
        if (!fieldModel.IsComponentReference) return;
        
        // hack: this configures the fieldModel at the same time as validating it.
        fieldModel.EntityReferenceField = tableModel.Fields.Find(i => i.Name == FieldModel.EntityReferenceFieldName);
        if (fieldModel.EntityReferenceField == null)
        {
            fieldModel.IsComponentReference = false;
            context.ReportDiagnostic(Diagnostic.Create(
                BrokenConvention,
                fieldModel.FieldSymbol.Locations.FirstOrDefault(),
                fieldModel.Name, $"Table must have an int field named '{FieldModel.EntityReferenceFieldName}'"
            ));
            return;
        }

        // The referenced table is associated via the EntityReferenceField. So we need to check if the referenced table also has
        // the EntityReferenceField.
        var referencedTable = model.TableMap[fieldModel.ReferencedTableModel.TableSymbol];
        if (!referencedTable.Fields.Any(f => f.Name == fieldModel.EntityReferenceField.Name))
        {
            fieldModel.IsComponentReference = false;
            context.ReportDiagnostic(Diagnostic.Create(
                BrokenConvention,
                fieldModel.FieldSymbol.Locations.FirstOrDefault(),
                fieldModel.Name, $"Target table must have an int field named '{fieldModel.EntityReferenceField.Name}'"
            ));
            return;
        }
    }
}