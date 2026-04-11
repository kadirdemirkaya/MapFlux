using MapFlux;
using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;
using MapFlux.Unit.Test.Profiles;
using System.Collections.Generic;
using Xunit;

namespace MapFlux.Unit.Test
{
    public class MapperTests
    {
        private readonly Mapper _mapper;

        public MapperTests()
        {
            _mapper = new Mapper();
        }

        [Fact]
        public void CreateMap_ShouldRegisterProfile()
        {
            // Arrange
            _mapper.CreateMap<TestProfile>();

            // Act
            var source = new Source { Id = 1, Name = "Test" };
            var result = _mapper.Map<Source, Target>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TargetId);
            Assert.Equal("Test", result.TargetName);
        }

        [Fact]
        public void Map_UndefinedMapping_ShouldThrowException()
        {
            // Arrange
            var source = new Source { Id = 1, Name = "Test" };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _mapper.Map<Source, Target>(source));
        }

        [Fact]
        public void Map_ComplexMapping_ShouldWork()
        {
            // Arrange
            _mapper.CreateMap<ComplexProfile>();
            var source = new ComplexSource
            {
                Id = 10,
                Items = new List<string> { "Item1", "Item2" },
                Nested = new NestedSource { Value = "NestedValue" }
            };

            // Act
            var result = _mapper.Map<ComplexSource, ComplexTarget>(source);

            // Assert
            Assert.Equal(10, result.Identifier);
            Assert.Equal(2, result.ItemCount);
            Assert.Equal("NestedValue", result.NestedValue);
        }

        [Fact]
        public void Map_ListMapping_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var source = new List<ElementSource>
            {
                new ElementSource { Id = 1, Name = "First" },
                new ElementSource { Id = 2, Name = "Second" }
            };

            // Act
            var result = _mapper.Map<List<ElementSource>, List<ElementTarget>>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].ElementId);
            Assert.Equal("First", result[0].ElementName);
            Assert.Equal(2, result[1].ElementId);
            Assert.Equal("Second", result[1].ElementName);
        }

        [Fact]
        public void ReverseMap_ShouldMapInReverseDirection()
        {
            // Arrange
            _mapper.CreateMap<ReverseProfile>();
            var target = new ReverseTarget { Id = 1, FullName = "John Doe" };

            // Act
            var result = _mapper.Map<ReverseTarget, ReverseSource>(target);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal("John Doe", result.Name);
        }

        [Fact]
        public void ForMember_Ignore_ShouldSkipProperty()
        {
            // Arrange
            _mapper.CreateMap<IgnoreProfile>();
            var source = new IgnoreSource { Id = 1, Name = "Test", Secret = "Hidden" };

            // Act
            var result = _mapper.Map<IgnoreSource, IgnoreTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
            Assert.Null(result.Secret);
        }

        [Fact]
        public void ForMember_NullSubstitute_ShouldUseDefaultValue()
        {
            // Arrange
            _mapper.CreateMap<NullSubstituteProfile>();
            var source = new NullSubstituteSource { Id = 1, Name = null };

            // Act
            var result = _mapper.Map<NullSubstituteSource, NullSubstituteTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Unknown", result.Name);
        }

        [Fact]
        public void AssertConfigurationIsValid_ShouldPassWithValidMappings()
        {
            // Arrange
            _mapper.CreateMap<TestProfile>();

            // Act & Assert - Should not throw
            _mapper.AssertConfigurationIsValid();
        }

        [Fact]
        public void AssertConfigurationIsValid_ShouldThrowOnUnmappedProperties()
        {
            // Arrange
            _mapper.CreateMap<InvalidProfile>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _mapper.AssertConfigurationIsValid());
            Assert.Contains("Unmapped properties found", exception.Message);
            Assert.Contains("UnmappedTarget", exception.Message);
        }

        [Fact]
        public void Map_ConventionBasedMapping_ShouldMatchByName()
        {
            // Arrange
            _mapper.CreateMap<ConventionProfile>();
            var source = new ConventionSource { Id = 1, Name = "Test", Email = "test@test.com" };

            // Act
            var result = _mapper.Map<ConventionSource, ConventionTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
            Assert.Equal("test@test.com", result.Email);
        }
    }
}
