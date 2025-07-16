using System;

namespace IntegrityTables;

public struct JoinedQueryEnumerator<T> where T : struct, IEquatable<T>
{
    public JoinedQueryEnumerator(QueryEnumerator<T> first, QueryEnumerator<T> second)
    {
        
    }
}