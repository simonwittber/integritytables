// namespace IntegrityTables.Tests;
// using IntegrityTables.Linq;
//
// public class LinqTests
// {
//     private HumanResourcesDatabase _db;
//
//     [SetUp]
//     public void Setup()
//     {
//         _db = new HumanResourcesDatabase();
//     }
//
//     [Test]
//     public void TestSelect()
//     {
//         _db.EmployeeTable.Add(new Employee());
//         var query = new TableQuery<Employee>(_db.EmployeeTable);
//         var employees = (from i in query select i.id);
//         Assert.That(employees.Count(), Is.EqualTo(1));
//     }
//     
//     public void TestWhereSelect()
//     {
//         _db.EmployeeTable.Add(new Employee());
//         _db.EmployeeTable.Add(new Employee());
//
//         var employees = (
//             from i in _db.EmployeeTable
//             where i.id == 1
//             select i.id);
//         Assert.That(employees.Count(), Is.EqualTo(1));
//     }
// }