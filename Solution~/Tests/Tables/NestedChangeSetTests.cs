namespace IntegrityTables.Tests;

[TestFixture]
public class NestedChangeSetTests
{
    public struct TestData : IEquatable<TestData>
    {
        public int Value;

        public bool Equals(TestData other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is TestData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(TestData left, TestData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TestData left, TestData right)
        {
            return !left.Equals(right);
        }
    }
        
    Table<TestData> NewTable() => new Table<TestData>();

    [Test]
    public void SingleChangeSet_Rollback_RemovesAdd()
    {
        var table = NewTable();
        table.BeginChangeSet();
        var row = table.Add(new TestData { Value = 42 });

        Assert.That(table.Count, Is.EqualTo(1), "Row should be added inside changeset");
        Assert.That(table.ContainsKey(row.id), Is.True);

        table.RollbackChangeSet();

        Assert.That(table.Count, Is.EqualTo(0), "Rollback should remove the added row");
        Assert.That(table.ContainsKey(row.id), Is.False);
    }

    [Test]
    public void SingleChangeSet_Commit_KeepsAdd()
    {
        var table = NewTable();
        table.BeginChangeSet();
        var row = table.Add(new TestData { Value = 99 });

        Assert.That(table.Count, Is.EqualTo(1));
        table.CommitChangeSet();

        Assert.That(table.Count, Is.EqualTo(1), "Commit should preserve the added row");
        Assert.That(table.ContainsKey(row.id), Is.True);
    }

    [Test]
    public void Nested_InnerCommit_OuterRollback_RemovesBothInnerAndOuter()
    {
        var table = NewTable();

        // Outer
        table.BeginChangeSet();
        var outer = table.Add(new TestData { Value = 1 });

        // Inner
        table.BeginChangeSet();
        var inner = table.Add(new TestData { Value = 2 });

        table.CommitChangeSet();  // commit inner
        Assert.That(table.ContainsKey(outer.id), Is.True, "Outer row still present after inner commit");
        Assert.That(table.ContainsKey(inner.id), Is.True, "Inner row present after inner commit");

        table.RollbackChangeSet(); // rollback outer
        Assert.That(table.ContainsKey(outer.id), Is.False, "Outer rollback should remove outer row");
        Assert.That(table.ContainsKey(inner.id), Is.False, "Outer rollback should also remove inner row");
        Assert.That(table.Count, Is.EqualTo(0));
    }

    [Test]
    public void Nested_InnerRollback_OuterCommit_KeepsOuterOnly()
    {
        var table = NewTable();

        // Outer
        table.BeginChangeSet();
        var outer = table.Add(new TestData { Value = 3 });

        // Inner
        table.BeginChangeSet();
        var inner = table.Add(new TestData { Value = 4 });

        table.RollbackChangeSet(); // rollback inner
        Assert.That(table.ContainsKey(outer.id), Is.True, "Inner rollback should not remove outer row");
        Assert.That(table.ContainsKey(inner.id), Is.False, "Inner rollback should remove inner row");
        Assert.That(table.Count, Is.EqualTo(1));

        table.CommitChangeSet();   // commit outer
        Assert.That(table.ContainsKey(outer.id), Is.True, "Outer commit should preserve outer row");
        Assert.That(table.Count, Is.EqualTo(1));
    }
}