using System;
using System.Collections.Generic;
using System.Linq;
using IntegrityTables;
using NUnit.Framework;

namespace IntegrityTables.Tests
{
    [TestFixture]
    public class ChunkContainerTests
    {
        private ChunkContainer<int> container;

        [SetUp]
        public void Setup()
        {
            container = new ChunkContainer<int>();
        }

        [TearDown]
        public void TearDown()
        {
            container?.Dispose();
        }

        [Test]
        public void Add_SingleElement_ReturnsCorrectIndex()
        {
            // Act
            var index = container.Add(42);

            // Assert
            Assert.That(index, Is.EqualTo(0));
            Assert.That(container.GetT1(index), Is.EqualTo(42));
        }

        [Test]
        public void Add_MultipleElements_ReturnsSequentialIndices()
        {
            // Act
            var indices = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                indices.Add(container.Add(i * 10));
            }

            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.That(indices[i], Is.EqualTo(i));
                Assert.That(container.GetT1(indices[i]), Is.EqualTo(i * 10));
            }
        }

        [Test]
        public void Add_MoreThanChunkSize_AllocatesNewChunk()
        {
            // Act - Add more than 64 elements (chunk size)
            var indices = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                indices.Add(container.Add(i));
            }

            // Assert
            for (int i = 0; i < 100; i++)
            {
                Assert.That(indices[i], Is.EqualTo(i));
                Assert.That(container.GetT1(indices[i]), Is.EqualTo(i));
            }
        }

        [Test]
        public void GetT1_ValidIndex_ReturnsCorrectValue()
        {
            // Arrange
            var index = container.Add(123);

            // Act
            var value = container.GetT1(index);

            // Assert
            Assert.That(value, Is.EqualTo(123));
        }

        [Test]
        public void SetT1_ValidIndex_UpdatesValue()
        {
            // Arrange
            var index = container.Add(100);

            // Act
            container.SetT1(index, 200);

            // Assert
            Assert.That(container.GetT1(index), Is.EqualTo(200));
        }

        [Test]
        public void GetT1_ByReference_AllowsDirectModification()
        {
            // Arrange
            var index = container.Add(50);

            // Act
            ref var value = ref container.GetT1(index);
            value = 75;

            // Assert
            Assert.That(container.GetT1(index), Is.EqualTo(75));
        }

        [Test]
        public void Remove_SingleElement_RemovesCorrectly()
        {
            // Arrange
            var index1 = container.Add(10);
            var index2 = container.Add(20);
            var index3 = container.Add(30);

            // Act
            container.Remove(index2);

            // Assert - After removal, the last element should move to the removed slot
            Assert.That(container.GetT1(index1), Is.EqualTo(10));
            Assert.That(container.GetT1(index2), Is.EqualTo(30)); // Last element moved here
        }

        [Test]
        public void Remove_LastElement_RemovesCorrectly()
        {
            // Arrange
            var index1 = container.Add(10);
            var index2 = container.Add(20);

            // Act
            container.Remove(index2);

            // Assert
            Assert.That(container.GetT1(index1), Is.EqualTo(10));
        }

        [Test]
        public void Remove_MultipleElements_MaintainsDataIntegrity()
        {
            // Arrange - Add many elements
            var indices = new List<int>();
            for (int i = 0; i < 20; i++)
            {
                indices.Add(container.Add(i * 100));
            }

            // Act - Remove some elements
            container.Remove(indices[5]);
            container.Remove(indices[10]);
            container.Remove(indices[15]);

            // Assert - Verify remaining elements are still accessible
            Assert.That(container.GetT1(indices[0]), Is.EqualTo(0));
            Assert.That(container.GetT1(indices[1]), Is.EqualTo(100));
            Assert.That(container.GetT1(indices[2]), Is.EqualTo(200));
        }

        [Test]
        public void Add_AfterRemoval_ReusesPartialChunks()
        {
            // Arrange - Fill more than one chunk
            var indices = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                indices.Add(container.Add(i));
            }

            // Act - Remove some elements from the first chunk
            container.Remove(indices[10]);
            container.Remove(indices[20]);
            container.Remove(indices[30]);

            // Add new elements - should reuse partial chunks
            var newIndex1 = container.Add(1000);
            var newIndex2 = container.Add(2000);

            // Assert - New elements should reuse slots in partial chunks
            Assert.That(container.GetT1(newIndex1), Is.EqualTo(1000));
            Assert.That(container.GetT1(newIndex2), Is.EqualTo(2000));
        }

        [Test]
        public void StressTest_AddAndRemoveMany_MaintainsIntegrity()
        {
            // Arrange
            var activeIndices = new HashSet<int>();
            var expectedValues = new Dictionary<int, int>();
            var random = new Random(42);

            // Act - Perform many add/remove operations
            for (int i = 0; i < 1000; i++)
            {
                if (activeIndices.Count < 500 || random.Next(2) == 0)
                {
                    // Add operation
                    var value = random.Next(10000);
                    var index = container.Add(value);
                    activeIndices.Add(index);
                    expectedValues[index] = value;
                }
                else
                {
                    // Remove operation
                    var indexToRemove = activeIndices.Skip(random.Next(activeIndices.Count)).First();
                    container.Remove(indexToRemove);
                    activeIndices.Remove(indexToRemove);
                    expectedValues.Remove(indexToRemove);
                }
            }

            // Assert - Verify all remaining elements have correct values
            foreach (var index in activeIndices)
            {
                Assert.That(container.GetT1(index), Is.EqualTo(expectedValues[index]));
            }
        }

        [Test]
        public void Dispose_ReleasesMemory_DoesNotThrow()
        {
            // Arrange
            for (int i = 0; i < 200; i++)
            {
                container.Add(i);
            }

            // Act & Assert
            Assert.DoesNotThrow(() => container.Dispose());
        }

        [Test]
        public void MultipleChunks_CrossChunkOperations_WorkCorrectly()
        {
            // Arrange - Add elements across multiple chunks
            var indices = new List<int>();
            for (int i = 0; i < 200; i++) // More than 3 chunks (64 * 3 = 192)
            {
                indices.Add(container.Add(i * 2));
            }

            // Act & Assert - Test operations across different chunks
            // Test first chunk
            Assert.That(container.GetT1(indices[0]), Is.EqualTo(0));
            Assert.That(container.GetT1(indices[30]), Is.EqualTo(60));
            Assert.That(container.GetT1(indices[63]), Is.EqualTo(126));

            // Test second chunk
            Assert.That(container.GetT1(indices[64]), Is.EqualTo(128));
            Assert.That(container.GetT1(indices[100]), Is.EqualTo(200));
            Assert.That(container.GetT1(indices[127]), Is.EqualTo(254));

            // Test third chunk
            Assert.That(container.GetT1(indices[128]), Is.EqualTo(256));
            Assert.That(container.GetT1(indices[150]), Is.EqualTo(300));
            Assert.That(container.GetT1(indices[199]), Is.EqualTo(398));

            // Test updates across chunks
            container.SetT1(indices[0], 9999);    // First chunk
            container.SetT1(indices[64], 8888);   // Second chunk
            container.SetT1(indices[128], 7777);  // Third chunk

            Assert.That(container.GetT1(indices[0]), Is.EqualTo(9999));
            Assert.That(container.GetT1(indices[64]), Is.EqualTo(8888));
            Assert.That(container.GetT1(indices[128]), Is.EqualTo(7777));
        }
    }

    [TestFixture]
    public class ChunkContainerStructTests
    {
        public struct TestStruct : IEquatable<TestStruct>
        {
            public int Value1;
            public float Value2;
            public bool Value3;

            public TestStruct(int v1, float v2, bool v3)
            {
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
            }

            public bool Equals(TestStruct other)
            {
                return Value1 == other.Value1 && 
                       Math.Abs(Value2 - other.Value2) < 0.001f && 
                       Value3 == other.Value3;
            }

            public override bool Equals(object obj)
            {
                return obj is TestStruct other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Value1, Value2, Value3);
            }
        }

        private ChunkContainer<TestStruct> container;

        [SetUp]
        public void Setup()
        {
            container = new ChunkContainer<TestStruct>();
        }

        [TearDown]
        public void TearDown()
        {
            container?.Dispose();
        }

        [Test]
        public void Add_CustomStruct_StoresAndRetrievesCorrectly()
        {
            // Arrange
            var testStruct = new TestStruct(42, 3.14f, true);

            // Act
            var index = container.Add(testStruct);

            // Assert
            var retrieved = container.GetT1(index);
            Assert.That(retrieved.Equals(testStruct), Is.True);
        }

        [Test]
        public void SetT1_CustomStruct_UpdatesCorrectly()
        {
            // Arrange
            var original = new TestStruct(1, 1.0f, false);
            var updated = new TestStruct(2, 2.0f, true);
            var index = container.Add(original);

            // Act
            container.SetT1(index, updated);

            // Assert
            var retrieved = container.GetT1(index);
            Assert.That(retrieved.Equals(updated), Is.True);
        }

        [Test]
        public void ReferenceModification_CustomStruct_WorksCorrectly()
        {
            // Arrange
            var testStruct = new TestStruct(100, 5.5f, false);
            var index = container.Add(testStruct);

            // Act
            ref var structRef = ref container.GetT1(index);
            structRef.Value1 = 200;
            structRef.Value2 = 10.5f;
            structRef.Value3 = true;

            // Assert
            var retrieved = container.GetT1(index);
            Assert.That(retrieved.Value1, Is.EqualTo(200));
            Assert.That(retrieved.Value2, Is.EqualTo(10.5f).Within(0.001f));
            Assert.That(retrieved.Value3, Is.True);
        }

        [Test]
        public void MultipleStructs_DifferentValues_AllStoredCorrectly()
        {
            // Arrange & Act
            var structs = new[]
            {
                new TestStruct(1, 1.1f, true),
                new TestStruct(2, 2.2f, false),
                new TestStruct(3, 3.3f, true),
                new TestStruct(4, 4.4f, false)
            };

            var indices = new int[structs.Length];
            for (int i = 0; i < structs.Length; i++)
            {
                indices[i] = container.Add(structs[i]);
            }

            // Assert
            for (int i = 0; i < structs.Length; i++)
            {
                var retrieved = container.GetT1(indices[i]);
                Assert.That(retrieved.Equals(structs[i]), Is.True, $"Struct at index {i} should match");
            }
        }
    }

    [TestFixture]
    public class ChunkContainerTwoTypesTests
    {
        public struct TestStruct : IEquatable<TestStruct>
        {
            public int Value1;
            public float Value2;

            public TestStruct(int v1, float v2)
            {
                Value1 = v1;
                Value2 = v2;
            }

            public bool Equals(TestStruct other)
            {
                return Value1 == other.Value1 && Math.Abs(Value2 - other.Value2) < 0.001f;
            }

            public override bool Equals(object obj)
            {
                return obj is TestStruct other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Value1, Value2);
            }
        }

        private ChunkContainer<int, float> container;

        [SetUp]
        public void Setup()
        {
            container = new ChunkContainer<int, float>();
        }

        [Test]
        public void SimpleTest()
        {
            var index = container.Add(3, 4.5f);
            Assert.That(index, Is.EqualTo(0));
            Assert.That(container.GetT1(index), Is.EqualTo(3));
            Assert.That(container.GetT2(index), Is.EqualTo(4.5f).Within(0.001f));
        }

        [TearDown]
        public void TearDown()
        {
            container?.Dispose();
        }

        [Test]
        public void Add_TwoTypes_ReturnsCorrectIndex()
        {
            // Act
            var index = container.Add(42, 3.14f);

            // Assert
            Assert.That(index, Is.EqualTo(0));
            Assert.That(container.GetT1(index), Is.EqualTo(42));
            Assert.That(container.GetT2(index), Is.EqualTo(3.14f).Within(0.001f));
        }

        [Test]
        public void Add_MultipleElements_StoresBothTypesCorrectly()
        {
            // Act
            var indices = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                indices.Add(container.Add(i * 10, i * 1.5f));
            }

            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.That(indices[i], Is.EqualTo(i));
                Assert.That(container.GetT1(indices[i]), Is.EqualTo(i * 10));
                Assert.That(container.GetT2(indices[i]), Is.EqualTo(i * 1.5f).Within(0.001f));
            }
        }

        [Test]
        public void GetT1_ValidIndex_ReturnsCorrectValue()
        {
            // Arrange
            var index = container.Add(123, 456.78f);

            // Act
            var value = container.GetT1(index);

            // Assert
            Assert.That(value, Is.EqualTo(123));
        }

        [Test]
        public void GetT2_ValidIndex_ReturnsCorrectValue()
        {
            // Arrange
            var index = container.Add(123, 456.78f);

            // Act
            var value = container.GetT2(index);

            // Assert
            Assert.That(value, Is.EqualTo(456.78f).Within(0.001f));
        }

        [Test]
        public void SetT1_ValidIndex_UpdatesOnlyT1()
        {
            // Arrange
            var index = container.Add(100, 200.5f);

            // Act
            container.SetT1(index, 300);

            // Assert
            Assert.That(container.GetT1(index), Is.EqualTo(300));
            Assert.That(container.GetT2(index), Is.EqualTo(200.5f).Within(0.001f)); // T2 should remain unchanged
        }

        [Test]
        public void SetT2_ValidIndex_UpdatesOnlyT2()
        {
            // Arrange
            var index = container.Add(100, 200.5f);

            // Act
            container.SetT2(index, 300.7f);

            // Assert
            Assert.That(container.GetT1(index), Is.EqualTo(100)); // T1 should remain unchanged
            Assert.That(container.GetT2(index), Is.EqualTo(300.7f).Within(0.001f));
        }

        [Test]
        public void GetT1_ByReference_AllowsDirectModification()
        {
            // Arrange
            var index = container.Add(50, 75.5f);

            // Act
            ref var t1Ref = ref container.GetT1(index);
            t1Ref = 150;

            // Assert
            Assert.That(container.GetT1(index), Is.EqualTo(150));
            Assert.That(container.GetT2(index), Is.EqualTo(75.5f).Within(0.001f)); // T2 should remain unchanged
        }

        [Test]
        public void GetT2_ByReference_AllowsDirectModification()
        {
            // Arrange
            var index = container.Add(50, 75.5f);

            // Act
            ref var t2Ref = ref container.GetT2(index);
            t2Ref = 125.25f;

            // Assert
            Assert.That(container.GetT1(index), Is.EqualTo(50)); // T1 should remain unchanged
            Assert.That(container.GetT2(index), Is.EqualTo(125.25f).Within(0.001f));
        }

        [Test]
        public void Add_MoreThanChunkSize_AllocatesNewChunkForBothTypes()
        {
            // Act - Add more than 64 elements (chunk size)
            var indices = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                var item = container.Add(i, i * 0.1f);
                indices.Add(item);
            }

            // Assert
            for (int i = 0; i < 100; i++)
            {
                Assert.That(indices[i], Is.EqualTo(i));
                Assert.That(container.GetT1(indices[i]), Is.EqualTo(i));
                Assert.That(container.GetT2(indices[i]), Is.EqualTo(i * 0.1f).Within(0.001f));
            }
        }

        [Test]
        public void Remove_Element_MaintainsBothTypesIntegrity()
        {
            // Arrange
            var index1 = container.Add(10, 1.1f);
            var index2 = container.Add(20, 2.2f);
            var index3 = container.Add(30, 3.3f);

            // Act
            container.Remove(index2);

            // Assert - After removal, the last element should move to the removed slot
            Assert.That(container.GetT1(index1), Is.EqualTo(10));
            Assert.That(container.GetT2(index1), Is.EqualTo(1.1f).Within(0.001f));
            
            // The last element (30, 3.3f) should have moved to index2's position
            Assert.That(container.GetT1(index2), Is.EqualTo(30));
            Assert.That(container.GetT2(index2), Is.EqualTo(3.3f).Within(0.001f));
        }

        [Test]
        public void MultipleChunks_CrossChunkOperations_WorkCorrectlyForBothTypes()
        {
            // Arrange - Add elements across multiple chunks
            var indices = new List<int>();
            for (int i = 0; i < 150; i++) // More than 2 chunks
            {
                indices.Add(container.Add(i * 100, i * 0.01f));
            }

            // Act & Assert - Test operations across different chunks
            // Test first chunk
            Assert.That(container.GetT1(indices[0]), Is.EqualTo(0));
            Assert.That(container.GetT2(indices[0]), Is.EqualTo(0.0f).Within(0.001f));
            Assert.That(container.GetT1(indices[63]), Is.EqualTo(6300));
            Assert.That(container.GetT2(indices[63]), Is.EqualTo(0.63f).Within(0.001f));

            // Test second chunk
            Assert.That(container.GetT1(indices[64]), Is.EqualTo(6400));
            Assert.That(container.GetT2(indices[64]), Is.EqualTo(0.64f).Within(0.001f));
            Assert.That(container.GetT1(indices[127]), Is.EqualTo(12700));
            Assert.That(container.GetT2(indices[127]), Is.EqualTo(1.27f).Within(0.001f));

            // Test third chunk
            Assert.That(container.GetT1(indices[128]), Is.EqualTo(12800));
            Assert.That(container.GetT2(indices[128]), Is.EqualTo(1.28f).Within(0.001f));
            Assert.That(container.GetT1(indices[149]), Is.EqualTo(14900));
            Assert.That(container.GetT2(indices[149]), Is.EqualTo(1.49f).Within(0.001f));

            // Test updates across chunks
            container.SetT1(indices[0], 9999);    // First chunk
            container.SetT1(indices[64], 8888);   // Second chunk
            container.SetT1(indices[128], 7777);  // Third chunk

            ref var t2_0 = ref container.GetT2(indices[0]);
            ref var t2_64 = ref container.GetT2(indices[64]);
            ref var t2_128 = ref container.GetT2(indices[128]);
            t2_0 = 99.99f;
            t2_64 = 88.88f;
            t2_128 = 77.77f;

            Assert.That(container.GetT1(indices[0]), Is.EqualTo(9999));
            Assert.That(container.GetT2(indices[0]), Is.EqualTo(99.99f).Within(0.001f));
            Assert.That(container.GetT1(indices[64]), Is.EqualTo(8888));
            Assert.That(container.GetT2(indices[64]), Is.EqualTo(88.88f).Within(0.001f));
            Assert.That(container.GetT1(indices[128]), Is.EqualTo(7777));
            Assert.That(container.GetT2(indices[128]), Is.EqualTo(77.77f).Within(0.001f));
        }

        [Test]
        public void StressTest_MixedOperations_MaintainsBothTypesIntegrity()
        {
            // Arrange
            var activeIndices = new HashSet<int>();
            var expectedT1Values = new Dictionary<int, int>();
            var expectedT2Values = new Dictionary<int, float>();
            var random = new Random(42);

            // Act - Perform many add/remove operations
            for (int i = 0; i < 500; i++)
            {
                if (activeIndices.Count < 250 || random.Next(2) == 0)
                {
                    // Add operation
                    var t1Value = random.Next(10000);
                    var t2Value = (float)(random.NextDouble() * 1000.0);
                    var index = container.Add(t1Value, t2Value);
                    activeIndices.Add(index);
                    expectedT1Values[index] = t1Value;
                    expectedT2Values[index] = t2Value;
                }
                else
                {
                    // Remove operation
                    var indexToRemove = activeIndices.Skip(random.Next(activeIndices.Count)).First();
                    container.Remove(indexToRemove);
                    activeIndices.Remove(indexToRemove);
                    expectedT1Values.Remove(indexToRemove);
                    expectedT2Values.Remove(indexToRemove);
                }
            }

            // Assert - Verify all remaining elements have correct values for both types
            foreach (var index in activeIndices)
            {
                Assert.That(container.GetT1(index), Is.EqualTo(expectedT1Values[index]));
                Assert.That(container.GetT2(index), Is.EqualTo(expectedT2Values[index]).Within(0.001f));
            }
        }
    }

    [TestFixture]
    public class ChunkContainerTwoCustomStructsTests
    {
        public struct Point : IEquatable<Point>
        {
            public int X, Y;
            
            public Point(int x, int y) { X = x; Y = y; }
            
            public bool Equals(Point other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is Point other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Y);
        }

        public struct Color : IEquatable<Color>
        {
            public byte R, G, B, A;
            
            public Color(byte r, byte g, byte b, byte a = 255)
            {
                R = r; G = g; B = b; A = a;
            }

            public bool Equals(Color other)
            {
                return R == other.R && G == other.G && B == other.B && A == other.A;
            }

            public override bool Equals(object obj)
            {
                return obj is Color other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(R, G, B, A);
            }

            public static bool operator ==(Color left, Color right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Color left, Color right)
            {
                return !left.Equals(right);
            }
        }

        private ChunkContainer<Point, Color> container;

        [SetUp]
        public void Setup()
        {
            container = new ChunkContainer<Point, Color>();
        }

        [TearDown]
        public void TearDown()
        {
            container?.Dispose();
        }

        [Test]
        public void Add_TwoCustomStructs_StoresAndRetrievesCorrectly()
        {
            // Arrange
            var point = new Point(10, 20);
            var color = new Color(255, 128, 64);

            // Act
            var index = container.Add(point, color);

            // Assert
            var retrievedPoint = container.GetT1(index);
            var retrievedColor = container.GetT2(index);
            
            Assert.That(retrievedPoint.Equals(point), Is.True);
            Assert.That(retrievedColor.R, Is.EqualTo(255));
            Assert.That(retrievedColor.G, Is.EqualTo(128));
            Assert.That(retrievedColor.B, Is.EqualTo(64));
            Assert.That(retrievedColor.A, Is.EqualTo(255));
        }

        [Test]
        public void ModifyStructFields_ByReference_UpdatesCorrectly()
        {
            // Arrange
            var point = new Point(5, 10);
            var color = new Color(100, 150, 200);
            var index = container.Add(point, color);

            // Act
            ref var pointRef = ref container.GetT1(index);
            ref var colorRef = ref container.GetT2(index);
            
            pointRef.X = 15;
            pointRef.Y = 25;
            colorRef.R = 50;
            colorRef.G = 75;

            // Assert
            var updatedPoint = container.GetT1(index);
            var updatedColor = container.GetT2(index);
            
            Assert.That(updatedPoint.X, Is.EqualTo(15));
            Assert.That(updatedPoint.Y, Is.EqualTo(25));
            Assert.That(updatedColor.R, Is.EqualTo(50));
            Assert.That(updatedColor.G, Is.EqualTo(75));
            Assert.That(updatedColor.B, Is.EqualTo(200)); // Should remain unchanged
            Assert.That(updatedColor.A, Is.EqualTo(255)); // Should remain unchanged
        }

        [Test]
        public void MultipleCustomStructPairs_AllStoredCorrectly()
        {
            // Arrange & Act
            var pairs = new[]
            {
                (new Point(1, 2), new Color(10, 20, 30)),
                (new Point(3, 4), new Color(40, 50, 60)),
                (new Point(5, 6), new Color(70, 80, 90)),
                (new Point(7, 8), new Color(100, 110, 120))
            };

            var indices = new int[pairs.Length];
            for (int i = 0; i < pairs.Length; i++)
            {
                indices[i] = container.Add(pairs[i].Item1, pairs[i].Item2);
            }

            // Assert
            for (int i = 0; i < pairs.Length; i++)
            {
                var retrievedPoint = container.GetT1(indices[i]);
                var retrievedColor = container.GetT2(indices[i]);
                
                Assert.That(retrievedPoint.Equals(pairs[i].Item1), Is.True, $"Point at index {i} should match");
                Assert.That(retrievedColor.R, Is.EqualTo(pairs[i].Item2.R), $"Color R at index {i} should match");
                Assert.That(retrievedColor.G, Is.EqualTo(pairs[i].Item2.G), $"Color G at index {i} should match");
                Assert.That(retrievedColor.B, Is.EqualTo(pairs[i].Item2.B), $"Color B at index {i} should match");
            }
        }
    }
}
