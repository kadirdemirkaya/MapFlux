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

    }
}
