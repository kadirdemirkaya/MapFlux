using MapFlux;
using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;
using MapFlux.Unit.Test.Profiles;
using System.Collections.Generic;
using System.Reflection;
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
        public void ReverseMap_ShouldNotOverwriteExplicitMap_WhenExplicitMapIsRegisteredFirst()
        {
            // Arrange
            _mapper.CreateMap<ExplicitBeforeReverseMapProfile>();
            var destination = new PrecedenceDestination { Name = "n", Full = "f" };

            // Act
            var result = _mapper.Map<PrecedenceDestination, PrecedenceSource>(destination);

            // Assert
            Assert.Equal("f", result.Name);
        }

        [Fact]
        public void ReverseMap_ShouldNotOverwriteExplicitMap_WhenExplicitMapIsRegisteredLast()
        {
            // Arrange
            _mapper.CreateMap<ReverseMapBeforeExplicitProfile>();
            var destination = new PrecedenceDestination { Name = "n", Full = "f" };

            // Act
            var result = _mapper.Map<PrecedenceDestination, PrecedenceSource>(destination);

            // Assert
            Assert.Equal("f", result.Name);
        }

        [Fact]
        public void CreateMap_BetweenTwoExplicitMaps_LaterOneWins_NameThenFullOrder()
        {
            // Arrange
            _mapper.CreateMap<ExplicitMapNameThenFullProfile>();
            var destination = new PrecedenceDestination { Name = "n", Full = "f" };

            // Act
            var result = _mapper.Map<PrecedenceDestination, PrecedenceSource>(destination);

            // Assert
            Assert.Equal("f", result.Name);
        }

        [Fact]
        public void CreateMap_BetweenTwoExplicitMaps_LaterOneWins_FullThenNameOrder()
        {
            // Arrange
            _mapper.CreateMap<ExplicitMapFullThenNameProfile>();
            var destination = new PrecedenceDestination { Name = "n", Full = "f" };

            // Act
            var result = _mapper.Map<PrecedenceDestination, PrecedenceSource>(destination);

            // Assert
            Assert.Equal("n", result.Name);
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
        public void ForMember_NullSubstituteWithoutMapFrom_ShouldUseConventionMatchedSource()
        {
            // Arrange
            _mapper.CreateMap<NullSubstituteConventionProfile>();
            var source = new NullSubstituteSource { Id = 1, Name = null };

            // Act
            var result = _mapper.Map<NullSubstituteSource, NullSubstituteTarget>(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Unknown", result.Name);
        }

        [Fact]
        public void ForMember_NullSubstituteWithoutMapFromOrConventionMatch_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => _mapper.CreateMap<NullSubstituteNoConventionProfile>());
            Assert.Contains("Nickname", ex.Message);
            Assert.Contains("MapFrom", ex.Message);
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
        public void Map_MissingNestedMap_ShouldThrowInvalidOperationExceptionNamingTheMember()
        {
            // Arrange
            _mapper.CreateMap<NestedMapMissingProfile>();
            var source = new ParentSource { Title = "Parent", Child = new ChildSource { Note = "Note" } };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<ParentSource, ParentTarget>(source));
            Assert.Contains("ParentTarget.Child", exception.Message);
            Assert.Contains("ChildSource", exception.Message);
            Assert.Contains("ChildTarget", exception.Message);
        }

        [Fact]
        public void AssertConfigurationIsValid_ShouldNotDetectMissingNestedMap()
        {
            // Arrange
            _mapper.CreateMap<NestedMapMissingProfile>();

            // Act & Assert - default validation only checks unmapped property names, not type compatibility
            _mapper.AssertConfigurationIsValid();
        }

        [Fact]
        public void AssertConfigurationIsValid_Strict_ShouldThrowOnMissingNestedMap()
        {
            // Arrange
            _mapper.CreateMap<NestedMapMissingProfile>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.AssertConfigurationIsValid(true));
            Assert.Contains("ParentTarget.Child", exception.Message);
            Assert.Contains("ChildSource", exception.Message);
            Assert.Contains("ChildTarget", exception.Message);
        }

        [Fact]
        public void AssertConfigurationIsValid_Strict_ShouldThrowOnMissingElementMap()
        {
            // Arrange
            _mapper.CreateMap<CollectionMismatchProfile>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.AssertConfigurationIsValid(true));
            Assert.Contains("CollectionMismatchTarget.Elements", exception.Message);
            Assert.Contains("ElementSource", exception.Message);
            Assert.Contains("ElementTarget", exception.Message);
        }

        [Fact]
        public void AssertConfigurationIsValid_Strict_ShouldPassWhenNestedAndElementMapsAreRegistered()
        {
            // Arrange
            _mapper.CreateMap<NestedMapCompleteProfile>();
            _mapper.CreateMap<CollectionShapeProfile>();

            // Act & Assert - Should not throw
            _mapper.AssertConfigurationIsValid(true);
        }

        [Fact]
        public void AssertConfigurationIsValid_StrictFalse_ShouldBehaveLikeDefaultOverload()
        {
            // Arrange
            _mapper.CreateMap<NestedMapMissingProfile>();

            // Act & Assert - Should not throw, same as the parameterless overload
            _mapper.AssertConfigurationIsValid(false);
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

        [Fact]
        public void Map_ArrayProperties_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<CollectionShapeProfile>();
            var source = new CollectionShapeSource
            {
                ArrayItems = new[] { new ElementSource { Id = 1, Name = "First" } },
                ArrayFromListItems = new List<ElementSource> { new ElementSource { Id = 2, Name = "Second" } }
            };

            // Act
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Single(result.ArrayItems);
            Assert.Equal(1, result.ArrayItems[0].ElementId);
            Assert.Equal("First", result.ArrayItems[0].ElementName);
            Assert.Single(result.ArrayFromListItems);
            Assert.Equal(2, result.ArrayFromListItems[0].ElementId);
        }

        [Fact]
        public void Map_CollectionInterfaceProperties_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<CollectionShapeProfile>();
            var source = new CollectionShapeSource
            {
                ListItems = new List<ElementSource> { new ElementSource { Id = 1, Name = "List" } },
                EnumerableItems = new List<ElementSource> { new ElementSource { Id = 2, Name = "Enumerable" } },
                CollectionItems = new List<ElementSource> { new ElementSource { Id = 3, Name = "Collection" } },
                ListInterfaceItems = new[] { new ElementSource { Id = 4, Name = "ListInterface" } },
                ReadOnlyListItems = new List<ElementSource> { new ElementSource { Id = 5, Name = "ReadOnlyList" } },
                ReadOnlyCollectionItems = new List<ElementSource> { new ElementSource { Id = 6, Name = "ReadOnlyCollection" } }
            };

            // Act
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Equal(1, Assert.Single(result.ListItems).ElementId);
            Assert.Equal("List", result.ListItems[0].ElementName);
            Assert.Equal(2, Assert.Single(result.EnumerableItems).ElementId);
            Assert.Equal(3, Assert.Single(result.CollectionItems).ElementId);
            Assert.Equal(4, Assert.Single(result.ListInterfaceItems).ElementId);
            Assert.Equal(5, Assert.Single(result.ReadOnlyListItems).ElementId);
            Assert.Equal(6, Assert.Single(result.ReadOnlyCollectionItems).ElementId);
        }

        [Fact]
        public void Map_AssignableElementCollectionProperty_ShouldCopyIntoDestinationShape()
        {
            // Arrange
            _mapper.CreateMap<CollectionShapeProfile>();
            var source = new CollectionShapeSource { Names = new[] { "First", "Second" } };

            // Act
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Equal(new List<string> { "First", "Second" }, result.Names);
        }

        [Fact]
        public void Map_StringProperty_ShouldNotBeTreatedAsCollection()
        {
            // Arrange
            _mapper.CreateMap<CollectionShapeProfile>();
            var source = new CollectionShapeSource { Text = "Plain" };

            // Act
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Equal("Plain", result.Text);
        }

        [Fact]
        public void Map_NullCollectionProperty_ShouldStayNull()
        {
            // Arrange
            _mapper.CreateMap<CollectionShapeProfile>();
            var source = new CollectionShapeSource { NullItems = null };

            // Act
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Null(result.NullItems);
        }

        [Fact]
        public void Map_ArrayPropertyWithNullElement_ShouldKeepNullInResult()
        {
            // Arrange
            _mapper.CreateMap<CollectionShapeProfile>();
            var source = new CollectionShapeSource
            {
                ArrayItems = new[] { new ElementSource { Id = 1, Name = "First" }, null }
            };

            // Act
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Equal(2, result.ArrayItems.Length);
            Assert.Equal(1, result.ArrayItems[0].ElementId);
            Assert.Null(result.ArrayItems[1]);
        }

        [Fact]
        public void Map_CollectionPropertyWithoutElementMapping_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<CollectionMismatchProfile>();
            var source = new CollectionMismatchSource
            {
                Elements = new List<ElementSource> { new ElementSource { Id = 1, Name = "First" } }
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<CollectionMismatchSource, CollectionMismatchTarget>(source));
            Assert.Contains("CollectionMismatchTarget.Elements", exception.Message);
            Assert.Contains("ElementSource", exception.Message);
            Assert.Contains("ElementTarget", exception.Message);
        }

        [Fact]
        public void Map_TopLevelArrayToArray_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var source = new[]
            {
                new ElementSource { Id = 1, Name = "First" },
                new ElementSource { Id = 2, Name = "Second" }
            };

            // Act
            var result = _mapper.Map<ElementSource[], ElementTarget[]>(source);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(1, result[0].ElementId);
            Assert.Equal("Second", result[1].ElementName);
        }

        [Fact]
        public void Map_TopLevelEnumerableToList_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            IEnumerable<ElementSource> source = new List<ElementSource>
            {
                new ElementSource { Id = 7, Name = "Seventh" }
            };

            // Act
            var result = _mapper.Map<IEnumerable<ElementSource>, List<ElementTarget>>(source);

            // Assert
            Assert.Equal(7, Assert.Single(result).ElementId);
        }

        [Fact]
        public void Map_TopLevelListToReadOnlyList_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var source = new List<ElementSource> { new ElementSource { Id = 8, Name = "Eighth" } };

            // Act
            var result = _mapper.Map<List<ElementSource>, IReadOnlyList<ElementTarget>>(source);

            // Assert
            Assert.Equal(8, Assert.Single(result).ElementId);
            Assert.Equal("Eighth", result[0].ElementName);
        }

        [Fact]
        public void Map_TopLevelListToArray_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var source = new List<ElementSource> { new ElementSource { Id = 9, Name = "Ninth" } };

            // Act
            var result = _mapper.Map<List<ElementSource>, ElementTarget[]>(source);

            // Assert
            Assert.Equal(9, Assert.Single(result).ElementId);
        }

        [Fact]
        public void Map_TopLevelNullArray_ShouldReturnNull()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            ElementSource[] source = null;

            // Act
            var result = _mapper.Map<ElementSource[], ElementTarget[]>(source);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_TopLevelArrayWithNullElement_ShouldKeepNullInResult()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var source = new[] { new ElementSource { Id = 1, Name = "First" }, null };

            // Act
            var result = _mapper.Map<ElementSource[], ElementTarget[]>(source);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(1, result[0].ElementId);
            Assert.Null(result[1]);
        }

        [Fact]
        public void Map_TopLevelAssignableElements_ShouldCopyIntoDestinationShape()
        {
            // Arrange
            var source = new[] { "First", "Second" };

            // Act
            var result = _mapper.Map<string[], List<string>>(source);

            // Assert
            Assert.Equal(new List<string> { "First", "Second" }, result);
        }

        [Fact]
        public void Map_TopLevelDirectlyAssignableCollection_ShouldReturnSourceInstance()
        {
            // Arrange
            var source = new List<string> { "First" };

            // Act
            var result = _mapper.Map<List<string>, IEnumerable<string>>(source);

            // Assert
            Assert.Same(source, result);
        }

        [Fact]
        public void Map_TopLevelCollectionWithoutElementMapping_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var source = new List<ElementSource> { new ElementSource { Id = 1, Name = "First" } };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<List<ElementSource>, List<SimpleTarget>>(source));
            Assert.Contains("ElementSource", exception.Message);
            Assert.Contains("SimpleTarget", exception.Message);
        }

        [Fact]
        public void CreateMap_DestinationWithoutParameterlessConstructor_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.CreateMap<NoParameterlessConstructorProfile>());
            Assert.Contains(nameof(NoParameterlessConstructorTarget), exception.Message);
        }

        [Fact]
        public void CreateMap_DestinationWithoutParameterlessConstructor_ShouldNotPoisonRetryOrValidPair()
        {
            // Arrange
            Assert.Throws<InvalidOperationException>(() => _mapper.CreateMap<NoParameterlessConstructorProfile>());

            // Act
            var retryException = Assert.Throws<InvalidOperationException>(
                () => _mapper.CreateMap<NoParameterlessConstructorProfile>());
            _mapper.CreateMap<SimpleProfile>();
            var result = _mapper.Map<SimpleSource, SimpleTarget>(new SimpleSource { Name = "A", Age = 1 });

            // Assert
            Assert.Contains(nameof(NoParameterlessConstructorTarget), retryException.Message);
            Assert.Equal("A", result.Name);
            Assert.Equal(1, result.Age);
        }

        [Fact]
        public void Map_SelfReferencingSource_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<CycleNodeProfile>();
            var node = new CycleNodeSource { Id = 1 };
            node.Next = node;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<CycleNodeSource, CycleNodeTarget>(node));
            Assert.Contains(nameof(CycleNodeSource), exception.Message);
            Assert.Contains(nameof(CycleNodeTarget), exception.Message);
            Assert.Contains("maximum depth of 32", exception.Message);
            Assert.Contains("cyclic", exception.Message);
        }

        [Fact]
        public void Map_MutuallyReferencingSources_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<CycleNodeProfile>();
            var first = new CycleNodeSource { Id = 1 };
            var second = new CycleNodeSource { Id = 2, Next = first };
            first.Next = second;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<CycleNodeSource, CycleNodeTarget>(first));
            Assert.Contains("maximum depth of 32", exception.Message);
        }

        [Fact]
        public void Map_CyclicCollectionMember_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<CycleNodeProfile>();
            var node = new CycleNodeSource { Id = 1 };
            node.Children = new List<CycleNodeSource> { node };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<CycleNodeSource, CycleNodeTarget>(node));
            Assert.Contains("maximum depth of 32", exception.Message);
        }

        [Fact]
        public void Map_AcyclicGraphWithinMaxDepth_ShouldMapEveryLevel()
        {
            // Arrange
            _mapper.CreateMap<CycleNodeProfile>();
            var head = BuildChain(20);

            // Act
            var result = _mapper.Map<CycleNodeSource, CycleNodeTarget>(head);

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
        public void Map_AfterCycleFailure_ShouldStillMapAcyclicGraph()
        {
            // Arrange
            _mapper.CreateMap<CycleNodeProfile>();
            var node = new CycleNodeSource { Id = 1 };
            node.Next = node;
            Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<CycleNodeSource, CycleNodeTarget>(node));

            // Act
            var result = _mapper.Map<CycleNodeSource, CycleNodeTarget>(BuildChain(20));

            // Assert
            Assert.Equal(0, result.Id);
            Assert.Equal(19, LastId(result));
        }

        [Fact]
        public void MaxDepth_LoweredBelowGraphDepth_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<CycleNodeProfile>();
            _mapper.MaxDepth = 2;

            // Act
            var mapped = _mapper.Map<CycleNodeSource, CycleNodeTarget>(BuildChain(2));
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<CycleNodeSource, CycleNodeTarget>(BuildChain(3)));

            // Assert
            Assert.Equal(1, LastId(mapped));
            Assert.Contains("maximum depth of 2", exception.Message);
        }

        [Fact]
        public void MaxDepth_RaisedAboveDefault_ShouldMapDeeperGraph()
        {
            // Arrange
            _mapper.CreateMap<CycleNodeProfile>();
            _mapper.MaxDepth = 120;

            // Act
            var result = _mapper.Map<CycleNodeSource, CycleNodeTarget>(BuildChain(100));

            // Assert
            Assert.Equal(99, LastId(result));
        }

        [Fact]
        public void MaxDepth_DefaultAndInvalidValues()
        {
            // Act & Assert
            Assert.Equal(32, _mapper.MaxDepth);
            Assert.Throws<ArgumentOutOfRangeException>(() => _mapper.MaxDepth = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => _mapper.MaxDepth = -1);
            Assert.Equal(32, _mapper.MaxDepth);
        }

        [Fact]
        public void Map_NestedMapRegisteredAfterOuterMap_ShouldMapTheNestedMember()
        {
            // Arrange
            _mapper.CreateMap<NestedMapMissingProfile>();
            _mapper.CreateMap<NestedChildProfile>();
            var source = new ParentSource { Title = "Parent", Child = new ChildSource { Note = "Note" } };

            // Act
            var result = _mapper.Map<ParentSource, ParentTarget>(source);

            // Assert
            Assert.Equal("Parent", result.Title);
            Assert.NotNull(result.Child);
            Assert.Equal("Note", result.Child.Note);
        }

        [Fact]
        public void Map_NestedMapRegisteredAfterAFailedMapCall_ShouldMapTheNestedMember()
        {
            // Arrange
            _mapper.CreateMap<NestedMapMissingProfile>();
            var source = new ParentSource { Title = "Parent", Child = new ChildSource { Note = "Note" } };

            // Act
            Assert.Throws<InvalidOperationException>(() => _mapper.Map<ParentSource, ParentTarget>(source));
            _mapper.CreateMap<NestedChildProfile>();
            var result = _mapper.Map<ParentSource, ParentTarget>(source);

            // Assert
            Assert.NotNull(result.Child);
            Assert.Equal("Note", result.Child.Note);
        }

        [Fact]
        public void Map_ElementMapRegisteredAfterOuterMap_ShouldMapTheCollectionMember()
        {
            // Arrange
            _mapper.CreateMap<CollectionOuterProfile>();
            _mapper.CreateMap<ElementProfile>();
            var source = new CollectionShapeSource
            {
                ListItems = new List<ElementSource> { new ElementSource { Id = 1, Name = "First" } }
            };

            // Act
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Equal(1, Assert.Single(result.ListItems).ElementId);
            Assert.Equal("First", result.ListItems[0].ElementName);
        }

        [Fact]
        public void Map_ElementMapRegisteredAfterAFailedMapCall_ShouldMapTheCollectionMember()
        {
            // Arrange
            _mapper.CreateMap<CollectionOuterProfile>();
            var source = new CollectionShapeSource
            {
                ListItems = new List<ElementSource> { new ElementSource { Id = 2, Name = "Second" } }
            };

            // Act
            Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source));
            _mapper.CreateMap<ElementProfile>();
            var result = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(source);

            // Assert
            Assert.Equal(2, Assert.Single(result.ListItems).ElementId);
            Assert.Equal("Second", result.ListItems[0].ElementName);
        }

        [Fact]
        public void Map_DerivedMemberValue_ShouldUseTheMapOfItsRuntimeType()
        {
            // Arrange
            _mapper.CreateMap<RuntimeTypeProfile>();
            var source = new RuntimeTypeSource
            {
                Title = "Title",
                Detail = new RuntimeTypeFirstDetailSource { Note = "Note", Extra = "Extra" }
            };

            // Act
            var result = _mapper.Map<RuntimeTypeSource, RuntimeTypeTarget>(source);

            // Assert
            Assert.Equal("Title", result.Title);
            Assert.Equal("Note", result.Detail.Note);
            Assert.Equal("Extra", result.Detail.Extra);
        }

        [Fact]
        public void Map_MemberValuesOfTwoRuntimeTypes_ShouldMapEachWithItsOwnMap()
        {
            // Arrange
            _mapper.CreateMap<RuntimeTypeProfile>();
            var first = new RuntimeTypeSource
            {
                Title = "First",
                Detail = new RuntimeTypeFirstDetailSource { Note = "FirstNote", Extra = "FirstExtra" }
            };
            var second = new RuntimeTypeSource
            {
                Title = "Second",
                Detail = new RuntimeTypeSecondDetailSource { Note = "SecondNote", Marker = "SecondMarker" }
            };

            // Act
            var firstResult = _mapper.Map<RuntimeTypeSource, RuntimeTypeTarget>(first);
            var secondResult = _mapper.Map<RuntimeTypeSource, RuntimeTypeTarget>(second);
            var firstAgain = _mapper.Map<RuntimeTypeSource, RuntimeTypeTarget>(first);

            // Assert
            Assert.Equal("FirstNote", firstResult.Detail.Note);
            Assert.Equal("FirstExtra", firstResult.Detail.Extra);
            Assert.Equal("SecondNote", secondResult.Detail.Note);
            Assert.Equal("SecondMarker", secondResult.Detail.Extra);
            Assert.Equal("FirstNote", firstAgain.Detail.Note);
            Assert.Equal("FirstExtra", firstAgain.Detail.Extra);
        }

        [Fact]
        public void Map_NullableMemberWithNullSubstitute_ShouldUseTheSubstitute()
        {
            // Arrange
            _mapper.CreateMap<NullSubstituteNullableProfile>();

            // Act
            var withValue = _mapper.Map<ConversionSource, ConversionTarget>(
                new ConversionSource { OptionalCount = 7 });
            var withoutValue = _mapper.Map<ConversionSource, ConversionTarget>(
                new ConversionSource { OptionalCount = null });

            // Assert
            Assert.Equal(7L, withValue.Score);
            Assert.Equal(42L, withoutValue.Score);
        }

        [Fact]
        public void Map_PrivateSetterMember_ShouldStillBeMapped()
        {
            // Arrange
            _mapper.CreateMap<PrivateSetterProfile>();

            // Act
            var result = _mapper.Map<Source, PrivateSetterTarget>(new Source { Id = 3, Name = "Private" });

            // Assert
            Assert.Equal(3, result.Id);
            Assert.Equal("Private", result.Name);
        }

        [Fact]
        public void Map_CollectionMemberMappedTwice_ShouldBuildEachResultIndependently()
        {
            // Arrange
            _mapper.CreateMap<CollectionShapeProfile>();
            var first = new CollectionShapeSource
            {
                ListItems = new List<ElementSource> { new ElementSource { Id = 1, Name = "First" } }
            };
            var second = new CollectionShapeSource
            {
                ListItems = new List<ElementSource>
                {
                    new ElementSource { Id = 2, Name = "Second" },
                    new ElementSource { Id = 3, Name = "Third" }
                }
            };

            // Act
            var firstResult = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(first);
            var secondResult = _mapper.Map<CollectionShapeSource, CollectionShapeTarget>(second);

            // Assert
            Assert.Equal(1, Assert.Single(firstResult.ListItems).ElementId);
            Assert.Equal(2, secondResult.ListItems.Count);
            Assert.Equal(2, secondResult.ListItems[0].ElementId);
            Assert.Equal(3, secondResult.ListItems[1].ElementId);
            Assert.NotSame(firstResult.ListItems, secondResult.ListItems);
        }

        [Fact]
        public void Map_TopLevelLazyEnumerableToArray_ShouldMapElements()
        {
            // Arrange
            _mapper.CreateMap<ElementProfile>();
            var items = new List<ElementSource>
            {
                new ElementSource { Id = 1, Name = "First" },
                new ElementSource { Id = 2, Name = "Second" }
            };

            // Act
            var result = _mapper.Map<IEnumerable<ElementSource>, ElementTarget[]>(items.Where(item => item.Id > 0));

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(1, result[0].ElementId);
            Assert.Equal("First", result[0].ElementName);
            Assert.Equal(2, result[1].ElementId);
            Assert.Equal("Second", result[1].ElementName);
        }

        [Fact]
        public void MapInto_ExistingDestination_ShouldFillSameInstance()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource { Id = 7, Name = "Mapped" };
            var destination = new MapIntoTarget { Id = 1, Name = "Old" };

            // Act
            var result = _mapper.Map<MapIntoSource, MapIntoTarget>(source, destination);

            // Assert
            Assert.Same(destination, result);
            Assert.Equal(7, destination.Id);
            Assert.Equal("Mapped", destination.Name);
        }

        [Fact]
        public void MapInto_IgnoredMember_ShouldKeepExistingValue()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource { Id = 7, Name = "Mapped", Secret = "FromSource" };
            var destination = new MapIntoTarget { Secret = "Kept" };

            // Act
            _mapper.Map<MapIntoSource, MapIntoTarget>(source, destination);

            // Assert
            Assert.Equal("Kept", destination.Secret);
            Assert.Equal(7, destination.Id);
        }

        [Fact]
        public void MapInto_MemberWithoutSourceCounterpart_ShouldKeepExistingValue()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource { Id = 7, Name = "Mapped" };
            var destination = new MapIntoTarget { Untouched = "Kept" };

            // Act
            _mapper.Map<MapIntoSource, MapIntoTarget>(source, destination);

            // Assert
            Assert.Equal("Kept", destination.Untouched);
        }

        [Fact]
        public void MapInto_NullSourceMembers_ShouldKeepExistingValues()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource { Id = 7, Name = null, Child = null, Items = null };
            var existingItems = new List<ElementTarget> { new ElementTarget { ElementId = 99 } };
            var existingChild = new MapIntoChildTarget { Note = "KeptNote" };
            var destination = new MapIntoTarget
            {
                Name = "KeptName",
                Child = existingChild,
                Items = existingItems
            };

            // Act
            _mapper.Map<MapIntoSource, MapIntoTarget>(source, destination);

            // Assert
            Assert.Equal(7, destination.Id);
            Assert.Equal("KeptName", destination.Name);
            Assert.Same(existingChild, destination.Child);
            Assert.Same(existingItems, destination.Items);
        }

        [Fact]
        public void MapInto_NestedMember_ShouldReplaceExistingInstance()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource
            {
                Id = 7,
                Child = new MapIntoChildSource { Note = "New" }
            };
            var existingChild = new MapIntoChildTarget { Note = "Old", Version = 4 };
            var destination = new MapIntoTarget { Child = existingChild };

            // Act
            _mapper.Map<MapIntoSource, MapIntoTarget>(source, destination);

            // Assert
            Assert.NotSame(existingChild, destination.Child);
            Assert.Equal("New", destination.Child.Note);
            Assert.Equal(0, destination.Child.Version);
            Assert.Equal(4, existingChild.Version);
        }

        [Fact]
        public void MapInto_CollectionMember_ShouldReplaceExistingCollection()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource
            {
                Id = 7,
                Items = new List<ElementSource>
                {
                    new ElementSource { Id = 1, Name = "First" },
                    new ElementSource { Id = 2, Name = "Second" }
                }
            };
            var existingItems = new List<ElementTarget> { new ElementTarget { ElementId = 99 } };
            var destination = new MapIntoTarget { Items = existingItems };

            // Act
            _mapper.Map<MapIntoSource, MapIntoTarget>(source, destination);

            // Assert
            Assert.NotSame(existingItems, destination.Items);
            Assert.Equal(2, destination.Items.Count);
            Assert.Equal(1, destination.Items[0].ElementId);
            Assert.Equal("Second", destination.Items[1].ElementName);
            Assert.Equal(99, Assert.Single(existingItems).ElementId);
        }

        [Fact]
        public void MapInto_ReverseMappedPair_ShouldFillExistingDestination()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoChildTarget { Note = "Reversed", Version = 3 };
            var destination = new MapIntoChildSource { Note = "Old", Version = 1 };

            // Act
            var result = _mapper.Map<MapIntoChildTarget, MapIntoChildSource>(source, destination);

            // Assert
            Assert.Same(destination, result);
            Assert.Equal("Reversed", destination.Note);
            Assert.Equal(3, destination.Version);
        }

        [Fact]
        public void MapInto_ThroughExistingDestinationMapperInterface_ShouldFillDestination()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            IExistingDestinationMapper mapper = _mapper;
            var destination = new MapIntoTarget { Untouched = "Kept" };

            // Act
            var result = mapper.Map(new MapIntoSource { Id = 7, Name = "Mapped" }, destination);

            // Assert
            Assert.Same(destination, result);
            Assert.Equal(7, result.Id);
            Assert.Equal("Mapped", result.Name);
            Assert.Equal("Kept", result.Untouched);
        }

        [Fact]
        public void MapInto_NullSource_ShouldReturnDestinationUnchanged()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var destination = new MapIntoTarget { Id = 5, Name = "Kept" };

            // Act
            var result = _mapper.Map<MapIntoSource, MapIntoTarget>(null, destination);

            // Assert
            Assert.Same(destination, result);
            Assert.Equal(5, destination.Id);
            Assert.Equal("Kept", destination.Name);
        }

        [Fact]
        public void MapInto_NullDestination_ShouldThrowArgumentNullException()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource { Id = 7 };

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => _mapper.Map<MapIntoSource, MapIntoTarget>(source, null));

            // Assert
            Assert.Equal("destination", exception.ParamName);
            Assert.Contains("requires a destination instance", exception.Message);
        }

        [Fact]
        public void MapInto_UndefinedMapping_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var source = new MapIntoSource { Id = 7 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<MapIntoSource, MapIntoTarget>(source, new MapIntoTarget()));

            // Assert
            Assert.Equal("Mapping from MapIntoSource to MapIntoTarget is not defined.", exception.Message);
        }

        [Fact]
        public void MapInto_CollectionDestination_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new List<ElementSource> { new ElementSource { Id = 1, Name = "First" } };
            var destination = new List<ElementTarget>();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<List<ElementSource>, List<ElementTarget>>(source, destination));

            // Assert
            Assert.Contains("Mapping onto an existing List<ElementTarget> is not supported", exception.Message);
            Assert.Empty(destination);
        }

        [Fact]
        public void MapInto_AfterSingleArgumentMap_ShouldKeepBuildingNewInstances()
        {
            // Arrange
            _mapper.CreateMap<MapIntoProfile>();
            var source = new MapIntoSource { Id = 7, Name = "Mapped", Secret = "FromSource" };
            var destination = new MapIntoTarget { Untouched = "Kept" };

            // Act
            _mapper.Map<MapIntoSource, MapIntoTarget>(source, destination);
            var created = _mapper.Map<MapIntoSource, MapIntoTarget>(source);

            // Assert
            Assert.NotSame(destination, created);
            Assert.Equal(7, created.Id);
            Assert.Equal("Mapped", created.Name);
            Assert.Null(created.Untouched);
            Assert.Null(created.Secret);
        }

        [Fact]
        public void CreateMapsFromAssemblies_ShouldRegisterEveryConcreteProfileInAssembly()
        {
            // Arrange
            _mapper.CreateMapsFromAssemblies(typeof(MapFlux.Console.Test.Profiles.UserProfile).Assembly);

            // Act
            var result = _mapper.Map<MapFlux.Console.Test.Models.Customer, MapFlux.Console.Test.Dtos.CustomerDto>(
                new MapFlux.Console.Test.Models.Customer { Id = 1, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" });

            // Assert
            Assert.Equal(1, result.Id);
            Assert.Equal("Ada", result.FirstName);
            Assert.Equal("Lovelace", result.LastName);
            Assert.Equal("ada@example.com", result.Email);
        }

        [Fact]
        public void CreateMapsFromAssemblies_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var assembly = typeof(MapFlux.Console.Test.Profiles.UserProfile).Assembly;
            _mapper.CreateMapsFromAssemblies(assembly);

            // Act
            var exception = Record.Exception(() => _mapper.CreateMapsFromAssemblies(assembly));

            // Assert
            Assert.Null(exception);
            var result = _mapper.Map<MapFlux.Console.Test.Models.Customer, MapFlux.Console.Test.Dtos.CustomerDto>(
                new MapFlux.Console.Test.Models.Customer { Id = 2, FirstName = "Grace", LastName = "Hopper", Email = "grace@example.com" });
            Assert.Equal("Grace", result.FirstName);
        }

        [Fact]
        public void CreateMapsFromAssemblies_NoAssemblies_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _mapper.CreateMapsFromAssemblies());
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

        private static int LastId(CycleNodeTarget head)
        {
            var current = head;

            while (current.Next != null)
            {
                current = current.Next;
            }

            return current.Id;
        }
    }
}
