using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace IntegrityTables.Benchmarks;

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

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser]
public class MyBenchmarks
{

    public int N = 1000;

    private Database db = null!;
    private Row<Room>[] rooms = null!;
    
    [IterationSetup]
    public void Setup()
    {
        db = new Database();
        rooms = [
            db.RoomTable.Add(new Room() { name = "Room 1" }),
            db.RoomTable.Add(new Room() { name = "Room 2" }),
            db.RoomTable.Add(new Room() { name = "Room 3" }),
        ];
        for(var i=0; i<N; i++)
        {
            var row = db.PlayerTable.Add(new Player() {userHash = i, position = new Vector2(i, i), roomId = rooms[i % rooms.Length].id});
        }
    }

    [Benchmark(OperationsPerInvoke = 1000*2)]
    public void AddRow_WithUniqueIndex()
    {
        for (var i = N+2; i < N*2; i++)
        {
            var row = db.PlayerTable.Add(new Player() {userHash = i});
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000*2)]
    public void AddRow()
    {
        for (var i = N+2; i < N*2; i++)
        {
            var row = db.RoomTable.Add(new Room() {});
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public void GetRow()
    {
        for (var i = 1; i < N; i++)
        {
            var row = db.PlayerTable.Get(i);
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public void Iterate_EntireTable_1000Rows()
    {
        for(var x = 0; x < N; x++)
        {
            foreach (var i in db.PlayerTable)
            {
                // no need to call Get here, since we already have the row
            }
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public void Iterate_RowContainer_1000Rows()
    {
        for(var x = 0; x < N; x++)
        {
            foreach (ref var i in db.PlayerTable.InternalRows())
            {
                // no need to call Get here, since we have a row facade that will
                // write directly to the row data.
            }
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public void Iterate_IndexedQuery_1000Rows()
    {
        for (var x = 0; x < N; x++)
        {
            foreach (var i in db.PlayerTable.SelectByIsEven(true))
            {
                // the id is usually not useful by itself, to make this benchmark more realistic,
                // we will call Get to retrieve the full row
                var p = db.PlayerTable.Get(i);
            }
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public void Iterate_NonIndexedQuery_1000Rows()
    {
        for (var x = 0; x < N; x++)
        {
            foreach (var i in db.PlayerTable.Query(((in Player data) => data.userHash < 500)))
            {
                // no need to call Get here, since we already have the row
            }
        }
    }
    
    [Benchmark(OperationsPerInvoke = 999)]
    public void UpdateRow()
    {
        for (var i = 1; i < N; i++)
        {
            var p = db.PlayerTable.Get(i);
            p.data.position = new Vector2(i*i, i*i);
            db.PlayerTable.Update(ref p);
        }
    }
    
    [Benchmark(OperationsPerInvoke = 999)]
    public void UpdateRow_ChangeForeignKey()
    {
        for (var i = 1; i < N; i++)
        {
            var p = db.PlayerTable.Get(i);
            p.data.roomId = rooms[(i+1) % rooms.Length].id;
            db.PlayerTable.Update(ref p);
        }
    }
    
    [Benchmark(OperationsPerInvoke = 3)]
    public void RemoveRow_CascadeDeleteForeignKey()
    {
        db.RoomTable.Remove(rooms[0], CascadeOperation.Delete);
        db.RoomTable.Remove(rooms[1], CascadeOperation.Delete);
        db.RoomTable.Remove(rooms[2], CascadeOperation.Delete);
    }
    
    [Benchmark(OperationsPerInvoke = 3)]
    public void RemoveRow_CascadeSetNullForeignKey()
    {
        db.RoomTable.Remove(rooms[0], CascadeOperation.SetNull);
        db.RoomTable.Remove(rooms[1], CascadeOperation.SetNull);
        db.RoomTable.Remove(rooms[2], CascadeOperation.SetNull);
    }
    
    [Benchmark(OperationsPerInvoke = 999)]
    public void UpdateRow_ChangeForeignKey_InsideChangeSet()
    {
        for (var i = 1; i < N; i++)
        {
            using var cs = db.NewChangeSet();
            var p = db.PlayerTable.Get(i);
            p.data.roomId = rooms[(i+1) % rooms.Length].id;
            db.PlayerTable.Update(ref p);
            cs.Commit();
        }
    }
}