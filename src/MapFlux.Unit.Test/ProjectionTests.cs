using System.Collections.Concurrent;
using System.Linq.Expressions;
using MapFlux.Unit.Test.Dtos;
using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Profiles;
using MapFlux.Unit.Test.Projection;

namespace MapFlux.Unit.Test
{
    public class ProjectionTests
    {
        private readonly Mapper _mapper = new();

        private void Configure(Action<IMapperConfigurationExpression> configure)
        {
            configure(new MapperConfigurationExpression(_mapper));
        }

        private void ConfigureCustomerMaps()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionAddress, ProjectionAddressDto>(m => { });
                config.CreateMap<ProjectionLine, ProjectionLineDto>(m => { });
                config.CreateMap<ProjectionCustomer, ProjectionCustomerDto>(m => m
                    .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.Name + " <" + s.Email + ">"))
                    .ForMember(d => d.Email, o => o.NullSubstitute("none"))
                    .ForMember(d => d.Secret, o => o.Ignore()));
            });
        }

        private static ProjectionCustomer FullCustomer() => new()
        {
            Id = 7,
            Name = "Ada",
            Email = "ada@example.com",
            Score = 42,
            Total = 12.5m,
            Status = ProjectionStatus.Closed,
            Secret = "hidden",
            Address = new ProjectionAddress { City = "Izmir", Street = "Kordon" },
            Lines = new List<ProjectionLine>
            {
                new() { Product = "Pen", Quantity = 2 },
                new() { Product = "Ink", Quantity = 1 }
            }
        };

        private static ProjectionCustomer SparseCustomer() => new()
        {
            Id = 8,
            Name = "Bo",
            Email = null,
            Score = null,
            Total = 0m,
            Status = ProjectionStatus.Active,
            Secret = "hidden",
            Address = null,
            Lines = new List<ProjectionLine>()
        };

        [Fact]
        public void GetProjection_ConventionMembers_MatchCaseInsensitively()
        {
            Configure(config => config.CreateMap<Source, ProjectionCaseDto>(m => { }));

            var result = new[] { new Source { Id = 3, Name = "x" } }.AsQueryable()
                .ProjectMapped<Source, ProjectionCaseDto>(_mapper)
                .Single();

            Assert.Equal(3, result.ID);
            Assert.Equal("x", result.NAME);
        }

        [Fact]
        public void GetProjection_FullMap_ContainsOnlyTranslatableNodes()
        {
            ConfigureCustomerMaps();

            var projection = _mapper.GetProjection<ProjectionCustomer, ProjectionCustomerDto>();
            var collector = new NodeCollector();
            collector.Visit(projection);

            var forbidden = new[]
            {
                ExpressionType.Invoke, ExpressionType.Block, ExpressionType.Throw, ExpressionType.Assign,
                ExpressionType.Loop, ExpressionType.Try, ExpressionType.Goto, ExpressionType.Label
            };
            Assert.DoesNotContain(collector.Nodes, node => forbidden.Contains(node.NodeType));

            var calls = collector.Nodes.OfType<MethodCallExpression>().ToList();
            Assert.NotEmpty(calls);
            Assert.All(calls, call =>
            {
                Assert.Equal(typeof(Enumerable), call.Method.DeclaringType);
                Assert.Contains(call.Method.Name, new[] { "Select", "ToList", "ToArray" });
            });

            Assert.All(collector.Nodes.OfType<ConstantExpression>(), constant =>
                Assert.True(constant.Value is null or string || constant.Value.GetType().IsPrimitive,
                    $"Unexpected constant of type {constant.Value?.GetType().Name}"));

            Assert.IsType<MemberInitExpression>(projection.Body);
        }

        [Fact]
        public void GetProjection_MapFrom_IsInlinedOnTheProjectionParameter()
        {
            ConfigureCustomerMaps();

            var projection = _mapper.GetProjection<ProjectionCustomer, ProjectionCustomerDto>();
            var collector = new NodeCollector();
            collector.Visit(projection);

            var declared = new HashSet<ParameterExpression>(collector.Nodes
                .OfType<LambdaExpression>()
                .SelectMany(lambda => lambda.Parameters));

            Assert.All(collector.Nodes.OfType<ParameterExpression>(), parameter => Assert.Contains(parameter, declared));
            Assert.Equal(2, declared.Count);

            var displayName = new[] { FullCustomer() }.AsQueryable().Select(projection).Single().DisplayName;
            Assert.Equal("Ada <ada@example.com>", displayName);
        }

        [Fact]
        public void ProjectMapped_FullCustomer_MatchesInMemoryMap()
        {
            ConfigureCustomerMaps();
            var customers = new[] { FullCustomer(), SparseCustomer() };

            var projected = customers.AsQueryable().ProjectMapped<ProjectionCustomer, ProjectionCustomerDto>(_mapper).ToList();
            var mapped = customers.Select(c => _mapper.Map<ProjectionCustomer, ProjectionCustomerDto>(c)).ToList();

            Assert.Equal(mapped.Count, projected.Count);

            for (var index = 0; index < mapped.Count; index++)
            {
                var expected = mapped[index];
                var actual = projected[index];

                Assert.Equal(expected.Id, actual.Id);
                Assert.Equal(expected.Name, actual.Name);
                Assert.Equal(expected.DisplayName, actual.DisplayName);
                Assert.Equal(expected.Email, actual.Email);
                Assert.Equal(expected.Score, actual.Score);
                Assert.Equal(expected.Total, actual.Total);
                Assert.Equal(expected.Status, actual.Status);
                Assert.Equal(expected.Secret, actual.Secret);
                Assert.Equal(expected.Address?.City, actual.Address?.City);
                Assert.Equal(expected.Address?.Street, actual.Address?.Street);
                Assert.Equal(
                    expected.Lines.Select(l => (l.Product, l.Quantity)),
                    actual.Lines.Select(l => (l.Product, l.Quantity)));
            }
        }

        [Fact]
        public void ProjectMapped_IgnoredMember_IsNotAssigned()
        {
            ConfigureCustomerMaps();

            var result = new[] { FullCustomer() }.AsQueryable()
                .ProjectMapped<ProjectionCustomer, ProjectionCustomerDto>(_mapper)
                .Single();

            Assert.Equal("untouched", result.Secret);
        }

        [Fact]
        public void ProjectMapped_NullSubstitute_ReplacesNullReferenceAndNullableValues()
        {
            Configure(config => config.CreateMap<ProjectionNullableSource, ProjectionSubstituteDto>(m => m
                .ForMember(d => d.Nickname, o => o.NullSubstitute("anonymous"))
                .ForMember(d => d.Rank, o => o.NullSubstitute(-1))));
            var sources = new[]
            {
                new ProjectionNullableSource(),
                new ProjectionNullableSource { Nickname = "neo", Rank = 3 }
            };

            var projected = sources.AsQueryable().ProjectMapped<ProjectionNullableSource, ProjectionSubstituteDto>(_mapper).ToList();

            Assert.Equal(("anonymous", -1), (projected[0].Nickname, projected[0].Rank));
            Assert.Equal(("neo", 3), (projected[1].Nickname, projected[1].Rank));

            var mapped = sources.Select(s => _mapper.Map<ProjectionNullableSource, ProjectionSubstituteDto>(s)).ToList();
            Assert.Equal(mapped.Select(d => (d.Nickname, d.Rank)), projected.Select(d => (d.Nickname, d.Rank)));
        }

        [Fact]
        public void ProjectMapped_NumericNullableAndEnumConversions_AreConverted()
        {
            ConfigureCustomerMaps();
            var customers = new[] { FullCustomer(), SparseCustomer() };

            var projected = customers.AsQueryable().ProjectMapped<ProjectionCustomer, ProjectionCustomerDto>(_mapper).ToList();

            Assert.Equal((42, 12.5d, 2), (projected[0].Score, projected[0].Total, projected[0].Status));
            Assert.Equal((0, 0d, 1), (projected[1].Score, projected[1].Total, projected[1].Status));
        }

        [Fact]
        public void ProjectMapped_NestedMap_IsInlinedAndNullNestedStaysNull()
        {
            ConfigureCustomerMaps();
            var customers = new[] { FullCustomer(), SparseCustomer() };

            var projected = customers.AsQueryable().ProjectMapped<ProjectionCustomer, ProjectionCustomerDto>(_mapper).ToList();

            Assert.Equal("Izmir", projected[0].Address!.City);
            Assert.Equal("Kordon", projected[0].Address!.Street);
            Assert.Null(projected[1].Address);
        }

        [Fact]
        public void ProjectMapped_CollectionMember_SelectsEachElementThroughItsMap()
        {
            ConfigureCustomerMaps();

            var result = new[] { FullCustomer() }.AsQueryable()
                .ProjectMapped<ProjectionCustomer, ProjectionCustomerDto>(_mapper)
                .Single();

            Assert.IsType<List<ProjectionLineDto>>(result.Lines);
            Assert.Equal(new[] { ("Pen", 2), ("Ink", 1) }, result.Lines.Select(l => (l.Product, l.Quantity)));
        }

        [Fact]
        public void ProjectMapped_ArrayAndCopiedAndPassedThroughCollections_AreBuilt()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionLine, ProjectionLineDto>(m => { });
                config.CreateMap<ProjectionShapesSource, ProjectionShapesDto>(m => { });
            });
            var source = new ProjectionShapesSource
            {
                Lines = new List<ProjectionLine> { new() { Product = "Pen", Quantity = 2 } },
                Numbers = new List<int> { 1, 2, 3 },
                Tags = new List<string> { "a", "b" }
            };

            var result = new[] { source }.AsQueryable().ProjectMapped<ProjectionShapesSource, ProjectionShapesDto>(_mapper).Single();

            Assert.Equal("Pen", Assert.Single(result.Lines).Product);
            Assert.Equal(new[] { 1, 2, 3 }, result.Numbers);
            Assert.Equal(new[] { "a", "b" }, result.Tags);
        }

        [Fact]
        public void ProjectMapped_CollectionInterfaceMembers_AreBuilt()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionLine, ProjectionLineDto>(m => { });
                config.CreateMap<ProjectionShapesSource, ProjectionInterfaceShapesDto>(m => { });
            });
            var source = new ProjectionShapesSource
            {
                Lines = new List<ProjectionLine> { new() { Product = "Pen", Quantity = 2 } },
                Numbers = new List<int> { 4 },
                Tags = new List<string> { "t" }
            };

            var result = new[] { source }.AsQueryable().ProjectMapped<ProjectionShapesSource, ProjectionInterfaceShapesDto>(_mapper).Single();

            Assert.Equal("Pen", Assert.Single(result.Lines).Product);
            Assert.Equal(new[] { 4 }, result.Numbers);
            Assert.Equal(new[] { "t" }, result.Tags);
        }

        [Fact]
        public void GetProjection_UnsupportedCollectionType_ThrowsNamingTheMember()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionLine, ProjectionLineDto>(m => { });
                config.CreateMap<ProjectionShapesSource, ProjectionUnsupportedCollectionDto>(m => { });
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.GetProjection<ProjectionShapesSource, ProjectionUnsupportedCollectionDto>());

            Assert.Contains("ProjectionUnsupportedCollectionDto.Lines", exception.Message);
            Assert.Contains("ObservableCollection<ProjectionLineDto>", exception.Message);
        }

        [Fact]
        public void ProjectMapped_ConstructorBoundRecord_UsesConstructorAndBindsRemainingMembers()
        {
            Configure(config => config.CreateMap<ProjectionCustomer, ProjectionPersonRecord>(m => { }));

            var projection = _mapper.GetProjection<ProjectionCustomer, ProjectionPersonRecord>();
            var result = new[] { FullCustomer() }.AsQueryable().Select(projection).Single();

            var init = Assert.IsType<MemberInitExpression>(projection.Body);
            Assert.Equal(2, init.NewExpression.Arguments.Count);
            Assert.Equal(new ProjectionPersonRecord(7, "Ada") { Email = "ada@example.com" }, result);
            Assert.Equal(_mapper.Map<ProjectionCustomer, ProjectionPersonRecord>(FullCustomer()), result);
        }

        [Fact]
        public void ProjectMapped_PrivateSetterDestination_IsAssigned()
        {
            _mapper.CreateMap<PrivateSetterProfile>();

            var result = new[] { new Source { Id = 5, Name = "p" } }.AsQueryable()
                .ProjectMapped<Source, PrivateSetterTarget>(_mapper)
                .Single();

            Assert.Equal((5, "p"), (result.Id, result.Name));
        }

        [Fact]
        public void GetProjection_ReverseMap_ProjectsTheReversePair()
        {
            Configure(config => config.CreateMap<ProjectionAddress, ProjectionAddressDto>(m => m.ReverseMap()));

            var result = new[] { new ProjectionAddressDto { City = "c", Street = "s" } }.AsQueryable()
                .ProjectMapped<ProjectionAddressDto, ProjectionAddress>(_mapper)
                .Single();

            Assert.Equal(("c", "s"), (result.City, result.Street));
        }

        [Fact]
        public void GetProjection_ExplicitMap_WinsOverReverseMapRegisteredLater()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionAddressDto, ProjectionAddress>(m => m
                    .ForMember(d => d.City, o => o.MapFrom(s => "X" + s.City)));
                config.CreateMap<ProjectionAddress, ProjectionAddressDto>(m => m.ReverseMap());
            });

            var result = new[] { new ProjectionAddressDto { City = "c", Street = "s" } }.AsQueryable()
                .ProjectMapped<ProjectionAddressDto, ProjectionAddress>(_mapper)
                .Single();

            Assert.Equal("Xc", result.City);
        }

        [Fact]
        public void GetProjection_UnregisteredPair_ThrowsTheSameMessageAsMap()
        {
            var mapException = Assert.Throws<InvalidOperationException>(
                () => _mapper.Map<Source, Target>(new Source()));
            var projectionException = Assert.Throws<InvalidOperationException>(
                () => _mapper.GetProjection<Source, Target>());

            Assert.Equal(mapException.Message, projectionException.Message);
        }

        [Fact]
        public void GetProjection_UnconvertibleMembers_ListsEveryMemberInOneException()
        {
            Configure(config => config.CreateMap<ProjectionUnconvertibleSource, ProjectionUnconvertibleDto>(m => { }));

            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.GetProjection<ProjectionUnconvertibleSource, ProjectionUnconvertibleDto>());

            Assert.Contains("ProjectionUnconvertibleSource to ProjectionUnconvertibleDto cannot be built", exception.Message);
            Assert.Contains("ProjectionUnconvertibleDto.Code: no conversion from String to Int32", exception.Message);
            Assert.Contains("ProjectionUnconvertibleDto.Key: no conversion from Guid to DateTime", exception.Message);
            Assert.DoesNotContain("ProjectionUnconvertibleDto.Id", exception.Message);
        }

        [Fact]
        public void GetProjection_DictionaryMemberWithoutConversion_Throws()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionLine, ProjectionLineDto>(m => { });
                config.CreateMap<ProjectionDictionarySource, ProjectionDictionaryDto>(m => { });
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.GetProjection<ProjectionDictionarySource, ProjectionDictionaryDto>());

            Assert.Contains("ProjectionDictionaryDto.Items", exception.Message);
        }

        [Fact]
        public void GetProjection_CyclicTypeGraph_ThrowsPointingToIgnore()
        {
            _mapper.CreateMap<CycleNodeProfile>();

            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.GetProjection<CycleNodeSource, CycleNodeTarget>());

            Assert.Contains("CycleNodeTarget.Next", exception.Message);
            Assert.Contains("CycleNodeTarget.Children", exception.Message);
            Assert.Contains("ForMember(d => d.Next, opt => opt.Ignore())", exception.Message);
            Assert.Contains("ForMember(d => d.Children, opt => opt.Ignore())", exception.Message);
        }

        [Fact]
        public void GetProjection_CycleBrokenWithIgnore_Builds()
        {
            Configure(config => config.CreateMap<CycleNodeSource, CycleNodeTarget>(m => m
                .ForMember(d => d.Next, o => o.Ignore())
                .ForMember(d => d.Children, o => o.Ignore())));

            var result = new[] { new CycleNodeSource { Id = 9 } }.AsQueryable()
                .ProjectMapped<CycleNodeSource, CycleNodeTarget>(_mapper)
                .Single();

            Assert.Equal(9, result.Id);
        }

        [Fact]
        public void GetProjection_IsCachedAndRebuiltAfterANewRegistration()
        {
            Configure(config => config.CreateMap<Source, ProjectionCaseDto>(m => { }));

            var first = _mapper.GetProjection<Source, ProjectionCaseDto>();
            var second = _mapper.GetProjection<Source, ProjectionCaseDto>();
            Assert.Same(first, second);

            Configure(config => config.CreateMap<Source, ProjectionCaseDto>(m => m.ForMember(d => d.NAME, o => o.Ignore())));

            var rebuilt = _mapper.GetProjection<Source, ProjectionCaseDto>();
            Assert.NotSame(first, rebuilt);

            var result = new[] { new Source { Id = 1, Name = "n" } }.AsQueryable().Select(rebuilt).Single();
            Assert.Equal(1, result.ID);
            Assert.Null(result.NAME);
        }

        [Fact]
        public void GetProjection_TwoMapperInstances_KeepTheirOwnProjections()
        {
            var other = new Mapper();
            Configure(config => config.CreateMap<Source, ProjectionCaseDto>(m => m.ForMember(d => d.NAME, o => o.Ignore())));
            new MapperConfigurationExpression(other).CreateMap<Source, ProjectionCaseDto>(m => { });

            var source = new[] { new Source { Id = 1, Name = "n" } }.AsQueryable();

            Assert.Null(source.ProjectMapped<Source, ProjectionCaseDto>(_mapper).Single().NAME);
            Assert.Equal("n", source.ProjectMapped<Source, ProjectionCaseDto>(other).Single().NAME);
        }

        [Fact]
        public void GetProjection_ConcurrentCalls_AllReturnAWorkingProjection()
        {
            ConfigureCustomerMaps();
            var projections = new ConcurrentBag<Expression<Func<ProjectionCustomer, ProjectionCustomerDto>>>();

            Parallel.For(0, 64, _ => projections.Add(_mapper.GetProjection<ProjectionCustomer, ProjectionCustomerDto>()));

            Assert.Equal(64, projections.Count);
            Assert.All(projections, projection =>
                Assert.Equal("Ada", new[] { FullCustomer() }.AsQueryable().Select(projection).Single().Name));
        }

        [Fact]
        public void TryGetProjection_ReportsSuccessAndFailureWithoutThrowing()
        {
            Configure(config =>
            {
                config.CreateMap<Source, ProjectionCaseDto>(m => { });
                config.CreateMap<ProjectionUnconvertibleSource, ProjectionUnconvertibleDto>(m => { });
            });

            Assert.True(_mapper.TryGetProjection<Source, ProjectionCaseDto>(out var projection, out var noErrors));
            Assert.NotNull(projection);
            Assert.Empty(noErrors);

            Assert.False(_mapper.TryGetProjection<ProjectionUnconvertibleSource, ProjectionUnconvertibleDto>(out var failed, out var errors));
            Assert.Null(failed);
            Assert.Equal(2, errors.Count);

            Assert.False(_mapper.TryGetProjection<Source, Target>(out _, out var undefined));
            Assert.Equal("Mapping from Source to Target is not defined.", Assert.Single(undefined));
        }

        [Fact]
        public void AssertProjectionIsValid_PassesForValidAndThrowsForInvalidMaps()
        {
            ConfigureCustomerMaps();
            Configure(config => config.CreateMap<ProjectionUnconvertibleSource, ProjectionUnconvertibleDto>(m => { }));

            _mapper.AssertProjectionIsValid<ProjectionCustomer, ProjectionCustomerDto>();
            Assert.Throws<InvalidOperationException>(
                () => _mapper.AssertProjectionIsValid<ProjectionUnconvertibleSource, ProjectionUnconvertibleDto>());
        }

        [Fact]
        public void ProjectMapped_NullArguments_ThrowArgumentNullException()
        {
            Configure(config => config.CreateMap<Source, ProjectionCaseDto>(m => { }));

            Assert.Equal("source", Assert.Throws<ArgumentNullException>(
                () => ((IQueryable<Source>)null!).ProjectMapped<Source, ProjectionCaseDto>(_mapper)).ParamName);
            Assert.Equal("mapper", Assert.Throws<ArgumentNullException>(
                () => new[] { new Source() }.AsQueryable().ProjectMapped<Source, ProjectionCaseDto>(null!)).ParamName);
        }

        [Fact]
        public void ProjectMapped_NullSourceMembers_AssignNullWhereMapKeepsTheInitializer()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionAddress, ProjectionAddressDto>(m => { });
                config.CreateMap<ProjectionNullableSource, ProjectionNullableDto>(m => { });
            });
            var source = new ProjectionNullableSource();

            var mapped = _mapper.Map<ProjectionNullableSource, ProjectionNullableDto>(source);
            var projected = new[] { source }.AsQueryable().ProjectMapped<ProjectionNullableSource, ProjectionNullableDto>(_mapper).Single();

            Assert.Equal(("initial", 7, "initial"), (mapped.Nickname, mapped.Rank, mapped.Address!.City));
            Assert.Null(projected.Nickname);
            Assert.Null(projected.Rank);
            Assert.Null(projected.Address);
        }

        [Fact]
        public void ProjectMapped_NestedMap_IsChosenFromTheDeclaredTypeNotTheRuntimeType()
        {
            Configure(config =>
            {
                config.CreateMap<ProjectionAnimal, ProjectionAnimalDto>(m => { });
                config.CreateMap<ProjectionDog, ProjectionAnimalDto>(m => m
                    .ForMember(d => d.Name, o => o.MapFrom(s => s.Name + " the " + s.Breed)));
                config.CreateMap<ProjectionOwner, ProjectionOwnerDto>(m => { });
            });
            var owner = new ProjectionOwner { Pet = new ProjectionDog { Name = "Rex", Breed = "Beagle" } };

            var mapped = _mapper.Map<ProjectionOwner, ProjectionOwnerDto>(owner);
            var projected = new[] { owner }.AsQueryable().ProjectMapped<ProjectionOwner, ProjectionOwnerDto>(_mapper).Single();

            Assert.Equal("Rex the Beagle", mapped.Pet.Name);
            Assert.Equal("Rex", projected.Pet.Name);
        }

        [Fact]
        public void ProjectMapped_NullCollectionInMemory_IsNotGuarded()
        {
            ConfigureCustomerMaps();
            var customer = FullCustomer();
            customer.Lines = null!;

            Assert.Null(_mapper.Map<ProjectionCustomer, ProjectionCustomerDto>(customer).Lines);
            Assert.Throws<ArgumentNullException>(() => new[] { customer }.AsQueryable()
                .ProjectMapped<ProjectionCustomer, ProjectionCustomerDto>(_mapper)
                .ToList());
        }

        [Fact]
        public void ProjectionApi_DoesNotChangeInMemoryMapping()
        {
            ConfigureCustomerMaps();

            var before = _mapper.Map<ProjectionCustomer, ProjectionCustomerDto>(FullCustomer());
            _mapper.GetProjection<ProjectionCustomer, ProjectionCustomerDto>();
            var after = _mapper.Map<ProjectionCustomer, ProjectionCustomerDto>(FullCustomer());

            Assert.Equal(
                (before.Id, before.DisplayName, before.Email, before.Score, before.Address!.City, before.Lines.Count),
                (after.Id, after.DisplayName, after.Email, after.Score, after.Address!.City, after.Lines.Count));
            Assert.IsAssignableFrom<IProjectionMapper>(_mapper);
        }

        private sealed class NodeCollector : ExpressionVisitor
        {
            public List<Expression> Nodes { get; } = new();

            public override Expression? Visit(Expression? node)
            {
                if (node is not null)
                {
                    Nodes.Add(node);
                }

                return base.Visit(node);
            }
        }
    }
}
