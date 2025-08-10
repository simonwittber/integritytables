using System;
using System.Collections.Generic;
using System.Threading;

namespace IntegrityTables;

public partial class Table<T> : ITable where T : struct
{
    public IRowContainer RowContainer;
    private readonly int _capacity;
}