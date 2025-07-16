using System.Collections.Generic;
using System.Text;
using IntegrityTables.SourceGeneration.Model;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration;

internal class MermaidBuilder
{
    public static void Build(SourceProductionContext context, DatabaseModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/*");
        sb.AppendLine("classDiagram");
        
        var tableSet = new HashSet<TableModel>();

        foreach (var g in model.Groups)
        {
            sb.AppendLine($"namespace \"{g.Key}\" {{");
            foreach (var table in g)
            {
                sb.AppendLine($"    class {table.TypeName}");
                tableSet.Add(table);
            }
            sb.AppendLine("}");
        }
        
        foreach (var table in model.Tables)
        {
            if(tableSet.Contains(table)) continue;
            sb.AppendLine($"class {table.TypeName}");
        }
        
        foreach (var table in model.Tables)
        {
            foreach (var field in table.Fields)
            {
                if (!field.IsReference) continue;
                sb.AppendLine($"{table.TypeName} \"1\" o-- \"0..*\" {field.ReferencedTableModel.TypeName}");
            }
        }
        
        

        sb.AppendLine("*/");
        var code = sb.ToString();
        
        context.AddSource($"{model.FileName("Mermaid","")}.g.dbml", code);
    }
}