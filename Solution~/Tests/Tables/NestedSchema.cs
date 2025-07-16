using IntegrityTables;

namespace Tests.MasterDB
{
    [GenerateDatabase(GenerateForUnity = false)]
    public partial class DaB
    {
        
    }

    [GenerateTable(typeof(DaB)), Serializable]
    public partial struct TableA
    {
        [Reference(typeof(TableA))] 
        public int x;
    }
    
}

namespace Tests.MasterDB.Xyz
{
    [GenerateTable(typeof(DaB)), Serializable]
    public partial struct TableB
    {
        
    }
}