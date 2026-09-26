using MapFlux;
using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;
using System.Collections.Generic;
using Xunit;

namespace MapFlux.Unit.Test
{
    public class ModelMapperTests
    {
        [Fact]
        public void Map_SimpleProperties_ShouldMapAutomatically()
        {
            // Arrange
            var source = new SimpleSource { Name = "John", Age = 30 };

            // Act
            var result = ModelMapper.Map<SimpleSource, SimpleTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John", result.Name);
            Assert.Equal(30, result.Age);
        }

        [Fact]
        public void Map_PropertyMappingAttribute_ShouldMapToDifferentName()
        {
            // Arrange
            var source = new AttributeSource { RawValue = 123.45 };

            // Act
            var result = ModelMapper.Map<AttributeSource, AttributeTarget>(source);

            // Assert
            Assert.Equal(123.45, result.MappedValue);
        }

        [Fact]
        public void Map_NestedObjects_ShouldMapRecursively()
        {
            // Arrange
            var source = new ParentSource
            {
                Title = "Parent",
                Child = new ChildSource { Note = "Hello" }
            };

            // Act
            var result = ModelMapper.Map<ParentSource, ParentTarget>(source);

            // Assert
            Assert.NotNull(result.Child);
            Assert.Equal("Hello", result.Child.Note);
        }

        [Fact]
        public void Map_NestedTargetWithoutParameterlessConstructor_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var source = new ModelNestedNoCtorSource
            {
                Title = "Parent",
                Child = new ModelNestedNoCtorChildSource { Note = "Hello" }
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => ModelMapper.Map<ModelNestedNoCtorSource, ModelNestedNoCtorTarget>(source));
            Assert.Contains($"{nameof(ModelNestedNoCtorTarget)}.{nameof(ModelNestedNoCtorTarget.Child)}", exception.Message);
            Assert.Contains(nameof(ModelNestedNoCtorChildTarget), exception.Message);
            Assert.Contains("parameterless constructor", exception.Message);
        }

        [Fact]
        public void Map_Collections_ShouldMapElements()
        {
            // Arrange
            var source = new ListSource
            {
                Items = new List<ItemSource>
                {
                    new ItemSource { Key = "K1" },
                    new ItemSource { Key = "K2" }
                }
            };

            // Act
            var result = ModelMapper.Map<ListSource, ListTarget>(source);

            // Assert
            Assert.NotNull(result.Items);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("K1", result.Items[0].Key);
            Assert.Equal("K2", result.Items[1].Key);
        }

        [Fact]
        public void Map_NullSource_ShouldReturnDefault()
        {
            // Arrange
            SimpleSource source = null;

            // Act
            var result = ModelMapper.Map<SimpleSource, SimpleTarget>(source);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_DeeplyNestedObjects_ShouldMapRecursively()
        {
            // Arrange
            var source = new DeepSource
            {
                Level1 = new Level1Source
                {
                    Name = "Level1",
                    Level2 = new Level2Source
                    {
                        Name = "Level2",
                        Level3 = new Level3Source { Name = "Level3" }
                    }
                }
            };

            // Act
            var result = ModelMapper.Map<DeepSource, DeepTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Level1);
            Assert.NotNull(result.Level1.Level2);
            Assert.NotNull(result.Level1.Level2.Level3);
            Assert.Equal("Level1", result.Level1.Name);
            Assert.Equal("Level2", result.Level1.Level2.Name);
            Assert.Equal("Level3", result.Level1.Level2.Level3.Name);
        }

        [Fact]
        public void Map_CaseInsensitivePropertyMatching_ShouldWork()
        {
            // Arrange
            var source = new CaseSource { username = "john", EMAIL = "john@test.com" };

            // Act
            var result = ModelMapper.Map<CaseSource, CaseTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("john", result.UserName);
            Assert.Equal("john@test.com", result.Email);
        }

        [Fact]
        public void Map_MultipleAttributes_ShouldMapCorrectly()
        {
            // Arrange
            var source = new MultiAttributeSource
            {
                Id = 1,
                InternalCode = "INT-001",
                ExternalCode = "EXT-001"
            };

            // Act
            var result = ModelMapper.Map<MultiAttributeSource, MultiAttributeTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("INT-001", result.InternalReference);
            Assert.Equal("EXT-001", result.ExternalReference);
        }

        [Fact]
        public void Map_EmptyCollections_ShouldReturnEmptyList()
        {
            // Arrange
            var source = new ListSource { Items = new List<ItemSource>() };

            // Act
            var result = ModelMapper.Map<ListSource, ListTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.Empty(result.Items);
        }

        [Fact]
        public void Map_NullNestedObject_ShouldLeaveNull()
        {
            // Arrange
            var source = new ParentSource { Title = "Parent", Child = null };

            // Act
            var result = ModelMapper.Map<ParentSource, ParentTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Parent", result.Title);
            Assert.Null(result.Child);
        }

        [Fact]
        public void Map_SelfReferencingSource_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var node = new CycleNodeSource { Id = 1 };
            node.Next = node;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => ModelMapper.Map<CycleNodeSource, CycleNodeTarget>(node));
            Assert.Contains(nameof(CycleNodeSource), exception.Message);
            Assert.Contains(nameof(CycleNodeTarget), exception.Message);
            Assert.Contains("maximum depth of 32", exception.Message);
            Assert.Contains("cyclic", exception.Message);
        }

        [Fact]
        public void Map_CyclicCollectionMember_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var node = new CycleNodeSource { Id = 1 };
            node.Children = new List<CycleNodeSource> { node };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => ModelMapper.Map<CycleNodeSource, CycleNodeTarget>(node));
            Assert.Contains("maximum depth of 32", exception.Message);
        }

        [Fact]
        public void Map_AcyclicGraphWithinMaxDepth_ShouldMapEveryLevelAfterCycleFailure()
        {
            // Arrange
            var cycle = new CycleNodeSource { Id = 1 };
            cycle.Next = cycle;
            Assert.Throws<InvalidOperationException>(
                () => ModelMapper.Map<CycleNodeSource, CycleNodeTarget>(cycle));

            // Act
            var result = ModelMapper.Map<CycleNodeSource, CycleNodeTarget>(BuildChain(20));

            // Assert
            var current = result;
            for (var depth = 0; depth < 20; depth++)
            {
                Assert.NotNull(current);
                Assert.Equal(depth, current.Id);
                current = current.Next;
            }
            Assert.Null(current);
        }

        [Fact]
        public void MaxDepth_LoweredBelowGraphDepth_ShouldThrowInvalidOperationException()
        {
            // Arrange
            Assert.Equal(32, ModelMapper.MaxDepth);

            try
            {
                ModelMapper.MaxDepth = 2;

                // Act
                var mapped = ModelMapper.Map<CycleNodeSource, CycleNodeTarget>(BuildChain(2));
                var exception = Assert.Throws<InvalidOperationException>(
                    () => ModelMapper.Map<CycleNodeSource, CycleNodeTarget>(BuildChain(3)));

                // Assert
                Assert.Equal(1, mapped.Next.Id);
                Assert.Contains("maximum depth of 2", exception.Message);
            }
            finally
            {
                ModelMapper.MaxDepth = 32;
            }
        }

        [Fact]
        public void MaxDepth_InvalidValues_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => ModelMapper.MaxDepth = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => ModelMapper.MaxDepth = -1);
            Assert.Equal(32, ModelMapper.MaxDepth);
        }

        [Fact]
        public void Map_ValueTypeAndStringElements_ShouldCopyElements()
        {
            // Arrange
            var source = new ModelCollectionSource
            {
                Numbers = new List<int> { 1, 2, 3 },
                OptionalNumbers = new List<int?> { 7, null },
                Names = new[] { "first", "second" }
            };

            // Act
            var result = ModelMapper.Map<ModelCollectionSource, ModelCollectionTarget>(source);

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, result.Numbers);
            Assert.Equal(new int?[] { 7, null }, result.OptionalNumbers);
            Assert.Equal(new[] { "first", "second" }, result.Names);
        }

        [Fact]
        public void Map_Arrays_ShouldMapElements()
        {
            // Arrange
            var source = new ModelCollectionSource
            {
                Names = new[] { "a", "b" },
                ArrayItems = new[] { new ItemSource { Key = "A1" }, new ItemSource { Key = "A2" } },
                ListToArrayItems = new List<ItemSource> { new ItemSource { Key = "L1" } },
                NumbersToReadOnlyList = new[] { 4, 5 }
            };

            // Act
            var result = ModelMapper.Map<ModelCollectionSource, ModelCollectionTarget>(source);

            // Assert
            Assert.Equal(new[] { "a", "b" }, result.Names);
            Assert.Equal(2, result.ArrayItems.Length);
            Assert.Equal("A1", result.ArrayItems[0].Key);
            Assert.Equal("A2", result.ArrayItems[1].Key);
            Assert.Single(result.ListToArrayItems);
            Assert.Equal("L1", result.ListToArrayItems[0].Key);
            Assert.Equal(new[] { 4, 5 }, result.NumbersToReadOnlyList);
        }

        [Fact]
        public void Map_Dictionaries_ShouldKeepKeysAndMapClassValues()
        {
            // Arrange
            var source = new ModelCollectionSource
            {
                Counts = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 },
                ItemsByKey = new Dictionary<string, ItemSource>
                {
                    ["first"] = new ItemSource { Key = "D1" }
                }
            };

            // Act
            var result = ModelMapper.Map<ModelCollectionSource, ModelCollectionTarget>(source);

            // Assert
            Assert.Equal(2, result.Counts.Count);
            Assert.Equal(1, result.Counts["one"]);
            Assert.Equal(2, result.Counts["two"]);
            Assert.Single(result.ItemsByKey);
            Assert.Equal("D1", result.ItemsByKey["first"].Key);
        }

        [Fact]
        public void Map_NullCollectionElements_ShouldStayNull()
        {
            // Arrange
            var source = new ModelCollectionSource
            {
                OptionalNumbers = new List<int?> { null, 3 },
                Names = new[] { null, "name" },
                ArrayItems = new[] { null, new ItemSource { Key = "A" } },
                ListToArrayItems = new List<ItemSource> { new ItemSource { Key = "L" }, null }
            };

            // Act
            var result = ModelMapper.Map<ModelCollectionSource, ModelCollectionTarget>(source);

            // Assert
            Assert.Null(result.OptionalNumbers[0]);
            Assert.Equal(3, result.OptionalNumbers[1]);
            Assert.Null(result.Names[0]);
            Assert.Null(result.ArrayItems[0]);
            Assert.Equal("A", result.ArrayItems[1].Key);
            Assert.Equal("L", result.ListToArrayItems[0].Key);
            Assert.Null(result.ListToArrayItems[1]);
        }

        [Fact]
        public void Map_EnumerableSourceToList_ShouldMapElements()
        {
            // Arrange
            var source = new ModelCollectionSource
            {
                EnumerableItems = new List<ItemSource> { new ItemSource { Key = "E1" } }
                    .Where(item => item.Key == "E1")
            };

            // Act
            var result = ModelMapper.Map<ModelCollectionSource, ModelCollectionTarget>(source);

            // Assert
            Assert.Single(result.EnumerableItems);
            Assert.Equal("E1", result.EnumerableItems[0].Key);
        }

        [Fact]
        public void Map_NullCollectionMembers_ShouldLeaveTargetNull()
        {
            // Arrange
            var source = new ModelCollectionSource();

            // Act
            var result = ModelMapper.Map<ModelCollectionSource, ModelCollectionTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Numbers);
            Assert.Null(result.Names);
            Assert.Null(result.ArrayItems);
            Assert.Null(result.EnumerableItems);
            Assert.Null(result.Counts);
            Assert.Null(result.ItemsByKey);
        }

        [Fact]
        public void Map_UnsupportedDestinationCollection_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var source = new ModelUnsupportedCollectionSource { Numbers = new List<int> { 1 } };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => ModelMapper.Map<ModelUnsupportedCollectionSource, ModelUnsupportedCollectionTarget>(source));
            Assert.Contains($"{nameof(ModelUnsupportedCollectionTarget)}.{nameof(ModelUnsupportedCollectionTarget.Numbers)}", exception.Message);
            Assert.Contains("List<Int32>", exception.Message);
            Assert.Contains("String", exception.Message);
        }

        [Fact]
        public void Map_UnmappableCollectionElement_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var source = new ModelUnmappableElementSource { Numbers = new List<int> { 1 } };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => ModelMapper.Map<ModelUnmappableElementSource, ModelUnmappableElementTarget>(source));
            Assert.Contains($"{nameof(ModelUnmappableElementTarget)}.{nameof(ModelUnmappableElementTarget.Numbers)}", exception.Message);
            Assert.Contains("Int32", exception.Message);
            Assert.Contains(nameof(ItemTarget), exception.Message);
        }

        private static CycleNodeSource BuildChain(int length)
        {
            var head = new CycleNodeSource { Id = 0 };
            var current = head;

            for (var index = 1; index < length; index++)
            {
                current.Next = new CycleNodeSource { Id = index };
                current = current.Next;
            }

            return head;
        }
    }
}
