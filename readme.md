# IntegrityTables

A lightweight, in-memory, code-generated database management system for C# applications. Uses source generators to define your schema as struct types and get fully-typed, allocation-free APIs for CRUD operations, relationships, transactions, triggers, indexes, and view models. Focus is speed and zero or low allocation code.
For more documentation, see the [Documentation Tests](Solution~/DocumentationTests).

## Features
- Check constraints
- Unique constraints and Indexes
- Foreign Key constraints and indexes
- Triggers
- Table level observers
- Row level observers
- View model generation
- Transactions (Change Sets)
- [Benchmarks](benchmarks.md)

## Tested

Unit Tests: Amost 200+ unit tests covering core functionality, performance, and edge cases.

Documentation Style Tests: 8 comprehensive tests  (Basics, ChangeSets, Constraints, Indexes, ManyToMany, Services, Triggers, ViewModels) that serve as living examples and verify generated APIs.

Benchmarked: The benchmark project helps us keep an eye on allocations and performance.

## Overview

### Define your database facade

    [GenerateDatabase]
    public partial class Database { }

### Declare tables:

    [GenerateTable(typeof(Database)), Serializable]
    public partial struct User
    {
        [Unique] public string email;
    }

Use the API:

    var db = new Database();
    var userRow = db.UserTable.Add(new User { email = "simon@simon.com" });
    var id = userRow.id;
    db.UserTable.Query(...);
    db.UserTable.Remove(...);
    db.UserTable.Update(...);


Unique/indexed lookups:

    if (db.UserTable.TryGetByEmail("alice@example.com", out var row)) { /* O(1) lookup */ }

Transactions with ChangeSets allow atomic group commits or rollbacks:

    using var cs = db.NewChangeSet();
    // Multiple operations
    cs.Commit(); // all or nothing


MIT © Simon Wittber
