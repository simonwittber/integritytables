using System.Text;
using IntegrityTables.SourceGeneration.Model;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration;

internal class DbmlBuilder
{
    public static void Build(SourceProductionContext context, DatabaseModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/*");
        foreach (var table in model.Tables)
        {
            sb.AppendLine($"Table {table.TypeName} {{");
            sb.AppendLine($"    id int [primary key]");
            foreach (var field in table.Fields)
            {
                
                sb.AppendLine($"    {field.Name} {field.TypeName} {(field.IsNotNull?"[not null]":"")}");
            }
            sb.AppendLine("}");
        }
        
        foreach (var table in model.Tables)
        {
            foreach (var field in table.Fields)
            {
                if (!field.IsReference) continue;
                sb.AppendLine($"Ref: {table.TypeName}.{field.Name} > {field.ReferencedTableModel.TypeName}.id");
            }
        }

        foreach (var g in model.Groups)
        {
            sb.AppendLine($"TableGroup \"{g.Key}\" {{");
            foreach (var table in g)
            {
                sb.AppendLine($"    {table.TypeName}");
            }
            sb.AppendLine("}");
        }
        
        

        sb.AppendLine("*/");
        var code = sb.ToString();
        
        context.AddSource($"{model.FileName("DBML","")}.g.dbml", code);
    }
}