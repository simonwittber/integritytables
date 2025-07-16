namespace IntegrityTables.Tests;

[TestFixture]
public class TestTriggerOrder
{
    struct TriggerOrderTestTable : IEquatable<TriggerOrderTestTable>
    {
        public int v;

        public bool Equals(TriggerOrderTestTable other)
        {
            return v == other.v;
        }

        public override bool Equals(object obj)
        {
            return obj is TriggerOrderTestTable other && Equals(other);
        }

        public override int GetHashCode()
        {
            return v;
        }

        public static bool operator ==(TriggerOrderTestTable left, TriggerOrderTestTable right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TriggerOrderTestTable left, TriggerOrderTestTable right)
        {
            return !left.Equals(right);
        }
    }
    
    Table<TriggerOrderTestTable> table;
    [SetUp]
    public void Setup()
    {
        table = new Table<TriggerOrderTestTable>();
    }
    
    [Test]
    public void TestTriggerOrdering()
    {
        var results = new List<int>();
        
      table.AfterAdd += (1, (in Row<TriggerOrderTestTable> row) => results.Add(1) ) ;
      table.AfterAdd += (2, (in Row<TriggerOrderTestTable> row) => results.Add(2) ) ;

      table.Add(new TriggerOrderTestTable() { v = 1 });
      
      Assert.That(results, Is.EquivalentTo(new List<int>() { 1, 2 }));
    }
    
    [Test]
    public void TestTriggerOutOfOrdering()
    {
        var results = new List<int>();
        
        table.AfterAdd += (4, (in Row<TriggerOrderTestTable> row) => results.Add(4) ) ;
        table.AfterAdd += (2, (in Row<TriggerOrderTestTable> row) => results.Add(2) ) ;

        table.Add(new TriggerOrderTestTable());
      
        Assert.That(results, Is.EquivalentTo(new List<int>() { 2, 4 }));
    }
    
    [Test]
    public void TestTriggerOrderingWithNegativePriority()
    {
        var results = new List<int>();
        table.AfterAdd += (2, (in Row<TriggerOrderTestTable> row) => results.Add(2) ) ;
        table.AfterAdd += (1, (in Row<TriggerOrderTestTable> row) => results.Add(1) ) ;
        table.AfterAdd += (-1, (in Row<TriggerOrderTestTable> row) => results.Add(-1) ) ;

        table.Add(new TriggerOrderTestTable());
      
        Assert.That(results, Is.EquivalentTo(new List<int>() { -1, 1, 2 }));
    }
}