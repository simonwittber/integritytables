using IntegrityTables;

namespace Tests.Tables;

[TestFixture]
public class IntegerMapTest
{
    private int[] keys, values;
    [SetUp]
    public void Setup()
    {
        keys = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        values = [10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
    }
    
    [Test]
    public void TestIdMap()
    {
        IIdMap idMap = new IdMap();
        for (int i = 0; i < keys.Length; i++)
        {
            idMap[keys[i]] = values[i];
        }
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.That(idMap[keys[i]], Is.EqualTo(values[i]));
        }
        Assert.That(idMap.ContainsKey(5), Is.True);
        Assert.That(idMap.ContainsKey(10), Is.False);
        Assert.That(idMap.TryGetValue(5, out var value), Is.True);
        Assert.That(value, Is.EqualTo(15));
        Assert.That(idMap.TryGetValue(10, out value), Is.False);
        Assert.That(value, Is.EqualTo(-1));
        idMap.Remove(5);
        Assert.That(idMap.ContainsKey(5), Is.False);
        Assert.That(idMap[5], Is.EqualTo(-1));
        idMap.Clear();
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.That(idMap[keys[i]], Is.EqualTo(-1));
        }
    }
    
    [Test]
    public void TestPagedMap()
    {
        IIdMap idMap = new PagedIdMap();
        for (int i = 0; i < keys.Length; i++)
        {
            idMap[keys[i]] = values[i];
        }
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.That(idMap[keys[i]], Is.EqualTo(values[i]));
        }
        Assert.That(idMap.ContainsKey(5), Is.True);
        Assert.That(idMap.ContainsKey(10), Is.False);
        Assert.That(idMap.TryGetValue(5, out var value), Is.True);
        Assert.That(value, Is.EqualTo(15));
        Assert.That(idMap.TryGetValue(10, out value), Is.False);
        Assert.That(value, Is.EqualTo(-1));
        idMap.Remove(5);
        Assert.That(idMap.ContainsKey(5), Is.False);
        Assert.That(idMap[5], Is.EqualTo(-1));
        idMap.Clear();
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.That(idMap[keys[i]], Is.EqualTo(-1));
        }
    }
}