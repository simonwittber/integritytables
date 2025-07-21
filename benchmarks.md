# Integrity Tables Benchmarks

See the [Benchmark Project](Solution~/Benchmarks) for source of the below tables.

## Integer Maps

Integrity Tables uses specialised structures for primary key and foreign key indexes, instead of using Dictionary<int,int>

IdMap is used for primary keys, where the range is 0 - N and there are few holes. The benchmark below is specialised for this use case.


PagedMap is used for foreign keys, where the range could be anything with many holes. The benchmark below is also specialised for this case. This benchmark cannot be used to compare IdMap to PagedMap, it only compares with Dictionary<int, int> with equivalent usage.

IdMap tests are with keys with a full range of 0 - 1000000, PagedMap tests have the same range but only use a subset of 100 keys out of the possible 1000000. This mirrors a potential real worl scenario where a parent table would have 100 related rows in another table.


| Method                           | Mean      | Error     | StdDev    | Median    | Op/s   | Allocated   |
|--------------------------------- |----------:|----------:|----------:|----------:|-------:|------------:|
| PagedAddGet                      |  4.227 ms | 0.0655 ms | 0.1889 ms |  4.240 ms | 236.60 |     4.11 KB |
| IdMapAddGet                      |  3.701 ms | 0.1429 ms | 0.4213 ms |  3.727 ms | 270.19 |  5120.11 KB |
| DictAddGetVsIdMap                | 30.639 ms | 0.6255 ms | 1.7947 ms | 30.600 ms |  32.64 | 52625.89 KB |
| DictAddGetVsPagedMap             |  8.746 ms | 0.1413 ms | 0.4007 ms |  8.658 ms | 114.34 |     7.22 KB |
| PreAllocatedDictAddGetVsPagedMap |  8.734 ms | 0.1072 ms | 0.2988 ms |  8.639 ms | 114.49 |     2.22 KB |
| PreAllocatedDictAddGetVsIdMap    | 21.326 ms | 2.1914 ms | 6.1087 ms | 19.012 ms |  46.89 | 22708.86 KB |


## General Benchmarks

| Method                                     | Mean         | Error        | StdDev       | Median       | Op/s         | Allocated |
|------------------------------------------- |-------------:|-------------:|-------------:|-------------:|-------------:|----------:|
| AddRow_WithUniqueIndex                     |     75.10 ns |     3.609 ns |     9.632 ns |     78.42 ns | 13,314,885.0 |      28 B |
| AddRow                                     |     42.16 ns |     2.153 ns |     5.519 ns |     43.60 ns | 23,718,215.3 |         - |
| GetRow                                     |     39.58 ns |     4.800 ns |    12.976 ns |     42.90 ns | 25,262,281.9 |         - |
| Iterate_EntireTable_1000Rows               | 12,603.56 ns |   399.672 ns | 1,159.522 ns | 12,016.90 ns |     79,342.7 |         - |
| Iterate_RowContainer_1000Rows              |    254.08 ns |     2.148 ns |     6.127 ns |    254.65 ns |  3,935,837.5 |         - |
| Iterate_IndexedQuery_1000Rows              | 11,551.07 ns |   108.301 ns |   307.233 ns | 11,549.20 ns |     86,572.1 |         - |
| Iterate_NonIndexedQuery_1000Rows           | 19,553.05 ns |   443.519 ns | 1,258.190 ns | 19,656.15 ns |     51,142.9 |      88 B |
| UpdateRow                                  |    192.81 ns |     3.937 ns |    10.371 ns |    195.30 ns |  5,186,450.5 |         - |
| UpdateRow_ChangeForeignKey                 |    263.54 ns |     6.268 ns |    17.053 ns |    265.32 ns |  3,794,553.3 |         - |
| RemoveRow_CascadeDeleteForeignKey          | 77,652.38 ns | 2,016.572 ns | 5,417.391 ns | 74,950.00 ns |     12,877.9 |         - |
| RemoveRow_CascadeSetNullForeignKey         | 91,394.19 ns | 3,661.057 ns | 9,960.177 ns | 86,883.33 ns |     10,941.6 |    3269 B |
| UpdateRow_ChangeForeignKey_InsideChangeSet |    422.64 ns |    11.941 ns |    32.079 ns |    424.72 ns |  2,366,083.8 |      32 B |
