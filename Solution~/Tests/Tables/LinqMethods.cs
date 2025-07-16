// namespace Tests;
//
// using IntegrityTables;
//
// [TestFixture]
// public class LinqMethods
// {
//     private HumanResourcesDatabase db;
//
//     [SetUp]
//     public void Setup()
//     {
//         db = new HumanResourcesDatabase();
//     }
//
//     [Test]
//     public void ClearResetsKeyGenerator()
//     {
//         foreach (var x in
//                  from i in db.DeskTable
//                  select i
//                 )
//             Assert.That(x.id, Is.Not.EqualTo(0));
//     }
// }