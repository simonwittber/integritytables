using System.Numerics;

namespace IntegrityTables.Tests.Memory;

[GenerateDatabase]
public partial class Database
{
}
// A test table with most of the features of IntegrityTables
// including unique index, foreign key, computed fields, and check constraints.
[GenerateTable(typeof(Database), Capacity = 1024*1204), Serializable]
public partial struct Player
{
    [Unique]
    public int userHash;

    [Reference(typeof(Room), PropertyName = "Room", CollectionName = "Players")]
    public int roomId;
    
    [HotField]
    [Computed] public bool isEven;
    [Computed] public bool isFive;

    // Some fields to fill out table size to something more realistic
    public Vector2 position;
    [Immutable]
    public float _float;
    public string _string;
    public bool _A, _B, _C, _D, _E;
    public int _int;
    public long _long;
    public double _double;
    
    [CheckConstraint]
    public static bool UserHashIsValid(in Row<Player> row)
    {
        return row.data.userHash >= 0;
    }
    
    public static partial bool Compute_isEven(Database db, Row<Player> row)
    {
        return row.data.userHash % 2 == 0;
    }
    
    public static partial bool Compute_isFive(Database db, Row<Player> row)
    {
        return row.data.userHash % 5 == 0;
    }
    
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Room
{
    public string name;
    
    // Some fields to fill out table size to something more realistic
    public Vector2 position;
    public float _float;
    public string _string;
    public bool _A, _B, _C, _D, _E;
    public int _int;
    public long _long;
    public double _double;
}



[TestFixture]
public class MemoryTests
{
    public int N = 100;

    private Database db;
    private Row<Room>[] rooms;
    
    [SetUp]
    public void Setup()
    {
        db = new Database();
        rooms = [
            db.RoomTable.Add(new Room() { name = "Room 1" }),
            db.RoomTable.Add(new Room() { name = "Room 2" }),
            db.RoomTable.Add(new Room() { name = "Room 3" }),
        ];
        // db.PlayerTable.Add(new Player() {userHash = int.MaxValue/2});
        for(var i=0; i<N; i++)
        {
            var row = db.PlayerTable.Add(new Player() {userHash = i, position = new Vector2(i, i), roomId = rooms[i % rooms.Length].id});
        }
    }

    [Test]
    public void TestAdd()
    {
        // warmup
        db.PlayerTable.Add(new Player() {userHash = int.MaxValue});
        
        var before = GetAllocatedBytes();
        for (var i = N+2; i < N*2; i++)
        {
            var row = db.PlayerTable.Add(new Player() {userHash = i});
        }
        var after = GetAllocatedBytes();
        var allocated = after - before;
        Assert.That(allocated, Is.LessThan(1024), "Allocation detected");
    }
    
    [Test]
    public void TestEquality()
    {
        var a = db.PlayerTable.Get(1);
        var b = db.PlayerTable.Get(2);
        var warmup = EqualityComparer<Row<Player>>.Default.Equals(a, b);
        var before = GetAllocatedBytes();
        var result = EqualityComparer<Row<Player>>.Default.Equals(a, b);
        var after = GetAllocatedBytes();
        var allocated = after - before;
        Assert.That(allocated, Is.Zero, "Allocation detected");
    } 
    [Test]
    public void TestUpdate()
    {
        var c = db.PlayerTable.Get(1);
        // warmup
        c.data.roomId = 0;
        db.PlayerTable.Update(ref c);
        c.data.roomId = rooms[0].id;
        var before = GetAllocatedBytes();
        db.PlayerTable.Update(ref c);
        var after = GetAllocatedBytes();
        var allocated = after - before;
        Assert.That(allocated, Is.Zero, "Allocation detected");
    }

    [Test]
    public void TestCascadeSetNull()
    {
        
        // warmup    
        db.RoomTable.Remove(rooms[2], CascadeOperation.SetNull);
        
        using var scope = db.CreateContext();
        var before = GetAllocatedBytes();
        db.RoomTable.Remove(rooms[1], CascadeOperation.SetNull);
        var after = GetAllocatedBytes();
        
        var allocated = after - before;
        Assert.That(allocated, Is.LessThan(1024), "Allocation detected");
    }
    
    [Test]
    public void TestCascadeDelete()
    {
        var parent = db.RoomTable.Get(1);
        //warmup
        db.RoomTable.Remove(parent, CascadeOperation.Delete);
        parent = db.RoomTable.Get(2);
        var before = GetAllocatedBytes();
        db.RoomTable.Remove(parent, CascadeOperation.Delete);
        var after = GetAllocatedBytes();
        var allocated = after - before;
        Assert.That(allocated, Is.Zero, "Allocation detected");
    }

    private static long GetAllocatedBytes()
    {
        return GC.GetAllocatedBytesForCurrentThread();
    }
}