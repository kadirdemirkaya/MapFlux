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
        public void Map_NullSource_ShouldReturnDefault()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            ElementSource source = null;

            // Act
            var result = _mapper.Map<ElementSource, ElementTarget>(source);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_NullList_ShouldReturnNull()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            List<ElementSource> source = null;

            // Act
            var result = _mapper.Map<List<ElementSource>, List<ElementTarget>>(source);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_ListWithNullElement_ShouldKeepNullInResult()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var source = new List<ElementSource>
            {
                new ElementSource { Id = 1, Name = "First" },
                null,
                new ElementSource { Id = 2, Name = "Second" }
            };

            // Act
            var result = _mapper.Map<List<ElementSource>, List<ElementTarget>>(source);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].ElementId);
            Assert.Null(result[1]);
            Assert.Equal(2, result[2].ElementId);
        }

        [Fact]
        public void Map_NestedListPropertyWithNullElement_ShouldKeepNullInResult()
        {
            // Arrange
            _mapper.CreateMap<NullSafetyContainerProfile>();
            var source = new NullSafetyContainerSource
            {
                Elements = new List<ElementSource>
                {
                    new ElementSource { Id = 1, Name = "First" },
                    null
                }
            };

            // Act
            var result = _mapper.Map<NullSafetyContainerSource, NullSafetyContainerTarget>(source);

            // Assert
            Assert.NotNull(result.Elements);
            Assert.Equal(2, result.Elements.Count);
            Assert.Equal(1, result.Elements[0].ElementId);
            Assert.Null(result.Elements[1]);
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
        public void ForMember_ConvertedDestinationBody_ShouldMapMember()
        {
            // Arrange
            _mapper.CreateMap<ForMemberConvertProfile>();
            var source = new Source { Id = 1, Name = "Test" };

            // Act
            var result = _mapper.Map<Source, Target>(source);

            // Assert
            Assert.Equal(1, result.TargetId);
            Assert.Equal("Test", result.TargetName);
        }

        [Fact]
        public void ForMember_NestedDestinationPath_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _mapper.CreateMap<ForMemberNestedPathProfile>());
            Assert.Contains("ForMember", exception.Message);
            Assert.Contains("d.Member", exception.Message);
        }

        [Fact]
        public void ForMember_NestedDestinationPath_ShouldNotRegisterMapping()
        {
            // Arrange
            try
            {
                _mapper.CreateMap<ForMemberNestedPathProfile>();
            }
            catch (ArgumentException)
            {
            }

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _mapper.Map<DeepSource, DeepTarget>(new DeepSource
            {
                Level1 = new Level1Source { Name = "top" }
            }));
        }

        [Fact]
        public void ForMember_NonMemberDestinationBody_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _mapper.CreateMap<ForMemberInvalidBodyProfile>());
            Assert.Contains("ForMember", exception.Message);
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

        [Fact]
        public void Map_NumericMembers_ShouldWidenAndNarrow()
        {
            // Arrange
            _mapper.CreateMap<ConversionProfile>();
            var source = new ConversionSource
            {
                Count = 5,
                Total = 9L,
                Amount = 42,
                Ratio = 1.5f,
                Name = "Test"
            };

            // Act
            var result = _mapper.Map<ConversionSource, ConversionTarget>(source);

            // Assert
            Assert.Equal(5L, result.Count);
            Assert.Equal(9, result.Total);
            Assert.Equal(42m, result.Amount);
            Assert.Equal(1.5d, result.Ratio);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public void Map_NullableMembers_ShouldConvertInBothDirections()
        {
            // Arrange
            _mapper.CreateMap<ConversionProfile>();
            var source = new ConversionSource { OptionalCount = 7, Score = 3, Code = 11 };

            // Act
            var result = _mapper.Map<ConversionSource, ConversionTarget>(source);

            // Assert
            Assert.Equal(7L, result.OptionalCount);
            Assert.Equal(3L, result.Score);
            Assert.Equal(11, result.Code);
        }

        [Fact]
        public void Map_NullNullableMember_ShouldUseDestinationDefault()
        {
            // Arrange
            _mapper.CreateMap<ConversionProfile>();
            var source = new ConversionSource { OptionalCount = null, Count = 4 };

            // Act
            var result = _mapper.Map<ConversionSource, ConversionTarget>(source);

            // Assert
            Assert.Equal(0L, result.OptionalCount);
            Assert.Equal(4L, result.Count);
        }

        [Fact]
        public void Map_EnumMembers_ShouldConvertToAndFromUnderlyingType()
        {
            // Arrange
            _mapper.CreateMap<ConversionProfile>();
            var source = new ConversionSource
            {
                Level = ConversionLevel.High,
                Priority = ConversionLevel.Low,
                Rank = 2
            };

            // Act
            var result = _mapper.Map<ConversionSource, ConversionTarget>(source);

            // Assert
            Assert.Equal(2, result.Level);
            Assert.Equal(1L, result.Priority);
            Assert.Equal(ConversionLevel.High, result.Rank);
        }

        [Fact]
        public void Map_MapFromWithDifferentMemberType_ShouldConvert()
        {
            // Arrange
            _mapper.CreateMap<ConversionMapFromProfile>();
            var source = new ConversionSource { Rank = 6, Count = 3, Priority = ConversionLevel.High };

            // Act
            var result = _mapper.Map<ConversionSource, ConversionTarget>(source);

            // Assert
            Assert.Equal(6L, result.Count);
            Assert.Equal(3m, result.Amount);
            Assert.Equal(2, result.Level);
        }

        [Fact]
        public void Map_UnconvertibleMemberTypes_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<UnconvertibleProfile>();
            var source = new UnconvertibleSource { Count = "12", Name = "Test" };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<UnconvertibleSource, UnconvertibleTarget>(source));
            Assert.Contains("UnconvertibleTarget.Count", exception.Message);
            Assert.Contains("String", exception.Message);
            Assert.Contains("Int32", exception.Message);
        }

        [Fact]
        public void CreateMap_UnconvertibleMemberTypes_ShouldNotThrowAtConfigurationTime()
        {
            // Act
            var configurationException = Record.Exception(() => _mapper.CreateMap<UnconvertibleProfile>());
            var validationException = Record.Exception(() => _mapper.AssertConfigurationIsValid());

            // Assert
            Assert.Null(configurationException);
            Assert.Null(validationException);
        }
    }
}
