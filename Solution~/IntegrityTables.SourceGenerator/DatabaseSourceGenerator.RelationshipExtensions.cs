using System.Linq;
using System.Text;
using IntegrityTables.SourceGeneration.Model;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration;

public partial class DatabaseSourceGenerator
{
    private void BuildManyToManyExtensions(DatabaseModel model, ManyToManyModel manyToMany, StringBuilder sb)
    {
        if (manyToMany.IsSymmetricJunction)
        {
            BuildSymmetricJunctionExtensions(model, manyToMany, sb);
            return;
        }

        var tableModel = manyToMany.TableModel;
        var leftField = tableModel.Fields[0];
        var rightField = tableModel.Fields[1];
        var rightTable = rightField.ReferencedTableModel;
        var leftTable = leftField.ReferencedTableModel;
        var leftFieldCollectionName = leftField.CollectionName;
        var rightFieldCollectionName = rightField.CollectionName;
        var leftTableQualifiedTypeName = leftTable.QualifiedTypeName;
        var rightTableQualifiedTypeName = rightTable.QualifiedTypeName;
        var leftFieldSymbolName = leftField.Name;
        var rightFieldSymbolName = rightField.Name;

        if (leftFieldCollectionName != null)
        {
            sb.AppendLine($@"
        //  {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>
        /// This allows adding to a many to many relationship between {leftTableQualifiedTypeName} and {rightTableQualifiedTypeName}
        /// </summary>
        public static Row<{tableModel.QualifiedTypeName}> AddTo{leftFieldCollectionName}(this in Row<{leftTableQualifiedTypeName}> row, Row<{rightTableQualifiedTypeName}> rightRow)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            return db.{tableModel.FacadeName}.Add(new {tableModel.QualifiedTypeName}() {{ {leftFieldSymbolName} = row.id, {rightFieldSymbolName} = rightRow.id }});
        }}
        
        // {DatabaseSourceGenerator.GenerationStamp()}
        public static void RemoveFrom{leftFieldCollectionName}(this in Row<{leftTableQualifiedTypeName}> row, Row<{rightTableQualifiedTypeName}> rightRow) 
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            var rightRowId = rightRow.id;
            var rowId = row.id;
            db.{tableModel.FacadeName}.Remove((in Row<{tableModel.TypeName}> row) => {{
                return row.data.{leftFieldSymbolName} == rowId && row.data.{rightFieldSymbolName} == rightRowId;
            }});
        }}");
        }

        if (rightFieldCollectionName != null)
            sb.AppendLine($@"
        //  {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>
        /// This allows adding to a many to many relationship between {rightTableQualifiedTypeName} and {leftTableQualifiedTypeName}
        /// </summary>
        public static Row<{tableModel.QualifiedTypeName}> AddTo{rightFieldCollectionName}(this in Row<{rightTableQualifiedTypeName}> row, Row<{leftTableQualifiedTypeName}> rightRow)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            return db.{tableModel.FacadeName}.Add(new {tableModel.QualifiedTypeName}() {{ {rightFieldSymbolName} = row.id, {leftFieldSymbolName} = rightRow.id }});
        }}

        // {DatabaseSourceGenerator.GenerationStamp()}
        public static void RemoveFrom{rightFieldCollectionName}(this in Row<{rightTableQualifiedTypeName}> row, Row<{leftTableQualifiedTypeName}> rightRow) 
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            var rightRowId = rightRow.id;
            var rowId = row.id;
            db.{tableModel.FacadeName}.Remove((in Row<{tableModel.TypeName}> row) => {{
                return row.data.{rightFieldSymbolName} == rowId && row.data.{leftFieldSymbolName} == rightRowId;
            }});
        }}");
        if (leftFieldCollectionName != null)
        {
            sb.AppendLine($@"        // {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>       
        /// This allows querying the many to many relationship between {leftTableQualifiedTypeName} and {rightTableQualifiedTypeName}
        /// </summary>
        public static ManyToManyEnumerator<{rightTableQualifiedTypeName}, {tableModel.QualifiedTypeName}> {leftFieldCollectionName}(this in Row<{leftTableQualifiedTypeName}> row)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            var rowId = row.id;
            return db.{rightTable.FacadeName}.ManyToManyJoin(
                db.{tableModel.FacadeName}, 
                (in Row<{rightTableQualifiedTypeName}> D, in Row<{tableModel.QualifiedTypeName}> ED) => D.id == ED.data.{rightField.Name} && ED.data.{leftField.Name} == rowId);
        }}");
        }

        if (rightFieldCollectionName != null)
        {
            sb.AppendLine($@"        // {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>       
        /// This allows querying the many to many relationship between {rightTableQualifiedTypeName} and {leftTableQualifiedTypeName}
        /// </summary>
        public static ManyToManyEnumerator<{leftTableQualifiedTypeName}, {tableModel.QualifiedTypeName}> {rightFieldCollectionName}(this in Row<{rightTableQualifiedTypeName}> row)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            var rowId = row.id;
            return db.{leftTable.FacadeName}.ManyToManyJoin(db.{tableModel.FacadeName}, (in Row<{leftTableQualifiedTypeName}> D, in Row<{tableModel.QualifiedTypeName}> ED) => D.id == ED.data.{leftField.Name} && ED.data.{rightField.Name} == rowId);
        }}");
        }
    }

    private void BuildSymmetricJunctionExtensions(DatabaseModel model, ManyToManyModel manyToMany, StringBuilder sb)
    {
        var tableModel = manyToMany.TableModel;
        var leftField = tableModel.Fields[0];
        var rightField = tableModel.Fields[1];
        var leftTable = leftField.ReferencedTableModel;
        var leftFieldCollectionName = leftField.CollectionName;
        var leftTableQualifiedTypeName = leftTable.QualifiedTypeName;
        var leftFieldSymbolName = leftField.Name;
        var rightFieldSymbolName = rightField.Name;

        if (leftFieldCollectionName != null)
            sb.AppendLine($@"
        //  {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>
        /// This allows adding to a self referencing many to many relationship on {leftTableQualifiedTypeName}
        /// </summary>
        public static Row<{tableModel.QualifiedTypeName}> AddTo{leftFieldCollectionName}(this in Row<{leftTableQualifiedTypeName}> row, in Row<{leftTableQualifiedTypeName}> rightRow)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            return db.{tableModel.FacadeName}.Add(new {tableModel.QualifiedTypeName}() {{ {leftFieldSymbolName} = row.id, {rightFieldSymbolName} = rightRow.id }});
        }}

        // {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>
        /// This allows removing from a self referencing many to many relationship on {leftTableQualifiedTypeName}
        /// </summary>
        public static void RemoveFrom{leftFieldCollectionName}(this in Row<{leftTableQualifiedTypeName}> row, in Row<{leftTableQualifiedTypeName}> rightRow)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            var rowId = row.id;
            var rightRowId = rightRow.id;
            db.{tableModel.FacadeName}.Remove((in Row<{tableModel.TypeName}> row) => {{
                return row.data.{leftFieldSymbolName} == rowId && row.data.{rightFieldSymbolName} == rightRowId
                || row.data.{rightFieldSymbolName} == rowId && row.data.{leftFieldSymbolName} == rightRowId;
            }});
        }}

        // {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>       
        /// This allows querying the self referencing many to many relationship on {leftTableQualifiedTypeName}
        /// </summary>
        public static ManyToManyEnumerator<{leftTableQualifiedTypeName}, {tableModel.QualifiedTypeName}> {leftFieldCollectionName}(this in Row<{leftTableQualifiedTypeName}> row)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            var rowId = row.id;
            return db.{leftTable.FacadeName}.ManyToManyJoin(db.{tableModel.FacadeName}, (in Row<{leftTableQualifiedTypeName}> D, in Row<{tableModel.QualifiedTypeName}> ED) => 
                D.id == ED.data.{rightField.Name} && ED.data.{leftField.Name} == rowId
                || D.id == ED.data.{leftField.Name} && ED.data.{rightField.Name} == rowId
            );
        }}");
    }

    private static void BuildQueryPropertyExtensionMethods(DatabaseModel model, TableModel table, StringBuilder sb)
    {
        foreach (var (method, attribute) in table.QueryMethods)
        {
            var propertyName = attribute.ConstructorArguments[0].Value as string;
            var returnType = method.ReturnType as INamedTypeSymbol;
            if (returnType != null && returnType.IsGenericType)
            {
                var genericType = returnType.TypeArguments[0] as INamedTypeSymbol;
                if (genericType != null)
                {
                    // get the generic type argument of the return type, which should be a QueryEnumerator<T>
                    var facadeName = model.TableMap[genericType].FacadeName;
                    sb.AppendLine($@"
        //  {DatabaseSourceGenerator.GenerationStamp()}
        public static QueryEnumerator<{genericType.ToDisplayString()}> {propertyName}(this in Row<{table.QualifiedTypeName}> row)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            return db.{facadeName}.Query({table.NameSpace}.{table.QualifiedTypeName}.{method.Name}(row.id));
        }}");
                }
            }
        }
    }

    private static void BuildEntityExtensionMethods(DatabaseModel model, TableModel table, StringBuilder sb)
    {
        if (!table.IsComponent) return;
    }

    private static void BuildOneToManyExtensions(DatabaseModel model, TableModel table, StringBuilder sb)
    {
        foreach (var field in table.Fields)
        {
            if (!field.IsReference) continue;
            if (string.IsNullOrEmpty(field.PropertyName)) continue;
            var referencedTableModel = field.ReferencedTableModel;
            // if field is NotNull, we don't need to use a nullable row.
            if (field.IsNotNull)
            {
                sb.AppendLine($@"
        //  {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>
        /// This is a many to one relationship between {table.QualifiedTypeName} and {referencedTableModel.QualifiedTypeName}
        /// Is cannot be null, so we can use a non-nullable row.
        /// </summary>
        public static Row<{referencedTableModel.QualifiedTypeName}> {field.PropertyName}(this in Row<{table.QualifiedTypeName}> row)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            return db.{referencedTableModel.FacadeName}.Get(row.data.{field.Name});
        }}");
            }
            else
            {
                // Else we need to use a nullable row.
                sb.AppendLine($@"
        //  {DatabaseSourceGenerator.GenerationStamp()}
        /// <summary>
        /// This is a many to one relationship between {table.QualifiedTypeName} and {referencedTableModel.QualifiedTypeName}
        /// Is might be null, so we can use a nullable row.
        /// </summary>
        public static Row<{referencedTableModel.QualifiedTypeName}>? {field.PropertyName}(this in Row<{table.QualifiedTypeName}> row)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            return db.{referencedTableModel.FacadeName}.TryGet(row.data.{field.Name}, out Row<{referencedTableModel.QualifiedTypeName}> d) ? d : (Row<{referencedTableModel.QualifiedTypeName}>?)null;
        }}

        // {DatabaseSourceGenerator.GenerationStamp()}
        public static bool TryGet{field.PropertyName}(this in Row<{table.QualifiedTypeName}> row, out Row<{referencedTableModel.QualifiedTypeName}> result)
        {{
            var db = Context<{model.DatabaseSymbol.Name}>.Current;
            return db.{referencedTableModel.FacadeName}.TryGet(row.data.{field.Name}, out result);
        }}");
            }
        }
    }
}