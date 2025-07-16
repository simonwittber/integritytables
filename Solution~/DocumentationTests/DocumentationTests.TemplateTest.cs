namespace DocumentationTests.TemplateTest;
using IntegrityTables;

[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct TestTable
{
    [Unique]
    public string testField;
}

public class TestDocumentation
{
    //[Test]
    public void TestMethod()
    {
        var db = new Database();
    }
}

