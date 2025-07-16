using System.Runtime.InteropServices;

namespace DocumentationTests.Blittable;

using IntegrityTables;

[GenerateDatabase]
public partial class Database
{
}

// The below struct has Blittable = true, so it will fail to compile if it contains any reference types, such as string.
// This is why it is commented out in this example.
/*
[GenerateTable(typeof(Database), Blittable = true), Serializable]
public partial struct User
{
    [Unique]
    public string email;
}
*/

// This table is going to store strings, which we can reference from other tables that are required to be blittable.
// This table could be serialized and sent once to a client, and then used to reference strings in other blittable structs which can be sent as pure byte arrays.
[GenerateTable(typeof(Database)), Serializable]
public partial struct InternedString
{
    public string text;
}

// This is the blittable table. Blittable = true tells the generator to ensure that the struct does not contain any reference types.
[GenerateTable(typeof(Database), Blittable = true), Serializable]
public partial struct User
{
    [Reference(typeof(InternedString), PropertyName = "Email", NotNull = true)]
    public int emailId;
}

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();
        using var scope = db.CreateContext();
        
        // There is a IsBlittable property on tables that are blittable.
        // Tables that are explicitly marked as blittable will have this property set to true.
        // Other tables may ore may not be blittable, depending on their contents.
        Assert.That(db.UserTable.IsBlittable, Is.True);
        Assert.That(db.InternedStringTable.IsBlittable, Is.False);

        var email = db.InternedStringTable.Add(new InternedString() {text = "simon@simon.com"});
        var user = db.UserTable.Add(new User() {emailId = email.id});
        
        Assert.That(user.Email().text(), Is.EqualTo("simon@simon.com"));
        {
            // because the User struct is blittable, we can simply grab it's memory address and pass it to unmanaged code,
            // or avoid serializing it when sending it over the network, storing to disk etc.
            var span = MemoryMarshal.CreateSpan(ref user.data, 1);
            var bytes = MemoryMarshal.AsBytes(span);
            Assert.That(bytes.Length, Is.EqualTo(Marshal.SizeOf<User>()));
        }
        {
            // The Row<T> is also blittable.
            var span = MemoryMarshal.CreateSpan(ref user, 1);
            var bytes = MemoryMarshal.AsBytes(span);
            // Row<T> adds 12 bytes of metadata to the blittable struct.
            var expectedSize = 12 + Marshal.SizeOf<User>();
            Assert.That(bytes.Length, Is.EqualTo(expectedSize));
        }
    }
}